using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using IdleGame.Classes;
using IdleGame.Services;
using IdleGame.Modules;

namespace IdleGame.Modules
{
    [Name("Planet and Party Commands")]
    public class PlanetModule : ModuleBase<SocketCommandContext>
    {
        private readonly SqlService _sqlService;

        private static readonly List<List<IDMChannel>> _dmList = new List<List<IDMChannel>>();
        
        public PlanetModule(SqlService sqlService = null)
        {
            _sqlService = sqlService;
        }

        [Command("createparty")]
        [Alias("party")]
        public async Task CreateParty()
        {
            var userId = Context.User.Id;
            if (!await CharacterCreated(userId))
                return;
            
            Program.PlayerList[userId].SetParty(_dmList.Count);

            _dmList.Add(new List<IDMChannel> {await Context.User.GetOrCreateDMChannelAsync()});

            await _dmList.Last()[0].SendMessageAsync("You create a party! Just tell me who to invite.");
        }
        
        
        
        
        //TODO: Invite to party
        
        
        
        //TODO: Join party
        
        
        
        //TODO: Leave party
        
        
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
}