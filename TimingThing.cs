using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static AcegikmoDiscordBot.Program;

namespace AcegikmoDiscordBot
{
    internal class TimingThing
    {
        private readonly Log _log;
        private const ulong LEWD_CHANNEL = 674327876669014078UL;
        private DateTime _nextUpdate;

        public TimingThing(Log log)
        {
            _log = log;
            SetNextUpdate();
        }

        private void SetNextUpdate()
        {
            _nextUpdate = DateTime.UtcNow.Date.AddDays(1);
        }

        public async Task MessageReceivedAsync(SocketMessage message)
        {
            if (DateTime.UtcNow > _nextUpdate && message.Channel is SocketTextChannel messageChannel && messageChannel.Guild.Id == ACEGIKMO_SERVER)
            {
                await DoTimer(messageChannel);
            }
            else if (message.Author.Id == ASHL && message.Content == "!dotimer" && message.Channel is SocketTextChannel messageChannel2 && messageChannel2.Guild.Id == ACEGIKMO_SERVER)
            {
                await DoTimer(messageChannel2);
            }
        }

        private async Task DoTimer(SocketTextChannel messageChannel)
        {
            var lewdchannel = messageChannel.Guild.GetTextChannel(LEWD_CHANNEL);
            await lewdchannel.SendMessageAsync("Please make sure you've read the topic of this channel, as this channel is \U0001F525*spicy*\U0001F525 and it's *important*.");
            SetNextUpdate();
            var modchannel = messageChannel.Guild.GetTextChannel(ACEGIKMO_DELETED_MESSAGES);
            await MemberizerCommand.Memberizer(_log, modchannel, 50);
            Console.WriteLine("Trimming...");
            _log.Trim();
            Console.WriteLine("Done trimming. Starting channel trim.");
            var toDelete = await GetMessagesToDelete(modchannel).Distinct().ToListAsync();
            Console.WriteLine($"Deleting {toDelete.Count} messages");
            DateTimeOffset twoWeeks = DateTime.UtcNow.AddDays(-13);
            await modchannel.DeleteMessagesAsync(toDelete.Where(item => SnowflakeUtils.FromSnowflake(item) > twoWeeks));
            var others = toDelete.Where(item => SnowflakeUtils.FromSnowflake(item) <= twoWeeks).ToList();
            if (others.Count > 0)
            {
                Console.WriteLine($"Done deleting bulk, now deleting others: {others.Count}");
                var i = 0;
                foreach (var thing in others)
                {
                    await modchannel.DeleteMessageAsync(thing);
                    await Task.Delay(1000);
                    Console.WriteLine($"{i++}/{others.Count}");
                }
            }
            Console.WriteLine($"Done");
        }

        private static async IAsyncEnumerable<ulong> GetMessagesToDelete(SocketTextChannel channel)
        {
            DateTimeOffset timeLimit = DateTime.UtcNow.AddDays(-7);
            var limit = 1000;
            var youngest = ulong.MaxValue;
            {
                var count = 0;
                await foreach (var collection in channel.GetMessagesAsync(limit))
                {
                    count++;
                    foreach (var item in collection)
                    {
                        if (item.CreatedAt < timeLimit)
                        {
                            yield return item.Id;
                        }
                        youngest = Math.Min(youngest, item.Id);
                    }
                }
                if (count != limit)
                {
                    Console.WriteLine($"Done yielding items in the first batch ({count} != {limit})");
                    yield break;
                }
            }
            for (var i = 0; ; i++)
            {
                var count = 0;
                await foreach (var collection in channel.GetMessagesAsync(youngest, Discord.Direction.Before, limit))
                {
                    foreach (var item in collection)
                    {
                        count++;
                        if (item.CreatedAt < timeLimit)
                            yield return item.Id;
                        youngest = Math.Min(youngest, item.Id);
                    }
                }
                if (count != limit)
                {
                    Console.WriteLine($"Done yielding items after {i + 1} iters ({count} != {limit})");
                    break;
                }
            }
        }
    }
}
