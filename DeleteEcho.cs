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
    internal class DeleteEcho : IResponder<IMessageDelete>
    {
        private readonly Log _log;
        private readonly IDiscordRestChannelAPI _channelAPI;

        public DeleteEcho(Log log, IDiscordRestChannelAPI channelAPI)
        {
            _log = log;
            _channelAPI = channelAPI;
        }

        public async Task<Result> RespondAsync(IMessageDelete delete, CancellationToken ct) {
            var messageId = delete.ID;
            var channelId = delete.ChannelID;
            var guildId = delete.GuildID;
            if (guildId.HasValue && guildId.IsAcegikmo() && channelId != Settings.DeletedMsgs) {
                var modChannel = Settings.DeletedMsgs;
                if (_log.TryGetMessage(messageId.Value, out var message))
                {
                    var after = _log.TryGetPreviousMessage(messageId.Value, channelId.Value, out var previous)
                        ? $" after <https://discordapp.com/channels/{guildId.Value.Value}/{previous.ChannelId}/{previous.MessageId}>"
                        : "";
                    var toSend = $"Message by {MentionUtils.MentionUser(message.AuthorId)} deleted in {MentionUtils.MentionChannel(message.ChannelId)}{after}:\n{message.Message}";
                    Console.WriteLine(toSend);
                    await _channelAPI.CreateMessageAsync(modChannel, toSend, ct: ct);
                }
                else
                {
                    await _channelAPI.CreateMessageAsync(modChannel, $"Message deleted, but not found in DB: {messageId.Value}", ct: ct);
                }
            }
            return Result.FromSuccess();
        }
    }
}
