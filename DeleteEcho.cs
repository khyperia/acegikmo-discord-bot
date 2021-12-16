using Discord;
using System;
using System.Threading.Tasks;
using static AcegikmoDiscordBot.Program;

namespace AcegikmoDiscordBot;

internal class DeleteEcho
{
    private readonly Log _log;

    public DeleteEcho(Log log)
    {
        _log = log;
    }

    public async Task MessageDeletedAsync(Cacheable<IMessage, ulong> messageId, Cacheable<IMessageChannel, ulong> socketCached)
    {
        var socket = await socketCached.GetOrDownloadAsync();
        if (socket is IGuildChannel socketGuild && socketGuild.GuildId == ACEGIKMO_SERVER && socket.Id != ACEGIKMO_DELETED_MESSAGES)
        {
            var modchannel = await socketGuild.Guild.GetTextChannelAsync(ACEGIKMO_DELETED_MESSAGES);
            if (_log.TryGetMessage(messageId.Id, out var message))
            {
                var after = _log.TryGetPreviousMessage(messageId.Id, socket.Id, out var previous)
                    ? $" after <https://discordapp.com/channels/{socketGuild.GuildId}/{previous.ChannelId}/{previous.MessageId}>"
                    : "";
                var toSend = $"Message by {MentionUtils.MentionUser(message.AuthorId)} deleted in {MentionUtils.MentionChannel(message.ChannelId)}{after}:\n{message.Message}";
                Console.WriteLine(toSend);
                await modchannel.SendMessageAsync(toSend);
            }
            else
            {
                await modchannel.SendMessageAsync($"Message deleted, but not found in DB: {messageId.Id}");
            }
        }
    }
}
