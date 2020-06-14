using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static AcegikmoDiscordBot.Program;

namespace AcegikmoDiscordBot
{
    internal class MemberizerCommand
    {
        private const ulong MembersRole = 528976700399419406UL;
        private readonly Log _log;

        public MemberizerCommand(Log log)
        {
            _log = log;
        }

        public async Task MessageReceivedAsync(SocketMessage message)
        {
            if (message.Author.Id == ASHL &&
                message.Content.StartsWith("!memberizer ") &&
                ulong.TryParse(message.Content.Substring("!memberizer ".Length), out var desiredCount) &&
                message.Channel is SocketGuildChannel channel)
            {
                var guild = channel.Guild;
                var gucciUsers = new Dictionary<SocketGuildUser, ulong>();
                var nonmembers = guild.Users.Where(user => !IsMember(user)).Select(user => user.Id).ToList();
                var counts = _log.MessageCounts(nonmembers, desiredCount);
                var msg = string.Join("\n", counts.Select(item => $"{MentionUtils.MentionUser(item.authorId)} has sent {item.count} messages"));
                if (!string.IsNullOrEmpty(msg))
                {
                    await message.Channel.SendMessageAsync(msg);
                }
            }
        }

        private bool IsMember(SocketGuildUser user)
        {
            foreach (var role in user.Roles)
            {
                if (role.Id == MembersRole)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
