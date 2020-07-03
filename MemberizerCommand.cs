using Discord;
using Discord.WebSocket;
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

        public static async Task Memberizer(Log log, SocketTextChannel channel, ulong desiredCount)
        {
            var guild = channel.Guild;
            var gucciUsers = new Dictionary<SocketGuildUser, ulong>();
            var nonmembers = guild.Users.Where(user => !IsMember(user)).Select(user => user.Id).ToList();
            var counts = log.MessageCounts(nonmembers, desiredCount);
            var msg = string.Join("\n", counts.Select(item => $"{MentionUtils.MentionUser(item.authorId)} has sent {item.count} messages"));
            if (!string.IsNullOrEmpty(msg))
            {
                await channel.SendMessageAsync(msg);
            }
        }

        public async Task MessageReceivedAsync(SocketMessage message)
        {
            if (message.Author.Id == ASHL &&
                message.Content.StartsWith("!memberizer ") &&
                ulong.TryParse(message.Content.Substring("!memberizer ".Length), out var desiredCount) &&
                message.Channel is SocketTextChannel channel)
            {
                await Memberizer(_log, channel, desiredCount);
            }
            if (message.Author.Id == ASHL && message.Content == "!membercount" && message.Channel is SocketGuildChannel ch)
            {
                var roles = string.Join(", ", ch.Guild.Roles.Select(role => $"{role.Name.Replace("@everyone", "at-everyone")}={role.Members.Count()}"));
                var msg = $"total={ch.Guild.MemberCount}, {roles}";
                await message.Channel.SendMessageAsync(msg);
            }
            if (message.Author.Id == ASHL && message.Content == "!roles" && message.Channel is SocketGuildChannel ch2)
            {
                var msg = string.Join(", ", ch2.Guild.Roles.Select(role => $"{role.Name.Replace("@everyone", "at-everyone")}={role.Id}"));
                await message.Channel.SendMessageAsync(msg);
            }
        }

        private static bool IsMember(SocketGuildUser user)
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
