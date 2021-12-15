using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Remora.Discord.Gateway;
using Remora.Discord.Gateway.Extensions;
using Remora.Rest.Core;

namespace AcegikmoDiscordBot
{
#pragma warning disable IDE1006 // Naming Styles
#pragma warning disable CS8618 // Non-nullable field is uninitialized.
#pragma warning disable CS0649
    [DataContract]
    internal class ConfigClass
    {
        [DataMember]
        internal string token;
    }
#pragma warning restore CS0649
#pragma warning restore CS8618 // Non-nullable field is uninitialized.
#pragma warning restore IDE1006 // Naming Styles

    internal class Program
    {
        public static Snowflake ASHL = new(189120139797594112UL);
        public static Snowflake ACEGIKMO_SERVER = new(920697136864174100UL);
        public static Snowflake ACEGIKMO_DELETED_MESSAGES = new(920699605858021406UL);

        public static readonly ConfigClass Config = GetConfig();
        private readonly DiscordGatewayClient _client;

        private static ConfigClass GetConfig()
        {
            using var stream = File.OpenRead("config.json");
            var json = new DataContractJsonSerializer(typeof(ConfigClass));
            return (ConfigClass)(json.ReadObject(stream) ?? throw new Exception("Deserialization of config.json failed"));
        }

        private static async Task Main()
        {
            var program = new Program();
            await program.Run();
        }

        private Program() {
            var services = new ServiceCollection()
                .AddDiscordGateway(_ => Config.token)
                .AddLogging(Console.WriteLine)
                .AddResponder<Log>()
                .AddResponder<HelpCommand>()
                .AddResponder<EchoCommand>()
                .AddResponder<DeleteEcho>()
                .AddResponder<GamesCommand>()
                .AddResponder<MemberizerCommand>()
                .AddResponder<TimingThing>()
                .AddResponder<PronounCommand>()
                .BuildServiceProvider();
            _client = services.GetRequiredService<DiscordGatewayClient>();
        }

        private async Task Run()
        {
            
            //_client.Log += a => { Console.WriteLine(a); return Task.CompletedTask; };
            //_client.MessageDeleted += new DeleteEcho(_log).MessageDeletedAsync;
            //_client.MessageReceived += EchoCommand.MessageReceivedAsync;
            //_client.MessageReceived += new GamesCommand().MessageReceivedAsync;
            //_client.MessageReceived += HelpCommand.MessageReceivedAsync;
            //_client.MessageReceived += PronounCommand.MessageReceivedAsync;
            //_client.MessageReceived += new MemberizerCommand(_log).MessageReceivedAsync;
            //_client.MessageReceived += new TimingThing(_log).MessageReceivedAsync;
            //_client.MessageReceived += _log.MessageReceivedAsync;
            //_client.MessageUpdated += _log.MessageUpdatedAsync;
            
            var res = await _client.RunAsync(CancellationToken.None);
            if (!res.IsSuccess) throw new Exception(res.Error.Message);

            // Block the program until it is closed.
            await Task.Delay(-1);
        }
    }
}
