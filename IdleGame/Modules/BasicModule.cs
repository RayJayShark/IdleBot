using System.ComponentModel;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace IdleGame.Modules
{
    [Name("Basic Commands")]
    public class BasicModule : ModuleBase<SocketCommandContext>
    {
        [Command("ping")]
        public Task PingAsync() 
            => ReplyAsync("pong!");

        [Command("shutdown")]
        public async Task ShutdownAsync()
        {
            await ReplyAsync("Goodbye! " + new Emoji("\uD83D\uDC4B"));
            await Program.Shutdown();
        }

        [Command("emoji")]
        [Remarks("<emoji>")]
        public async Task GetEmojiCode(string e)
        {
            var emoji = new Emoji(e);
            await ReplyAsync("```" + emoji.Name + "```");
        }

        [Command("cleardm")]
        [Description("Clears all bot-sent messages from your DMs.")]
        public async Task ClearDm()
        {
            // Hits rate limit
            var ch = await Context.User.GetOrCreateDMChannelAsync();
            var messages = await ch.GetMessagesAsync().FlattenAsync();
            foreach (var m in messages)
            {
                if (m.Author.IsBot)
                {
                    await m.DeleteAsync();
                }
            }
        }
        
    }
}