using System;

namespace IdleGame
{
    public class Enemy
    {
        private string _name;
        private uint _level;
        private uint _hp;
        private uint _strength;
        private uint _defence;

        public Enemy()
        {
            //Create random enemy
            var rand = new Random();
            _name = "pleb";
            _level = (uint) rand.Next(1, 11);
            _hp = _level * 10;
            _strength = _level * 2;
            _defence = _level * 2;
        }

        public string GetName()
        {
            return _name;
        }

        public bool TakeDamage(uint damage)
        {
            if (damage >= _hp)
            {
                return true;    //Killed
            }

            _hp -= damage;
            return false;
        }

        public static Enemy[] CreateMultiple(int amount)
        {
            var enemyArray = new Enemy[amount];
            for (int i = 0; i < amount; i++)
            {
                enemyArray[i] = new Enemy();
            }
            return enemyArray;
        }
        
    }
}