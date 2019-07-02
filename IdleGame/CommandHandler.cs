using System;
using System.Globalization;
using System.Reflection;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;

namespace IdleGame
{
    public class CommandHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;

        public CommandHandler(DiscordSocketClient client, CommandService commands)
        {
            _commands = commands;
            _client = client;
        }
        
        public async Task InstallCommandsAsync()
        {
            _client.MessageReceived += HandleCommandAsync;

            await _commands.AddModulesAsync(assembly: Assembly.GetEntryAssembly(), services: null);
        }
        
        private async Task HandleCommandAsync(SocketMessage messageParam)
        {
            var message = messageParam as SocketUserMessage;
            if (message == null) return;

            int argPos = 0;

            char prefix;
            if (Environment.GetEnvironmentVariable("COMMAND_PREFIX")[0] == null)
            {
                prefix = '+';
            }
            else
            {
                prefix = Environment.GetEnvironmentVariable("COMMAND_PREFIX")[0];
            }
            
            if (!(message.HasCharPrefix(prefix, ref argPos) || message.HasMentionPrefix(_client.CurrentUser, ref argPos)) || message.Author.IsBot)
                return;
            
            var context = new SocketCommandContext(_client, message);

            var result = await _commands.ExecuteAsync(context: context, argPos: argPos, services: null);
        }
    }
}