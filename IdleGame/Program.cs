using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
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
                Console.WriteLine("Connecting to MySQL...");
                conn.Open();
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
            string sql = $"INSERT INTO player (ID, Name) VALUES ('{id}','{name}')";
            try
            {
                cmd = new MySqlCommand(sql, conn);
                cmd.ExecuteNonQuery();
                return 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return 0;
            }
        }
    }
}