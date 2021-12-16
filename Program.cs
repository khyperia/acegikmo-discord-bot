using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace AcegikmoDiscordBot;

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

internal class Program : IDisposable
{
    public static ulong ASHL = 139525105846976512UL;
    public static ulong MODERATOR_ROLE = 474015848311291944UL;
    public static ulong ACEGIKMO_SERVER = 422202998610198528UL;
    public static ulong ACEGIKMO_DELETED_MESSAGES = 612767753031647240UL;
    public static ulong ACEGIKMO_MOD_LOUNGE = 474020456903737350UL;

    private readonly Log _log;
    public static readonly ConfigClass Config = GetConfig();
    private readonly DiscordSocketClient _client;

    private static ConfigClass GetConfig()
    {
        using var stream = File.OpenRead("config.json");
        var json = new DataContractJsonSerializer(typeof(ConfigClass));
        return (ConfigClass)(json.ReadObject(stream) ?? throw new Exception("Deserialization of config.json failed"));
    }

    private static async Task Main()
    {
        using var program = new Program();
        await program.Run();
    }

    private Program()
    {
        _client = new DiscordSocketClient(new DiscordSocketConfig
        {
            GatewayIntents =
                GatewayIntents.Guilds |
                GatewayIntents.GuildMembers |
                GatewayIntents.GuildMessages |
                GatewayIntents.GuildMessageReactions,
        });
        _log = new Log();
        Console.CancelKeyPress += (sender, args) => Dispose();
    }

    private async Task Run()
    {
        _client.Log += a => { Console.WriteLine(a); return Task.CompletedTask; };
        _client.MessageDeleted += new DeleteEcho(_log).MessageDeletedAsync;
        _client.MessageReceived += EchoCommand.MessageReceivedAsync;
        var games = new GamesCommand();
        _client.MessageReceived += games.MessageReceivedAsync;
        _client.SlashCommandExecuted += games.SlashCommandExecuted;
        _client.MessageReceived += HelpCommand.MessageReceivedAsync;
        _client.SlashCommandExecuted += PronounCommand.SlashCommandExecuted;
        _client.MessageReceived += new MemberizerCommand(_log).MessageReceivedAsync;
        var tmpBan = new TmpBan();
        _client.SlashCommandExecuted += tmpBan.SlashCommandExecuted;
        _client.MessageReceived += new TimingThing(tmpBan, _log).MessageReceivedAsync;
        _client.MessageReceived += _log.MessageReceivedAsync;
        _client.MessageUpdated += _log.MessageUpdatedAsync;

        await _client.LoginAsync(TokenType.Bot, Config.token);
        await _client.StartAsync();

        _client.Ready += async () =>
        {
            var acegikmo = _client.GetGuild(ACEGIKMO_SERVER);
            var commands = await acegikmo.BulkOverwriteApplicationCommandAsync(
                GamesCommand.Commands
                .Concat(PronounCommand.Commands)
                .Concat(TmpBan.Commands)
                .ToArray()
            );
        };

        // Block the program until it is closed.
        await Task.Delay(-1);
    }

    public void Dispose()
    {
        _client.Dispose();
        _log.Dispose();
    }
}
