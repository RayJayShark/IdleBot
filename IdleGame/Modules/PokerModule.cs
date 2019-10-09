using System;
using System.Threading.Tasks;
using Discord.Commands;
using IdleGame.Poker;
using IdleGame.Services;

namespace IdleGame.Modules
{
    [Name("Poker Commands")]
    public class PokerModule : ModuleBase<SocketCommandContext>
    {
        public PokerService Pokerservice { get; set; }

        [Command("newgame")]
        [Alias("newg")]
        public async Task StartNewGame()
            => await Pokerservice.NewGame(Context);



        [Command("joingame")]
        [Alias("join")]
        public async Task JoinGame()
            => await Pokerservice.JoinGame(Context);

        [Command("playerlist")]
        [Alias("plist")]
        public async Task ListPlayers()
            => await Pokerservice.ListPlayers(Context);
        
        [Command("start")]
        [Alias("begin", "startgame")]
        public async Task BeginGame()
        {
            //TODO: Start playing the new game
        }

        [Command("ptest")]
        public async Task Test()
        {
            Console.WriteLine("Starting test...");
            try
            {
                Pokerservice.Test();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
        
    }
}