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
        private readonly Config _config;
        private DateTime _nextUpdate;

        public TimingThing(Log log, Config config)
        {
            _log = log;
            _config = config;
        }

        private void SetNextUpdate()
        {
            _nextUpdate = DateTime.UtcNow.Date.AddDays(1);
        }

        public async Task MessageReceivedAsync(SocketMessage message)
        {
            if (DateTime.UtcNow > _nextUpdate && message.Channel is SocketTextChannel messageChannel && messageChannel.Guild.Id == _config.server)
            {
                await DoTimer(messageChannel);
            }
            else if (message.Author.Id == ASHL && message.Content == "!dotimer" && message.Channel is SocketTextChannel messageChannel2 && messageChannel2.Guild.Id == _config.server)
            {
                await DoTimer(messageChannel2);
            }
        }

        private async Task DoTimer(SocketTextChannel messageChannel)
        {
            SetNextUpdate();
            var modchannel = messageChannel.Guild.GetTextChannel(_config.channel);
            await MemberizerCommand.Memberizer(_log, modchannel, 50);
            Console.WriteLine("Trimming...");
            _log.Trim();
            Console.WriteLine("Done trimming. Starting channel trim.");
            var toDelete = await GetMessagesToDelete(modchannel).Distinct().ToListAsync();
            Console.WriteLine($"Deleting {toDelete.Count} messages");
            await modchannel.DeleteMessagesAsync(toDelete);
            Console.WriteLine($"Done");
        }

        private async IAsyncEnumerable<ulong> GetMessagesToDelete(SocketTextChannel channel)
        {
            var limit = 1000;
            var youngest = ulong.MaxValue;
            await foreach (var collection in channel.GetMessagesAsync(limit))
            {
                foreach (var item in collection)
                {
                    yield return item.Id;
                    youngest = Math.Min(youngest, item.Id);
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
