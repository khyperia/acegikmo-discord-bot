using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using System.Collections.Immutable;

namespace DiscordDeleteEcho
{
    [DataContract]
    internal class Config
    {
        [DataMember]
        internal string token;

        [DataMember]
        internal ulong server;

        [DataMember]
        internal ulong channel;
    }

    class Program
    {
        readonly Dictionary<ulong, string> _messages = new Dictionary<ulong, string>();
        readonly Config _config = Config();
        DiscordSocketClient _client;

        static Config Config()
        {
            using (var stream = File.OpenRead("config.json"))
            {
                var json = new DataContractJsonSerializer(typeof(Config));
                return (Config)json.ReadObject(stream);
            }
        }

        static Task Main() => new Program().Run();

        async Task Run()
        {
            _client = new DiscordSocketClient();

            _client.Log += a => { Console.WriteLine(a); return Task.CompletedTask; };
            _client.MessageReceived += MessageReceivedAsync;
            _client.MessageUpdated += MessageUpdatedAsync;
            _client.MessageDeleted += MessageDeletedAsync;

            await _client.LoginAsync(TokenType.Bot, _config.token);
            await _client.StartAsync();


            // Block the program until it is closed.
            await Task.Delay(-1);
        }

        private async Task MessageDeletedAsync(Cacheable<IMessage, ulong> messageId, ISocketMessageChannel socket)
        {
            if (_messages.TryGetValue(messageId.Id, out var message))
            {
                var modchannel = (IMessageChannel)_client.GetChannel(_config.channel);
                await modchannel.SendMessageAsync(message);
            }
        }

        private bool IsValidChannel(ulong id) => _client.GetGuild(_config.server).Channels.Select(c => c.Id).Contains(id);

        private Task MessageReceivedAsync(SocketMessage message)
        {
            if (IsValidChannel(message.Channel.Id))
            {
                var text = Format(message);
                Console.WriteLine(text);
                _messages[message.Id] = text;
            }
            return Task.CompletedTask;
        }

        private Task MessageUpdatedAsync(Cacheable<IMessage, ulong> oldMessage, SocketMessage message, ISocketMessageChannel socket)
        {
            if (message.Content != null)
            {
                if (IsValidChannel(message.Channel.Id))
                {
                    var text = Format(message);
                    Console.WriteLine(text);
                    _messages[message.Id] = text;
                }
            }
            return Task.CompletedTask;
        }

        private string Format(SocketMessage message)
        {
            //var result = $"Message deleted in {MentionUtils.MentionChannel(message.Channel.Id)}: <{message.Author.Mention}> ";
            var result = $"Message by {message.Author.Mention} deleted in {MentionUtils.MentionChannel(message.Channel.Id)}:\n";
            if (!string.IsNullOrWhiteSpace(message.Content))
            {
                result += message.Content;
            }
            if (message.Attachments != null && (!(message.Attachments is ImmutableArray<Attachment>) || !((ImmutableArray<Attachment>)message.Attachments).IsDefault))
            {
                foreach (var attachment in message.Attachments)
                {
                    result += "\n" + attachment.ProxyUrl;
                }
            }
            return result;
        }
    }
}
