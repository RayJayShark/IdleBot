using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Timers;
using Dapper;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using dotenv.net;
using MySql.Data.MySqlClient;
using Org.BouncyCastle.Cms;


namespace IdleGame
{
    class Program
    {
        private static DiscordSocketClient _client;
        private CommandHandler _commands;
        private static MySqlConnection _conn;
        private static MySqlCommand _cmd;
        private static MySqlDataReader _reader;
        private string _connStr;
        public static Dictionary<string, Player> PlayerList;

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
            
            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Info
            });
            
            _commands = new CommandHandler(_client, new CommandService());
            await _commands.InstallCommandsAsync();
            
            _client.Log += Log;

            await _client.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("DISCORD_TOKEN"));
            await _client.StartAsync();

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

        public static async Task Shutdown()
        {
            await _client.StopAsync();
            _conn.Close();
            Environment.Exit(0);
        }

        // SQL functions
        private static Dictionary<string, Player> QueryPlayers()
        {
            var players = _conn.Query<Player>("SELECT * FROM player");
            Dictionary<string, Player> TempPlayerList = new Dictionary<string, Player>();
            foreach (var p in players)
            {
                TempPlayerList.Add(p.Name, p);
            }

            return TempPlayerList;
        }

        public static int AddPlayer(ulong id, string name)
        {
            if (PlayerList.ContainsKey(name))
            {
                return 1;
            }
            PlayerList.Add(name, new Player(id, name));
            return 0;
        }

        private static void GiveExp(object source, ElapsedEventArgs e)
        {
            foreach (var p in PlayerList)
            {
                PlayerList[p.Key].Exp += 1;
                if (PlayerList[p.Key].LevelUp())
                {
                    _client.GetGuild(ulong.Parse(Environment.GetEnvironmentVariable("GUILD_ID")))
                        .GetTextChannel(ulong.Parse(Environment.GetEnvironmentVariable("CHANNEL_ID")))
                        .SendMessageAsync($"{p.Key} has leveled up! They are now Level {PlayerList[p.Key].Level}");

                }
            }
            Console.WriteLine("Exp Given!");
        }
    }
}