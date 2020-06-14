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
        private static readonly Json<Dictionary<ulong, ulong>> Json = new Json<Dictionary<ulong, ulong>>("memberizer.json");

        private const ulong MembersRole = 528976700399419406UL;
        private readonly TimeSpan _saveFrequency = TimeSpan.FromMinutes(10);
        private DateTime _lastSaved = DateTime.UtcNow;

        private void TrySave()
        {
            var now = DateTime.UtcNow;
            if (now - _lastSaved > _saveFrequency)
            {
                _lastSaved = now;
                Json.Save();
            }
        }

        public async Task MessageReceivedAsync(SocketMessage message)
        {
            if (message.Author.Id == ASHL &&
                message.Content == "!memberizer")
            {
                var sum = Json.Data.Values.Sum(v => (long)v);
                var msg = $"{Json.Data.Count} users have sent {sum} messages";
                await message.Channel.SendMessageAsync(msg);
            }
            if (message.Author.Id == ASHL &&
                message.Content.StartsWith("!memberizer ") &&
                ulong.TryParse(message.Content.Substring("!memberizer ".Length), out var desiredCount) &&
                message.Channel is SocketGuildChannel channel)
            {
                var guild = channel.Guild;
                var gucciUsers = new Dictionary<SocketGuildUser, ulong>();
                foreach (var user in guild.Users)
                {
                    if (IsMember(user))
                    {
                        Json.Data.Remove(user.Id);
                    }
                    else if (Json.Data.TryGetValue(user.Id, out var count) && count >= desiredCount)
                    {
                        gucciUsers.Add(user, count);
                    }
                }
                Json.Save();
                _lastSaved = DateTime.UtcNow;
                var msg = string.Join("\n", gucciUsers.OrderBy(kvp => kvp.Value).Select(kvp => $"{kvp.Key.Mention} has sent {kvp.Value} messages"));
                if (!string.IsNullOrEmpty(msg))
                {
                    await message.Channel.SendMessageAsync(msg);
                }
            }
            if (message.Author.Id == ASHL &&
                message.Content == "!memberizer-init" &&
                message.Channel is SocketGuildChannel ch)
            {
                var guild = ch.Guild;
                var numMessages = 0;
                foreach (var ch2 in guild.TextChannels)
                {
                    foreach (var msg in ch2.CachedMessages)
                    {
                        if (msg.Author is SocketGuildUser auth && !IsMember(auth))
                        {
                            Increment(auth.Id);
                            numMessages++;
                        }
                    }
                }
                Json.Save();
                _lastSaved = DateTime.UtcNow;
                var response = $"Added {numMessages} messages to db";
                await message.Channel.SendMessageAsync(response);
            }
            if (message.Author is SocketGuildUser author)
            {
                if (!IsMember(author))
                {
                    Increment(author.Id);
                    TrySave();
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

        private void Increment(ulong userId)
        {
            if (!Json.Data.TryGetValue(userId, out var value))
            {
                value = 0;
            }
            value++;
            Json.Data[userId] = value;
        }
    }
}
