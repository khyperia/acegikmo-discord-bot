using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Gateway.Responders;
using Remora.Rest.Core;
using Remora.Results;
using static AcegikmoDiscordBot.Program;

namespace AcegikmoDiscordBot
{
    internal class MemberizerCommand : IResponder<IMessageCreate>
    {
        private const ulong MembersRole = 528976700399419406UL;
        
        private readonly Log _log;
        private readonly IDiscordRestChannelAPI _channelApi;
        private readonly IDiscordRestGuildAPI _guildApi;
        private readonly IDiscordRestUserAPI _userApi;
        
        public MemberizerCommand(Log log, IDiscordRestChannelAPI channelApi, IDiscordRestGuildAPI guildApi, IDiscordRestUserAPI userApi) {
            _log = log;
            _channelApi = channelApi;
            _guildApi = guildApi;
            _userApi = userApi;
        }

        public async Task Memberizer(Log log, Snowflake channel, Snowflake guild, ulong desiredCount) {
            var members = await _guildApi.ListGuildMembersAsync(guild);
            var nonmembers = members.Entity.Where(user => !IsMember(user)).Select(user => user.User.Value.ID.Value).ToList();
            var counts = log.MessageCounts(nonmembers, desiredCount);
            var msg = string.Join("\n", counts.Select(item => $"{MentionUtils.MentionUser(item.authorId)} has sent {item.count} messages"));
            if (!string.IsNullOrEmpty(msg)) {
                await _channelApi.CreateMessageAsync(channel, msg);
            }
        }

        public async Task<Result> RespondAsync(IMessageCreate message, CancellationToken ct = new()) {
            if (message.Author.IsAshl() && message.Content.StartsWith("!memberizer ") &&
                    ulong.TryParse(message.Content["!memberizer ".Length..], out var desiredCount)) {
                await Memberizer(_log, message.ChannelID, message.GuildID.Value, desiredCount);
            }
            if (message.Author.IsAshl() && message.Content == "!membercount") {
                var roles = await _guildApi.GetGuildRolesAsync(message.GuildID.Value);
                var users = await _guildApi.ListGuildMembersAsync(message.GuildID.Value);
                var roleList = string.Join(", ", roles.Entity.Select(role => {
                    var count = users.Entity.Count(user => user.Roles.Contains(role.ID));
                    return $"{role.Name.Replace("@everyone", "at-everyone")}={count}";
                }));
                var msg = $"total={users.Entity.Count}, {roleList}";
                await _channelApi.CreateMessageAsync(message.ChannelID, msg);
            }
            if (message.Author.IsAshl() && message.Content == "!roles") {
                var roles = await _guildApi.GetGuildRolesAsync(message.GuildID.Value);
                var msg = string.Join(", ", roles.Entity.Select(role => $"{role.Name.Replace("@everyone", "at-everyone")}={role.ID.Value}"));
                await _channelApi.CreateMessageAsync(message.ChannelID, msg);
            }
            return Result.FromSuccess();
        }

        private bool IsMember(IGuildMember user) {
            return user.Roles.Any(role => role.Value == MembersRole);
        }
    }
}
