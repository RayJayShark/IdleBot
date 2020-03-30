using System;
using System.Linq;
using System.Timers;

namespace IdleGame.Services
{
    public class TimerService
    {
        private readonly Timer _expTimer = new Timer();
        private readonly Timer _enemyTimer = new Timer();
        private SqlService _sqlService;
        
        public TimerService(int expTime, int enemyTime, SqlService sqlService)
        {
            _expTimer.Interval = expTime * 1000;
            _expTimer.Elapsed += GiveExp;

            _enemyTimer.Interval = enemyTime * 1000;
            _enemyTimer.Elapsed += RefreshEnemies;

            _expTimer.Enabled = true;
            _enemyTimer.Enabled = true;

            _sqlService = sqlService;
        }
        
        private void GiveExp(object source, ElapsedEventArgs e)
        {
            foreach (var p in Program.PlayerList)
            {
                var guild = Program.GetGuild(ulong.Parse(Environment.GetEnvironmentVariable("GUILD_ID")));
                var user = guild.GetUser(p.Value.GetId());
                if (user.VoiceState.HasValue && user.VoiceChannel.Id != guild.AFKChannel.Id)
                {
                    Program.PlayerList[p.Key].GiveExp(uint.Parse(Environment.GetEnvironmentVariable("IDLE_EXP")));
                }
                Program.PlayerList[p.Key].GiveHp(uint.Parse(Environment.GetEnvironmentVariable("IDLE_HP")));
            }
            LogService.GameLog("Exp given");
            _sqlService.UpdateDatabase();
        }
        
        private static void RefreshEnemies(object source, ElapsedEventArgs e)
        {
            Program.Enemies.Clear();
            Program.Enemies.AddRange(Enemy.CreatePerPlayer(Program.PlayerList.Count * 3));
            Program.Enemies = Program.Enemies.OrderBy(en => en.GetLevel()).ToList();
            LogService.GameLog("Enemies refreshed");
        }
    }
}