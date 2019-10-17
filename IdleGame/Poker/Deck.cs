using System;
using System.Collections.Generic;

namespace IdleGame.Poker
{
    public class Deck
    {
        private Stack<Card> _cards = new Stack<Card>();

        public Deck()
        {
            foreach (var c in "cshd")
            {
                for (int i = 1; i <= 13; i++)
                {
                    _cards.Push(new Card(c.ToString(), i));
                }
            }
        }

        public Card DrawCard()
        {
            return _cards.Pop();
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

        public void Shuffle()
        {
            for (int j = 0; j < 3; j++)
            {
                var deck = new List<Card>(_cards.ToArray());

                var newDeck = new Stack<Card>();
                for (int i = deck.Count - 1; i >= 0; i--)
                {
                    var r = new Random();
                    var indexToGrab = r.Next(0, i);
                    newDeck.Push(deck[indexToGrab]);
                    deck.RemoveAt(indexToGrab);
                }

                _cards = newDeck;
            }

            Console.WriteLine("Deck shuffled!");
        }
    }
}