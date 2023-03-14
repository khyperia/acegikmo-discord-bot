using Discord.WebSocket;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace AcegikmoDiscordBot;

internal static partial class TwitterBonker
{
    [GeneratedRegex("\\bhttps?://twitter\\.com/\\S*/status/\\S*")]
    private static partial Regex MyRegex();

    public static async Task MessageReceivedAsync(SocketMessage message)
    {
        var matches = MyRegex().Matches(message.Content);
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
