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

        public string GetName()
        {
            return name;
        }
    }
}