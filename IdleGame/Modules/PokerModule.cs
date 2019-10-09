using System.Threading.Tasks;
using Discord.Commands;
using IdleGame.Services;

namespace IdleGame.Modules
{
    [Name("Poker Commands")]
    public class PokerModule : ModuleBase<SocketCommandContext>
    {
        private PokerService _pokerservice { get; set; }

        [Command("newgame")]
        [Alias("new")]
        public async Task StartNewGame()
        {
            //TODO: Start new game, allow to join
        }
        
        [Command("joingame")]
        [Alias("join")]
        public async Task JoinGame()
        {
            //TODO: Join active game
        }

        [Command("start")]
        [Alias("begin", "startgame")]
        public async Task BeginGame()
        {
            //TODO: Start playing the new game
        }
    }
}