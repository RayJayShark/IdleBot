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
using Discord.WebSocket;
using dotenv.net;
using Microsoft.Extensions.DependencyInjection;
using MySql.Data.MySqlClient;


namespace IdleGame
{
    class Program
    {
        private static DiscordSocketClient _client;
        private CommandService _commands;
        private IServiceProvider services;
        private static MySqlConnection _conn;
        private static MySqlCommand _cmd;
        private static MySqlDataReader _reader;
        private string _connStr;
        public static Dictionary<ulong, Player> PlayerList;
        public static Dictionary<uint, string> itemMap = new Dictionary<uint, string>();

        static void Main(string[] arg) => new Program().MainAsync().GetAwaiter().GetResult();
        public async Task MainAsync()
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
            try
            {
                Console.WriteLine("Testing MySQL...");
                _conn.Open();
                _conn.Close();
                Console.WriteLine("Test Complete!");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Environment.Exit(1);
            }

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
            
            services = new ServiceCollection()
                .AddSingleton(_client)
                .BuildServiceProvider();
            
            _commands = new CommandService();
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), services);
            _client.MessageUpdated += MessageUpdated;
            _client.MessageReceived += HandleCommandAsync;
            //TODO: Check database and other stuff when disconnected.

            var expTimer = new Timer();
            expTimer.Elapsed += GiveExp;
            expTimer.Interval = int.Parse(Environment.GetEnvironmentVariable("EXP_TIMER")) * 1000;
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

            await _commands.ExecuteAsync(context: context, argPos: argPos, services: services);
        }

        private async Task MessageUpdated(Cacheable<IMessage, ulong> before, SocketMessage after,
            ISocketMessageChannel channel)
        {
            var message = await before.GetOrDownloadAsync();
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
            return new Player(0, "");
        }

        // SQL functions
        public static int AddPlayer(ulong id, string name)
        {
            if (PlayerList.ContainsKey(id))
            {
                return 1;
            }
            PlayerList.Add(id, new Player(id, name));
            PlayerList[id].Inventory.Add(1, 10);
            _conn.Execute($"INSERT INTO players VALUES(Id = {id}, Name = '{name}')");
            _conn.Execute($"INSERT INTO inventory VALUES(PlayerId = {id}, ItemId = 1, Quantity = 10)");
            return 0;
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

        private static void UpdateDatabase()
        {
            CleanInventories();
            foreach (var p in PlayerList)
            {
                _conn.Execute($"UPDATE player SET CurHp = {p.Value.CurHp}, MaxHp = {p.Value.MaxHp}, Money = {p.Value.Money}, Level = {p.Value.Level}, Exp = {p.Value.Exp} WHERE Id = {p.Key}");
                
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