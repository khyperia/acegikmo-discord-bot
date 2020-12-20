using Discord;
using Discord.Rest;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static AcegikmoDiscordBot.Program;

namespace AcegikmoDiscordBot
{
    internal class GamesCommand
    {
        private readonly Json<Dictionary<ulong, Dictionary<string, List<ulong>>>> _json = new Json<Dictionary<ulong, Dictionary<string, List<ulong>>>>("games.json");

        private Dictionary<ulong, Dictionary<string, List<ulong>>> AllGameDicts => _json.Data;
        private Dictionary<string, List<ulong>>? GameDict(SocketMessage message)
        {
            if (!(message.Channel is SocketGuildChannel chan))
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

        public async Task MessageReceivedAsync(SocketMessage message)
        {
            if (message.Content.StartsWith("!addgame "))
            {
                var game = message.Content.Substring("!addgame ".Length).ToLower();
                await AddGame(message, game);
            }
            if (message.Content.StartsWith("!delgame "))
            {
                var game = message.Content.Substring("!delgame ".Length).ToLower();
                await DelGame(message, game);
            }
            if (message.Content.StartsWith("!pinggame "))
            {
                var game = message.Content.Substring("!pinggame ".Length).ToLower();
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
                var game = message.Content.Substring("!nukegame ".Length);
                await NukeGame(message, game);
            }
            if (message.Author.Id == ASHL && message.Content.StartsWith("!addusergame "))
            {
                var cmd = message.Content.Substring("!addusergame ".Length);
                await AddUserGame(message, cmd);
            }
            if (message.Author.Id == ASHL && message.Content.StartsWith("!delusergame "))
            {
                var cmd = message.Content.Substring("!delusergame ".Length);
                await DelUserGame(message, cmd);
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
            var msg = $"All pingable games (and number of people): {string.Join(", ", GameDict(message).OrderBy(kvp => kvp.Key).Select(kvp => $"{kvp.Key.Replace("@", "@\u200B")} ({kvp.Value.Count})"))}";
            var maxLength = 2000;
            while (msg.Length > 2000)
            {
                var slice = msg.Substring(0, maxLength);
                await message.Channel.SendMessageAsync(slice);
                msg = msg.Substring(maxLength);
            }
            await message.Channel.SendMessageAsync(msg);
        }

        private async Task MyGames(SocketMessage message)
        {
            var gameDict = GameDict(message);
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
                if (!gameDict.TryGetValue(thing[1], out var list))
                {
                    list = gameDict[thing[1]] = new List<ulong>();
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
            }
            else if (!gameDict.TryGetValue(thing[1], out var list))
            {
                await message.Channel.SendMessageAsync("game not found");
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
                    gameDict.Remove(thing[1]);
                }
                SaveDict();
                await Checkmark(message);
            }
        }

        private bool TryParseId(string s, out ulong id) => ulong.TryParse(s, out id) ||
                (s.StartsWith($"<@") && s.EndsWith(">") && ulong.TryParse(s[2..^1], out id)) ||
                (s.StartsWith($"<@!") && s.EndsWith(">") && ulong.TryParse(s[3..^1], out id));
    }
}
