using System.Threading;
using System.Threading.Tasks;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace AcegikmoDiscordBot; 

internal class HelpCommand: IResponder<IMessageCreate> {
    private readonly IDiscordRestChannelAPI _discordAPI;
    public HelpCommand(IDiscordRestChannelAPI discordAPI) {
        _discordAPI = discordAPI;
    }
    public async Task<Result> RespondAsync(IMessageCreate message, CancellationToken ct = new CancellationToken()) {
        if (message.Content == "!help")
        {
            await _discordAPI.CreateMessageAsync(message.ChannelID, @"delegate void(); says hi~
!addgame <game> - adds yourself to the list of people to be pinged when <game> is being played
!delgame <game> - removes yourself from the list
!mygames - lists games you're a part of
!pinggame <game> - pings everyone who has added themselves to <game> using !addgame
!games - list all games currently registered in the list of pingable games
!pronoun <she/her, he/him, they/them, him/her/they> - gives you a pronoun role. run again to remove.");
        }
        return Success;
    }
}