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
#pragma warning disable IDE1006 // Naming Styles
#pragma warning disable CS8618 // Non-nullable field is uninitialized.
#pragma warning disable CS0649
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
#pragma warning restore CS0649
#pragma warning restore CS8618 // Non-nullable field is uninitialized.
#pragma warning restore IDE1006 // Naming Styles

    struct Message
    {
        public long Timestamp;
        public string Text;

        public Message(long timestamp, string text)
        {
            Timestamp = timestamp;
            Text = text;
        }

        public string Write(ulong id)
        {
            var msg = Text
                .Replace(@"\", @"\\")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n");
            return $"{Timestamp} {id} {msg}";
        }

        public static Message Read(string text, out ulong id)
        {
            var arr = text.Split(new[] { ' ' }, 3);
            var msg = arr[2]
                .Replace("\\n", "\n")
                .Replace("\\r", "\r")
                .Replace(@"\\", @"\");
            id = ulong.Parse(arr[1]);
            return new Message(long.Parse(arr[0]), msg);
        }
    }

    class Program: IDisposable
    {
        readonly Dictionary<ulong, Message> _messages = new Dictionary<ulong, Message>();
        readonly Config _config = Config();
        readonly DiscordSocketClient _client;
        readonly StreamWriter _writer;

        static Config Config()
        {
            using var stream = File.OpenRead("config.json");
            var json = new DataContractJsonSerializer(typeof(Config));
            return (Config)json.ReadObject(stream);
        }

        static async Task Main()
        {
            var program = new Program();
            await program.Run();
            program.Dispose();
        }

        Program()
        {
            _client = new DiscordSocketClient();
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
            catch (Exception e) {
                Console.WriteLine("Error loading messages.txt:");
                Console.WriteLine(e);
            }
            _writer = File.CreateText("messages.txt");
            Console.CancelKeyPress += (sender, args) => Dispose();
        }

        async Task Run()
        {
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
                await modchannel.SendMessageAsync(message.Text);
            }
        }

        private bool IsValidChannel(ulong id) => _client.GetGuild(_config.server).Channels.Select(c => c.Id).Contains(id);

        private Task MessageReceivedAsync(SocketMessage message)
        {
            if (IsValidChannel(message.Channel.Id))
            {
                var text = Format(message);
                Console.WriteLine(text);
                var ticks = message.Timestamp.UtcTicks;
                Append(message.Id, ticks, text);
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

        public void Dispose()
        {
            _writer.Dispose();
            _client.Dispose();
        }
    }
}
