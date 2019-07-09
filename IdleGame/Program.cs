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
using Microsoft.Extensions.DependencyInjection;
using MySql.Data.MySqlClient;


namespace IdleGame
{
    class Program
    {
        private const string One = "1⃣";
        private const string Two = "2⃣";
        private const string Three = "3⃣";
        private static DiscordSocketClient _client;
        private CommandService _commands;
        private IServiceProvider _services;
        private static MySqlConnection _conn;
        private string _connStr;
        private static RestUserMessage _message;
        private static ulong _newPlayerId;
        private static string _newPlayerName;
        private static string _newPlayerFaction;
        private static string _newPlayerClass;
        public static Dictionary<ulong, Player> PlayerList;
        public static Dictionary<uint, string> itemMap = new Dictionary<uint, string>();

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
                itemMap.Add(i.Id, i.Name);
            }

            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Info,
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

            await Task.Delay(-1);
        }
        
        private Task Log(LogMessage msg)
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

        private async Task MessageUpdated(Cacheable<IMessage, ulong> before, SocketMessage after,
            ISocketMessageChannel channel)
        {
            var message = await before.GetOrDownloadAsync();
        }

        private async Task HandleDisconnect(Exception ex)
        {
            await TestMySqlConnection();
        }

#pragma warning disable 1998
        private static async Task TestMySqlConnection()
#pragma warning restore 1998
        {
            try
            {
                Console.WriteLine("Testing MySQL...");
                _conn.Open();
                _conn.Close();
                Console.WriteLine("Test Complete!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("SQL test failed. Please ensure your MySQL server is running and the credentials are correct in the .env file.");
                Console.WriteLine(ex.ToString());
                Environment.Exit(1);
            }
        }

        public static async Task Shutdown()
        {
            await _client.StopAsync();
            UpdateDatabase();
            Environment.Exit(0);
        }

        public static Player FindPlayer(string name)
        {
            foreach (var p in PlayerList)
            {
                if (p.Value.Name.Equals(name))
                {
                    return p.Value;
                }
            }
            return new Player(0, "", "", "");
        }

        // SQL functions
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

            var embed = new EmbedBuilder {Color = Color.Blue, Title = _newPlayerName + ": Choose Your Faction"};
            embed.AddField("1. Human", "Humans are dumb. Only plebs choose to be a human.");
            embed.AddField("2. Klingon", "The boys be crazy. Total warrior tribe vibes. Like to kill, love to die.");
            embed.AddField("3. Vulcan", "Everything is logic with these guys. They also have pointy ass ears. Nerds.");

            _message = await _client.GetGuild(ulong.Parse(Environment.GetEnvironmentVariable("GUILD_ID")))
                .GetTextChannel(channelId)
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
            embed.AddField("1. Captain", "Captians");
            embed.AddField("2. Marksman", "Marksmans");
            embed.AddField("3. Smuggler", "Smuggles");

            _message = await _client.GetGuild(ulong.Parse(Environment.GetEnvironmentVariable("GUILD_ID")))
                .GetTextChannel(ulong.Parse(Environment.GetEnvironmentVariable("CHANNEL_ID")))
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

            switch (reaction.Emote.Name)
            {
                case One:
                    _newPlayerClass = "Captain";
                    break;
                case Two:
                    _newPlayerClass = "Marksman";
                    break;
                case Three:
                    _newPlayerClass = "Smuggler";
                    break;
                default:
                    _client.ReactionAdded += ChooseClass;
                    break;
            }

            try
            {
                _conn.Execute($"INSERT INTO player (Id,Name,Faction,Class) VALUES({_newPlayerId}, '{_newPlayerName}', '{_newPlayerFaction}', '{_newPlayerClass}')");
                _conn.Execute($"INSERT INTO inventory VALUES({_newPlayerId}, 1, 10)");
                PlayerList.Add(_newPlayerId, new Player(_newPlayerId, _newPlayerName, _newPlayerFaction, _newPlayerClass));
                PlayerList[_newPlayerId].Inventory.Add(1, 10);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            await _message.Channel.SendMessageAsync($"{_newPlayerName}, the {_newPlayerFaction} {_newPlayerClass}, has started their journey!");
            await _message.DeleteAsync();

            _newPlayerId = 0;
            _newPlayerClass = "";
            _newPlayerFaction = "";
            _newPlayerName = "";
        }
        
        private static Dictionary<ulong, Player> QueryPlayers()
        {
            Dictionary<ulong, Player> tempPlayerList = new Dictionary<ulong, Player>();
            var players = _conn.Query<Player>("SELECT * FROM player");
            foreach (var p in players)
            {
                var inv = _conn.Query<InventoryQuery>($"SELECT ItemId, Quantity FROM inventory WHERE PlayerId = {p.Id}");
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

                tempPlayerList.Add(p.Id, p);
            }

            return tempPlayerList;
        }
        
        public static void UpdateDatabase()
        {
            CleanInventories();
            foreach (var p in PlayerList)
            {
                _conn.Execute($"UPDATE player SET CurHp = {p.Value.CurHp}, Money = {p.Value.Money}, Level = {p.Value.Level}, Exp = {p.Value.Exp}, SkillPoints = {p.Value.SkillPoints}, Boost = '{p.Value.GetBoost().ToDateTime():yyyy-MM-dd HH:mm:ss}' WHERE Id = {p.Key}");
                
                foreach (var i in p.Value.Inventory)
                {
                    _conn.Execute($"UPDATE inventory SET Quantity = {i.Value} WHERE PlayerId = {p.Key} AND ItemId = {i.Key}");
                }
            }
            
            _conn.Execute("DELETE FROM inventory WHERE Quantity = 0");
            Console.WriteLine("Database updated");
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
                var user = guild.GetUser(p.Value.Id);
                if (user.VoiceState.HasValue && user.VoiceChannel.Id != guild.AFKChannel.Id)
                {
                    PlayerList[p.Key].Exp += 1;
                    if (PlayerList[p.Key].LevelUp())
                    {
                        guild.GetTextChannel(ulong.Parse(Environment.GetEnvironmentVariable("CHANNEL_ID")))
                            .SendMessageAsync(
                                $"{_client.GetUser(p.Key).Mention} has leveled up! They are now Level {PlayerList[p.Key].Level}");
                    }
                }
            }
            Console.WriteLine("Exp Given!");
            UpdateDatabase();
        }
    }

    struct InventoryQuery
    {
        public uint ItemId;
        public uint Quantity;
    }

    struct ItemQuery
    {
        public uint Id;
        public string Name;
    }
}