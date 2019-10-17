using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using IdleGame.Poker;

namespace IdleGame.Services
{
    public class PokerService
    {
        private enum States
        {
            Closed,
            Pregame,
            Beginning,
            Preflop,
            Flop,
            Turn,
            River,
            Showdown
        };
        
        private States _gameState;
        private List<PokerPlayer> _playerList;
        private Deck _deck;
        private Card[] _river;
        private int _pot = 0;
        private int _dealer;
        private int _currentPlayer;

        public PokerService()
        {
            _gameState = States.Closed;
            _playerList = new List<PokerPlayer>();
            _river = new Card[5];
        }

        public void Test()
        {
            Console.WriteLine("Service test good");
        }
        
        // Pregame
        
        public async Task NewGame(SocketCommandContext context) 
        {
            if (_gameState == States.Pregame)
            {
                await context.Channel.SendMessageAsync(
                    $"Currently in pregame. Try joining with \"{Environment.GetEnvironmentVariable("COMMAND_PREFIX")}joingame\" or starting with \"{Environment.GetEnvironmentVariable("COMMAND_PREFIX")}start\"!");
                return;
            }

            if (_gameState > States.Pregame)
            {
                await context.Channel.SendMessageAsync(
                    "A game is currently in progress. Wait for it to finish before attempting to start a new one.");
                return;
            }

            var user = (IGuildUser) context.User;
            _playerList.Add(new PokerPlayer(user.Id, user.Nickname));
            _gameState = States.Pregame;
            await context.Channel.SendMessageAsync(
                $"New game started! New players can join with \"{Environment.GetEnvironmentVariable("COMMAND_PREFIX")}joingame\" and the game can be started with \"{Environment.GetEnvironmentVariable("COMMAND_PREFIX")}start\"!");
        }

        public async Task ClosePregame(SocketCommandContext context)
        {
            if (_gameState > States.Pregame)
            {
                await context.Channel.SendMessageAsync("Game is currently in progress.");
                return;
            }
            
            if (_gameState == States.Closed)
            {
                await context.Channel.SendMessageAsync(
                    $"A game lobby hasn't been opened. Use \"{Environment.GetEnvironmentVariable("COMMAND_PREFIX")}newgame\" to start one!");
                return;
            }
            
            _playerList = new List<PokerPlayer>();
            _gameState = States.Closed;
            await context.Channel.SendMessageAsync("Pregame lobby closed.");
        }

        public async Task JoinGame(SocketCommandContext context)
        {
            if (_gameState != States.Pregame)
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
            if (_gameState != States.Pregame)
            {
                await context.Channel.SendMessageAsync("Not in pregame.");
                return;
            }

            foreach (var p in _playerList)
            {
                if (p.Equals(context.User.Id))
                {
                    _playerList.Remove(p);
                    await context.Channel.SendMessageAsync("You have successfully left the pregame lobby.");
                    if (_playerList.Count == 0)
                    {
                        await ClosePregame(context); 
                    }
                    return;
                }
            }

            await context.Channel.SendMessageAsync("You are not in the lobby.");
        }

        public async Task ListPlayers(SocketCommandContext context)
        {
            if (_gameState == States.Closed)
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

            _gameState = States.Beginning;
            _deck = new Deck();
            foreach (var player in _playerList)
            {
                player.GiveMoney(100);        //TODO: Change to env variable
            }

            await DealHands(context);
        }

        public async Task EndGame(SocketCommandContext context)
        {
            if (_gameState == States.Pregame)
            {
                await context.Channel.SendMessageAsync($"Still in pregame. Use \"{Environment.GetEnvironmentVariable("COMMAND_PREFIX")}close\" if you want to close the pregame lobby.");
                return;
            }

            if (_gameState == States.Closed)
            {
                await context.Channel.SendMessageAsync("No game is in progress.");
                return;
            }

            _gameState = States.Closed;
            _playerList = new List<PokerPlayer>();
            _pot = 0;
            _dealer = 0;
            await context.Channel.SendMessageAsync("Game has ended.");
        }
        
        // Ingame

        private async Task DealHands(SocketCommandContext context)
        {
            await context.Channel.SendMessageAsync("Shuffling cards...");
            _deck.Shuffle();

            await context.Channel.SendMessageAsync("Dealing cards...");
            foreach (var p in _playerList)
            {
                p.GiveHand(_deck.DrawCards(2));
                p.SendDM("Your hand: " + p.GetHand());
            }
            await context.Channel.SendMessageAsync("Hands dealt.");

            await PlayRound(context);
        }

        private async Task PlayRound(SocketCommandContext context)
        {
            switch (_gameState)
            {
                case States.Beginning:
                    var smallBlind = 0;
                    var bigBlind = 0;
                    if (_dealer == _playerList.Count - 2)
                    {
                        smallBlind = _dealer + 1;
                        bigBlind = 0;
                    }
                    else if (_dealer == _playerList.Count - 1)
                    {
                        smallBlind = 0;
                        bigBlind = 1;
                    }

                    _playerList[smallBlind].TakeMoney(5);
                    _playerList[bigBlind].TakeMoney(10);
                    _pot += 15;
                    await context.Channel.SendMessageAsync($"Small blind of 5 posted by {_playerList[smallBlind].GetName()}, big blind of 10 posted by {_playerList[bigBlind].GetName()}.");
                    _gameState++;
                    await PlayRound(context);
                    return;
                case States.Preflop:
                    
                    break;
                case States.Flop:

                    break;
                case States.Turn:

                    break;
                case States.River:

                    break;
                
                case States.Showdown:

                    return;
            }
        }
    }
}