using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using IdleGame.Classes;

namespace IdleGame.Modules
{
    public class GameModule : ModuleBase<SocketCommandContext>
    {
        protected const string Y = "\uD83C\uDDFE";
        protected const string N = "\uD83C\uDDF3";
        protected ulong UserId;
        protected ulong DeleteId;
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
            string user = guildUser.Nickname == string.Empty ? Context.User.Username : guildUser.Nickname;
            await Program.AddPlayer(Context.User.Id, user, Context.Channel.Id);
        }

        [Command("boost")]
        public async Task Boost()
        {
            var userId = Context.User.Id;
            if (!CharacterCreated(userId))
                return;

            try
            {
                var hour = Program.PlayerList[userId].HoursSinceLastBoost();
                if (hour < int.Parse(Environment.GetEnvironmentVariable("BOOST_HOURS")))
                {
                    await ReplyAsync(
                        $"You still have {(double.Parse(Environment.GetEnvironmentVariable("BOOST_HOURS")) - hour):N} hours until you can boost again.");
                    return;
                }
                
                if (Program.PlayerList[userId].GiveExp(10))
                {
                    await Context.Guild.GetTextChannel(ulong.Parse(Environment.GetEnvironmentVariable("CHANNEL_ID")))
                        .SendMessageAsync(
                            $"{Context.User.Mention} has leveled up! They are now Level {Program.PlayerList[userId].Level}");
                }

                Program.PlayerList[userId].ResetBoost();
                Program.UpdateDatabase();
                await ReplyAsync("You've been boosted!");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
        
        [Command("info")]
        [Alias("stats", "stat")]
        public async Task GetPlayerInfo([Remainder] string name = "")
        {
            Player player;
            if (name.Equals(""))
            {
                if (!CharacterCreated(Context.User.Id))
                    return;
                player = Program.PlayerList[Context.User.Id];
            }
            else
            {
                player = Program.FindPlayer(name);
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
                $"Health: {player.GetCurrentHp()}/{player.Stats.GetHealth()}\nStrength: {player.Stats.GetStrength()}\nDefence: {player.Stats.GetDefence()}");

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
                player = Program.PlayerList[Context.User.Id];
                await ReplyAsync($"You are currently Level {player.Level} with {player.GetExp()} XP ({(player.Level * 10) - player.GetExp()} to the next level)");
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
                    await ReplyAsync($"{player.Name} is currently Level {player.Level} with {player.GetExp()} XP ({(player.Level * 10) - player.GetExp()} to the next level)");
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
                var player = Program.PlayerList[Context.User.Id];
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

        [Command("listenemies")]
        [Alias("lsenemies", "listen", "lsen", "le")]
        public async Task ListEnemies()
        {
            var embed = new EmbedBuilder();
            var page = new Paginator();
            page.Color = Color.DarkRed;
            page.Title = "Current Enemies";
            int i = 1;
            foreach (var e in Program.Enemies)
            {
                embed.AddField(i + ". " +e.GetName(), e.GetStats());
                if (i % 3 == 0)
                {
                    page.AddPage(embed);
                    embed = new EmbedBuilder();
                }
                i++;
            }

            if (embed.Fields.Count > 0)
            {
                page.AddPage(embed);
            }

            page.SendMessage(Context);
        }

        [Command("attack")]
        public async Task AttackEnemy(uint enemyId)
        {
            //TODO: Can't attack with no health
            //TODO: Add replies and updates
            //TODO: Make a better algorithm
            if (!CharacterCreated(Context.User.Id))
                return;

            if (enemyId > Program.Enemies.Count || enemyId == 0)
            {
                await ReplyAsync("Index out of bounds");
                return;
            }

            var index = (int)enemyId - 1;
            var player = Program.PlayerList[Context.User.Id];
            if (player.GetCurrentHp() == 0)
            {
                await ReplyAsync("You have no health! Try eating a taco or waiting a bit.");
                return;
            }
            var enemy = Program.Enemies[index];
            if (player.Stats.GetStrength() <= enemy.GetDefence())
            {
                if (enemy.TakeDamage(1))
                {
                    player.GiveExp(enemy.GetLevel() * 10);
                    Program.Enemies.RemoveAt(index);
                    return;
                }
            }
            else
            {
                if (enemy.TakeDamage(player.Stats.GetStrength() - enemy.GetDefence()))
                {
                    player.GiveExp(enemy.GetLevel() * 10);
                    Program.Enemies.RemoveAt(index);
                    return;
                }
            }
            
            if (enemy.GetStrength() <= player.Stats.GetDefence())
            {
                player.TakeDamage(1);
            }
            else
            {
                player.TakeDamage(enemy.GetStrength() - player.Stats.GetDefence());
            }
        }
        
        [Command("reset")]
        public async Task ResetPlayer()
        {
            if (!CharacterCreated(Context.User.Id))
                return;
            
            var message = await ReplyAsync(
                $"{Context.User.Mention} this will reset your character, including all progress. However, you will keep your inventory. \nThere is no going back! Are you sure?");
            UserId = Context.User.Id;
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
                if (reaction.UserId == UserId)
                {
                    if (reaction.Emote.Name == Y)
                    {
                        var player = Program.PlayerList[UserId];
                        switch (player.Class)
                        {
                            case "Captain":
                                Program.PlayerList[UserId] = new Captain(player.Id, player.Name, player.Faction) {Inventory = player.Inventory};
                                break;
                            case "Marksman":
                                Program.PlayerList[UserId] = new Marksman(player.Id, player.Name, player.Faction) {Inventory = player.Inventory};
                                break;
                            case "Smuggler":
                                Program.PlayerList[UserId] = new Smuggler(player.Id, player.Name, player.Faction) {Inventory = player.Inventory};
                                break;
                        }
                        await reaction.Message.Value.DeleteAsync();
                        await ReplyAsync("Your character was successfully reset.");
                        Program.UpdateDatabase();
                        _resetId = 0;
                        UserId = 0;
                        Context.Client.ReactionAdded -= ResetConfirmation;
                    }
                    else if (reaction.Emote.Name == N)
                    {
                        await reaction.Message.Value.DeleteAsync();
                        _resetId = 0;
                        UserId = 0;
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
                        Program.ExecuteSql($"DELETE FROM inventory WHERE PlayerId = {UserId}");
                        Program.ExecuteSql($"DELETE FROM stats WHERE PlayerId = {UserId}");
                        Program.ExecuteSql($"DELETE FROM player WHERE Id = {UserId}");
                        await reaction.Message.Value.DeleteAsync();
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
        
        private bool CharacterCreated(ulong userId)
        {
            if (Program.PlayerList.ContainsKey(userId))
            {
                return true;
            }
            Console.WriteLine($"You don't have a character. Use \"{Environment.GetEnvironmentVariable("COMMAND_PREFIX")}new\" to make one!");
            return false;
        }
    }

    [Group("")]
    [RequireOwner]
    public class AdminModule : GameModule
    {
        [Group("give")]
        public class GiveModule : ModuleBase<SocketCommandContext>
        {

            [Command("item")]
            public async Task GiveItem(uint itemId, uint amount, [Remainder] string playerName = "")
            {
                ulong playerId = playerName== string.Empty ? Context.User.Id : Program.FindPlayer(playerName).Id;

                if (!Program.PlayerList.ContainsKey(playerId))
                {
                    await ReplyAsync("Character doesn't exist.");
                    return;
                }
                
                if (Program.PlayerList[playerId].Inventory.ContainsKey(itemId))
                {
                    Program.PlayerList[playerId].Inventory[itemId] += amount;
                }
                else
                {
                    Program.PlayerList[playerId].Inventory.Add(itemId, amount);
                }

                await ReplyAsync(
                    $"{Program.PlayerList[playerId].Name} now has {Program.PlayerList[playerId].Inventory[itemId]} {Program.itemMap[itemId]}");
            }

            [Command("exp")]
            [Alias("xp")]
            //TODO: Use levelup command instead
            public async Task GiveExp(uint amount, [Remainder] string playerName = "")
            {
                var playerId = playerName == string.Empty ? Context.User.Id : Program.FindPlayer(playerName).Id;
                
                if (!Program.PlayerList.ContainsKey(playerId))
                {
                    await ReplyAsync("Character doesn't exist.");
                    return;
                }
                
                Program.PlayerList[playerId].GiveExp(amount);
                await ReplyAsync(
                    $"{Program.PlayerList[playerId].Name} is now {Program.PlayerList[playerId].Level} with {Program.PlayerList[playerId].GetExp()} xp");
            }

            [Command("level")]
            [Alias("lvl")]
            public async Task GiveLevel(uint amount, [Remainder] string playerName = "")
            {
                var playerId = playerName == string.Empty ? Context.User.Id : Program.FindPlayer(playerName).Id;
                
                if (!Program.PlayerList.ContainsKey(playerId))
                {
                    await ReplyAsync("Character doesn't exist.");
                    return;
                }
                
                Program.PlayerList[playerId].Level += amount;
                await ReplyAsync(
                    $"{Program.PlayerList[playerId].Name} is now Level {Program.PlayerList[playerId].Level}");
            }
        }

        [Command("delete")]
        public async Task DeleteCharacter([Remainder] string playerName)
        {
            var player = Program.FindPlayer(playerName);
            if (player.Id == 0)
            {
                await ReplyAsync("They no have character");
                return;
            }
            
            var message = await ReplyAsync(
                $"{Context.User.Mention} this will delete {playerName}'s character, including all progress and inventory. \nThere is no going back! Are you sure?");
            UserId = player.Id;
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
                if (reaction.UserId == Context.User.Id)
                {
                    if (reaction.Emote.Name == Y)
                    {
                        Program.PlayerList.Remove(UserId);
                        Program.ExecuteSql($"DELETE FROM inventory WHERE PlayerId = {UserId}");
                        Program.ExecuteSql($"DELETE FROM stats WHERE PlayerId = {UserId}");
                        Program.ExecuteSql($"DELETE FROM player WHERE Id = {UserId}");
                        await reaction.Message.Value.DeleteAsync();
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
}