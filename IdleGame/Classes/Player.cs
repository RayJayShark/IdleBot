using System;
using System.Collections.Generic;
using Google.Protobuf.WellKnownTypes;

namespace IdleGame.Classes
{
    public abstract class Player
    {
        //TODO: Use DateTimeOffset for boost???
        protected ulong Id;
        protected string Name;
        protected string Faction;
        protected string Class;
        protected uint Money;
        protected uint Level;
        protected uint CurHp;
        protected uint Exp;
        protected Timestamp Boost = DateTime.UnixEpoch.ToTimestamp();
        public Dictionary<uint, uint> Inventory = new Dictionary<uint, uint>();  //Key = id, Value = quantity
        public PlayerStats Stats;

        public ulong GetId()
        {
            return Id;
        }

        public string GetName()
        {
            return Name;
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

        public uint GetLevel()
        {
            return Level;
        }
        
        public Timestamp GetBoost()
        {
            return Boost;
        }
        
        public double HoursSinceLastBoost()
        {
            return double.Parse(((DateTime.UtcNow.ToTimestamp().Seconds - Boost.Seconds) / 60.0 / 60).ToString());
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
        private byte Health;
        private byte Strength;
        private byte Defence;

        public byte GetHealth()
        {
            return Health;
        }

        public byte GetStrength()
        {
            return Strength;
        }

        public byte GetDefence()
        {
            return Defence;
        }
        
        public PlayerStats(byte health, byte strength, byte defence)
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