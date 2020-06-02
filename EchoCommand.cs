using Discord.WebSocket;
using System.Threading.Tasks;

namespace AcegikmoDiscordBot
{
    internal class EchoCommand
    {
        public async Task MessageReceivedAsync(SocketMessage message)
        {
            if (message.Author.Id == 139525105846976512UL && message.Content.StartsWith("!echo "))
            {
                var msg = message.Content.Substring("!echo ".Length).Trim('`');
                await message.Channel.SendMessageAsync(msg);
            }
        }
    }
}
