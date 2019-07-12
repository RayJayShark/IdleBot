using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using static IdleGame.Program;


namespace IdleGame
{
    public class GameModule : ModuleBase<SocketCommandContext>
    {
        private const string Y = "\uD83C\uDDFE";
        private const string N = "\uD83C\uDDF3";
        private ulong _userId;
        private ulong _deleteId;
        private ulong _resetId;

        [Command("intro")]
        public async Task Intro()
        {
            //TODO: Resend intro based on class and faction? Or just remove this command?
            var user = Context.User;
            var channel = await user.GetOrCreateDMChannelAsync();
            await channel.SendMessageAsync("test");
        }

        [Command("new")]
        public async Task NewPlayer()
        {
            var guildUser = (IGuildUser) Context.User;
            string user = guildUser.Nickname;
            await AddPlayer(Context.User.Id, user, Context.Channel.Id);
        }

        [Command("boost")]
        public async Task Boost()
        {
            var userId = Context.User.Id;
            if (!CharacterCreated(userId))
                return;

            try
            {
                var hour = PlayerList[userId].HoursSinceLastBoost();
                if (hour < int.Parse(Environment.GetEnvironmentVariable("BOOST_HOURS")))
                {
                    await ReplyAsync(
                        $"You still have {(double.Parse(Environment.GetEnvironmentVariable("BOOST_HOURS")) - hour):N} hours until you can boost again.");
                    return;
                }

                PlayerList[userId].Exp += 10;
                if (PlayerList[userId].LevelUp())
                {
                    await Context.Guild.GetTextChannel(ulong.Parse(Environment.GetEnvironmentVariable("CHANNEL_ID")))
                        .SendMessageAsync(
                            $"{Context.User.Mention} has leveled up! They are now Level {PlayerList[userId].Level}");
                }

                PlayerList[userId].ResetBoost();
                UpdateDatabase();
                await ReplyAsync("You've been boosted!");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
        
        [Command("info")]
        public async Task GetPlayerInfo([Remainder] string name = "")
        {
            Player player;
            if (name.Equals(""))
            {
                if (!CharacterCreated(Context.User.Id))
                    return;
                player = PlayerList[Context.User.Id];
            }
            else
            {
                player = FindPlayer(name);
                if (player.Id == 0)
                {
                    await ReplyAsync($"{name} doesn't have a character. Tell 'em to create one!");
                    return;
                }
            }
            
            var embed = new EmbedBuilder();
            
            switch (player.Class)
            {
                case "Captain":
                    embed.Color = Color.Gold;
                    break;
                case "Marksman":
                    embed.Color = Color.DarkRed;
                    break;
                case "Smuggler":
                    embed.Color = Color.Green;
                    break;
            }

            embed.Title = player.Name;
            embed.Description = player.Faction + "\n" + player.Class;
            embed.AddField("Stats:",
                $"Health: {player.Stats.GetHealth()}\nStrength: {player.Stats.GetStrength()}\nDefence: {player.Stats.GetDefence()}");

            await ReplyAsync("", false, embed.Build());
        }
        
        [Command("level")]
        public async Task CheckLevel([Remainder] string name = "")
        {
            Player player;
            if (name.Equals(""))
            {
                if (!CharacterCreated(Context.User.Id))
                    return;
                player = PlayerList[Context.User.Id];
                await ReplyAsync($"You are currently Level {player.Level} with {player.Exp} XP ({(player.Level * 10) - player.Exp} to the next level)");
            }
            else
            {
                player = FindPlayer(name);
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
            if (!CharacterCreated(Context.User.Id))
                return;
            //TODO: Make prettier embed
            try
            {
                var player = PlayerList[Context.User.Id];
                var embed = new EmbedBuilder {Title = player.Name + "'s inventory"};
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
        
        [Command("stats")]
        [Alias("stat")]
        public async Task GetStats()
        {
            if (!CharacterCreated(Context.User.Id))
                return;
            
            var player = PlayerList[Context.User.Id];
            await ReplyAsync($"{player.Name}'s stats:\n``Health`` = {player.Stats.GetHealth() * 10}\n``Strength`` = {player.Stats.GetStrength()}\n``Defence`` = {player.Stats.GetDefence()}");
        }
        
        [Command("reset")]
        public async Task ResetPlayer()
        {
            if (!CharacterCreated(Context.User.Id))
                return;
            
            var message = await ReplyAsync(
                $"{Context.User.Mention} this will reset your character, including all progress. However, you will keep your inventory. \nThere is no going back! Are you sure?");
            _userId = Context.User.Id;
            await message.AddReactionAsync(new Emoji(Y));
            await message.AddReactionAsync(new Emoji(N));
            _resetId = message.Id;
            
            Context.Client.ReactionAdded += ResetConfirmation;
        }
        
        private async Task ResetConfirmation(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel channel,
            SocketReaction reaction)
        {
            if (reaction.MessageId == _resetId)
            {
                if (reaction.UserId == _userId)
                {
                    if (reaction.Emote.Name == Y)
                    {
                        var player = PlayerList[_userId];
                        PlayerList[_userId] = new Player(player.Id, player.Name, player.Faction, player.Class, baseClassMap[player.Class]) {Inventory = player.Inventory};
                        await reaction.Message.Value.DeleteAsync();
                        await ReplyAsync("Your character was successfully reset.");
                        UpdateDatabase();
                        _resetId = 0;
                        _userId = 0;
                        Context.Client.ReactionAdded -= ResetConfirmation;
                    }
                    else if (reaction.Emote.Name == N)
                    {
                        await reaction.Message.Value.DeleteAsync();
                        _resetId = 0;
                        _userId = 0;
                        Context.Client.ReactionAdded -= ResetConfirmation;
                    }
                }
            }
        }

        [Command("delete")]
        public async Task DeletePlayer()
        {
            if (!CharacterCreated(Context.User.Id))
                return;
            
            var message = await ReplyAsync(
                $"{Context.User.Mention} this will delete your character, including all progress and inventory. \nThere is no going back! Are you sure?");
            _userId = Context.User.Id;
            await message.AddReactionAsync(new Emoji(Y));
            await message.AddReactionAsync(new Emoji(N));
            _deleteId = message.Id;
            
            Context.Client.ReactionAdded += DeleteConfirmation;
        }

        private async Task DeleteConfirmation(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel channel,
            SocketReaction reaction)
        {
            if (reaction.MessageId == _deleteId)
            {
                if (reaction.UserId == _userId)
                {
                    if (reaction.Emote.Name == Y)
                    {
                        PlayerList.Remove(_userId);
                        ExecuteSql($"DELETE FROM inventory WHERE PlayerId = {_userId}");
                        ExecuteSql($"DELETE FROM stats WHERE PlayerId = {_userId}");
                        ExecuteSql($"DELETE FROM player WHERE Id = {_userId}");
                        await reaction.Message.Value.DeleteAsync();
                        await ReplyAsync("Your character was successfully deleted.");
                        Context.Client.ReactionAdded -= DeleteConfirmation;
                        _deleteId = 0;
                        _userId = 0;
                    }
                    else if (reaction.Emote.Name == N)
                    {
                        await reaction.Message.Value.DeleteAsync();
                        Context.Client.ReactionAdded -= DeleteConfirmation;
                        _deleteId = 0;
                        _userId = 0;
                    }
                }
            }
        }
        
        private bool CharacterCreated(ulong userId)
        {
            if (PlayerList.ContainsKey(userId))
            {
                return true;
            }
            Console.WriteLine($"You don't have a character. Use \"{Environment.GetEnvironmentVariable("COMMAND_PREFIX")}new\" to make one!");
            return false;
        }
    }

    [Group("")]
    [RequireOwner]
    public class AdminModule : ModuleBase<SocketCommandContext>
    {
        [Group("give")]
        public class GiveModule : ModuleBase<SocketCommandContext>
        {

            [Command("item")]
            public async Task GiveItem(uint itemId, uint amount, [Remainder] string playerName = "")
            {
                ulong playerId = playerName== string.Empty ? Context.User.Id : FindPlayer(playerName).Id;

                if (!PlayerList.ContainsKey(playerId))
                {
                    await ReplyAsync("Character doesn't exist.");
                    return;
                }
                
                if (PlayerList[playerId].Inventory.ContainsKey(itemId))
                {
                    PlayerList[playerId].Inventory[itemId] += amount;
                }
                else
                {
                    PlayerList[playerId].Inventory.Add(itemId, amount);
                }

                await ReplyAsync(
                    $"{PlayerList[playerId].Name} now has {PlayerList[playerId].Inventory[itemId]} {itemMap[itemId]}");
            }

            [Command("exp")]
            [Alias("xp")]
            public async Task GiveExp(uint amount, [Remainder] string playerName = "")
            {
                var playerId = playerName == string.Empty ? Context.User.Id : FindPlayer(playerName).Id;
                
                if (!PlayerList.ContainsKey(playerId))
                {
                    await ReplyAsync("Character doesn't exist.");
                    return;
                }
                
                PlayerList[playerId].Exp += amount;
                PlayerList[playerId].LevelUp();
                await ReplyAsync(
                    $"{PlayerList[playerId].Name} is now {PlayerList[playerId].Level} with {PlayerList[playerId].Exp} xp");
            }

            [Command("level")]
            [Alias("lvl")]
            public async Task GiveLevel(uint amount, [Remainder] string playerName = "")
            {
                var playerId = playerName == string.Empty ? Context.User.Id : FindPlayer(playerName).Id;
                
                if (!PlayerList.ContainsKey(playerId))
                {
                    await ReplyAsync("Character doesn't exist.");
                    return;
                }
                
                PlayerList[playerId].Level += amount;
                await ReplyAsync(
                    $"{PlayerList[playerId].Name} is now Level {PlayerList[playerId].Level}");
            }
        }
    }
}