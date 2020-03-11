using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using IdleGame.Classes;
using IdleGame.Services;

namespace IdleGame.Modules
{
    [Name("Game Commands")]
    public class GameModule : ModuleBase<SocketCommandContext>
    {
        protected readonly SqlService _sqlService;
        
        protected const string Y = "\uD83C\uDDFE";
        protected const string N = "\uD83C\uDDF3";
        private const string Check = "✅";
        private const string X = "❎";
        protected ulong UserId;
        protected ulong DeleteId;
        private ulong _resetId;
        private IUserMessage _attackMessage;
        private int _attackIndex;

        public GameModule(SqlService sqlService = null)
        {
            _sqlService = sqlService;
        }

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
            var user = guildUser.Nickname ?? Context.User.Username;
            await _sqlService.AddPlayer(Context.User.Id, user, Context.Channel.Id);
        }

        [Command("boost")]
        public async Task Boost()
        {
            var userId = Context.User.Id;
            if (!await CharacterCreated(userId))
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

                Program.PlayerList[userId].GiveExp(10);

                Program.PlayerList[userId].ResetBoost();
                _sqlService.UpdateDatabase();
                await ReplyAsync("You've been boosted!");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
        
        [Command("info")]
        [Alias("stats", "stat", "level", "lvl")]
        [Remarks("[player]")]
        public async Task GetPlayerInfo([Remainder] string name = "")
        {
            Player player;
            if (name.Equals(""))
            {
                if (!await CharacterCreated(Context.User.Id))
                    return;
                player = Program.PlayerList[Context.User.Id];
            }
            else
            {
                player = Program.FindPlayer(name);
                if (player.GetId() == 0)
                {
                    await ReplyAsync($"{name} doesn't have a character. Tell 'em to create one!");
                    return;
                }
            }
            
            var embed = new EmbedBuilder();
            
            switch (player.GetClass())
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

            embed.Title = $"{player.GetName()} - {player.GetFaction()} {player.GetClass()}";
            embed.Description = $"Level {player.GetLevel()}\nExp: {player.GetExp()}/{player.GetLevel() * 10}\nMoney: {player.GetMoney()}";
            embed.AddField("Stats:",
                $"Health: {player.GetCurrentHp()}/{player.Stats.GetHealth()}\nStrength: {player.Stats.GetStrength()}\nDefence: {player.Stats.GetDefence()}");

            await ReplyAsync("", false, embed.Build());
        }

        [Command("inventory")]
        [Alias("inv")]
        public async Task CheckInventory()
        {
            if (!await CharacterCreated(Context.User.Id))
                return;
            try
            {
                var player = Program.PlayerList[Context.User.Id];
                var embed = new EmbedBuilder {Title = player.GetName() + "'s inventory"};
                foreach (var i in player.Inventory)
                {
                    embed.Description +=  Program.ItemMap[i.Key].Name + " " + i.Value + "\n";
                }

                await ReplyAsync("", false, embed.Build());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        [Command("use")]
        [Alias("eat")]
        [Remarks("<item> [amount]")]
        public async Task UseItem(string itemName, uint amount = 1)
        {
            if (!await CharacterCreated(Context.User.Id))
                return;

            if (string.IsNullOrEmpty(itemName))
            {
                await ReplyAsync($"Command for using items in your inventory.\nUsage: *{Environment.GetEnvironmentVariable("COMMAND_PREFIX")}use <item>*");
                return;
            }

            var itemId = Program.FindItemId(itemName);
            if (itemId == 0)
            {
                await ReplyAsync($"\"{itemName}\" isn't an item.");
                return;
            }

            var player = Program.PlayerList[Context.User.Id];
            if (!player.Inventory.ContainsKey(itemId) || player.Inventory[itemId] < amount)
            {
                await ReplyAsync($"You don't have {amount} {itemName}(s).");
                return;
            }

            var hpToGive = Program.ItemMap[itemId].Value * amount;
            player.GiveHp(hpToGive);
            player.TakeItem(itemId, amount);

            await ReplyAsync($"You ate {amount} {itemName}(s) and gained {hpToGive} HP!");
            _sqlService.UpdateDatabase();
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
                embed.AddField($"{i}. {e.GetName()} - Lvl {e.GetLevel()}", e.GetStats());
                if (i % 4 == 0)
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
        [Remarks("<enemyId>")]
        public async Task AttackEnemy(uint enemyId)
        {
            //TODO: Make a better algorithm
            if (!await CharacterCreated(Context.User.Id))
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


            var embed = new EmbedBuilder
            {
                Title = $"{player.GetName()} vs {enemy.GetName()}",
                Description = $"**{enemy.GetName()}** - HP:{enemy.GetHp()}/{enemy.GetMaxHp()} - {HealthBar(enemy.GetHp(), enemy.GetMaxHp())}\n" +
                              $"STR {enemy.GetStrength()}\nDEF {enemy.GetDefence()}\n\nSTR {player.Stats.GetStrength()}\nDEF {player.Stats.GetDefence()}\n" +
                              $"**{player.GetName()}** - HP:{player.GetCurrentHp()}/{player.Stats.GetHealth()} - {HealthBar(player.GetCurrentHp(), player.Stats.GetHealth())}",
                Color = Color.DarkRed
            };
            var message = await ReplyAsync("", false, embed.Build());

            await message.AddReactionAsync(new Emoji(Check));
            await message.AddReactionAsync(new Emoji(X));
            UserId = Context.User.Id;
            _attackMessage = message;
            _attackIndex = index;
            Context.Client.ReactionAdded += AttackAgain;
        }

        private async Task AttackAgain(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel channel,
            SocketReaction reaction)
        {
            if (reaction.MessageId != _attackMessage.Id || reaction.User.Value.IsBot)
                return;

            if (reaction.UserId != UserId || (!reaction.Emote.Name.Equals(Check) && !reaction.Emote.Name.Equals(X)))
            {
                await reaction.Message.Value.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
            }

            if (reaction.Emote.Name.Equals(X))
            {
                await reaction.Message.Value.DeleteAsync();
                UserId = 0;
                _attackIndex = 0;
                Context.Client.ReactionAdded -= AttackAgain;
            }
            else
            {
                var player = Program.PlayerList[UserId];
                if (player.GetCurrentHp() == 0)
                {
                    await reaction.Message.Value.DeleteAsync();
                    await ReplyAsync("You have no health! Try eating a taco or waiting a bit.");
                    Context.Client.ReactionAdded -= AttackAgain;
                    return;
                }
                var enemy = Program.Enemies[_attackIndex];

                var damageToEnemy = player.Stats.GetStrength() <= enemy.GetDefence() 
                    ? 1 
                    : player.Stats.GetStrength() - enemy.GetDefence();
                if (enemy.TakeDamage(player.GetId(), damageToEnemy))
                {
                    enemy.DistributeExp();
                    await reaction.Message.Value.DeleteAsync();
                    var endingEmbed = new EmbedBuilder();
                    endingEmbed.Title = $"{player.GetName()} killed {enemy.GetName()}!";
                    var expDist = "";
                    foreach (var (id, damage) in enemy.GetAttackLog())
                    {
                        if (!Program.PlayerList.ContainsKey(id))
                            continue;
                        expDist +=
                            $"**{Program.PlayerList[id].GetName()}**: {damage} Damage dealt, {(uint) (((double) damage / enemy.GetMaxHp()) * (enemy.GetLevel() * 10))} Exp gained\n";
                    }

                    endingEmbed.AddField("Exp Distribution:", expDist);
                    var rewards = enemy.Rewards();
                    if (rewards.Item1)
                    {
                        player.GiveMoney(rewards.Item2);
                        player.GiveItem("taco");
                        endingEmbed.Footer = new EmbedFooterBuilder {Text = $"{player.GetName()} also got {rewards.Item2} money and a taco!"};
                    }
                    else
                    {
                        player.GiveMoney(rewards.Item2);
                        endingEmbed.Footer = new EmbedFooterBuilder {Text = $"{player.GetName()} also got {rewards.Item2} money!"};
                    }
                    await ReplyAsync("", false, endingEmbed.Build());
                    Program.Enemies.RemoveAt(_attackIndex);
                    _sqlService.UpdateDatabase();
                    Context.Client.ReactionAdded -= AttackAgain;
                    return;
                }
                
                var damageToPlayer = enemy.GetStrength() <= player.Stats.GetDefence()
                    ? 1
                    : enemy.GetStrength() - player.Stats.GetDefence();
                player.TakeDamage(damageToPlayer);
                var embed = new EmbedBuilder
                {
                    Title = $"{player.GetName()} vs {enemy.GetName()}",
                    Description = $"**{enemy.GetName()}** - HP:{enemy.GetHp()}/{enemy.GetMaxHp()} {HealthBar(enemy.GetHp(), enemy.GetMaxHp())}\n" +
                                  $"STR {enemy.GetStrength()}\nDEF {enemy.GetDefence()}\n\nSTR {player.Stats.GetStrength()}\nDEF {player.Stats.GetDefence()}\n" +
                                  $"**{player.GetName()}** - HP:{player.GetCurrentHp()}/{player.Stats.GetHealth()} {HealthBar(player.GetCurrentHp(), player.Stats.GetHealth())}",
                    Color = Color.DarkRed
                };
                await reaction.Message.Value.ModifyAsync(m => m.Embed = embed.Build());
                await reaction.Message.Value.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
            }
        }

        private static string HealthBar(uint curHp, uint maxHp)
        {
            var percent = (int) Math.Round(((double) curHp / maxHp) * 10, MidpointRounding.AwayFromZero);
            var bar = "[" + new string('❤', percent) + new string('❌', 10 - percent) + "]";
            return bar;
        }

        [Command("reset")]
        public async Task ResetPlayer()
        {
            if (!await CharacterCreated(Context.User.Id))
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
                        switch (player.GetClass())
                        {
                            case "Captain":
                                Program.PlayerList[UserId] = new Captain(player.GetId(), player.GetName(), player.GetFaction()) {Inventory = player.Inventory};
                                break;
                            case "Marksman":
                                Program.PlayerList[UserId] = new Marksman(player.GetId(), player.GetName(), player.GetFaction()) {Inventory = player.Inventory};
                                break;
                            case "Smuggler":
                                Program.PlayerList[UserId] = new Smuggler(player.GetId(), player.GetName(), player.GetFaction()) {Inventory = player.Inventory};
                                break;
                        }
                        await reaction.Message.Value.DeleteAsync();
                        await ReplyAsync("Your character was successfully reset.");
                        _sqlService.UpdateDatabase();
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
            if (!await CharacterCreated(Context.User.Id))
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
                        await _sqlService.ExecuteSql($"DELETE FROM inventory WHERE PlayerId = {UserId}");
                        await _sqlService.ExecuteSql($"DELETE FROM stats WHERE PlayerId = {UserId}");
                        await _sqlService.ExecuteSql($"DELETE FROM player WHERE Id = {UserId}");
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
        
        private async Task<bool> CharacterCreated(ulong userId)
        {
            if (Program.PlayerList.ContainsKey(userId))
            {
                return true;
            }
            await ReplyAsync($"You don't have a character. Use \"{Environment.GetEnvironmentVariable("COMMAND_PREFIX")}new\" to make one!");
            return false;
        }
    }

    [Name("Admin Commands")]
    [Group("")]
    [Remarks("admin")]
    [RequireOwner]
    public class AdminModule : GameModule
    {
        [Name("Admin Give Commands")]
        [Group("give")]
        public class GiveModule : AdminModule
        {

            [Command("item")]
            [Remarks("<itemId> <amount> [player]")]
            public async Task GiveItem(uint itemId, uint amount, [Remainder] string playerName = "")
            {
                ulong playerId = playerName== string.Empty ? Context.User.Id : Program.FindPlayer(playerName).GetId();

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
                    $"{Program.PlayerList[playerId].GetName()} now has {Program.PlayerList[playerId].Inventory[itemId]} {Program.ItemMap[itemId]}");
            }

            [Command("exp")]
            [Alias("xp")]
            [Remarks("<amount> [player]")]
            public async Task GiveExp(uint amount, [Remainder] string playerName = "")
            {
                var playerId = playerName == string.Empty ? Context.User.Id : Program.FindPlayer(playerName).GetId();
                
                if (!Program.PlayerList.ContainsKey(playerId))
                {
                    await ReplyAsync("Character doesn't exist.");
                    return;
                }
                
                Program.PlayerList[playerId].GiveExp(amount);
                await ReplyAsync(
                    $"{Program.PlayerList[playerId].GetName()} is now {Program.PlayerList[playerId].GetLevel()} with {Program.PlayerList[playerId].GetExp()} xp");
            }
        }

        [Command("delete")]
        [Remarks("<player>")]
        public async Task DeleteCharacter([Remainder] string playerName)
        {
            var player = Program.FindPlayer(playerName);
            if (player.GetId() == 0)
            {
                await ReplyAsync("They no have character");
                return;
            }
            
            var message = await ReplyAsync(
                $"{Context.User.Mention} this will delete {playerName}'s character, including all progress and inventory. \nThere is no going back! Are you sure?");
            UserId = player.GetId();
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
                        await _sqlService.ExecuteSql($"DELETE FROM inventory WHERE PlayerId = {UserId}");
                        await _sqlService.ExecuteSql($"DELETE FROM stats WHERE PlayerId = {UserId}");
                        await _sqlService.ExecuteSql($"DELETE FROM player WHERE Id = {UserId}");
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