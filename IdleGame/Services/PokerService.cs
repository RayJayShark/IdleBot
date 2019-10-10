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
        private List<PokerPlayer> _playerList;     //TODO: Change to queue?
        private Deck _deck;
        private int pot;
        private int dealer;
        private int currentPlayer;

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
        
        // Pregame
        
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
                $"New game started! New players can join with \"{Environment.GetEnvironmentVariable("COMMAND_PREFIX")}joingame\" and the game can be started with \"{Environment.GetEnvironmentVariable("COMMAND_PREFIX")}start\"!");
        }

        public async Task ClosePregame(SocketCommandContext context)
        {
            if (_gameInProgress)
            {
                await context.Channel.SendMessageAsync("Game is currently in progress.");
                return;
            }
            
            if (!_preGame)
            {
                await context.Channel.SendMessageAsync(
                    $"A game lobby hasn't been opened. Use \"{Environment.GetEnvironmentVariable("COMMAND_PREFIX")}newgame\" to start one!");
                return;
            }
            
            _playerList = new List<PokerPlayer>();
            _preGame = false;
            await context.Channel.SendMessageAsync("Pregame lobby closed.");
        }

        public async Task JoinGame(SocketCommandContext context)
        {
            if (!_preGame)
            {
                await context.Channel.SendMessageAsync("Not in pregame.");
                return;
            }
            
            var user = (IGuildUser) context.User;
            var player = new PokerPlayer(user.Id, user.Nickname);

            foreach (var p in _playerList)
            {
                if (p.Equals(player))
                {
                    await context.Channel.SendMessageAsync("You're already in the game!");
                    return;
                }
            }
            
            _playerList.Add(player);
            await context.Channel.SendMessageAsync($"Welcome to the game {player.GetName()}!");
        }

        public async Task LeaveGame(SocketCommandContext context)
        {
            if (!_preGame)
            {
                await context.Channel.SendMessageAsync("Not in pregame.");
                return;
            }

            if (_playerList.Count == 1)
            {
                await context.Channel.SendMessageAsync("You have successfully left the pregame lobby.");
                await ClosePregame(context);
                return;
            }
            
            foreach (var p in _playerList)
            {
                if (p.Equals(context.User.Id))
                {
                    _playerList.Remove(p);
                    await context.Channel.SendMessageAsync("You have successfully left the pregame lobby.");
                    return;
                }
            }

            await context.Channel.SendMessageAsync("You are not in the lobby.");
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

        public async Task StartGame(SocketCommandContext context)
        {
            await context.Channel.SendMessageAsync("Starting game...");

            _preGame = false;
            _gameInProgress = true;
            _deck = new Deck();

            await context.Channel.SendMessageAsync("Shuffling cards...");
            _deck.Shuffle();

            await context.Channel.SendMessageAsync("Dealing cards...");
            foreach (var p in _playerList)
            {
                p.GiveHand(_deck.DrawCards(2));
                var ch = await context.Guild.GetUser(p.GetId()).GetOrCreateDMChannelAsync();
                await ch.SendMessageAsync("Your hand: " + p.GetHand());
            }
            await context.Channel.SendMessageAsync("Hands dealt.");
        }
    }
    
    // Ingame
}