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
        private string connStr = "server=localhost;user=root;database=idlegame;port=3306";
        private static MySqlConnection conn;
        private static MySqlCommand cmd;
        private static MySqlDataReader reader;

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

            conn = new MySqlConnection(connStr);
            try
            {
                Console.WriteLine("Testing MySQL...");
                conn.Open();
                conn.Close();
                Console.WriteLine("Test Complete!");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Environment.Exit(1);
            }
            
            
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
            conn.Close();
            Environment.Exit(0);
        }

        // SQL functions
        public static int AddPlayer(ulong id, string name)
        {
            string sql = $"SELECT COUNT(Id) FROM player WHERE Id = {id}";
            try
            {
                conn.Open();
                cmd = new MySqlCommand(sql, conn);
                reader = cmd.ExecuteReader();
                reader.Read();
                if (int.Parse(reader[0].ToString()) != 0)
                {
                    conn.Close();
                    return 1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            conn.Close();
            sql = $"INSERT INTO player (Id, Name) VALUES ('{id}','{name}')";
            try
            {
                conn.Open();
                cmd = new MySqlCommand(sql, conn);
                cmd.ExecuteNonQuery();
                conn.Close();
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
                conn.Open();
                cmd = new MySqlCommand("SELECT Id, Exp FROM player", conn);
                reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    players.Add(ulong.Parse(reader[0].ToString()), int.Parse(reader[1].ToString()));
                }

                conn.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            try
            {
                conn.Open();
                foreach (KeyValuePair<ulong, int> p in players)
                {
                    cmd = new MySqlCommand($"UPDATE player SET Exp = {p.Value + Environment.GetEnvironmentVariable("IDLE_EXP")} WHERE Id = {p.Key}", conn);
                    cmd.ExecuteNonQuery();
                }
                Console.WriteLine("Exp given!");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            conn.Close();
        }
    }
}