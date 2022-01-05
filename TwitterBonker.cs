using Discord.WebSocket;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace AcegikmoDiscordBot;

internal class TwitterBonker
{
    private static readonly Regex regex = new("\\bhttps?://twitter.com/\\S*/status/\\S*");

    public static async Task MessageReceivedAsync(SocketMessage message)
    {
        var matches = regex.Matches(message.Content);
        foreach (Match match in matches)
        {
            var uri = new Uri(match.Value);
            var queryValues = HttpUtility.ParseQueryString(uri.Query);
            if (queryValues.Get("t") != null || queryValues.Get("s") != null)
            {
                var msg = $"pls remove tracking info from twitter links <3 like so: <https://twitter.com{uri.AbsolutePath}>";
                await message.Channel.SendMessageAsync(msg);
            }
        }
    }
}
