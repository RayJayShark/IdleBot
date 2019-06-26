using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using dotenv.net;


namespace IdleGame
{
    class Program
    {
        private static DiscordSocketClient _client;
        private CommandHandler _commands;
        private string connStr = "server=localhost;user=root;database=idlegame;port=3306";

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
            Environment.Exit(0);
        }
    }
}