using System;
using Discord;

namespace IdleGame.Poker
{
    public struct Card
    {
        private readonly string _suit;
        private readonly int _value;
        private readonly string _color;
        private readonly Emoji _emoji;

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
                    _suit = "clubs";
                    _color = "black";
                    _emoji = new Emoji("♣");
                    break;
                case "spades":
                case "s":
                    _suit = "spades";
                    _color = "black";
                    _emoji = new Emoji("♠");
                    break;
                case "hearts":
                case "h":
                    _suit = "hearts";
                    _color = "red";
                    _emoji = new Emoji("♥");
                    break;
                case "diamonds": 
                case "d":
                    _suit = "diamonds";
                    _color = "red";
                    _emoji = new Emoji("♦");
                    break;
                default:
                    throw new Exception("Invalid suit. Use plural name or first character.");
            }
            this._value = value;
        }

        public int GetValue()
        {
            return _value;
        }

        public string GetSuit()
        {
            return _suit;
        }
        
        public override string ToString()
        {
            switch (_value)
            {
                case 1:
                    return "A" + _emoji;
                case 11:
                    return "J" + _emoji;
                case 12:
                    return "Q" + _emoji;
                case 13:
                    return "K" + _emoji;
                default:
                    return $"{_value}{_emoji}";
            }
        }

        public bool Equals(Card card)
        {
            if (String.CompareOrdinal(_suit, card._suit) == 1 && _value == card._value)
            {
                return true;
            }

            return false;
        }
    }
}