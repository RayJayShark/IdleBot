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
        
    }
}