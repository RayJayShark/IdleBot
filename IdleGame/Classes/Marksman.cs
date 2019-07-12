using System;
using Google.Protobuf.WellKnownTypes;

namespace IdleGame.Classes
{
    public class Marksman : Player
    {
        private Timestamp _boost = DateTime.UnixEpoch.ToTimestamp();
        
        public Marksman(ulong id, string name, string faction)
        {
            Id = id;
            Name = name;
            Faction = faction;
            Class = "Marksman";
            CurHp = 10;
            Money = 10;
            Level = 1;
            Exp = 0;
            Stats = new PlayerStats(70 ,10, 7);
        }
        
        public Marksman(ulong Id, string Name, string Faction, string Class, uint CurHp, uint Money, uint Level, uint Exp, DateTime Boost)
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
        
        public Marksman(ulong Id, string Name, string Faction, string Class, uint CurHp, uint Money, uint Level, uint Exp, DateTime Boost, PlayerStats Stats)
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