using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;


namespace IdleGame
{
    public class GameModule : ModuleBase<SocketCommandContext>
    {
        private const string Y = "\uD83C\uDDFE";
        private const string N = "\uD83C\uDDF3";
        private readonly string NoChar = $"You don't have a character. Use \"{Environment.GetEnvironmentVariable("COMMAND_PREFIX")}new\" to make one!";
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
        public async Task CheckLevel([Remainder] string name = "")
        {
            Player player;
            if (name.Equals(""))
            {
                if (!Program.PlayerList.ContainsKey(Context.User.Id))
                {
                    await ReplyAsync(NoChar);
                    return;
                }
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

        [Command("inventory")]
        [Alias("inv")]
        public async Task CheckInventory()
        {
            if (!Program.PlayerList.ContainsKey(Context.User.Id))
            {
                await ReplyAsync(NoChar);
            }
            else
            {
                //TODO: Make prettier embed
                try
                {
                    var player = Program.PlayerList[Context.User.Id];
                    var embed = new EmbedBuilder();
                    embed.Color = Color.Blue;
                    embed.Title = player.Name + "'s inventory";
                    foreach (var i in player.Inventory)
                    {
                        embed.Description +=  Program.itemMap[i.Key] + " " + i.Value + "\n";
                    }

                    await ReplyAsync("", false, embed.Build());
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
        }
        
        [Command("reset")]
        public async Task ResetPlayer()
        {
            var message = await ReplyAsync(
                $"{Context.User.Mention} this will reset your character, including all progress. However, you will keep your inventory. \nThere is no going back! Are you sure?");
            UserId = Context.User.Id;
            await message.AddReactionAsync(new Emoji(Y));
            await message.AddReactionAsync(new Emoji(N));
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
                    if (reaction.Emote.Name == Y)
                    {
                        var player = Program.PlayerList[UserId];
                        Program.PlayerList[UserId] = new Player(player.Id, player.Name) {Inventory = player.Inventory};
                        await ReplyAsync("Your character was successfully reset.");
                        ResetId = 0;
                        UserId = 0;
                        Context.Client.ReactionAdded -= ResetConfirmation;
                    }
                    else if (reaction.Emote.Name == N)
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
            await message.AddReactionAsync(new Emoji(Y));
            await message.AddReactionAsync(new Emoji(N));
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
                    if (reaction.Emote.Name == Y)
                    {
                        Program.PlayerList.Remove(UserId);
                        await ReplyAsync("Your character was successfully deleted.");
                        Context.Client.ReactionAdded -= DeleteConfirmation;
                        DeleteId = 0;
                        UserId = 0;
                    }
                    else if (reaction.Emote.Name == N)
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

    [Group("admin")]
    [RequireOwner]
    public class AdminModule : ModuleBase<SocketCommandContext>
    {
        [Command("give")]
        public async Task GiveItem(uint amount, uint itemId, [Remainder] string playerName = "")
        {
            ulong playerId;
            if (playerName == "")
            {
                playerId = Context.User.Id;
            }
            else
            {
                playerId = Program.FindPlayer(playerName).Id;
            }

            if (Program.PlayerList[playerId].Inventory.ContainsKey(itemId))
            {
                Program.PlayerList[playerId].Inventory[itemId] += amount;
            }
            else
            {
                Program.PlayerList[playerId].Inventory.Add(itemId, amount);
            }

            await ReplyAsync($"You now have {Program.PlayerList[playerId].Inventory[itemId]} {Program.itemMap[itemId]}");
        }
    }
}