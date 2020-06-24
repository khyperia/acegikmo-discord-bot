using Discord;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace AcegikmoDiscordBot
{
    internal class DeleteEcho
    {
        private readonly Log _log;
        private readonly Config _config;

        public DeleteEcho(Log log, Config config)
        {
            _log = log;
            _config = config;
        }

        public async Task MessageDeletedAsync(Cacheable<IMessage, ulong> messageId, ISocketMessageChannel socket)
        {
            if (socket is IGuildChannel socketGuild && socketGuild.GuildId == _config.server && socket.Id != _config.channel)
            {
                var modchannel = await socketGuild.Guild.GetTextChannelAsync(_config.channel);
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
}
