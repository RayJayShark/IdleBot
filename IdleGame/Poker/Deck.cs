using System;
using System.Collections.Generic;

namespace IdleGame.Poker
{
    public class Deck
    {
        public List<Card> _cards = new List<Card>();

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
    }
    
}