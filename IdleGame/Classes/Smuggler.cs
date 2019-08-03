using System;
using Google.Protobuf.WellKnownTypes;

namespace IdleGame.Classes
{
    public class Smuggler : Player
    {
        private Timestamp _boost = DateTime.UnixEpoch.ToTimestamp();
        private uint _curHp;
        private uint _exp;
        
        public Smuggler(ulong id, string name, string faction)
        {
            Id = id;
            Name = name;
            Faction = faction;
            Class = "Smuggler";
            _curHp = 100;
            Money = 10;
            Level = 1;
            _exp = 0;
            Stats = new PlayerStats(100, 7, 7);
        }
        
        public Smuggler(ulong Id, string Name, string Faction, string Class, uint CurHp, uint Money, uint Level, uint Exp, DateTime Boost)
        {
            this.Id = Id;
            this.Name = Name;
            this.Faction = Faction;
            this.Class = Class;
            _curHp = CurHp;
            this.Money = Money;
            this.Level = Level;
            this._exp = Exp;
            _boost = Boost.Add(TimeZoneInfo.Local.BaseUtcOffset.Add(TimeSpan.FromHours(1))).ToUniversalTime().ToTimestamp();
        }
        
        public Smuggler(ulong Id, string Name, string Faction, string Class, uint CurHp, uint Money, uint Level, uint Exp, DateTime Boost, PlayerStats Stats)
        {
            this.Id = Id;
            this.Name = Name;
            this.Faction = Faction;
            this.Class = Class;
            _curHp = CurHp;
            this.Money = Money;
            this.Level = Level;
            this._exp = Exp;
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
        
        public override uint GetExp()
        {
            return _exp;
        }
        
        public override void GiveExp(uint exp)
        {
            _exp += exp;
            LevelUp();
        }
        
        public override void LevelUp()
        {
            if (_exp >= 10 * Level)
            {
                _exp -= 10 * Level;
                Level++;
                if (Level % 10 == 0)
                {
                    Stats.AddHealth(30);
                    Stats.AddStrength(2);
                    Stats.AddDefence(2);
                }
                else if (Level % 5 == 0)
                {
                    Stats.AddHealth(20);
                    Stats.AddStrength();
                    Stats.AddDefence();
                }
                else
                {
                    Stats.AddDefaults();
                }
                _curHp = Stats.GetHealth();
                if (_exp >= 10 * Level)
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
}