using System.Collections.Generic;
using IdleGame.Classes;

namespace IdleGame
{
    public class Party
    {
        private readonly int _id;
        private readonly List<Player> _playerList;

        public Party(int id)
        {
            _id = id;
            _playerList = new List<Player>();
        }

        public Party(int id, Player player)
        {
            _id = id;
            _playerList = new List<Player> {player};
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
            player.SetParty(_id);
            return true;
        }

        public void RemovePlayer(Player player)
        {
            if (_playerList.Remove(player))
                player.SetParty(-1);
        }
    }
}