using Discord.WebSocket;
using System.Threading.Tasks;

namespace AcegikmoDiscordBot
{
    internal class HelpCommand
    {
        public async Task MessageReceivedAsync(SocketMessage message)
        {
            if (message.Content == "!help")
            {
                await message.Channel.SendMessageAsync(@"delegate void(); says hi~
!addgame <game> - adds yourself to the list of people to be pinged when <game> is being played
!delgame <game> - removes yourself from the list
!pinggame <game> - pings everyone who has added themselves to <game> using !addgame
!games - list all games currently registered in the list of pingable games");
            }
        }
    }
}
