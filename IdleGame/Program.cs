using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using dotenv.net;
using IdleGame.Classes;
using IdleGame.Services;
using Microsoft.Extensions.DependencyInjection;


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
        public static readonly List<Enemy> Enemies = new List<Enemy>();

        private static SqlService _sqlService;

        private static void Main(string[] arg) => new Program().MainAsync().GetAwaiter().GetResult();
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

            _services = new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton(_sqlService)
                .BuildServiceProvider();

            _commands = new CommandService();
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
            
            PlayerList = _sqlService.QueryPlayers();
            ItemMap = _sqlService.QueryItems();
            
            _client.MessageUpdated += MessageUpdated;
            _client.MessageReceived += HandleCommandAsync;
            _client.Disconnected += HandleDisconnect;

            Enemies.AddRange(Enemy.CreateMultiple(10));
            var timerService = new TimerService(int.Parse(Environment.GetEnvironmentVariable("EXP_SECONDS")),
                int.Parse(Environment.GetEnvironmentVariable("ENEMY_REFRESH_SECONDS")));

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

        public static void AddMessageEvent(Func<SocketMessage, Task> task)
        {
            _client.MessageReceived += task;
        }

        public static void RemoveMessageEvent(Func<SocketMessage, Task> task)
        {
            _client.MessageReceived -= task;
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

        public static bool ValidItemId(uint itemId)
        {
            return ItemMap.ContainsKey(itemId);

        }
    }
}