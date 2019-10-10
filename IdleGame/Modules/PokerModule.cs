using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Discord.Commands;
using IdleGame.Poker;
using IdleGame.Services;

namespace IdleGame.Modules
{
    [Name("Poker Commands")]
    public class PokerModule : ModuleBase<SocketCommandContext>
    {
        public PokerService PokerService { get; set; }

        [Command("newgame")]
        [Alias("newg")]
        public async Task StartNewGame()
            => await PokerService.NewGame(Context);

        [Command("close")]
        [Description("Closes the pregame lobby.")]
        public async Task ClosePregame()
            => await PokerService.ClosePregame(Context);

        [Command("joingame")]
        [Alias("join")]
        public async Task JoinGame()
            => await PokerService.JoinGame(Context);

        [Command("leavegame")]
        [Alias("leave")]
        public async Task LeaveGame()
            => await PokerService.LeaveGame(Context);

        [Command("playerlist")]
        [Alias("plist")]
        public async Task ListPlayers()
            => await PokerService.ListPlayers(Context);

        [Command("start")]
        [Alias("begin", "startgame")]
        public async Task BeginGame()
            => await PokerService.StartGame(Context);

        [Command("ptest")]
        public async Task Test()
        {
            Console.WriteLine("Starting test...");
            try
            {
                PokerService.Test();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
        
    }
}