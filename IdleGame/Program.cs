using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Timers;
using Dapper;
using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using dotenv.net;
using Google.Protobuf.WellKnownTypes;
using IdleGame.Classes;
using Microsoft.Extensions.DependencyInjection;
using MySql.Data.MySqlClient;
using Org.BouncyCastle.Asn1.Crmf;


namespace IdleGame
{
    class Program
    {
        //TODO: Add attack log to enemies
        private const string One = "1⃣";
        private const string Two = "2⃣";
        private const string Three = "3⃣";
        private static DiscordSocketClient _client;
        private static CommandService _commands;
        private static IServiceProvider _services;
        private static MySqlConnection _conn;
        private string _connStr;
        private static RestUserMessage _message;
        private static ulong _channelId;
        private static ulong _newPlayerId;
        private static string _newPlayerName;
        private static string _newPlayerFaction;
        private static string _newPlayerClass;
        public static Dictionary<ulong, Player> PlayerList;
        public static readonly Dictionary<uint, ItemQuery> ItemMap = new Dictionary<uint, ItemQuery>();
        public static List<Enemy> Enemies = new List<Enemy>();
        private static Timer _enemyTimer;

        static void Main(string[] arg) => new Program().MainAsync().GetAwaiter().GetResult();
        private async Task MainAsync()
        {
            if (!File.Exists(".env"))
            {
                File.Copy(".env.example", ".env");
                Console.WriteLine(".env file created. Please configure and restart.");
                return;
            }
            DotEnv.Config(false);

            _connStr = $"server={Environment.GetEnvironmentVariable("MYSQL_SERVER")};" +
                      $"user={Environment.GetEnvironmentVariable("MYSQL_USER")};" +
                      $"password={Environment.GetEnvironmentVariable("MYSQL_PASSWORD")};" +
                      $"database={Environment.GetEnvironmentVariable("MYSQL_DATABASE")};" +
                      $"port={Environment.GetEnvironmentVariable("MYSQL_PORT")}";

            _conn = new MySqlConnection(_connStr);
            await TestMySqlConnection();

            PlayerList = QueryPlayers();
            var itemQuery = _conn.Query<ItemQuery>("SELECT * FROM item");
            foreach (var i in itemQuery)
            {
                ItemMap.Add(i.Id, i);
            }

            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Verbose,
                MessageCacheSize = 100
            });

            _client.Log += Log;

