using Discord;
using Discord.Rest;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using static AcegikmoDiscordBot.Program;

namespace AcegikmoDiscordBot
{
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
            else if (AllGameDicts.TryGetValue(chan.Guild.Id, out var dict))
            {
                return dict;
            }
            else
            {
                var result = new Dictionary<string, List<ulong>>();
                AllGameDicts.Add(chan.Guild.Id, result);
                return result;
            }
        }

        private void SaveDict() => _json.Save();

        public static async Task Checkmark(SocketMessage message)
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

        public static async Task CrossReact(SocketMessage message)
        {
            var obtainedMessage = await message.Channel.GetMessageAsync(message.Id);
            if (obtainedMessage is RestUserMessage rest)
            {
                await rest.AddReactionAsync(new Emoji("\u274c"));
            }
            else
            {
                await message.Channel.SendMessageAsync("\u274c");
            }
        }

        public async Task MessageReceivedAsync(SocketMessage message)
        {
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
            if (message.Author.Id == ASHL && message.Content == "!downloadusers" && message.Channel is SocketGuildChannel chan)
            {
                await chan.Guild.DownloadUsersAsync();
                await message.Channel.SendMessageAsync($"mc={chan.Guild.MemberCount} dmc={chan.Guild.DownloadedMemberCount} count={chan.Users.Count} ham={chan.Guild.HasAllMembers}");
            }
        }

        private async Task AddGame(SocketMessage message, string game)
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
            if (list.Contains(message.Author.Id))
            {
                await message.Channel.SendMessageAsync($"You're already in {game.Replace("@", "@\u200B")}");
            }
            else
            {
                list.Add(message.Author.Id);
                SaveDict();
                await Checkmark(message);
            }
        }

        private async Task DelGame(SocketMessage message, string game)
        {
            var gameDict = GameDict(message);
            if (gameDict == null)
            {
                return;
            }
            if (!gameDict.TryGetValue(game, out var list) || !list.Remove(message.Author.Id))
            {
                await message.Channel.SendMessageAsync($"You are not in the list for {game.Replace("@", "@\u200B")}");
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

        private async Task PingGame(SocketMessage message, string game)
        {
            var gameDict = GameDict(message);
            if (gameDict == null)
            {
                return;
            }
            if (!gameDict.TryGetValue(game, out var list))
            {
                await message.Channel.SendMessageAsync($"Nobody's in the list for {game.Replace("@", "@\u200B")}.");
            }
            else if (!list.Contains(message.Author.Id))
            {
                await message.Channel.SendMessageAsync($"You are not in the list for {game.Replace("@", "@\u200B")}, so you can't ping it.");
            }
            else if (list.Count == 1)
            {
                await message.Channel.SendMessageAsync($"You're the only one registered for {game.Replace("@", "@\u200B")}, sorry :c");
            }
            else
            {
                await message.Channel.SendMessageAsync($"{message.Author.Mention} wants to play {game.Replace("@", "@\u200B")}! {string.Join(", ", list.Where(id => id != message.Author.Id).Select(MentionUtils.MentionUser))}");
            }
        }

        private async Task ListGames(SocketMessage message)
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
                var slice = msg[..maxLength];
                await message.Channel.SendMessageAsync(slice);
                msg = msg[maxLength..];
            }
            await message.Channel.SendMessageAsync(msg);
        }

        private async Task MyGames(SocketMessage message)
        {
            var gameDict = GameDict(message);
            if (gameDict == null)
            {
                return;
            }
            var result = string.Join(", ", gameDict.Where(kvp => kvp.Value.Contains(message.Author.Id)).Select(kvp => kvp.Key.Replace("@", "@\u200B")).OrderBy(x => x));
            if (string.IsNullOrEmpty(result))
            {
                await message.Channel.SendMessageAsync($"You're not in any games list");
            }
            else
            {
                await message.Channel.SendMessageAsync($"Your games: {result}");
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
            if (!TryParseId(cmd, out var id))
            {
                await message.Channel.SendMessageAsync("bad user ID");
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
                    await message.Channel.SendMessageAsync("user not found");
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
                    list = gameDict[game] = new List<ulong>();
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

        private static bool TryParseId(string s, out ulong id) => ulong.TryParse(s, out id) ||
                (s.StartsWith($"<@") && s.EndsWith(">") && ulong.TryParse(s[2..^1], out id)) ||
                (s.StartsWith($"<@!") && s.EndsWith(">") && ulong.TryParse(s[3..^1], out id));
    }
}
