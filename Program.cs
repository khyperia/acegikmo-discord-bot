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

    internal class Program : IDisposable
    {
        public static ulong ASHL = 139525105846976512UL;

        private readonly Log _log;
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
            using var program = new Program();
            await program.Run();
        }

        private Program()
        {
            _client = new DiscordSocketClient();
            _log = new Log();
            Console.CancelKeyPress += (sender, args) => Dispose();
        }

        private async Task Run()
        {
            _client.Log += a => { Console.WriteLine(a); return Task.CompletedTask; };
            _client.MessageDeleted += new DeleteEcho(_log, _config).MessageDeletedAsync;
            _client.MessageReceived += new EchoCommand().MessageReceivedAsync;
            _client.MessageReceived += new GamesCommand().MessageReceivedAsync;
            _client.MessageReceived += new HelpCommand().MessageReceivedAsync;
            _client.MessageReceived += new MemberizerCommand().MessageReceivedAsync;
            _client.MessageReceived += _log.MessageReceivedAsync;
            _client.MessageUpdated += _log.MessageUpdatedAsync;

            await _client.LoginAsync(TokenType.Bot, _config.token);
            await _client.StartAsync();

            // Block the program until it is closed.
            await Task.Delay(-1);
        }

        public void Dispose()
        {
            _client.Dispose();
            _log.Dispose();
        }
    }
}
