using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace AcegikmoDiscordBot; 

internal class GamesCommand : IResponder<IMessageCreate> {
    private readonly Json<Dictionary<ulong, Dictionary<string, List<ulong>>>> _json = new("games.json");

    private readonly IDiscordRestChannelAPI _discordAPI;
    private readonly IDiscordRestGuildAPI _guildAPI;

    public GamesCommand(IDiscordRestChannelAPI discordApi, IDiscordRestGuildAPI guildApi) {
        _discordAPI = discordApi;
        _guildAPI = guildApi;
    }

    private Dictionary<ulong, Dictionary<string, List<ulong>>> AllGameDicts => _json.Data;
    private Dictionary<string, List<ulong>>? GameDict(IMessageCreate message) {
        if (!message.GuildID.HasValue) return null;
        if (AllGameDicts.TryGetValue(message.GuildID.Value.Value, out var dict))
            return dict;

        var result = new Dictionary<string, List<ulong>>();
        AllGameDicts.Add(message.GuildID.Value.Value, result);
        return result;
    }

    private void SaveDict() => _json.Save();

    public async Task Checkmark(IMessageCreate message) {
        await _discordAPI.CreateReactionAsync(message.ChannelID, message.ID, "✅");
    }

    public async Task CrossReact(IMessageCreate message) {
        await _discordAPI.CreateReactionAsync(message.ChannelID, message.ID, "❌");
    }

    public async Task<Result> RespondAsync(IMessageCreate message, CancellationToken ct = new()) {
        if (message.Content.StartsWith("!addgame "))
        {
            var game = message.Content["!addgame ".Length..].ToLower();
            await AddGame(message, game);
        }
        if (message.Content.StartsWith("!delgame "))
        {
            var game = message.Content["!delgame ".Length..].ToLower();
            await DelGame(message, game);
        }
        if (message.Content.StartsWith("!pinggame "))
        {
            var game = message.Content["!pinggame ".Length..].ToLower();
            await PingGame(message, game);
        }
        if (message.Content == "!games")
        {
            await ListGames(message);
        }
        if (message.Content == "!mygames")
        {
            await MyGames(message);
        }
        if (message.Author.IsAshl() && message.Content.StartsWith("!nukegame "))
        {
            var game = message.Content["!nukegame ".Length..].ToLower();
            await NukeGame(message, game);
        }
        if (message.Author.IsAshl() && message.Content.StartsWith("!nukeuser "))
        {
            var cmd = message.Content["!nukeuser ".Length..].ToLower();
            await NukeUser(message, cmd);
        }
        if (message.Author.IsAshl() && message.Content.StartsWith("!addusergame "))
        {
            var cmd = message.Content["!addusergame ".Length..];
            await AddUserGame(message, cmd);
        }
        if (message.Author.IsAshl() && message.Content.StartsWith("!delusergame "))
        {
            var cmd = message.Content["!delusergame ".Length..];
            await DelUserGame(message, cmd);
        }
        if (message.Author.IsAshl() && message.Content == "!downloadusers" && message.GuildID.HasValue) {
            var getGuild = await _guildAPI.GetGuildAsync(message.GuildID.Value, true);
            if(!getGuild.IsSuccess) return Result.FromError(getGuild.Error);
            var guild = getGuild.Entity;
            var getChannel = await _discordAPI.GetChannelAsync(message.ChannelID);
            if(!getChannel.IsSuccess) return Result.FromError(getChannel.Error);
            var channel= getChannel.Entity;
            var response = $"mc={guild.MemberCount.Value} amc={guild.ApproximateMemberCount.Value} cmc={channel.MemberCount} manual count={guild.Members.Value.Count}";
            await _discordAPI.CreateMessageAsync(message.ChannelID, response);
        }
        return Result.FromSuccess();
    }

    private async Task AddGame(IMessageCreate message, string game)
    {
        var gameDict = GameDict(message);
        if (gameDict == null)
        {
            return;
        }
        if (!gameDict.TryGetValue(game, out var list))
        {
            list = gameDict[game] = new List<ulong>();
        }
        if (list.Contains(message.Author.ID.Value))
        {
            await _discordAPI.CreateMessageAsync(message.ChannelID, $"You're already in {game.Replace("@", "@\u200B")}");
        }
        else
        {
            list.Add(message.Author.ID.Value);
            SaveDict();
            await Checkmark(message);
        }
    }

    private async Task DelGame(IMessageCreate message, string game)
    {
        var gameDict = GameDict(message);
        if (gameDict == null)
        {
            return;
        }
        if (!gameDict.TryGetValue(game, out var list) || !list.Remove(message.Author.ID.Value))
        {
            await _discordAPI.CreateMessageAsync(message.ChannelID, $"You are not in the list for {game.Replace("@", "@\u200B")}");
        }
        else
        {
            if (list.Count == 0)
            {
                gameDict.Remove(game);
            }
            SaveDict();
            await Checkmark(message);
        }
    }

    private async Task PingGame(IMessageCreate message, string game)
    {
        var gameDict = GameDict(message);
        if (gameDict == null)
        {
            return;
        }
        if (!gameDict.TryGetValue(game, out var list))
        {
            await _discordAPI.CreateMessageAsync(message.ChannelID, $"Nobody's in the list for {game.Replace("@", "@\u200B")}.");
        }
        else if (!list.Contains(message.Author.ID.Value))
        {
            await _discordAPI.CreateMessageAsync(message.ChannelID, $"You are not in the list for {game.Replace("@", "@\u200B")}, so you can't ping it.");
        }
        else if (list.Count == 1)
        {
            await _discordAPI.CreateMessageAsync(message.ChannelID, $"You're the only one registered for {game.Replace("@", "@\u200B")}, sorry :c");
        }
        else
        {
            await _discordAPI.CreateMessageAsync(message.ChannelID, $"{MentionUtils.MentionUser(message.Author.ID.Value)} wants to play " +
                                                                    $"{game.Replace("@", "@\u200B")}! \n" +
                                                                    $"{string.Join(", ", list.Where(id => id != message.Author.ID.Value).Select(MentionUtils.MentionUser))}");
        }
    }

