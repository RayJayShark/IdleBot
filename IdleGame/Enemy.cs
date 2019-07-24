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
            _name += (char) rand.Next(65, 91);
            for (int i = 0; i < rand.Next(2, 12); i++)
            {
                _name += (char) rand.Next(97, 123);
            }
            _level = (uint) rand.Next(1, 11);
            _hp = _level * 10;
            _strength = _level * 2;
            _defence = _level * 2;
        }

        public string GetName()
        {
            return _name;
        }

        public uint GetStrength()
        {
            return _strength;
        }

        public uint GetDefence()
        {
            return _defence;
        }

        public string GetStats()
        {
            return $"Health: {_hp}\nStrength: {_strength}\nDefence: {_defence}";
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