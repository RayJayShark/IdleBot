using System;
using System.Collections.Generic;
using Google.Protobuf.WellKnownTypes;

namespace IdleGame.Classes
{
    public abstract class Player
    {
        //TODO: Change to interface?
        public ulong Id;
        public string Name;
        public string Faction;
        public string Class;
        public uint Money;
        public uint Level;
        private Timestamp _boost = DateTime.UnixEpoch.ToTimestamp();
        public Dictionary<uint, uint> Inventory = new Dictionary<uint, uint>();  //Key = id, Value = quantity
        public PlayerStats Stats;

        protected Player()
        {
            
        }

        public abstract uint GetExp();
        
        public abstract bool GiveExp(uint exp);
        
        public abstract bool LevelUp();

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

        public abstract uint GetCurrentHp();

        public abstract void GiveHp(uint health);

        public abstract bool TakeDamage(uint damage);

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