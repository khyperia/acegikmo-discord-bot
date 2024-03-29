using System;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using static AcegikmoDiscordBot.Program;

namespace AcegikmoDiscordBot;

internal class GamesCommand
{
    private readonly Json<Dictionary<ulong, Dictionary<string, List<ulong>>>> _json = new("games.json");

    private Dictionary<ulong, Dictionary<string, List<ulong>>> AllGameDicts => _json.Data;

    private Dictionary<string, List<ulong>>? GameDict(SocketMessage message)
    {
        if (message.Channel is not SocketGuildChannel chan)
        {
            return null;
        }

        if (AllGameDicts.TryGetValue(chan.Guild.Id, out var dict))
        {
            return dict;
        }

        var result = new Dictionary<string, List<ulong>>();
        AllGameDicts.Add(chan.Guild.Id, result);
        return result;
    }

    private Dictionary<string, List<ulong>>? GameDict(SocketSlashCommand command)
    {
        if (command.Channel is not SocketGuildChannel chan)
        {
            return null;
        }

        if (AllGameDicts.TryGetValue(chan.Guild.Id, out var dict))
        {
            return dict;
        }

        var result = new Dictionary<string, List<ulong>>();
        AllGameDicts.Add(chan.Guild.Id, result);
        return result;
    }

    private void SaveDict() => _json.Save();

    private static async Task Checkmark(SocketMessage message)
    {
        var obtainedMessage = await message.Channel.GetMessageAsync(message.Id);
        if (obtainedMessage is RestUserMessage rest)
        {
            await rest.AddReactionAsync(new Emoji("\u2705"));
        }
        else
        {
            await message.Channel.SendMessageAsync("\u2705");
        }
    }

    public async Task MessageReceivedAsync(SocketMessage message)
    {
        if (message.Author.Id == ASHL && message.Content.StartsWith("!nukegame "))
        {
            var game = message.Content["!nukegame ".Length..].ToLower();
            await NukeGame(message, game);
        }

        if (message.Author.Id == ASHL && message.Content.StartsWith("!nukeuser "))
        {
            var cmd = message.Content["!nukeuser ".Length..].ToLower();
            await NukeUser(message, cmd);
        }

        if (message.Author.Id == ASHL && message.Content.StartsWith("!addusergame "))
        {
            var cmd = message.Content["!addusergame ".Length..];
            await AddUserGame(message, cmd);
        }

        if (message.Author.Id == ASHL && message.Content.StartsWith("!delusergame "))
        {
            var cmd = message.Content["!delusergame ".Length..];
            await DelUserGame(message, cmd);
        }

        if (message.Author.Id == ASHL && message is { Content: "!downloadusers", Channel: SocketGuildChannel chan })
        {
            await chan.Guild.DownloadUsersAsync();
            await message.Channel.SendMessageAsync(
                $"mc={chan.Guild.MemberCount} dmc={chan.Guild.DownloadedMemberCount} count={chan.Users.Count} ham={chan.Guild.HasAllMembers}");
        }
    }

    public static readonly SlashCommandProperties[] Commands =
    {
        new SlashCommandBuilder()
            .WithName("addgame")
            .WithDescription("Add yourself to the list of games to be pinged with /pinggame")
            .AddOption("game", ApplicationCommandOptionType.String, "The game to add", isRequired: true)
            .Build(),
        new SlashCommandBuilder()
            .WithName("delgame")
            .WithDescription("Remove yourself to the list of games to be pinged with /pinggame")
            .AddOption("game", ApplicationCommandOptionType.String, "The game to remove", isRequired: true)
            .Build(),
        new SlashCommandBuilder()
            .WithName("pinggame")
            .WithDescription("Ping everyone who has added themselves to the game list")
            .AddOption("game", ApplicationCommandOptionType.String, "The game to ping", isRequired: true)
            .Build(),
        new SlashCommandBuilder()
            .WithName("listgame")
            .WithDescription("List everyone registered to a game (like /pinggame, without pinging)")
            .AddOption("game", ApplicationCommandOptionType.String, "The game to list", isRequired: true)
            .Build(),
        new SlashCommandBuilder()
            .WithName("games")
            .WithDescription("List all games")
            .Build(),
        new SlashCommandBuilder()
            .WithName("mygames")
            .WithDescription("List games you've registered for")
            .Build()
    };

