using System;
using System.Collections.Generic;

namespace IdleGame
{
    public class Enemy
    {
        private readonly string _name;
        private readonly uint _level;
        private uint _hp;
        private readonly uint _maxHp;
        private readonly uint _strength;
        private readonly uint _defence;
        private Dictionary<ulong, uint> AttackLog = new Dictionary<ulong, uint>(); // <UserId, DamageDealt>

        private Enemy()
        {
            //Create random enemy
            var rand = new Random();
            _name += (char) rand.Next(65, 91);
            for (var i = 0; i < rand.Next(2, 12); i++)
            {
                _name += (char) rand.Next(97, 123);
            }
            
            uint highestLevel = 0;
            var lowestLevel = uint.MaxValue;
            foreach (var p in Program.PlayerList)
            {
                if (p.Value.GetLevel() > highestLevel)
                    highestLevel = p.Value.GetLevel();
                if (p.Value.GetLevel() < lowestLevel)
                    lowestLevel = p.Value.GetLevel();
            }

            _level = (uint) rand.Next(Math.Clamp((int) lowestLevel - 5, 1, int.MaxValue), (int) highestLevel + 5);
            _hp = (uint) rand.Next((int) _level * 2, (int) _level * 5);
            _maxHp = _hp;
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

        public uint GetMaxHp()
        {
            return _maxHp;
        }

        public Dictionary<ulong, uint> GetAttackLog()
        {
            return AttackLog;
        }

        public string GetStats()
        {
            return $"Health: {_hp}\nStrength: {_strength}\nDefence: {_defence}";
        }

        public bool TakeDamage(ulong userId, uint damage)
        {
            if (damage >= _hp)
            {
                if (AttackLog.ContainsKey(userId))
                {
                    AttackLog[userId] += _hp;
                }
                else
                {
                    AttackLog.Add(userId, _hp);
                }
                return true;    //Killed
            }

            _hp -= damage;
            if (AttackLog.ContainsKey(userId))
            {
                AttackLog[userId] += damage;
            }
            else
            {
                AttackLog.Add(userId, damage);
            }
            
            return false;
        }

        public void DistributeExp()
        {
            foreach (var (id, damage) in AttackLog)
            {
                var expToGive = (uint) (damage / (double) _maxHp) * (_level * 10);
                if (Program.PlayerList.ContainsKey(id))
                {
                    Program.PlayerList[id].GiveExp(expToGive);
                }
                else
                {
                    Console.WriteLine($"Error giving {expToGive} Exp to ID: {id} - Not found in player list");
                }
            }
        }

        public (bool, uint) Rewards()
        {
            var rand = new Random();
            var money = (uint) rand.Next(1, 11);
            if (rand.Next(1, 11) == 5)
            {
                return (true, money);          // Give taco
            }
            return (false, money);             // Don't give taco
        }

        public static Enemy[] CreateMultiple(int amount)
        {
            var enemyArray = new Enemy[amount];
            for (var i = 0; i < amount; i++)
            {
                enemyArray[i] = new Enemy();
            }
            return enemyArray;
        }
        
    }
}