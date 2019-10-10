using System;
using System.Collections.Generic;

namespace IdleGame.Poker
{
    public class Deck
    {
        private List<Card> _cards = new List<Card>();        //TODO: Change to stack?

        public Deck()
        {
            foreach (var c in "cshd")
            {
                for (int i = 1; i <= 13; i++)
                {
                    _cards.Add(new Card(c.ToString(), i));
                }
            }
        }

        public Card DrawCard()
        {
            var r = new Random();
            var i = r.Next(0, _cards.Count - 1);
            var card = _cards[i];
            _cards.RemoveAt(i);
            return card;
        }

        public Card[] DrawCards(int amount)
        {
            Card[] cards = new Card[amount];
            for (int i = 0; i < amount; i++)
            {
                cards[i] = DrawCard();
            }

            return cards;
        }
        
        public void RemoveCard(string suit, int value)
        {
            _cards.Remove(new Card(suit, value));
        }

        public void Shuffle()
        {
            var newDeck = new List<Card>();
            for (int i = 0; i < _cards.Count; i++)
            {
                newDeck.Add(DrawCard());
            }

            _cards = newDeck;
            Console.WriteLine("Deck shuffled!");
        }
    }
}