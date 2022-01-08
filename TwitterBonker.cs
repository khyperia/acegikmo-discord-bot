using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace AcegikmoDiscordBot;

internal class TwitterBonker
{
    private static readonly Regex regex = new("\\bhttps?://twitter.com/\\S*/status/\\S*");
    private readonly Json<Dictionary<ulong, ulong>> _json = new("twitterbonker.json");

    public async Task MessageReceivedAsync(SocketMessage message)
    {
        var matches = regex.Matches(message.Content);
        foreach (Match match in matches)
        {
            var uri = new Uri(match.Value);
            var queryValues = HttpUtility.ParseQueryString(uri.Query);
            if (queryValues.Get("t") != null || queryValues.Get("s") != null)
            {
                if (!_json.Data.TryGetValue(message.Author.Id, out var count))
                {
                    count = 0;
                }
                count++;
                _json.Data[message.Author.Id] = count;
                _json.Save();

                string msg = count switch
                {
                    < 5 => $"pls remove tracking info from twitter links <3 like so: <https://twitter.com{uri.AbsolutePath}>",
                    < 10 => $"hey, {message.Author.Mention}, it'd be super swell of you to remove tracking info from twitter links before posting next time! <https://twitter.com{uri.AbsolutePath}>",
                    _ => $"{message.Author.Mention}, you've not removed tracking info from twitter links {count} times already, please do it next time maybe? <3 <https://twitter.com{uri.AbsolutePath}>",
                };
                await message.Channel.SendMessageAsync(msg);
            }
        }
    }
}
