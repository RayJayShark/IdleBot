using System;
using Discord;

namespace IdleGame.Poker
{
    public class PokerPlayer
    {
        private ulong id;
        private string name;
        private int money = 100;
        private HoleHand _holeHand;
        private IDMChannel _dmChannel;

        public PokerPlayer(ulong id, string name)
        {
            this.id = id;
            this.name = name;
            var g = Program.GetGuild(ulong.Parse(Environment.GetEnvironmentVariable("GUILD_ID")));
            var u = g.GetUser(id);
            _dmChannel = u.GetOrCreateDMChannelAsync().Result;
            
        }

        public void GiveHand(Card[] cards)
        {
            _holeHand = new HoleHand(cards);
        }

        public ulong GetId()
        {
            return id;
        }
        
        public string GetName()
        {
            return name;
        }

        public int GetMoney()
        {
            return money;
        }

        public void GiveMoney(int amount)
        {
            money += amount;
            SendDM("Money: " + money);
        }

        public int TakeMoney(int amount)
        {
            money -= amount;
            SendDM("Money: " + money);
            return money;
        }

        public HoleHand GetHand()
        {
            return _holeHand;
        }

        public void SendDM(string message)
        {
            _dmChannel.SendMessageAsync(message);
        }

        public bool Equals(PokerPlayer p)
        {
            return id == p.id;
        }

        public bool Equals(ulong id)
        {
            return id == this.id;
        }
        
    }
}