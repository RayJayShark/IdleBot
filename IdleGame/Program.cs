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
            expTimer.Interval = int.Parse(Environment.GetEnvironmentVariable("EXP_TIMER")) * 60 * 1000;
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
            
        } 
        
        public static int AddPlayer(ulong id, string name)
        {
            string sql = $"SELECT COUNT(Id) FROM player WHERE Id = {id}";
            try
            {
                _conn.Open();
                _cmd = new MySqlCommand(sql, _conn);
                _reader = _cmd.ExecuteReader();
                _reader.Read();
                if (int.Parse(_reader[0].ToString()) != 0)
                {
                    _conn.Close();
                    return 1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            _conn.Close();
            sql = $"INSERT INTO player (Id, Name) VALUES ('{id}','{name}')";
            try
            {
                _conn.Open();
                _cmd = new MySqlCommand(sql, _conn);
                _cmd.ExecuteNonQuery();
                _conn.Close();
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return 2;
            }
        }
        
        private static void GiveExp(object source, ElapsedEventArgs e)
        {
            Dictionary<ulong, int> players = new Dictionary<ulong, int>();
            
            try
            {
                _conn.Open();
                _cmd = new MySqlCommand("SELECT Id, Exp FROM player", _conn);
                _reader = _cmd.ExecuteReader();

                while (_reader.Read())
                {
                    players.Add(ulong.Parse(_reader[0].ToString()), int.Parse(_reader[1].ToString()));
                }

                _conn.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            try
            {
                _conn.Open();
                foreach (KeyValuePair<ulong, int> p in players)
                {
                    _cmd = new MySqlCommand($"UPDATE player SET Exp = {p.Value + Environment.GetEnvironmentVariable("IDLE_EXP")} WHERE Id = {p.Key}", _conn);
                    _cmd.ExecuteNonQuery();
                }
                Console.WriteLine("Exp given!");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            _conn.Close();
        }
    }
}