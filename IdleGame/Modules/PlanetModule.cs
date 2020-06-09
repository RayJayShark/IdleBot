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

        private static readonly List<Party> _partyList = new List<Party>();
        
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

            var player = Program.PlayerList[userId];
            if (player.GetParty() >= 0)
            {
                await ReplyAsync("You are already in a party.");
                return;
            }
            
            _partyList.Add(new Party(_partyList.Count, player, Context.Client));

            await _partyList.Last().GetDmChannel(userId).SendMessageAsync("You created a party! Just tell me who to invite.");
        }
        }
        
        
        
        
        //TODO: Invite to party
        
        
        
        //TODO: Join party
        
        
        



        [Command("leaveparty")]
        [Alias("leave")]
        public async Task LeaveParty()
        {
            var userId = Context.User.Id;
            if (!await CharacterCreated(userId))
                return;
            
            var partyPlayer = Program.PlayerList[userId];
            var partyIndex = partyPlayer.GetParty();
            if (partyIndex < 0)
            {
                await ReplyAsync(
                    $"You are not in a party. You can start one with the command \"{Environment.GetEnvironmentVariable("COMMAND_PREFIX")}createparty\"!");
                return;
            }
            
            _partyList[partyIndex].RemovePlayer(partyPlayer);
            if (_partyList[partyIndex].Count() == 0)
            {
                _partyList.RemoveAt(partyIndex);
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
}