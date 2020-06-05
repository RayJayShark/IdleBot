using System;
using System.Collections.Generic;
using Google.Protobuf.WellKnownTypes;

namespace IdleGame.Classes
{
    public abstract class Player
    {
        //TODO: Use DateTimeOffset for boost???
        protected ulong Id;
        protected string Avatar;
        protected string Name;
        protected string Faction;
        protected string Class;
        protected uint Money;
        protected uint Level;
        protected uint CurHp;
        protected uint Exp;
        protected int Party = -1;
        protected Timestamp Boost = DateTime.UnixEpoch.ToTimestamp();
        public Dictionary<uint, uint> Inventory = new Dictionary<uint, uint>();  //Key = id, Value = quantity
        public PlayerStats Stats;

        public ulong GetId()
        {
            return Id;
        }

        public string GetAvatar()
        {
            return Avatar;
        }

        public void SetAvatar(string avatar)
        {
            Avatar = avatar;
        }

        public string GetName()
        {
            return Name;
        }

        public void SetName(string s)
        {
            Name = s;
        }

        public string GetFaction()
        {
            return Faction;
        }

        public string GetClass()
        {
            return Class;
        }

        public uint GetMoney()
        {
            return Money;
        }

        public void GiveMoney(uint amount)
        {
            Money += amount;
        }

        public void TakeMoney(uint amount)
        {
            if (amount > Money)
            {
                Money = 0;
            }
            else
            {
                Money -= amount;
            }
        }

        public uint GetLevel()
        {
            return Level;
        }

        public int GetParty()
        {
            return Party;
        }

        public void SetParty(int newParty)
        {
            Party = newParty;
        }
        
        public Timestamp GetBoost()
        {
            return Boost;
        }
        
        public double HoursSinceLastBoost()
        {
            return ((DateTime.UtcNow.ToTimestamp().Seconds - Boost.Seconds) / 60.0 / 60);
        }

        public void ResetBoost()
        {
            Boost = DateTime.UtcNow.ToTimestamp();
        }
        
        public uint GetExp()
        {
            return Exp;
        }

        public void GiveExp(uint exp)
        {
            Exp += exp;
            LevelUp();
        }

        public uint GetCurrentHp()
        {
            return CurHp;
        }

        public void GiveHp(uint health)
        {
            CurHp += health;

            if (CurHp > Stats.GetHealth())
            {
                CurHp = Stats.GetHealth();
            }
        }

        public bool GiveItem(string itemName, uint amount = 1)
        {
            var itemId = Program.FindItemId(itemName);
            if (itemId == 0)
            {
                Console.WriteLine($"\"{itemName}\" isn't an item.");
                return false;
            }
            return GiveItem(itemId, amount);
        }
        public bool GiveItem(uint itemId, uint amount = 1)
        {
            if (!Program.ValidItemId(itemId))
            {
                Console.WriteLine($"\"{itemId}\" not a valid item id");
                return false;
            }

            if (!Inventory.ContainsKey(itemId))
            {
                Inventory.Add(itemId, amount);
            }
            else
            {
                Inventory[itemId] += amount;
            }

            return true;
        }
        
        public bool TakeItem(string itemName, uint amount = 1)
        {
            var itemId = Program.FindItemId(itemName);
            if (itemId == 0)
            {
                Console.WriteLine($"\"{itemName}\" isn't an item.");
                return false;
            }
            return GiveItem(itemId, amount);
        }

        public bool TakeItem(uint itemId, uint amount)
        {
            if (!Program.ValidItemId(itemId))
            {
                Console.WriteLine($"\"{itemId}\" not a valid item id");
                return false;
            }
            
            if (!Inventory.ContainsKey(itemId) || Inventory[itemId] < amount)
            {
                Console.WriteLine($"{Name} doesn't have enough of item id \"{itemId}\"");
                return false;
            }
            
            if (Inventory[itemId] == amount)
            {
                Inventory.Remove(itemId);
            }
            else
            {
                Inventory[itemId] -= amount;
            }
            return true;
        }

        public bool TakeDamage(uint damage)
        {
            if (damage >= CurHp)
            {
                CurHp = 0;
                return true;      // dead
            }

            CurHp -= damage;
            return false;
        }
        
        protected virtual void LevelUp()
        {
            if (Exp >= 10 * Level)
            {
                Exp -= 10 * Level;
                Level++;
                Stats.AddDefaults();
                CurHp = Stats.GetHealth();
                if (Exp >= 10 * Level)
                {
                    LevelUp();
                    return;
                }

                var guild = Program.GetGuild(ulong.Parse(Environment.GetEnvironmentVariable("GUILD_ID")));
                guild.GetTextChannel(ulong.Parse(Environment.GetEnvironmentVariable("CHANNEL_ID")))
                    .SendMessageAsync($"{guild.GetUser(Id).Mention} has leveled up! They are now Level {Level}");
            }
        }
    }

    public class PlayerStats
    {
        private ushort Health;
        private ushort Strength;
        private ushort Defence;

        public ushort GetHealth()
        {
            return Health;
        }

        public ushort GetStrength()
        {
            return Strength;
        }

        public ushort GetDefence()
        {
            return Defence;
        }
        
        public PlayerStats(ushort health, ushort strength, ushort defence)
        {
            Health = health;
            Strength = strength;
            Defence = defence;
        }

        public void AddDefaults()
        {
            Health += 5;
            Strength += 1;
            Defence += 1;
        }
        
        public void AddHealth(byte points = 5)
        {
            Health += points;
        }
        
        public void AddStrength(byte points = 1)
        {
            Strength += points;
        }
        
        public void AddDefence(byte points = 1)
        {
            Defence += points;
        }
    }
}