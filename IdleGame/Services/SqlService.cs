using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using dotenv.net;
using IdleGame.Classes;
using MySql.Data.MySqlClient;

namespace IdleGame.Services
{
    public class SqlService
    {
       
        private MySqlConnection _conn;

        private const string One = "1⃣";
        private const string Two = "2⃣";
        private const string Three = "3⃣";
        private ulong _newPlayerId;
        private string _newPlayerName;
        private static string _newPlayerFaction;
        private static string _newPlayerClass;
        private ulong _channelId;
        private RestUserMessage _message;

        private LogService _logService { get; set; }
        
        
        public SqlService(string connStr)
        {
            _conn = new MySqlConnection(connStr);
            TestMySqlConnection();
        }
        
        public void TestMySqlConnection()
        {
            try
            {
                LogService.DatabaseLog("Testing MySQL...");
                _conn.Open();
                _conn.Close();
                LogService.DatabaseLog("Test Complete!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("SQL test failed. Please ensure your MySQL server is running and the credentials are correct in the .env file.");
                Console.WriteLine(ex.ToString());
                Environment.Exit(1);
            }
        }
        
        public async Task ExecuteSql(string sql)
        {
            _conn.Execute(sql);
        }

        public Dictionary<ulong, Player> QueryPlayers()
        {
            Dictionary<ulong, Player> tempPlayerList = new Dictionary<ulong, Player>();
            // Query Captains
            var cPlayers = _conn.Query<Captain>("SELECT * FROM player WHERE Class = 'Captain'");
            foreach (var p in cPlayers)
            {
                var stats = _conn.QuerySingle<PlayerStats>($"SELECT Health, Strength, Defence FROM stats WHERE PlayerId = {p.GetId()}");
                p.Stats = stats;
                var inv = _conn.Query<InventoryQuery>($"SELECT ItemId, Quantity FROM inventory WHERE PlayerId = {p.GetId()}");
                if (inv.Count() != 0)
                {
                    foreach (var i in inv)
                    {
                        if (i.Quantity > 0)
                        {
                            p.Inventory.Add(i.ItemId, i.Quantity);
                        }
                    }
                }

                tempPlayerList.Add(p.GetId(), p);
            }
            
            // Query Marksmen
            var mPlayers = _conn.Query<Marksman>("SELECT * FROM player WHERE Class = 'Marksman'");
            foreach (var p in mPlayers)
            {
                var stats = _conn.QuerySingle<PlayerStats>($"SELECT Health, Strength, Defence FROM stats WHERE PlayerId = {p.GetId()}");
                p.Stats = stats;
                var inv = _conn.Query<InventoryQuery>($"SELECT ItemId, Quantity FROM inventory WHERE PlayerId = {p.GetId()}");
                if (inv.Count() != 0)
                {
                    foreach (var i in inv)
                    {
                        if (i.Quantity > 0)
                        {
                            p.Inventory.Add(i.ItemId, i.Quantity);
                        }
                    }
                }

                tempPlayerList.Add(p.GetId(), p);
            }
            
            // Query Smuggler
            var sPlayers = _conn.Query<Smuggler>("SELECT * FROM player WHERE Class = 'Smuggler'");
            foreach (var p in sPlayers)
            {
                var stats = _conn.QuerySingle<PlayerStats>($"SELECT Health, Strength, Defence FROM stats WHERE PlayerId = {p.GetId()}");
                p.Stats = stats;
                var inv = _conn.Query<InventoryQuery>($"SELECT ItemId, Quantity FROM inventory WHERE PlayerId = {p.GetId()}");
                if (inv.Count() != 0)
                {
                    foreach (var i in inv)
                    {
                        if (i.Quantity > 0)
                        {
                            p.Inventory.Add(i.ItemId, i.Quantity);
                        }
                    }
                }

                tempPlayerList.Add(p.GetId(), p);
            }
            
            return tempPlayerList;
        }

        public Dictionary<uint, ItemQuery> QueryItems()
        {
            var itemQuery = _conn.Query<ItemQuery>("SELECT * FROM item");
            return itemQuery.ToDictionary(i => i.Id);
        }
        
        private static void CleanInventories()        // Clear inventory items with zeros
        {
            foreach (var p in Program.PlayerList)
            {
                foreach (var (id, amount) in p.Value.Inventory)
                {
                    if (amount == 0)
                    {
                        Program.PlayerList[p.Key].Inventory.Remove((amount));
                    }
                }
            }
        }
        
        public void UpdateDatabase()
        {
            CleanInventories();
            foreach (var p in Program.PlayerList)
            {
                _conn.Execute($"UPDATE player SET CurHp = {p.Value.GetCurrentHp()}, Money = {p.Value.GetMoney()}, Level = {p.Value.GetLevel()}, Exp = {p.Value.GetExp()}, Boost = '{p.Value.GetBoost().ToDateTime():yyyy-MM-dd HH:mm:ss}' WHERE Id = {p.Key}");
                _conn.Execute($"UPDATE stats SET Health = {p.Value.Stats.GetHealth()}, Strength = {p.Value.Stats.GetStrength()}, Defence = {p.Value.Stats.GetDefence()} WHERE PlayerId = {p.Value.GetId()}");
                
                foreach (var i in p.Value.Inventory)
                {
                    _conn.Execute($"UPDATE inventory SET Quantity = {i.Value} WHERE PlayerId = {p.Key} AND ItemId = {i.Key}");
                }
            }
            
            _conn.Execute("DELETE FROM inventory WHERE Quantity = 0");
            LogService.DatabaseLog("Database updated");
        }

        public async Task AddPlayer(ulong id, string name, ulong channelId)
        {
            if (Program.PlayerList.ContainsKey(id))
            {
                await Program.GetGuild(ulong.Parse(Environment.GetEnvironmentVariable("GUILD_ID")))
                    .GetTextChannel(ulong.Parse(Environment.GetEnvironmentVariable("CHANNEL_ID")))
                    .SendMessageAsync("You already have a character!");
                return;
            }

            _newPlayerId = id;
            _newPlayerName = name;
            _channelId = channelId;

            var embed = new EmbedBuilder {Color = Color.Blue, Title = _newPlayerName + ": Choose Your Faction"};
            embed.AddField("1. Human", "Humans are dumb. Only plebs choose to be a human.");
            embed.AddField("2. Klingon", "The boys be crazy. Total warrior tribe vibes. Like to kill, love to die.");
            embed.AddField("3. Vulcan", "Everything is logic with these guys. They also have pointy ass ears. Nerds.");

            _message = await Program.GetGuild(ulong.Parse(Environment.GetEnvironmentVariable("GUILD_ID")))
                .GetTextChannel(_channelId)
                .SendMessageAsync("", false, embed.Build());
            await _message.AddReactionAsync(new Emoji(One));
            await _message.AddReactionAsync(new Emoji(Two));
            await _message.AddReactionAsync(new Emoji(Three));

            Program.AddReactionEvent(ChooseFaction);
        }

        private async Task ChooseFaction(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel channel,
            SocketReaction reaction)
        {
            if (reaction.MessageId != _message.Id || reaction.UserId != _newPlayerId)
                return;

            Program.RemoveReactionEvent(ChooseFaction);

            switch (reaction.Emote.Name)
            {
                case One:
                    _newPlayerFaction = "Human";
                    break;
                case Two:
                    _newPlayerFaction = "Klingon";
                    break;
                case Three:
                    _newPlayerFaction = "Vulcan";
                    break;
                default:
                    Program.AddReactionEvent(ChooseFaction);
                    break;
            }

            await _message.DeleteAsync();
            var embed = new EmbedBuilder {Color = Color.Blue, Title = _newPlayerName + ": Choose Your Class"};
            embed.AddField("1. Captain", "70 Health\n7 Strength\n10 Defence");
            embed.AddField("2. Marksman", "70 Health\n10 Strength\n7 Defence");
            embed.AddField("3. Smuggler", "100 Health\n7 Strength\n7 Defence");

            _message = await Program.GetGuild(ulong.Parse(Environment.GetEnvironmentVariable("GUILD_ID")))
                .GetTextChannel(_channelId)
                .SendMessageAsync("", false, embed.Build());
            await _message.AddReactionAsync(new Emoji(One));
            await _message.AddReactionAsync(new Emoji(Two));
            await _message.AddReactionAsync(new Emoji(Three));
            
            Program.AddReactionEvent(ChooseClass);
        }

        private async Task ChooseClass(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel channel,
            SocketReaction reaction)
        {
            if (reaction.MessageId != _message.Id || reaction.UserId != _newPlayerId)
                return;

            Program.RemoveReactionEvent(ChooseClass);
            PlayerStats stats;
            switch (reaction.Emote.Name)
            {
                case One:
                    _newPlayerClass = "Captain";
                    Program.PlayerList.Add(_newPlayerId, new Captain(_newPlayerId, _newPlayerName, _newPlayerFaction));
                    break;
                case Two:
                    _newPlayerClass = "Marksman";
                    Program.PlayerList.Add(_newPlayerId, new Marksman(_newPlayerId, _newPlayerName, _newPlayerFaction));
                    break;
                case Three:
                    _newPlayerClass = "Smuggler";
                    Program.PlayerList.Add(_newPlayerId, new Smuggler(_newPlayerId, _newPlayerName, _newPlayerFaction));
                    break;
                default:
                    Program.AddReactionEvent(ChooseClass);
                    return;
            }

            try
            {
                _conn.Execute($"INSERT INTO player (Id,Name,Faction,Class,CurHp) VALUES({_newPlayerId}, '{_newPlayerName}', '{_newPlayerFaction}', '{_newPlayerClass}', {Program.PlayerList[_newPlayerId].Stats.GetHealth()})");
                _conn.Execute($"INSERT INTO inventory VALUES({_newPlayerId}, 1, 10)");
                _conn.Execute($"INSERT INTO stats VALUES({_newPlayerId}, {Program.PlayerList[_newPlayerId].Stats.GetHealth()}, {Program.PlayerList[_newPlayerId].Stats.GetStrength()}, {Program.PlayerList[_newPlayerId].Stats.GetDefence()})");
                Program.PlayerList[_newPlayerId].Inventory.Add(1, 10);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            await _message.Channel.SendMessageAsync($"{_newPlayerName}, the {_newPlayerFaction} {_newPlayerClass}, has started their journey!");
            //TODO: Send DM introduction
            await _message.DeleteAsync();

            _newPlayerId = 0;
            _channelId = 0;
            _newPlayerClass = "";
            _newPlayerFaction = "";
            _newPlayerName = "";
        }
    }
    
    public struct ItemQuery
    {
        public uint Id;
        public string Name;
        public uint Value;
    }
    
    public struct InventoryQuery
    {
        public uint ItemId;
        public uint Quantity;
    }
}