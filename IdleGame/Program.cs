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
        public static Dictionary<ulong, Player> PlayerList;

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
            _conn.Execute($"INSERT INTO players VALUES(Id = {id}, Name = '{name}')");
            return 0;
        }
        
        private static Dictionary<ulong, Player> QueryPlayers()
        {
            Dictionary<ulong, Player> tempPlayerList = new Dictionary<ulong, Player>();
            var players = _conn.Query<Player>("SELECT * FROM player");
            foreach (var p in players)
            {
                tempPlayerList.Add(p.Id, p);
            }

            return tempPlayerList;
        }

        private static void UpdateDatabase()
        {
            foreach (var p in PlayerList)
            {
                _conn.Execute($"UPDATE player SET CurHp = {p.Value.CurHp}, MaxHp = {p.Value.MaxHp}, Money = {p.Value.Money}, Level = {p.Value.Level}, Exp = {p.Value.Exp} WHERE Id = {p.Key}");
            }
            Console.WriteLine("Database updated");
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
}