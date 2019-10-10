namespace IdleGame.Poker
{
    public class PokerPlayer
    {
        private ulong id;
        private string name;
        private Hand hand;

        public PokerPlayer(ulong id, string name)
        {
            this.id = id;
            this.name = name;
        }

        public void GiveHand(Card[] cards)
        {
            hand = new Hand(cards);
        }

        public ulong GetId()
        {
            return id;
        }
        
        public string GetName()
        {
            return name;
        }

        public Hand GetHand()
        {
            return hand;
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