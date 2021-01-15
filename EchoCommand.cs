using Discord;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;
using static AcegikmoDiscordBot.Program;

namespace AcegikmoDiscordBot
{
    internal class EchoCommand
    {
        public async Task MessageReceivedAsync(SocketMessage message)
        {
            if (message.Author.Id == ASHL && message.Content.StartsWith("!echo "))
            {
                var msg = message.Content.Substring("!echo ".Length).Trim('`');
                await message.Channel.SendMessageAsync(msg);
            }
            if (message.Content.StartsWith("!snowflaketotime ") && ulong.TryParse(message.Content.Substring("!snowflaketotime ".Length), out var snowflake))
            {
                var time = SnowflakeUtils.FromSnowflake(snowflake);
                await message.Channel.SendMessageAsync(time.DateTime.ToString("r"));
            }
            if (message.Content.StartsWith("!timetosnowflake ") && DateTime.TryParse(message.Content.Substring("!timetosnowflake ".Length), out var thingyTime))
            {
                var theSnowflake = SnowflakeUtils.ToSnowflake(thingyTime);
                await message.Channel.SendMessageAsync(theSnowflake.ToString());
            }
        }
    }
}
