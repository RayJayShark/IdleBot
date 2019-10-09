using System;

namespace IdleGame.Poker
{
    public struct Card
    {
        public readonly string suit;
        public readonly int value;
        public readonly string color;

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
                    break;
            }
            this.value = value;
        }

        public override string ToString()
        {
            if (value > 1 && value < 11)
            {
                return value.ToString() + "of" + suit.Substring(0, 1).ToUpper();
            }

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
                    return "";
            }
        }
    }
}