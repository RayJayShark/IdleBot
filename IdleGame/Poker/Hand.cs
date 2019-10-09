using System;

namespace IdleGame.Poker
{
    public struct Hand
    {
        private Card[] _cards;

        public Hand(Card card1, Card card2)
        {
            _cards = new Card[] {card1, card2};
        }

        public Hand(Card[] cards)
        {
            if (cards.Length == 2)
            {
                _cards = cards;
            }
            else
            {
                throw new Exception("Hand must contain 2 cards.");
            }
        }
    }
}