    internal async Task SlashCommandExecuted(SocketSlashCommand command)
    {
        switch (command.Data.Name)
        {
            case "addgame":
                {
                    var game = (string)command.Data.Options.First().Value;
                    await AddGame(command, game.ToLower());
                }
                break;
            case "delgame":
                {
                    var game = (string)command.Data.Options.First().Value;
                    await DelGame(command, game.ToLower());
                }
                break;
            case "pinggame":
                {
                    var game = (string)command.Data.Options.First().Value;
                    await PingGame(command, game.ToLower());
                }
                break;
            case "listgame":
                {
                    var game = (string)command.Data.Options.First().Value;
                    await ListGame(command, game.ToLower());
                }
                break;
            case "games":
                {
                    await ListGames(command);
                }
                break;
            case "mygames":
                {
                    await MyGames(command);
                }
                break;
        }
    }

    private async Task AddGame(SocketSlashCommand command, string game)
    {
        var gameDict = GameDict(command);
        if (gameDict == null)
        {
            return;
        }

        if (!gameDict.TryGetValue(game, out var list))
        {
            list = gameDict[game] = new();
        }

        if (list.Contains(command.User.Id))
        {
            await command.RespondAsync($"You're already in {game.Replace("@", "@\u200B")}");
        }
        else
        {
            list.Add(command.User.Id);
            SaveDict();
            await command.RespondAsync($"Added you to the ping list for {game}");
        }
    }

    private async Task DelGame(SocketSlashCommand command, string game)
    {
        var gameDict = GameDict(command);
        if (gameDict == null)
        {
            return;
        }

        if (!gameDict.TryGetValue(game, out var list) || !list.Remove(command.User.Id))
        {
            await command.RespondAsync($"You are not in the list for {game.Replace("@", "@\u200B")}");
        }
        else
        {
            if (list.Count == 0)
            {
                gameDict.Remove(game);
            }

            SaveDict();
            await command.RespondAsync($"Removed you from the ping list for {game}");
        }
    }

    private async Task PingGame(SocketSlashCommand command, string game)
    {
        var gameDict = GameDict(command);
        if (gameDict == null)
        {
            return;
        }

        if (!gameDict.TryGetValue(game, out var list))
        {
            await command.RespondAsync($"Nobody's in the list for {game.Replace("@", "@\u200B")}.");
        }
        else if (!list.Contains(command.User.Id))
        {
            await command.RespondAsync(
                $"You are not in the list for {game.Replace("@", "@\u200B")}, so you can't ping it.");
        }
        else if (list.Count == 1)
        {
            await command.RespondAsync($"You're the only one registered for {game.Replace("@", "@\u200B")}, sorry :c");
        }
        else
        {
            await command.RespondAsync(
                $"{command.User.Mention} wants to play {game.Replace("@", "@\u200B")}! {string.Join(", ", list.Where(id => id != command.User.Id).Select(MentionUtils.MentionUser))}");
        }
    }

    private async Task ListGame(SocketSlashCommand command, string game)
    {
        var gameDict = GameDict(command);
        if (gameDict == null)
        {
            return;
        }

        if (!gameDict.TryGetValue(game, out var list))
        {
            await command.RespondAsync($"Nobody's in the list for {game.Replace("@", "@\u200B")}.");
        }
        else
        {
            await command.RespondAsync("", embeds: new[]
            {
                new EmbedBuilder
                {
                    Title = $"People registered to {game.Replace("@", "@\u200B")}",
                    Description = $"{string.Join(", ", list.Select(MentionUtils.MentionUser))}",
                }.Build()
            });
        }
    }