            await _client.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("DISCORD_TOKEN"));
            await _client.StartAsync();
            
            _services = new ServiceCollection()
                .AddSingleton(_client)
                .BuildServiceProvider();

            _commands = new CommandService();
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
            _client.MessageUpdated += MessageUpdated;
            _client.MessageReceived += HandleCommandAsync;
            _client.Disconnected += HandleDisconnect;

            var expTimer = new Timer();
            expTimer.Elapsed += GiveExp;
            expTimer.Interval = int.Parse(Environment.GetEnvironmentVariable("EXP_SECONDS")) * 1000;
            expTimer.Enabled = true;
            
            //TODO: Make timer us env variable
            Enemies.AddRange(Enemy.CreateMultiple(10));
            _enemyTimer = new Timer();
            _enemyTimer.Elapsed += RefreshEnemies;
            _enemyTimer.Interval = 60 * 60 * 1000;
            _enemyTimer.Enabled = true;

            await Task.Delay(-1);
        }
        
        private static Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
        
        private async Task HandleCommandAsync(SocketMessage messageParam)
        {
            var message = messageParam as SocketUserMessage;
            if (message == null) return;

            int argPos = 0;

            char prefix;
            try
            {
                prefix = Environment.GetEnvironmentVariable("COMMAND_PREFIX")[0];
            }
            catch (Exception ex)
            {
                prefix = '+';
            }
            
            if (!(message.HasCharPrefix(prefix, ref argPos) || message.HasMentionPrefix(_client.CurrentUser, ref argPos)) || message.Author.IsBot)
                return;
            
            var context = new SocketCommandContext(_client, message);

            await _commands.ExecuteAsync(context: context, argPos: argPos, services: _services);
        }

        private static async Task MessageUpdated(Cacheable<IMessage, ulong> before, SocketMessage after,
            ISocketMessageChannel channel)
        {
            var message = await before.GetOrDownloadAsync();
        }

        private static async Task HandleDisconnect(Exception ex)
        {
            await TestMySqlConnection();
        }

        private static void RefreshEnemies(object source, ElapsedEventArgs e)
        {
            Enemies.Clear();
            Enemies.AddRange(Enemy.CreateMultiple(10));
        }

        public static SocketGuild GetGuild(ulong id)
        {
            return _client.GetGuild(id);
        }
        
        public static async Task Shutdown()
        {
            await _client.StopAsync();
            UpdateDatabase();
            Environment.Exit(0);
        }

        public static Player FindPlayer(string name)
        {
            name = name.ToLower();
            foreach (var p in PlayerList)
            {
                if (p.Value.GetName().ToLower().Equals(name))
                {
                    return p.Value;
                }
            }
            return new BlankCharacter();
        }

        public static uint FindItemId(string itemName)
        {
            foreach (var i in ItemMap)
            {
                if (i.Value.Name.ToLower().Equals(itemName.ToLower()))
                {
                    return i.Key;
                } 
            }

            return 0;
        } 

        private static string GetTimeStamp()
        {
            return $"{DateTime.Now.Hour:D2}:{DateTime.Now.Minute:D2}:{DateTime.Now.Second:D2}";
        }

        // SQL functions
        private static async Task TestMySqlConnection()
        {
            try
            {
                Console.WriteLine($"{GetTimeStamp()} Database    Testing MySQL...");
                _conn.Open();
                _conn.Close();
                Console.WriteLine($"{GetTimeStamp()} Database    Test Complete!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("SQL test failed. Please ensure your MySQL server is running and the credentials are correct in the .env file.");
                Console.WriteLine(ex.ToString());
                Environment.Exit(1);
            }
        }
        
        public static void ExecuteSql(string sql)
        {
            _conn.Execute(sql);
        }
        
        public static async Task AddPlayer(ulong id, string name, ulong channelId)
        {
            if (PlayerList.ContainsKey(id))
            {
                await _client.GetGuild(ulong.Parse(Environment.GetEnvironmentVariable("GUILD_ID")))
                    .GetTextChannel(ulong.Parse(Environment.GetEnvironmentVariable("CHANNEL_ID")))
                    .SendMessageAsync("You already have a character!");
                return;
            }

            _newPlayerId = id;
            _newPlayerName = name;
            _channelId = channelId;

            var embed = new EmbedBuilder {Color = Color.Blue, Title = _newPlayerName + ": Choose Your Faction"};
            embed.AddField("1. Human", "Humans are dumb. Only plebs choose to be a human.");
            embed.AddField("2. Klingon", "The boys be crazy. Total warrior tribe vibes. Like to kill, love to die.");
            embed.AddField("3. Vulcan", "Everything is logic with these guys. They also have pointy ass ears. Nerds.");

            _message = await _client.GetGuild(ulong.Parse(Environment.GetEnvironmentVariable("GUILD_ID")))
                .GetTextChannel(_channelId)
                .SendMessageAsync("", false, embed.Build());
            await _message.AddReactionAsync(new Emoji(One));
            await _message.AddReactionAsync(new Emoji(Two));
            await _message.AddReactionAsync(new Emoji(Three));

            _client.ReactionAdded += ChooseFaction;
        }

        private static async Task ChooseFaction(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel channel,
            SocketReaction reaction)
        {
            if (reaction.MessageId != _message.Id || reaction.UserId != _newPlayerId)
                return;

            _client.ReactionAdded -= ChooseFaction;

            switch (reaction.Emote.Name)
            {
                case One:
                    _newPlayerFaction = "Human";
                    break;
                case Two:
                    _newPlayerFaction = "Klingon";
                    break;
                case Three:
                    _newPlayerFaction = "Vulcan";
                    break;
                default:
                    _client.ReactionAdded += ChooseFaction;
                    break;
            }

            await _message.DeleteAsync();
            var embed = new EmbedBuilder {Color = Color.Blue, Title = _newPlayerName + ": Choose Your Class"};
            embed.AddField("1. Captain", "70 Health\n7 Strength\n10 Defence");
            embed.AddField("2. Marksman", "70 Health\n10 Strength\n7 Defence");
            embed.AddField("3. Smuggler", "100 Health\n7 Strength\n7 Defence");

            _message = await _client.GetGuild(ulong.Parse(Environment.GetEnvironmentVariable("GUILD_ID")))
                .GetTextChannel(_channelId)
                .SendMessageAsync("", false, embed.Build());
            await _message.AddReactionAsync(new Emoji(One));
            await _message.AddReactionAsync(new Emoji(Two));
            await _message.AddReactionAsync(new Emoji(Three));
            
            _client.ReactionAdded += ChooseClass;
        }

        private static async Task ChooseClass(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel channel,
            SocketReaction reaction)
        {
            if (reaction.MessageId != _message.Id || reaction.UserId != _newPlayerId)
                return;

            _client.ReactionAdded -= ChooseClass;
            PlayerStats stats;
            switch (reaction.Emote.Name)
            {
                case One:
                    _newPlayerClass = "Captain";
                    PlayerList.Add(_newPlayerId, new Captain(_newPlayerId, _newPlayerName, _newPlayerFaction));
                    break;
                case Two:
                    _newPlayerClass = "Marksman";
                    PlayerList.Add(_newPlayerId, new Marksman(_newPlayerId, _newPlayerName, _newPlayerFaction));
                    break;
                case Three:
                    _newPlayerClass = "Smuggler";
                    PlayerList.Add(_newPlayerId, new Smuggler(_newPlayerId, _newPlayerName, _newPlayerFaction));
                    break;
                default:
                    _client.ReactionAdded += ChooseClass;
                    return;
            }

            try
            {
                _conn.Execute($"INSERT INTO player (Id,Name,Faction,Class,CurHp) VALUES({_newPlayerId}, '{_newPlayerName}', '{_newPlayerFaction}', '{_newPlayerClass}', {PlayerList[_newPlayerId].Stats.GetHealth()})");
                _conn.Execute($"INSERT INTO inventory VALUES({_newPlayerId}, 1, 10)");
                _conn.Execute($"INSERT INTO stats VALUES({_newPlayerId}, {PlayerList[_newPlayerId].Stats.GetHealth()}, {PlayerList[_newPlayerId].Stats.GetStrength()}, {PlayerList[_newPlayerId].Stats.GetDefence()})");
                PlayerList[_newPlayerId].Inventory.Add(1, 10);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            await _message.Channel.SendMessageAsync($"{_newPlayerName}, the {_newPlayerFaction} {_newPlayerClass}, has started their journey!");
            //TODO: Send DM introduction
            await _message.DeleteAsync();

            _newPlayerId = 0;
            _channelId = 0;
            _newPlayerClass = "";
            _newPlayerFaction = "";
            _newPlayerName = "";
        }
        
        private static Dictionary<ulong, Player> QueryPlayers()
        {
            Dictionary<ulong, Player> tempPlayerList = new Dictionary<ulong, Player>();
            // Query Captains
            var cPlayers = _conn.Query<Captain>("SELECT * FROM player WHERE Class = 'Captain'");
            foreach (var p in cPlayers)
            {
                var stats = _conn.QuerySingle<PlayerStats>($"SELECT Health, Strength, Defence FROM stats WHERE PlayerId = {p.GetId()}");
                p.Stats = stats;
                var inv = _conn.Query<InventoryQuery>($"SELECT ItemId, Quantity FROM inventory WHERE PlayerId = {p.GetId()}");
                if (inv.Count() != 0)
                {
                    foreach (var i in inv)
                    {
                        if (i.Quantity > 0)
                        {
                            p.Inventory.Add(i.ItemId, i.Quantity);
                        }
                    }
                }

                tempPlayerList.Add(p.GetId(), p);
            }
            
            // Query Marksmen
            var mPlayers = _conn.Query<Marksman>("SELECT * FROM player WHERE Class = 'Marksman'");
            foreach (var p in mPlayers)
            {
                var stats = _conn.QuerySingle<PlayerStats>($"SELECT Health, Strength, Defence FROM stats WHERE PlayerId = {p.GetId()}");
                p.Stats = stats;
                var inv = _conn.Query<InventoryQuery>($"SELECT ItemId, Quantity FROM inventory WHERE PlayerId = {p.GetId()}");
                if (inv.Count() != 0)
                {
                    foreach (var i in inv)
                    {
                        if (i.Quantity > 0)
                        {
                            p.Inventory.Add(i.ItemId, i.Quantity);
                        }
                    }
                }

                tempPlayerList.Add(p.GetId(), p);
            }
            
            // Query Smuggler
            var sPlayers = _conn.Query<Smuggler>("SELECT * FROM player WHERE Class = 'Smuggler'");
            foreach (var p in sPlayers)
            {
                var stats = _conn.QuerySingle<PlayerStats>($"SELECT Health, Strength, Defence FROM stats WHERE PlayerId = {p.GetId()}");
                p.Stats = stats;
                var inv = _conn.Query<InventoryQuery>($"SELECT ItemId, Quantity FROM inventory WHERE PlayerId = {p.GetId()}");
                if (inv.Count() != 0)
                {
                    foreach (var i in inv)
                    {
                        if (i.Quantity > 0)
                        {
                            p.Inventory.Add(i.ItemId, i.Quantity);
                        }
                    }
                }

                tempPlayerList.Add(p.GetId(), p);
            }
            
            return tempPlayerList;
        }
        
        public static void UpdateDatabase()
        {
            CleanInventories();
            foreach (var p in PlayerList)
            {
                _conn.Execute($"UPDATE player SET CurHp = {p.Value.GetCurrentHp()}, Money = {p.Value.GetMoney()}, Level = {p.Value.GetLevel()}, Exp = {p.Value.GetExp()}, Boost = '{p.Value.GetBoost().ToDateTime():yyyy-MM-dd HH:mm:ss}' WHERE Id = {p.Key}");
                _conn.Execute($"UPDATE stats SET Health = {p.Value.Stats.GetHealth()}, Strength = {p.Value.Stats.GetStrength()}, Defence = {p.Value.Stats.GetDefence()} WHERE PlayerId = {p.Value.GetId()}");
                
                foreach (var i in p.Value.Inventory)
                {
                    _conn.Execute($"UPDATE inventory SET Quantity = {i.Value} WHERE PlayerId = {p.Key} AND ItemId = {i.Key}");
                }
            }
            
            _conn.Execute("DELETE FROM inventory WHERE Quantity = 0");
            Console.WriteLine($"{GetTimeStamp()} Database    Database updated");
        }

        private static void CleanInventories()        // Clear inventory items with zeros
        {
            foreach (var p in PlayerList)
            {
                foreach (var i in p.Value.Inventory)
                {
                    if (i.Value == 0)
                    {
                        PlayerList[p.Key].Inventory.Remove((i.Key));
                    }
                }
            }
        }

        private static void GiveExp(object source, ElapsedEventArgs e)
        {
            foreach (var p in PlayerList)
            {
                var guild = _client.GetGuild(ulong.Parse(Environment.GetEnvironmentVariable("GUILD_ID")));
                var user = guild.GetUser(p.Value.GetId());
                if (user.VoiceState.HasValue && user.VoiceChannel.Id != guild.AFKChannel.Id)
                {
                    PlayerList[p.Key].GiveExp(uint.Parse(Environment.GetEnvironmentVariable("IDLE_EXP")));
                }
                PlayerList[p.Key].GiveHp(uint.Parse(Environment.GetEnvironmentVariable("IDLE_HP")));
            }
            Console.WriteLine($"{GetTimeStamp()} Game\t     Exp Given!");
            UpdateDatabase();
        }
    }

    internal struct InventoryQuery
    {
        public uint ItemId;
        public uint Quantity;
    }

    internal struct ItemQuery
    {
        public uint Id;
        public string Name;
        public uint Value;
    }
}