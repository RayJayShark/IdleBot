using System;

namespace IdleGame.Poker
{
    public struct Card
    {
        private readonly string suit;
        private readonly int value;
        private readonly string color;

        public Card(string suit, int value)
        {
            if (value < 1 || value > 13)
            {
                throw new Exception("Index for card out of bounds.");
            }

            switch (suit.ToLower())
            {
                case "clubs":
                case "c":
                    this.suit = "clubs";
                    color = "black";
                    break;
                case "spades":
                case "s":
                    this.suit = "spades";
                    color = "black";
                    break;
                case "hearts":
                case "h":
                    this.suit = "hearts";
                    color = "red";
                    break;
                case "diamonds": 
                case "d":
                    this.suit = "diamonds";
                    color = "red";
                    break;
                default:
                    throw new Exception("Invalid suit. Use plural name or first character.");
            }
            this.value = value;
        }

        public override string ToString()
        {
            switch (value)
            {
                case 1:
                    return "Aof" + suit.Substring(0, 1).ToUpper();
                case 11:
                    return "Jof" + suit.Substring(0, 1).ToUpper();
                case 12:
                    return "Qof" + suit.Substring(0, 1).ToUpper();
                case 13:
                    return "Kof" + suit.Substring(0, 1).ToUpper();
                default:
                    return $"{value}of{suit.Substring(0,1).ToUpper()}";
            }
        }

        public bool Equals(Card card)
        {
            if (String.CompareOrdinal(suit, card.suit) == 1 && value == card.value)
            {
                return true;
            }

            return false;
        }
    }
}