    private async Task ListGames(SocketSlashCommand command)
    {
        var gameDict = GameDict(command);
        if (gameDict == null)
        {
            return;
        }

        var msg =
            $"All pingable games (and number of people): {string.Join(", ", gameDict.OrderBy(kvp => kvp.Key).Select(kvp => $"{kvp.Key.Replace("@", "@\u200B")} ({kvp.Value.Count})"))}";
        var maxLength = 2000;
        var first = true;

        async Task Send(string text)
        {
            if (first)
            {
                await command.RespondAsync(text, ephemeral: true);
                first = false;
            }
            else
            {
                await command.FollowupAsync(text, ephemeral: true);
            }
        }

        while (msg.Length > 2000)
        {
            var slice = msg[..maxLength];
            await Send(slice);
            msg = msg[maxLength..];
        }

        await Send(msg);
    }

    private async Task MyGames(SocketSlashCommand command)
    {
        var gameDict = GameDict(command);
        if (gameDict == null)
        {
            return;
        }

        var result = string.Join(", ",
            gameDict.Where(kvp => kvp.Value.Contains(command.User.Id)).Select(kvp => kvp.Key.Replace("@", "@\u200B"))
                .OrderBy(x => x));
        if (string.IsNullOrEmpty(result))
        {
            await command.RespondAsync($"You're not in any games list", ephemeral: true);
        }
        else
        {
            await command.RespondAsync($"Your games: {result}", ephemeral: true);
        }
    }

    private async Task NukeGame(SocketMessage message, string game)
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
            await message.Channel.SendMessageAsync($"game not found: {HttpUtility.JavaScriptStringEncode(game, true)}");
        }
    }

    private async Task NukeUser(SocketMessage message, string cmd)
    {
        var gameDict = GameDict(message);
        if (gameDict == null)
        {
            return;
        }

        List<string>? emptyKeys = null;
        foreach (var idStr in cmd.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            if (!TryParseId(idStr, out var id))
            {
                await message.Channel.SendMessageAsync($"bad user ID: {idStr}");
                break;
            }

            var changed = false;
            foreach (var kvp in gameDict)
            {
                changed |= kvp.Value.Remove(id);
                if (kvp.Value.Count == 0)
                {
                    emptyKeys ??= new();
                    emptyKeys.Add(kvp.Key);
                }
            }

            if (!changed)
            {
                await message.Channel.SendMessageAsync($"user not found: {idStr}");
                break;
            }
        }

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

    private async Task AddUserGame(SocketMessage message, string cmd)
    {
        var gameDict = GameDict(message);
        if (gameDict == null)
        {
            return;
        }

        var thing = cmd.Split(' ', 2);
        if (thing.Length != 2)
        {
            await message.Channel.SendMessageAsync("!addusergame id game");
        }
        else if (!TryParseId(thing[0], out var id))
        {
            await message.Channel.SendMessageAsync("bad user ID");
        }
        else
        {
            var game = thing[1].ToLower();
            if (!gameDict.TryGetValue(game, out var list))
            {
                list = gameDict[game] = new();
            }

            if (list.Contains(id))
            {
                await message.Channel.SendMessageAsync("user already in list");
            }
            else
            {
                list.Add(id);
                SaveDict();
                await Checkmark(message);
            }
        }
    }

    private async Task DelUserGame(SocketMessage message, string cmd)
    {
        var gameDict = GameDict(message);
        if (gameDict == null)
        {
            return;
        }

        var thing = cmd.Split(' ', 2);
        if (thing.Length != 2)
        {
            await message.Channel.SendMessageAsync("!delusergame id game");
            return;
        }

        var game = thing[1].ToLower();
        if (!gameDict.TryGetValue(game, out var list))
        {
            await message.Channel.SendMessageAsync($"game not found: {HttpUtility.JavaScriptStringEncode(game, true)}");
        }
        else if (!TryParseId(thing[0], out var id))
        {
            await message.Channel.SendMessageAsync("bad user ID");
        }
        else if (!list.Remove(id))
        {
            await message.Channel.SendMessageAsync("user not in list");
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

    private static bool TryParseId(string s, out ulong id) =>
        ulong.TryParse(s, out id) ||
        (s.StartsWith($"<@") && s.EndsWith(">") && ulong.TryParse(s[2..^1], out id)) ||
        (s.StartsWith($"<@!") && s.EndsWith(">") && ulong.TryParse(s[3..^1], out id));
}
