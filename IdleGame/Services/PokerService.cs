using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using IdleGame.Poker;

namespace IdleGame.Services
{
    public class PokerService
    {
        private bool _gameInProgress;
        private bool _preGame;
        private List<PokerPlayer> _playerList;
        private Deck _deck;

        public PokerService()
        {
            _gameInProgress = false;
            _preGame = false;
            _playerList = new List<PokerPlayer>();
        }

        public void Test()
        {
            Console.WriteLine("Service test good");
        }
        
        public async Task NewGame(SocketCommandContext context) 
        {
            if (_preGame)
            {
                await context.Channel.SendMessageAsync(
                    $"Currently in pregame. Try joining with \"{Environment.GetEnvironmentVariable("COMMAND_PREFIX")}joingame\" or starting with \"{Environment.GetEnvironmentVariable("COMMAND_PREFIX")}start\"!");
                return;
            }

            if (_gameInProgress)
            {
                await context.Channel.SendMessageAsync(
                    "A game is currently in progress. Wait for it to finish before attempting to start a new one.");
                return;
            }

            var user = (IGuildUser) context.User;
            _playerList.Add(new PokerPlayer(user.Id, user.Nickname));
            _preGame = true;
            await context.Channel.SendMessageAsync(
                $"New game started! Join with \"{Environment.GetEnvironmentVariable("COMMAND_PREFIX")}joingame\" and start with \"{Environment.GetEnvironmentVariable("COMMAND_PREFIX")}start\"!");
        }

        public async Task JoinGame(SocketCommandContext context)
        {
            if (!_preGame)
            {
                await context.Channel.SendMessageAsync("Not in pregame.");
                return;
            }

            var user = (IGuildUser) context.User;
            _playerList.Add(new PokerPlayer(user.Id, user.Nickname));
            await context.Channel.SendMessageAsync("Successfully joined game!");
        }

        public async Task ListPlayers(SocketCommandContext context)
        {
            if (!_preGame && !_gameInProgress)
            {
                await context.Channel.SendMessageAsync("No game has been started.");
                return;
            }

            var embed = new EmbedBuilder() {Title = "Player List:"};
            foreach (var p in _playerList)
            {
                embed.Description += p.GetName() + "\n";
            }

            embed.Description += "Total: " + _playerList.Count;

            await context.Channel.SendMessageAsync("", false, embed.Build());
        }
    }
}