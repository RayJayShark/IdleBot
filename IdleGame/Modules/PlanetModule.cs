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

        [Command("invite")]
        [Remarks("<player name>")]
        public async Task InvitePlayer([Remainder] string playerName)
        {
            if (!Context.Channel.Name.StartsWith('@'))
            {
                await ReplyAsync("Players must be invited to your party through a DM command.");
                return;
            }

            var userId = Context.User.Id;
            if (!await CharacterCreated(userId))
                return;
            
            var partyPlayer = Program.PlayerList[Context.User.Id];
            if (partyPlayer.GetParty() < 0)
            {
                await ReplyAsync(
                    $"You are not in a party. You can start one with the command \"{Environment.GetEnvironmentVariable("COMMAND_PREFIX")}createparty\"!");
                return;
            }

            var player = Program.FindPlayer(playerName);
            if (player.GetId() == 0)
            {
                await ReplyAsync(
                    $"Could not find player with the name \"{playerName}\". Are you sure that's their player name?");
                return;
            }

            if (player.GetParty() >= 0)
            {
                await ReplyAsync(
                    $"**{player.GetName()}** is already in a party. Tell them to leave their party and invite them again.");
                return;
            }

            _partyList[partyPlayer.GetParty()].AddInvite(player.GetId());
            var channel = await Context.Client.GetUser(player.GetId()).GetOrCreateDMChannelAsync();
            await channel.SendMessageAsync($"**{partyPlayer.GetName()}** has invited you to a party. Would you like to join? Send \"{Environment.GetEnvironmentVariable("COMMAND_PREFIX")}accept\" or \"{Environment.GetEnvironmentVariable("COMMAND_PREFIX")}reject\" to reply.");
        }
        
        [Command("accept")]
        public async Task AcceptInvite()
        {
            var userId = Context.User.Id;
            if (!Context.Channel.Name.StartsWith('@') || !await CharacterCreated(userId))
                return;

            var player = Program.PlayerList[userId];
            if (player.GetParty() >= 0)
            {
                await ReplyAsync(
                    $"You are already in a party. Use \"{Environment.GetEnvironmentVariable("COMMAND_PREFIX")}leave\" to leave your current party.");
                return;
            }

            foreach (var party in _partyList)
            {
                if (party.AcceptInvite(player))
                {
                    foreach (var playerDm in party.GetAllDmChannels())
                    {
                        await playerDm.SendMessageAsync($"**{player.GetName()}** has joined the party!");
                    }

                    return;
                }
            }

            await ReplyAsync("You haven't been invited to a party. Tell a friend to invite you!");
        }

        [Command("reject")]
        public async Task RejectInvite()
        {
            var userId = Context.User.Id;
            if (!Context.Channel.Name.StartsWith('@') || !await CharacterCreated(userId))
                return;

            foreach (var party in _partyList)
            {
                party.RejectInvite(userId);
            }

            await ReplyAsync("All party invites rejected.");
        }

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
            
            await ReplyAsync($"You have successfully left the party.");
            foreach (var dmChannel in _partyList[partyIndex].GetAllDmChannels())
            {
                await dmChannel.SendMessageAsync($"**{partyPlayer.GetName()}** has left the party.");
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