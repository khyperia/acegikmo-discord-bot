using Discord.WebSocket;
using System.Threading.Tasks;

namespace AcegikmoDiscordBot;

internal static class HelpCommand
{
    public static async Task MessageReceivedAsync(SocketMessage message)
    {
        if (message.Content == "!help")
        {
            await message.Channel.SendMessageAsync(@"delegate void(); says hi~
!addgame <game> - adds yourself to the list of people to be pinged when <game> is being played
!delgame <game> - removes yourself from the list
!mygames - lists games you're a part of
!pinggame <game> - pings everyone who has added themselves to <game> using !addgame
!games - list all games currently registered in the list of pingable games
!pronoun <she/her, he/him, they/them, him/her/they> - gives you a pronoun role. run again to remove.");
        }
    }
}
