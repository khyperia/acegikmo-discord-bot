using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Remora.Discord.Gateway;
using Remora.Discord.Gateway.Extensions;
using Remora.Rest.Core;

namespace AcegikmoDiscordBot; 

#pragma warning disable IDE1006 // Naming Styles
#pragma warning disable CS8618 // Non-nullable field is uninitialized.
#pragma warning disable CS0649
internal class ConfigClass {
    public string token { get; set; }
}

internal class SettingsClass {
    public ulong ashl { get; set; }
    public ulong server { get; set; }
    public ulong deleted_messages { get; set; }
    public ulong lewd { get; set; }
    public Dictionary<string, ulong> pronouns { get; set; }

    public Snowflake Ashl => new (ashl);
    public Snowflake Server => new (server);
    public Snowflake DeletedMsgs => new (deleted_messages);
    public Snowflake Lewd => new (lewd);

    public Snowflake? MatchPronoun(string input) {
        if (pronouns.TryGetValue(input, out var pronoun)) return new(pronoun);
        return null;
    }
}
#pragma warning restore CS0649
#pragma warning restore CS8618 // Non-nullable field is uninitialized.
#pragma warning restore IDE1006 // Naming Styles

internal class Program {
    public static readonly ConfigClass Config = GetConfig();
    public static readonly SettingsClass Settings = GetSettings();
    private readonly DiscordGatewayClient _client;

    private static ConfigClass GetConfig() {
        using var stream = File.OpenRead("config.json");
        return JsonSerializer.Deserialize<ConfigClass>(stream)!;
    }
    
    private static SettingsClass GetSettings() {
        using var stream = File.OpenRead("settings.json");
        return JsonSerializer.Deserialize<SettingsClass>(stream)!;
    }

    private static async Task Main() {
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

    private async Task Run() {
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