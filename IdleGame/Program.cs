using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Net.WebSockets;
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
using IdleGame.Services;
using Microsoft.Extensions.DependencyInjection;
using MySql.Data.MySqlClient;
using Org.BouncyCastle.Asn1.Crmf;


namespace IdleGame
{
    //TODO: Initialize database if not already
    class Program
    {
        private static DiscordSocketClient _client;
        private static CommandService _commands;
        private static IServiceProvider _services;
        public static Dictionary<ulong, Player> PlayerList;
        public static Dictionary<uint, ItemQuery> ItemMap = new Dictionary<uint, ItemQuery>();
        public static List<Enemy> Enemies = new List<Enemy>();
        private static Timer _enemyTimer;

        private static SqlService _sqlService;
        private static LogService _logService;

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

            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Verbose,
                MessageCacheSize = 100
            });

            _client.Log += Log;

            await _client.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("DISCORD_TOKEN"));
            await _client.StartAsync();
            
            var connStr = $"server={Environment.GetEnvironmentVariable("MYSQL_SERVER")};" +
                       $"user={Environment.GetEnvironmentVariable("MYSQL_USER")};" +
                       $"password={Environment.GetEnvironmentVariable("MYSQL_PASSWORD")};" +
                       $"database={Environment.GetEnvironmentVariable("MYSQL_DATABASE")};" +
                       $"port={Environment.GetEnvironmentVariable("MYSQL_PORT")}";

            
            _sqlService = new SqlService(connStr);
            _logService = new LogService();

            _services = new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton(_logService)
                .AddSingleton(_sqlService)
                .AddSingleton<TimerService>()
                .BuildServiceProvider();

            _commands = new CommandService();
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
            
            PlayerList = _sqlService.QueryPlayers();
            ItemMap = _sqlService.QueryItems();
            
            _client.MessageUpdated += MessageUpdated;
            _client.MessageReceived += HandleCommandAsync;
            _client.Disconnected += HandleDisconnect;

            var expTimer = new Timer();
            expTimer.Elapsed += GiveExp;
            expTimer.Interval = int.Parse(Environment.GetEnvironmentVariable("EXP_SECONDS")) * 1000;
            expTimer.Enabled = true;
            
            //TODO: Make timer use env variable
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

        private async Task HandleDisconnect(Exception ex)
        {
            _sqlService.TestMySqlConnection();
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

        public static void AddReactionEvent(Func<Cacheable<IUserMessage, ulong>, ISocketMessageChannel, SocketReaction, Task> task)
        {
            _client.ReactionAdded += task;
        }
        
        public static void RemoveReactionEvent(Func<Cacheable<IUserMessage, ulong>, ISocketMessageChannel, SocketReaction, Task> task)
        {
            _client.ReactionAdded -= task;
        }
        
        public static async Task Shutdown()
        {
            await _client.StopAsync();
            _sqlService.UpdateDatabase();
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

        private void GiveExp(object source, ElapsedEventArgs e)
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
            LogService.GameLog("Exp given");
            _sqlService.UpdateDatabase();
        }
    }
}