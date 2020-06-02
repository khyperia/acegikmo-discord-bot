using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AcegikmoDiscordBot
{
    internal class DeleteEcho : IDisposable
    {
        private readonly DiscordSocketClient _client;
        private readonly Config _config;
        private readonly Dictionary<ulong, Message> _messages = new Dictionary<ulong, Message>();
        private readonly Dictionary<ulong, string> _lastMessageLink = new Dictionary<ulong, string>();
        private readonly StreamWriter _writer;

        public DeleteEcho(DiscordSocketClient client, Config config)
        {
            _client = client;
            _config = config;
            try
            {
                var loaded = 0;
                var trimmed = 0;
                var thresh = DateTimeOffset.UtcNow.AddMonths(-1).UtcTicks;
                foreach (var line in File.ReadLines("messages.txt"))
                {
                    var msg = Message.Read(line, out var id);
                    if (msg.Timestamp > thresh)
                    {
                        loaded++;
                        _messages[id] = msg;
                    }
                    else
                    {
                        trimmed++;
                    }
                }
                Console.WriteLine($"Loaded {loaded} messages");
                Console.WriteLine($"Trimmed {trimmed} messages over a month old");
            }
            catch (Exception e)
            {
                Console.WriteLine("Error loading messages.txt:");
                Console.WriteLine(e);
            }
            _writer = File.AppendText("messages.txt");
        }

        private bool IsValidChannel(ulong id) => _client.GetGuild(_config.server).Channels.Select(c => c.Id).Contains(id);

        public async Task MessageDeletedAsync(Cacheable<IMessage, ulong> messageId, ISocketMessageChannel socket)
        {
            if (_messages.TryGetValue(messageId.Id, out var message))
            {
                var modchannel = (IMessageChannel)_client.GetChannel(_config.channel);
                await modchannel.SendMessageAsync(message.Text);
            }
        }

        public async Task MessageReceivedAsync(SocketMessage message)
        {
            if (IsValidChannel(message.Channel.Id))
            {
                var text = Format(message);
                Console.WriteLine(text);
                var ticks = message.Timestamp.UtcTicks;
                Append(message.Id, ticks, text);
            }
        }

        public Task MessageUpdatedAsync(Cacheable<IMessage, ulong> oldMessage, SocketMessage message, ISocketMessageChannel socket)
        {
            if (message.Content != null)
            {
                if (IsValidChannel(message.Channel.Id))
                {
                    var text = Format(message);
                    Console.WriteLine(text);
                    var ticks = message.Timestamp.UtcTicks;
                    Append(message.Id, ticks, text);
                }
            }
            return Task.CompletedTask;
        }

        private void Append(ulong id, long ticks, string text)
        {
            var message = new Message(ticks, text);
            _messages[id] = message;
            _writer.WriteLine(message.Write(id));
            _writer.Flush();
        }

        private string GetLastMessageLink(SocketMessage newMessage)
        {
            var channel = newMessage.Channel;
            if (!_lastMessageLink.TryGetValue(channel.Id, out var lastMessageLink))
            {
                lastMessageLink = "";
            }
            if (channel is IGuildChannel ch)
            {
                var newLink = $"https://discordapp.com/channels/{ch.Guild.Id}/{channel.Id}/{newMessage.Id}";
                _lastMessageLink[channel.Id] = newLink;
            }
            return lastMessageLink;
        }

        private string Format(SocketMessage message)
        {
            var link = GetLastMessageLink(message);
            var result = $"Message by {message.Author.Mention} deleted in {MentionUtils.MentionChannel(message.Channel.Id)} after <{link}>:\n";
            if (!string.IsNullOrWhiteSpace(message.Content))
            {
                result += message.Content;
            }
            if (message.Attachments != null && !(message.Attachments is ImmutableArray<Attachment> array && array.IsDefault))
            {
                foreach (var attachment in message.Attachments)
                {
                    result += "\n" + attachment.ProxyUrl;
                }
            }
            return result;
        }

        public void Dispose() => _writer.Dispose();
    }
}
