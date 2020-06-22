using Discord;
using Discord.WebSocket;
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
            if (message.Author.Id == ASHL && message.Content.StartsWith("!snowflaketotime ") && ulong.TryParse(message.Content.Substring("!snowflaketotime ".Length), out var snowflake))
            {
                var time = SnowflakeUtils.FromSnowflake(snowflake);
                await message.Channel.SendMessageAsync(time.DateTime.ToString("r"));
            }
        }
    }
}
