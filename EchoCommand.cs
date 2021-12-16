using Discord;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;
using static AcegikmoDiscordBot.Program;

namespace AcegikmoDiscordBot;

internal class EchoCommand
{
    public static async Task MessageReceivedAsync(SocketMessage message)
    {
        if (message.Author.Id == ASHL && message.Content.StartsWith("!echo "))
        {
            var msg = message.Content["!echo ".Length..].Trim('`');
            await message.Channel.SendMessageAsync(msg);
        }
        if (message.Content.StartsWith("!snowflaketotime ") && ulong.TryParse(message.Content["!snowflaketotime ".Length..], out var snowflake))
        {
            var time = SnowflakeUtils.FromSnowflake(snowflake);
            await message.Channel.SendMessageAsync(time.DateTime.ToString("r"));
        }
        if (message.Content.StartsWith("!timetosnowflake ") && DateTime.TryParse(message.Content["!timetosnowflake ".Length..], out var thingyTime))
        {
            var theSnowflake = SnowflakeUtils.ToSnowflake(thingyTime);
            await message.Channel.SendMessageAsync(theSnowflake.ToString());
        }
    }
}
