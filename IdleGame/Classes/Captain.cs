using System;
using Google.Protobuf.WellKnownTypes;

namespace IdleGame.Classes
{
    public class Captain : Player
    {
        private Timestamp _boost = DateTime.UnixEpoch.ToTimestamp();
        private uint _curHp;
        
        public Captain(ulong id, string name, string faction)
        {
            Id = id;
            Name = name;
            Faction = faction;
            Class = "Captain";
            _curHp = 70;
            Money = 10;
            Level = 1;
            Exp = 0;
            Stats = new PlayerStats(70, 7, 10);
        }
        
        public Captain(ulong Id, string Name, string Faction, string Class, uint CurHp, uint Money, uint Level, uint Exp, DateTime Boost)
        {
            this.Id = Id;
            this.Name = Name;
            this.Faction = Faction;
            this.Class = Class;
            _curHp = CurHp;
            this.Money = Money;
            this.Level = Level;
            this.Exp = Exp;
            _boost = Boost.Add(TimeZoneInfo.Local.BaseUtcOffset.Add(TimeSpan.FromHours(1))).ToUniversalTime().ToTimestamp();
        }
        
        public Captain(ulong Id, string Name, string Faction, string Class, uint CurHp, uint Money, uint Level, uint Exp, DateTime Boost, PlayerStats Stats)
        {
            this.Id = Id;
            this.Name = Name;
            this.Faction = Faction;
            this.Class = Class;
            _curHp = CurHp;
            this.Money = Money;
            this.Level = Level;
            this.Exp = Exp;
            _boost = Boost.Add(TimeZoneInfo.Local.BaseUtcOffset).ToUniversalTime().ToTimestamp();
            this.Stats = Stats;
        }
        
        public override uint GetCurrentHp()
        {
            return _curHp;
        }

        public override void GiveHp(uint health)
        {
            _curHp += health;

            if (_curHp > Stats.GetHealth())
            {
                _curHp = Stats.GetHealth();
            }
        }

        public override bool TakeDamage(uint damage)
        {
            if (damage >= _curHp)
            {
                _curHp = 0;
                return true;      // dead
            }

            _curHp -= damage;
            return false;         // Not dead
        }
        
        public override bool LevelUp()
        {
            if (Exp >= 10 * Level)
            {
                Exp -= 10 * Level;
                Level++;
                if (Level % 10 == 0)
                {
                    Stats.AddHealth(20);
                    Stats.AddStrength(2);
                    Stats.AddDefence(5);
                }
                else if (Level % 5 == 0)
                {
                    Stats.AddHealth(10);
                    Stats.AddStrength();
                    Stats.AddDefence(3);
                }
                else
                {
                    Stats.AddDefaults();
                }
                return true;    // Leveled Up!
            }

            return false;       // Not enough Exp to level
        }
    }
}