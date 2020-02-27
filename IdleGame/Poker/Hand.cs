using System;

namespace IdleGame.Poker
{
    public class Hand
    {
        private Card[] cards;
        private int score;
        private int rank0 = 0;
        private int rank1 = 0;
        private int rank2 = 0;

        public Hand(Card[] cards)
        {
            if (cards.Length != 5)
            {
                throw new Exception("A hand must consist of 5 cards.");
            }
            this.cards = cards;
            CalculateScore();
        }

        public Hand(Card card1, Card card2, Card card3, Card card4, Card card5)
        {
            cards = new Card[5];
            cards[0] = card1;
            cards[1] = card2;
            cards[2] = card3;
            cards[3] = card4;
            cards[4] = card5;
            CalculateScore();
        }

        public void CalculateScore()
        {
            // Pair
            var pair = new int[2];
            var next = false;
            for (var i = 0; i < 4; i++)
            {
                for (var j = i + 1; j < 5; j++)
                {
                    if (cards[i].GetValue() == cards[j].GetValue())
                    {
                        next = true;
                        pair[0] = i;
                        pair[1] = j;
                        break;
                    }
                }
            }
        }

        private void CheckPair()
        {
            
        }
    }
}