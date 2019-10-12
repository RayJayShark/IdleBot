namespace IdleGame.Poker
{
    public class PokerPlayer
    {
        private ulong id;
        private string name;
        private int money = 100;
        private HoleHand _holeHand;

        public PokerPlayer(ulong id, string name)
        {
            this.id = id;
            this.name = name;
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
        }

        public int TakeMoney(int amount)
        {
            return money -= amount;
        }

        public HoleHand GetHand()
        {
            return _holeHand;
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