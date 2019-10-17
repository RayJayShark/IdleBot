using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Globalization;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using IdleGame.Poker;
using MySqlX.XDevAPI.Common;

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
            BetweenFlop,
            Flop,
            AfterFlop,
            Turn,
            PreRiver,
            River,
            Showdown
        };
        
        private States _gameState;
        private List<PokerPlayer> _playerList;
        private List<PokerPlayer> _foldedPlayers;
        private Deck _deck;
        private Card[] _river;
        private int _pot = 0;
        private int _dealer;
        private int _currentPlayer;
        private int _playerToMatch;
        private int _call;

        public PokerService()
        {
            _gameState = States.Closed;
            _playerList = new List<PokerPlayer>();
            _foldedPlayers = new List<PokerPlayer>();
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
            //TODO: Make more efficient
            foreach (var p in _playerList)
            {
                if (p.Equals(context.User.Id))
                {
                    _playerList.Remove(p);
                    _foldedPlayers.Remove(p);
                    await context.Channel.SendMessageAsync("You have successfully left the game.");
                    if (_playerList.Count == 0)
                    {
                        await ClosePregame(context); 
                    }
                    return;
                }
            }

            await context.Channel.SendMessageAsync("You are not in the game.");
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
            if (_gameState > States.Pregame)
            {
                await context.Channel.SendMessageAsync("Game is currently in progress.");
                return;
            }

            if (_gameState == States.Closed)
            {
                await context.Channel.SendMessageAsync(
                    $"No pregame open. Try \"{Environment.GetEnvironmentVariable("COMMAND_PREFIX")}newgame\"");
                return;
            }
            
            await context.Channel.SendMessageAsync("Starting game...");

            _gameState = States.Beginning;
            _deck = new Deck();
            foreach (var player in _playerList)
            {
                player.GiveMoney(100);        //TODO: Change to env variable
            }

            await DealHands(context.Message);
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
            
            // Helpers

        private async Task DealHands(SocketMessage message)
        {
            Console.WriteLine("Shuffling cards...");
            _deck.Shuffle();

            Console.WriteLine("Dealing cards...");
            foreach (var p in _playerList)
            {
                p.GiveHand(_deck.DrawCards(2));
                p.SendDM("Your hand: " + p.GetHand());
            }
            Console.WriteLine("Hands dealt.");

            await StartRound(message);
        }

        private void IncrementPlayer()
        {
            if (_currentPlayer == _playerList.Count - 1)
            {
                _currentPlayer = 0;
            }
            else
            {
                _currentPlayer++;
            }

            if (_foldedPlayers.Contains(_playerList[_currentPlayer]))
            {
                IncrementPlayer();
            }
        }

        private void IncrementDealer()
        {
            if (_dealer == _playerList.Count)
            {
                _dealer = 0;
            }
            else
            {
                _dealer++;
            }
        }

        private bool CheckForEnd()
        {
            if (_foldedPlayers.Count == 0)
            {
                if (_currentPlayer == _playerList.Count - 1)
                {
                    return _playerToMatch == 0;
                }
                
                return _playerToMatch == _currentPlayer + 1;
            }
            
            var nextPlayer = _currentPlayer;
            if (nextPlayer == _playerList.Count - 1)
            {
                nextPlayer = 0;
            }
            else
            {
                nextPlayer++;
            }
            
            while (_foldedPlayers.Contains(_playerList[nextPlayer]))
            {
                if (nextPlayer == _playerList.Count - 1)
                {
                    nextPlayer = 0;
                }
                else
                {
                    nextPlayer++;
                }
            }

            return nextPlayer == _playerToMatch;
        }
        
            // Gameplay

        private async Task StartRound(SocketMessage message)
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
                    else
                    {
                        smallBlind = _dealer + 1;
                        bigBlind = smallBlind + 1;
                    }

                    if (bigBlind == _playerList.Count - 1)
                    {
                        _currentPlayer = 0;
                    }
                    else
                    {
                        _currentPlayer = bigBlind + 1;
                    }
                    
                    _playerList[smallBlind].TakeMoney(5);
                    _playerList[bigBlind].TakeMoney(10);
                    _pot += 15;
                    _call = 10;
                    _playerToMatch = bigBlind;
                    await message.Channel.SendMessageAsync($"Small blind of 5 posted by {_playerList[smallBlind].GetName()}, big blind of 10 posted by {_playerList[bigBlind].GetName()}.");
                    _gameState++;
                    await StartRound(message);
                    return;
                case States.Preflop:
                    await message.Channel.SendMessageAsync($"{_playerList[_currentPlayer].GetName()}, would you like to **Call** the {_call - _playerList[_currentPlayer].GetTotalCall()} money, **Raise** by an *amount*, or **Fold**?");
                    Program.AddMessageEvent(PlayRound);
                    return;
                case States.BetweenFlop:
                    if (_dealer == _playerList.Count - 1)
                    {
                        _currentPlayer = 0;
                    }
                    else
                    {
                        _currentPlayer = _dealer + 1;
                    }

                    _playerToMatch = _currentPlayer;
                    _call = 0;

                    var flopCards = "";
                    _deck.DrawCard();
                    for (var i = 0; i < 3; i++)
                    {
                        _river[i] = _deck.DrawCard();
                        flopCards += _river[i] + "   ";
                    }
                    await message.Channel.SendMessageAsync(flopCards);
                    _gameState++;
                    await StartRound(message);
                    return;
                case States.Flop:
                    if (_call == 0)
                    {
                        await message.Channel.SendMessageAsync(
                            $"{_playerList[_currentPlayer].GetName()}, would you like to **Check**, **Raise** by an *amount*, or **Fold**?");
                    }
                    else
                    {
                        await message.Channel.SendMessageAsync(
                            $"{_playerList[_currentPlayer].GetName()}, would you like to **Call** the {_call - _playerList[_currentPlayer].GetTotalCall()} money, **Raise** by an *amount*, or **Fold**?"); 
                    }

                    Program.AddMessageEvent(PlayRound);
                    return;
                case States.AfterFlop:
                    if (_dealer == _playerList.Count - 1)
                    {
                        _currentPlayer = 0;
                    }
                    else
                    {
                        _currentPlayer = _dealer + 1;
                    }

                    _playerToMatch = _currentPlayer;
                    _call = 0;

                    var turnCards = "";
                    for (var i = 0; i < 3; i++)
                    {
                        turnCards += _river[i] + "   ";
                    }

                    _deck.DrawCard();
                    _river[3] = _deck.DrawCard();
                    turnCards += _river[3];

                    await message.Channel.SendMessageAsync(turnCards);
                    _gameState++;
                    await StartRound(message);
                    return;
                case States.Turn:
                    if (_call == 0)
                    {
                        await message.Channel.SendMessageAsync(
                            $"{_playerList[_currentPlayer].GetName()}, would you like to **Check**, **Raise** by an *amount*, or **Fold**?");
                    }
                    else
                    {
                        await message.Channel.SendMessageAsync(
                            $"{_playerList[_currentPlayer].GetName()}, would you like to **Call** the {_call - _playerList[_currentPlayer].GetTotalCall()} money, **Raise** by an *amount*, or **Fold**?"); 
                    }

                    Program.AddMessageEvent(PlayRound);
                    return;
                case States.PreRiver:
                    if (_dealer == _playerList.Count - 1)
                    {
                        _currentPlayer = 0;
                    }
                    else
                    {
                        _currentPlayer = _dealer + 1;
                    }
                    
                    _playerToMatch = _currentPlayer;
                    _call = 0;
                    
                    var riverCards = "";
                    for (var i = 0; i < 4; i++)
                    {
                        riverCards += _river[i] + "   ";
                    }

                    _deck.DrawCard();
                    _river[4] = _deck.DrawCard();
                    riverCards += _river[4];

                    await message.Channel.SendMessageAsync(riverCards);
                    _gameState++;
                    await StartRound(message);
                    return;
                case States.River:
                    if (_call == 0)
                    {
                        await message.Channel.SendMessageAsync(
                            $"{_playerList[_currentPlayer].GetName()}, would you like to **Check**, **Raise** by an *amount*, or **Fold**?");
                    }
                    else
                    {
                        await message.Channel.SendMessageAsync(
                            $"{_playerList[_currentPlayer].GetName()}, would you like to **Call** the {_call - _playerList[_currentPlayer].GetTotalCall()} money, **Raise** by an *amount*, or **Fold**?"); 
                    }

                    Program.AddMessageEvent(PlayRound);
                    return;
                case States.Showdown:
                    // TODO: Make algorithm to compare hands
                    return;
            }
        }

        private async Task PlayRound(SocketMessage message)
        {
            if (message.Author.Id != _playerList[_currentPlayer].GetId())
            {
                return;
            }

            var command = message.Content.ToLower().Split(' ');
            switch (command[0])
            {
                case "fold":
                    if (CheckForEnd())
                    {
                        _foldedPlayers.Add(_playerList[_currentPlayer]);
                        if (_foldedPlayers.Count == _playerList.Count - 1)
                        {
                            await message.Channel.SendMessageAsync(_playerList[_currentPlayer].GetName() + " folds.");
                            Program.RemoveMessageEvent(PlayRound);
                            var winner = 0;
                            for (var i = 0; i < _playerList.Count; i++)
                            {
                                if (_foldedPlayers.Contains(_playerList[i]))
                                {
                                    continue;
                                }

                                winner = i;
                            }
                            await WinPot(message, winner);
                            return;
                        }
                        await message.Channel.SendMessageAsync(_playerList[_currentPlayer].GetName() + " folds. Onto the next stage...");
                        _gameState++;
                        foreach (var p in _playerList)
                        {
                           p.ResetCall(); 
                        }
                    }
                    else
                    {
                        _foldedPlayers.Add(_playerList[_currentPlayer]);
                        await message.Channel.SendMessageAsync(_playerList[_currentPlayer].GetName() + " folds.");
                        IncrementPlayer();
                        if (_foldedPlayers.Count == _playerList.Count - 1)
                        {
                            Program.RemoveMessageEvent(PlayRound);
                            var winner = 0;
                            for (var i = 0; i < _playerList.Count; i++)
                            {
                                if (_foldedPlayers.Contains(_playerList[i]))
                                {
                                    continue;
                                }

                                winner = i;
                            }
                            await WinPot(message, winner);
                            return;
                        }
                    }
                    break;
                case "call":
                    if (_call == 0)
                    {
                        await message.Channel.SendMessageAsync("Nothing to call.");
                        return;
                    }
                    
                    var toCall = _call - _playerList[_currentPlayer].GetTotalCall();
                    _pot += toCall;
                    _playerList[_currentPlayer].Call(toCall);
                    if (CheckForEnd())
                    {
                        await message.Channel.SendMessageAsync(_playerList[_currentPlayer].GetName() + " calls. Onto the next stage...");
                        _gameState++;
                        foreach (var p in _playerList)
                        {
                            p.ResetCall(); 
                        }
                    }
                    else
                    {
                        await message.Channel.SendMessageAsync(_playerList[_currentPlayer].GetName() + " calls.");
                        IncrementPlayer();
                    }
                    break;
                case "raise":
                    if (command.Length < 2 || !int.TryParse(command[1], out var raise))
                    {
                        await message.Channel.SendMessageAsync("Invalid raise amount. Must be a positive integer.");
                        return;
                    }

                    _call += raise;
                    _playerToMatch = _currentPlayer;
                    await message.Channel.SendMessageAsync(
                        _playerList[_currentPlayer].GetName() + " raises by " + raise);
                    IncrementPlayer();
                    break;
                case "check":
                    if (_call > 0)
                    {
                        await message.Channel.SendMessageAsync("Cannot check. Would you like to **Call** the " + (_call - _playerList[_currentPlayer].GetTotalCall()) + "money?");
                        return;
                    }

                    if (CheckForEnd())
                    {
                        await message.Channel.SendMessageAsync("All players checked. Onto the next stage...");
                        _gameState++;
                    }
                    else
                    {
                        IncrementPlayer();
                    }
                    break;
                default:
                    return;
            }
            
            Program.RemoveMessageEvent(PlayRound);
            await StartRound(message);
        }

        private async Task WinPot(SocketMessage message, int player)
        {
            _playerList[player].GiveMoney(_pot);
            await message.Channel.SendMessageAsync($"{_playerList[player].GetName()} won this round with a pot of {_pot}!");
            _pot = 0;
            _currentPlayer = 0;
            _gameState = States.Beginning;
            _river = new Card[5];
            _foldedPlayers = new List<PokerPlayer>();
            _deck = new Deck();
            foreach (var p in _playerList)
            {
                p.ClearHand();
            }

            IncrementDealer();
            await DealHands(message);
        }
    }
}