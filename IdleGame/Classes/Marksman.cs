using System;
using Google.Protobuf.WellKnownTypes;

namespace IdleGame.Classes
{
    public class Marksman : Player
    {

        public Marksman(ulong id, string name, string faction)
        {
            Id = id;
            Name = name;
            Faction = faction;
            Class = "Marksman";
            CurHp = 70;
            Money = 10;
            Level = 1;
            Exp = 0;
            Stats = new PlayerStats(70 ,10, 7);
        }
        
        public Marksman(ulong Id, string Avatar, string Name, string Faction, string Class, uint CurHp, uint Money, uint Level, uint Exp, DateTime Boost)
        {
            this.Id = Id;
            this.Avatar = Avatar;
            this.Name = Name;
            this.Faction = Faction;
            this.Class = Class;
            this.CurHp = CurHp;
            this.Money = Money;
            this.Level = Level;
            this.Exp = Exp;
            this.Boost = Boost.Add(TimeZoneInfo.Local.BaseUtcOffset.Add(TimeSpan.FromHours(1))).ToUniversalTime().ToTimestamp();
        }
        
        public Marksman(ulong Id, string Avatar, string Name, string Faction, string Class, uint CurHp, uint Money, uint Level, uint Exp, DateTime Boost, PlayerStats Stats)
        {
            this.Id = Id;
            this.Avatar = Avatar;
            this.Name = Name;
            this.Faction = Faction;
            this.Class = Class;
            this.CurHp = CurHp;
            this.Money = Money;
            this.Level = Level;
            this.Exp = Exp;
            this.Boost = Boost.Add(TimeZoneInfo.Local.BaseUtcOffset).ToUniversalTime().ToTimestamp();
            this.Stats = Stats;
        }

        protected override void LevelUp()
        {
            if (Exp >= 10 * Level)
            {
                Exp -= 10 * Level;
                Level++;
                if (Level % 10 == 0)
                {
                    Stats.AddHealth(20);
                    Stats.AddStrength(5);
                    Stats.AddDefence(2);
                }
                else if (Level % 5 == 0)
                {
                    Stats.AddHealth(10);
                    Stats.AddStrength(3);
                    Stats.AddDefence();
                }
                else
                {
                    Stats.AddDefaults();
                }
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
}