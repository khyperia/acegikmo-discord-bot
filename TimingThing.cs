using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Gateway.Responders;
using Remora.Rest.Core;
using Remora.Results;
using static AcegikmoDiscordBot.Program;

namespace AcegikmoDiscordBot; 

internal class TimingThing: IResponder<IMessageCreate> {
    private readonly Log _log;
    private readonly IDiscordRestChannelAPI _channelAPI;
    private readonly MemberizerCommand _memberizer;
        
    private DateTime _nextUpdate;

    public TimingThing(Log log, IDiscordRestChannelAPI channelAPI, MemberizerCommand memberizer) {
        _log = log;
        _channelAPI = channelAPI;
        _memberizer = memberizer;
        SetNextUpdate();
    }

    private void SetNextUpdate() {
        _nextUpdate = DateTime.UtcNow.Date.AddDays(1);
    }

    public async Task<Result> RespondAsync(IMessageCreate message, CancellationToken ct = new()) {
        if (DateTime.UtcNow > _nextUpdate && message.GuildID.HasValue && message.GuildID.IsAcegikmo()) {
            await DoTimer();
        }
        else if (message.Author.IsAshl() && message.Content == "!dotimer" && message.GuildID.IsAcegikmo()) {
            await DoTimer();
        }
        return Result.FromSuccess();
    }

    private async Task DoTimer() {
        var lewdchannel = Settings.Lewd;
        await _channelAPI.CreateMessageAsync(lewdchannel, "Please make sure you've read the topic of this channel, as this channel is \U0001F525*spicy*\U0001F525 and it's *important*.");
        SetNextUpdate();
        var modchannel = Settings.DeletedMsgs;
        await _memberizer.Memberizer(_log, modchannel, Settings.Server, 50);
        Console.WriteLine("Trimming...");
        _log.Trim();
        Console.WriteLine("Done trimming. Starting channel trim.");
        var toDelete = await GetMessagesToDelete(modchannel);
        Console.WriteLine($"Deleting {toDelete} messages");
        DateTimeOffset twoWeeks = DateTime.UtcNow.AddDays(-13);
        await _channelAPI.BulkDeleteMessagesAsync(modchannel, toDelete.Where(item => item.Timestamp > twoWeeks).ToList());
        var others = toDelete.Where(item => item.Timestamp <= twoWeeks).ToList();
        if (others.Count > 0) {
            Console.WriteLine($"Done deleting bulk, now deleting others: {others.Count}");
            var i = 0;
            foreach (var thing in others) {
                await _channelAPI.DeleteMessageAsync(modchannel, thing);
                await Task.Delay(1000);
                Console.WriteLine($"{i++}/{others.Count}");
            }
        }
        Console.WriteLine($"Done");
    }

    private async Task<List<Snowflake>> GetMessagesToDelete(Snowflake channel) {
        var toDelete = new List<Snowflake>();
        DateTimeOffset timeLimit = DateTime.UtcNow.AddDays(-7);
        var limit = 1000;
        var youngest = ulong.MaxValue;
        {
            var count = 0;
            var messages = await _channelAPI.GetChannelMessagesAsync(channel, limit: limit);
            foreach (var message in messages.Entity) {
                count++;
                if (message.Timestamp < timeLimit)
                {
                    toDelete.Add(message.ID);
                }
                youngest = Math.Min(youngest, message.ID.Value);
            }
            if (count != limit) {
                Console.WriteLine($"Done yielding items in the first batch ({count} != {limit})");
                return toDelete;
            }
        }
        for (var i = 0; ; i++) {
            var count = 0;
            var messages = await _channelAPI.GetChannelMessagesAsync(channel, before: new Snowflake(youngest), limit: limit);
            foreach (var message in messages.Entity) {
                count++;
                if (message.Timestamp < timeLimit)
                    toDelete.Add(message.ID);
                youngest = Math.Min(youngest, message.ID.Value);
            }
            if (count != limit) {
                Console.WriteLine($"Done yielding items after {i + 1} iters ({count} != {limit})");
                break;
            }
        }
        return toDelete;
    }
}