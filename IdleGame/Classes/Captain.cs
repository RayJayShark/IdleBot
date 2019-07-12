using System;
using Google.Protobuf.WellKnownTypes;

namespace IdleGame.Classes
{
    public class Captain : Player
    {
        private Timestamp _boost = DateTime.UnixEpoch.ToTimestamp();
        
        public Captain(ulong id, string name, string faction)
        {
            Id = id;
            Name = name;
            Faction = faction;
            Class = "Captain";
            CurHp = 10;
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
            this.CurHp = CurHp;
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
            this.CurHp = CurHp;
            this.Money = Money;
            this.Level = Level;
            this.Exp = Exp;
            _boost = Boost.Add(TimeZoneInfo.Local.BaseUtcOffset).ToUniversalTime().ToTimestamp();
            this.Stats = Stats;
        }
    }
}