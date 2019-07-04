using System;
using System.Net.Mail;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Org.BouncyCastle.Crypto.Engines;

namespace IdleGame
{
    public class GameModule : ModuleBase<SocketCommandContext>
    {

        [Command("new")]
        public async Task NewPlayer()
        {
            var guildUser = (IGuildUser) Context.User;
            string user = guildUser.Nickname;
            int success = Program.AddPlayer(Context.User.Id, user);

            if (success == 0)
            {
                await ReplyAsync($"Player \"{user}\" created successfully. Enjoy your journey!");
            }
            else
            {
                await ReplyAsync("You already have a character!");
            }
        }

        [Command("level")]
        public async Task CheckLevel(string name = "")
        {
            Player player;
            if (name.Equals(""))
            {
                player = Program.PlayerList[Context.User.Id];
                await ReplyAsync($"You are currently Level {player.Level} with {player.Exp} XP");
            }
            else
            {
                player = Program.FindPlayer(name);
                if (player.Id == 0)
                {
                    await ReplyAsync($"{name} doesn't have a character. Tell 'em to create one!");
                }
                else
                {
                    await ReplyAsync($"{player.Name} is currently Level {player.Level} with {player.Exp} XP");
                }
            }
        }
        
    }
}