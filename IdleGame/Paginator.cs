using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace IdleGame
{
    public class Paginator
    {
        private List<EmbedBuilder> _pages = new List<EmbedBuilder>();
        private List<Embed> _builtPages = new List<Embed>();
        private static string _forward = "➡";
        private static string _backward = "⬅";
        private int _currentPage;
        private ulong _messageId;
        public string Title;
        public Color Color = Color.Default;

        public Paginator()
        {
            
        }

        public Paginator(EmbedBuilder embedBuilder)
        {
            // Auto paginate
        }

        public void AddPage(EmbedBuilder embedBuilder)
        {
            if (Title != string.Empty)
            {
                embedBuilder.Title = Title;
            }

            embedBuilder.Color = Color;
            _pages.Add(embedBuilder);
        }

        public void SendMessage(SocketCommandContext context)
        {
            for (int i = 0; i < _pages.Count; i++)
            {
                _pages[i].Title += $" ({i + 1}/{_pages.Count})";
                _builtPages.Add(_pages[i].Build());
            }

            var message = context.Channel.SendMessageAsync("", false, _builtPages[0]).Result;
            message.AddReactionAsync(new Emoji(_forward));
            _currentPage = 1;
            _messageId = message.Id;

            context.Client.ReactionAdded += Paginate;
        }

        private async Task Paginate(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel channel,
            SocketReaction reaction)
        {
            if (reaction.MessageId != _messageId)
                return;
            
            if (reaction.User.Value.IsBot)
                return;

            if (reaction.Emote.Name == _forward && _currentPage < _builtPages.Count)
            {
                // Next page
                await reaction.Message.Value.DeleteAsync();
                var message = channel.SendMessageAsync("", false, _builtPages[_currentPage]).Result;
                _currentPage++;
                await message.AddReactionAsync(new Emoji(_backward));
                if (_currentPage < _builtPages.Count)
                {
                    await message.AddReactionAsync(new Emoji(_forward));
                }
                _messageId = message.Id;
            }
            else if (reaction.Emote.Name == _backward && _currentPage > 1)
            {
                // Last page
                await reaction.Message.Value.DeleteAsync();
                var message = channel.SendMessageAsync("", false, _builtPages[_currentPage - 2]).Result;
                _currentPage--;
                if (_currentPage > 1)
                {
                    await message.AddReactionAsync(new Emoji(_backward));
                }
                await message.AddReactionAsync(new Emoji(_forward));
                _messageId = message.Id;
            }
            else
            {
                await reaction.Message.Value.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
            }
        }
    }
}