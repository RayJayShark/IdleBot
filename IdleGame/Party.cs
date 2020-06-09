using System.Collections.Generic;
using Discord;
using Discord.WebSocket;
using IdleGame.Classes;

namespace IdleGame
{
    public class Party
    {
        private readonly int _id;
        private readonly DiscordSocketClient _client;
        private readonly List<Player> _playerList;
        private readonly List<IDMChannel> _dmList;

        public Party(int id, DiscordSocketClient client)
        {
            _id = id;
            _client = client;
            _playerList = new List<Player>();
            _dmList = new List<IDMChannel>();
        }

        public Party(int id, Player player, DiscordSocketClient client)
        {
            _id = id;
            _client = client;
            _playerList = new List<Player> {player};
            _dmList = new List<IDMChannel> { _client.GetUser(player.GetId()).GetOrCreateDMChannelAsync().Result };
            player.SetParty(id);
        }

        public int Count()
        {
            return _playerList.Count;
        }
        
        public int GetId()
        {
            return _id;
        }

        public bool ContainsPlayer(Player player)
        {
            return _playerList.Contains(player);
        }

        public bool AddPlayer(Player player)
        {
            if (ContainsPlayer(player))
                return false;
            
            _playerList.Add(player);
            _dmList.Add(_client.GetUser(player.GetId()).GetOrCreateDMChannelAsync().Result);
            player.SetParty(_id);
            return true;
        }

        public void RemovePlayer(Player player)
        {
            var index = _playerList.FindIndex(p => p.GetId() == player.GetId());
            _playerList.RemoveAt(index);
            _dmList.RemoveAt(index);
            player.SetParty(-1);
            
        }

        public IDMChannel GetDmChannel(Player player)
        {
            var index = _playerList.FindIndex(p => p.GetId() == player.GetId());
            return _dmList[index];
        }

        public IDMChannel GetDmChannel(ulong id)
        {
            var index = _playerList.FindIndex(p => p.GetId() == id);
            return _dmList[index];
        }

        public IDMChannel GetDmChannel(int index)
        {
            return _dmList[index];
        }
    }
}