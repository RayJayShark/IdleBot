using System;
using System.Net.Mail;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using Google.Protobuf.WellKnownTypes;
using Org.BouncyCastle.Crypto.Engines;

namespace IdleGame
{
    public class GameModule : ModuleBase<SocketCommandContext>
    {
        private ulong UserId;
        private ulong DeleteId;
        private ulong ResetId;
        
        [Command("intro")]
        public async Task Intro()
        {
            var user = Context.User;
            var channel = await user.GetOrCreateDMChannelAsync();
            await channel.SendMessageAsync("test");
        }
        
        [Command("new")]
        public async Task NewPlayer()
        {
            var guildUser = (IGuildUser) Context.User;
            string user = guildUser.Nickname;
            int success = Program.AddPlayer(Context.User.Id, user);

            if (success == 0)
            {
                await ReplyAsync($"Player \"{user}\" created successfully. Enjoy your journey! Use the {Environment.GetEnvironmentVariable("COMMAND_PREFIX")}intro command for an introduction!");
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
                await ReplyAsync($"You are currently Level {player.Level} with {player.Exp} XP ({(player.Level * 10) - player.Exp} to the next level)");
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
                    await ReplyAsync($"{player.Name} is currently Level {player.Level} with {player.Exp} XP ({(player.Level * 10) - player.Exp} to the next level)");
                }
            }
        }
        
        [Command("reset")]
        public async Task ResetPlayer()
        {
            var message = await ReplyAsync(
                $"{Context.User.Mention} this will reset your character, including all progress. However, you will keep your inventory. \nThere is no going back! Are you sure?");
            UserId = Context.User.Id;
            var y = new Emoji("\uD83C\uDDFE");
            var n = new Emoji("\uD83C\uDDF3");
            await message.AddReactionAsync(y);
            await message.AddReactionAsync(n);
            ResetId = message.Id;
            
            Context.Client.ReactionAdded += ResetConfirmation;
        }
        
        private async Task ResetConfirmation(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel channel,
            SocketReaction reaction)
        {
            if (reaction.MessageId == ResetId)
            {
                if (reaction.UserId == UserId)
                {
                    if (reaction.Emote.Name == "\uD83C\uDDFE")
                    {
                        //Reset Character
                        ResetId = 0;
                        UserId = 0;
                        Context.Client.ReactionAdded -= ResetConfirmation;
                    }
                    else if (reaction.Emote.Name == "\uD83C\uDDF3")
                    {
                        await reaction.Message.Value.DeleteAsync();
                        ResetId = 0;
                        UserId = 0;
                        Context.Client.ReactionAdded -= ResetConfirmation;
                    }
                }
            }
        }

        [Command("delete")]
        public async Task DeletePlayer()
        {
            var message = await ReplyAsync(
                $"{Context.User.Mention} this will delete your character, including all progress and inventory. \nThere is no going back! Are you sure?");
            UserId = Context.User.Id;
            var y = new Emoji("\uD83C\uDDFE");
            var n = new Emoji("\uD83C\uDDF3");
            await message.AddReactionAsync(y);
            await message.AddReactionAsync(n);
            DeleteId = message.Id;
            
            Context.Client.ReactionAdded += DeleteConfirmation;
        }

        private async Task DeleteConfirmation(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel channel,
            SocketReaction reaction)
        {
            if (reaction.MessageId == DeleteId)
            {
                if (reaction.UserId == UserId)
                {
                    if (reaction.Emote.Name == "\uD83C\uDDFE")
                    {
                        //Delete Character
                        Context.Client.ReactionAdded -= DeleteConfirmation;
                        DeleteId = 0;
                        UserId = 0;
                    }
                    else if (reaction.Emote.Name == "\uD83C\uDDF3")
                    {
                        await reaction.Message.Value.DeleteAsync();
                        Context.Client.ReactionAdded -= DeleteConfirmation;
                        DeleteId = 0;
                        UserId = 0;
                    }
                }
            }
        }
        
    }
}