using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;

namespace DiscordDeleteEcho
{
    internal class GamesCommand
    {
        private static readonly DataContractJsonSerializer Json = new DataContractJsonSerializer(typeof(Dictionary<string, List<ulong>>));
        private Dictionary<string, List<ulong>> _gameDict;

        public GamesCommand()
        {
            try
            {
                using var stream = File.OpenRead("games.json");
                _gameDict = (Dictionary<string, List<ulong>>)Json.ReadObject(stream);
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("games.json not found, defaulting to empty dict");
                _gameDict = new Dictionary<string, List<ulong>>();
            }
        }

        private void SaveDict()
        {
            using var stream = File.Create("games.json");
            Json.WriteObject(stream, _gameDict);
        }

        public async Task MessageReceivedAsync(SocketMessage message)
        {
            if (message.Content.StartsWith("!addgame "))
            {
                var game = message.Content.Substring("!addgame ".Length).ToLower();
                if (!_gameDict.TryGetValue(game, out var list))
                {
                    list = _gameDict[game] = new List<ulong>();
                }
                if (list.Contains(message.Author.Id))
                {
                    await message.Channel.SendMessageAsync($"You're already in {game}");
                }
                else
                {
                    list.Add(message.Author.Id);
                    SaveDict();
                    if (list.Count == 1)
                    {
                        await message.Channel.SendMessageAsync($"Added you to {game}, which now contains 1 person.");
                    }
                    else
                    {
                        await message.Channel.SendMessageAsync($"Added you to {game}, which now contains {list.Count} people.");
                    }
                }
            }
            if (message.Content.StartsWith("!delgame "))
            {
                var game = message.Content.Substring("!delgame ".Length).ToLower();
                if (!_gameDict.TryGetValue(game, out var list) || !list.Remove(message.Author.Id))
                {
                    await message.Channel.SendMessageAsync($"You are not in the list for {game}");
                }
                else
                {
                    if (list.Count == 0)
                    {
                        _gameDict.Remove(game);
                    }
                    SaveDict();
                    await message.Channel.SendMessageAsync($"You have been removed from {game}");
                }
            }
            if (message.Content.StartsWith("!pinggame "))
            {
                var game = message.Content.Substring("!pinggame ".Length).ToLower();
                if (!_gameDict.TryGetValue(game, out var list) || !list.Contains(message.Author.Id))
                {
                    await message.Channel.SendMessageAsync($"You are not in the list for {game}, so you can't ping it.");
                }
                else if (list.Count == 1)
                {
                    await message.Channel.SendMessageAsync($"You're the only one registered for {game}, sorry :c");
                }
                else
                {
                    await message.Channel.SendMessageAsync($"{message.Author.Mention} wants to play {game}! {string.Join(", ", list.Where(id => id != message.Author.Id).Select(MentionUtils.MentionUser))}");
                }
            }
            if (message.Content == "!games")
            {
                await message.Channel.SendMessageAsync($"All pingable games: {string.Join(", ", _gameDict.Keys)}");
            }
            if (message.Author.Id == 139525105846976512UL && message.Content.StartsWith("!nukegame "))
            {
                var game = message.Content.Substring("!nukegame ".Length);
                _gameDict.Remove(game);
                SaveDict();
                await message.Channel.SendMessageAsync("boom.");
            }
            if (message.Author.Id == 139525105846976512UL && message.Content.StartsWith("!delusergame "))
            {
                var cmd = message.Content.Substring("!delusergame ".Length);
                var thing = cmd.Split(' ', 2);
                if (thing.Length != 2)
                {
                    await message.Channel.SendMessageAsync("!delusergame id game");
                }
                else if (!_gameDict.TryGetValue(thing[1], out var list))
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
                        _gameDict.Remove(thing[1]);
                    }
                    SaveDict();
                    await message.Channel.SendMessageAsync("boom.");
                }
            }
            if (message.Author.Id == 139525105846976512UL && message.Content.StartsWith("!addusergame "))
            {
                var cmd = message.Content.Substring("!addusergame ".Length);
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
                    if (!_gameDict.TryGetValue(thing[1], out var list))
                    {
                        list = _gameDict[thing[1]] = new List<ulong>();
                    }
                    if (list.Contains(id))
                    {
                        await message.Channel.SendMessageAsync("user already in list");
                    }
                    else
                    {
                        list.Add(id);
                        SaveDict();
                        await message.Channel.SendMessageAsync("nyoom.");
                    }
                }
            }
        }

        private bool TryParseId(string s, out ulong id) => ulong.TryParse(s, out id) ||
                (s.StartsWith($"<@") && s.EndsWith(">") && ulong.TryParse(s[2..^1], out id)) ||
                (s.StartsWith($"<@!") && s.EndsWith(">") && ulong.TryParse(s[3..^1], out id));
    }
}