    private async Task ListGames(IMessageCreate message)
    {
        var gameDict = GameDict(message);
        if (gameDict == null)
        {
            return;
        }
        var msg = $"All pingable games (and number of people): {string.Join(", ", gameDict.OrderBy(kvp => kvp.Key).Select(kvp => $"{kvp.Key.Replace("@", "@\u200B")} ({kvp.Value.Count})"))}";
        var maxLength = 2000;
        while (msg.Length > 2000)
        {
            var slice = msg.Substring(0, maxLength);
            await _discordAPI.CreateMessageAsync(message.ChannelID, slice);
            msg = msg[maxLength..];
        }
        await _discordAPI.CreateMessageAsync(message.ChannelID, msg);
    }

    private async Task MyGames(IMessageCreate message)
    {
        var gameDict = GameDict(message);
        if (gameDict == null)
        {
            return;
        }
        var result = string.Join(", ", gameDict.Where(kvp => kvp.Value.Contains(message.Author.ID.Value)).Select(kvp => kvp.Key.Replace("@", "@\u200B")).OrderBy(x => x));
        if (string.IsNullOrEmpty(result))
        {
            await _discordAPI.CreateMessageAsync(message.ChannelID, $"You're not in any games list");
        }
        else
        {
            await _discordAPI.CreateMessageAsync(message.ChannelID, $"Your games: {result}");
        }
    }

    private async Task NukeGame(IMessageCreate message, string game)
    {
        var gameDict = GameDict(message);
        if (gameDict == null)
        {
            return;
        }
        if (gameDict.Remove(game))
        {
            SaveDict();
            await Checkmark(message);
        }
        else
        {
            await _discordAPI.CreateMessageAsync(message.ChannelID, $"game not found: {HttpUtility.JavaScriptStringEncode(game, true)}");
        }
    }

    private async Task NukeUser(IMessageCreate message, string cmd)
    {
        var gameDict = GameDict(message);
        if (gameDict == null)
        {
            return;
        }
        if (!TryParseId(cmd, out var id))
        {
            await _discordAPI.CreateMessageAsync(message.ChannelID, "bad user ID");
        }
        else
        {
            var changed = false;
            List<string>? emptyKeys = null;
            foreach (var kvp in gameDict)
            {
                changed |= kvp.Value.Remove(id);
                if (kvp.Value.Count == 0)
                {
                    if (emptyKeys == null)
                    {
                        emptyKeys = new List<string>();
                    }
                    emptyKeys.Add(kvp.Key);
                }
            }
            if (!changed)
            {
                await _discordAPI.CreateMessageAsync(message.ChannelID, "user not found");
            }
            else
            {
                if (emptyKeys != null)
                {
                    foreach (var emptyKey in emptyKeys)
                    {
                        gameDict.Remove(emptyKey);
                    }
                }
                SaveDict();
                await Checkmark(message);
            }
        }
    }

    private async Task AddUserGame(IMessageCreate message, string cmd)
    {
        var gameDict = GameDict(message);
        if (gameDict == null)
        {
            return;
        }
        var thing = cmd.Split(' ', 2);
        if (thing.Length != 2)
        {
            await _discordAPI.CreateMessageAsync(message.ChannelID, "!addusergame id game");
        }
        else if (!TryParseId(thing[0], out var id))
        {
            await _discordAPI.CreateMessageAsync(message.ChannelID, "bad user ID");
        }
        else
        {
            var game = thing[1].ToLower();
            if (!gameDict.TryGetValue(game, out var list))
            {
                list = gameDict[game] = new List<ulong>();
            }
            if (list.Contains(id))
            {
                await _discordAPI.CreateMessageAsync(message.ChannelID, "user already in list");
            }
            else
            {
                list.Add(id);
                SaveDict();
                await Checkmark(message);
            }
        }
    }

    private async Task DelUserGame(IMessageCreate message, string cmd)
    {
        var gameDict = GameDict(message);
        if (gameDict == null)
        {
            return;
        }
        var thing = cmd.Split(' ', 2);
        if (thing.Length != 2)
        {
            await _discordAPI.CreateMessageAsync(message.ChannelID, "!delusergame id game");
            return;
        }
        var game = thing[1].ToLower();
        if (!gameDict.TryGetValue(game, out var list))
        {
            await _discordAPI.CreateMessageAsync(message.ChannelID, $"game not found: {HttpUtility.JavaScriptStringEncode(game, true)}");
        }
        else if (!TryParseId(thing[0], out var id))
        {
            await _discordAPI.CreateMessageAsync(message.ChannelID, "bad user ID");
        }
        else if (!list.Remove(id))
        {
            await _discordAPI.CreateMessageAsync(message.ChannelID, "user not in list");
        }
        else
        {
            if (list.Count == 0)
            {
                gameDict.Remove(game);
            }
            SaveDict();
            await Checkmark(message);
        }
    }

    private static bool TryParseId(string s, out ulong id) => ulong.TryParse(s, out id) ||
                                                              (s.StartsWith($"<@") && s.EndsWith(">") && ulong.TryParse(s[2..^1], out id)) ||
                                                              (s.StartsWith($"<@!") && s.EndsWith(">") && ulong.TryParse(s[3..^1], out id));
        
}