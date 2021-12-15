using System;
using System.Threading;
using System.Threading.Tasks;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Gateway.Responders;
using Remora.Rest.Core;
using Remora.Results;
using static AcegikmoDiscordBot.Program;

namespace AcegikmoDiscordBot
{
    internal class EchoCommand : IResponder<IMessageCreate> {
        private readonly IDiscordRestChannelAPI _channelAPI;
        public EchoCommand(IDiscordRestChannelAPI channelAPI)
        {
            _channelAPI = channelAPI;
        }
        public async Task<Result> RespondAsync(IMessageCreate message, CancellationToken ct = new CancellationToken()) {
            
            if (message.Author.IsAshl() && message.Content.StartsWith("!echo "))
            {
                var msg = message.Content["!echo ".Length..].Trim('`');
                await _channelAPI.CreateMessageAsync(message.ChannelID, msg, ct: ct);
            }
            if (message.Content.StartsWith("!snowflaketotime ") && ulong.TryParse(message.Content["!snowflaketotime ".Length..], out var snowflake)) {
                var sf = new Snowflake(snowflake);
                var time = sf.Timestamp;
                await _channelAPI.CreateMessageAsync(message.ChannelID, time.DateTime.ToString("r"), ct: ct);
            }
            if (message.Content.StartsWith("!timetosnowflake ") && DateTime.TryParse(message.Content["!timetosnowflake ".Length..], out var thingyTime))
            {
                var theSnowflake = Snowflake.CreateTimestampSnowflake(thingyTime);
                await _channelAPI.CreateMessageAsync(message.ChannelID, theSnowflake.ToString());
            }
            return Result.FromSuccess();
        }
    }
}
