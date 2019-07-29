using System;
using System.Reflection.Metadata.Ecma335;

namespace IdleGame
{
    public class Enemy
    {
        private readonly string _name;
        private readonly uint _level;
        private uint _hp;
        private readonly uint _strength;
        private readonly uint _defence;

        private Enemy()
        {
            //Create random enemy
            var rand = new Random();
            _name += (char) rand.Next(65, 91);
            for (int i = 0; i < rand.Next(2, 12); i++)
            {
                _name += (char) rand.Next(97, 123);
            }

            uint highestLevel = 0;
            foreach (var p in Program.PlayerList)
            {
                if (p.Value.Level > highestLevel)
                    highestLevel = p.Value.Level;
            }
            
            _level = (uint) rand.Next(1, (int) highestLevel + 1);
            _hp = (uint) rand.Next((int) _level * 2, (int) _level * 5);
            _strength = (uint) rand.Next((int) _level, (int) _level * 2);
            _defence = (uint) rand.Next((int) _level, (int) _level * 2);
        }

        public string GetName()
        {
            return _name;
        }

        public uint GetLevel()
        {
            return _level;
        }

        public uint GetStrength()
        {
            return _strength;
        }

        public uint GetDefence()
        {
            return _defence;
        }

        public uint GetHp()
        {
            return _hp;
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