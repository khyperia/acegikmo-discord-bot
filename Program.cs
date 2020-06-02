using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace AcegikmoDiscordBot
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

    internal struct Message
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

    internal class Program : IDisposable
    {
        private readonly DeleteEcho _deleteEcho;
        private readonly Config _config = Config();
        private readonly DiscordSocketClient _client;

        private static Config Config()
        {
            using var stream = File.OpenRead("config.json");
            var json = new DataContractJsonSerializer(typeof(Config));
            return (Config)json.ReadObject(stream);
        }

        private static async Task Main()
        {
            var program = new Program();
            await program.Run();
            program.Dispose();
        }

        private Program()
        {
            _client = new DiscordSocketClient();
            _deleteEcho = new DeleteEcho(_client, _config);
            Console.CancelKeyPress += (sender, args) => Dispose();
        }

        private async Task Run()
        {
            _client.Log += a => { Console.WriteLine(a); return Task.CompletedTask; };
            _client.MessageReceived += _deleteEcho.MessageReceivedAsync;
            _client.MessageUpdated += _deleteEcho.MessageUpdatedAsync;
            _client.MessageDeleted += _deleteEcho.MessageDeletedAsync;
            _client.MessageReceived += new EchoCommand().MessageReceivedAsync;
            _client.MessageReceived += new GamesCommand().MessageReceivedAsync;
            _client.MessageReceived += new HelpCommand().MessageReceivedAsync;

            await _client.LoginAsync(TokenType.Bot, _config.token);
            await _client.StartAsync();

            // Block the program until it is closed.
            await Task.Delay(-1);
        }

        public void Dispose()
        {
            _deleteEcho.Dispose();
            _client.Dispose();
        }
    }
}
