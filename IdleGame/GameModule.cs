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
        
        
    }
}