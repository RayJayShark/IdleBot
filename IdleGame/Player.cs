using System;
using System.Collections.Generic;
using Discord;
using Google.Protobuf.WellKnownTypes;

namespace IdleGame
{
    public class Player
    {
        public ulong Id;
        public string Name;
        public string Faction;
        public string Class;
        public uint CurHp;
        public uint Money;
        public uint Level;
        public uint Exp;
        public byte SkillPoints = 0;
        private Timestamp _boost = DateTime.UnixEpoch.ToTimestamp();
        public Dictionary<uint, uint> Inventory = new Dictionary<uint, uint>();  //Key = id, Value = quantity
        public PlayerStats Stats;

        public Player(ulong id, string name, string faction, string cl)
        {
            Id = id;
            Name = name;
            Faction = faction;
            Class = cl;
            CurHp = 10;
            Money = 10;
            Level = 1;
            Exp = 0;
        }

        public Player(ulong Id, string Name, string Faction, string Class, uint CurHp, uint Money, uint Level, uint Exp, byte SkillPoints, DateTime Boost)
        {
            this.Id = Id;
            this.Name = Name;
            this.Faction = Faction;
            this.Class = Class;
            this.CurHp = CurHp;
            this.Money = Money;
            this.Level = Level;
            this.Exp = Exp;
            _boost = Boost.ToUniversalTime().ToTimestamp();
        }

        public bool LevelUp()
        {
            if (Exp > 10 * Level)
            {
                Level++;
                Exp = 0;
                return true;    // Leveled Up!
            }

            return false;       // Not enough Exp to level
        }

        public Timestamp GetBoost()
        {
            return _boost;
        }
        
        public double HoursSinceLastBoost()
        {
            return double.Parse(((DateTime.UtcNow.ToTimestamp().Seconds -_boost.Seconds) / 60.0 / 60).ToString());
        }

        public void ResetBoost()
        {
            _boost = DateTime.UtcNow.ToTimestamp();
        }
    }

    public class PlayerStats
    {
        private byte Health;
        private byte Strength;
        private byte Defence;

        public PlayerStats(byte health, byte strength, byte defence)
        {
            Health = health;
            Strength = strength;
            Defence = defence;
        }

        public void AddPointHealth(byte points = 1)
        {
            Health += points;
        }
        
        public void AddPointStrength(byte points = 1)
        {
            Strength += points;
        }
        
        public void AddPointDefence(byte points = 1)
        {
            Defence += points;
        }
    }
}