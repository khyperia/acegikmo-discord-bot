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
                string msg = $"link without tracking blob: <https://twitter.com{uri.AbsolutePath}>";
                await message.Channel.SendMessageAsync(msg);
            }
        }
    }
}
