using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static AcegikmoDiscordBot.Program;

namespace AcegikmoDiscordBot;

internal class TmpBan
{
    private readonly Json<Dictionary<ulong, long>> BanData = new("tmpban.json");

    public static SlashCommandProperties[] Commands = {
        new SlashCommandBuilder()
            .WithName("tmpban")
            .WithDescription("Temporarily ban a user")
            .AddOption("user", ApplicationCommandOptionType.User, "The user to ban", isRequired: true)
            .AddOption("days", ApplicationCommandOptionType.Integer, "The number of days to ban for", isRequired: true)
            .Build(),
        new SlashCommandBuilder()
            .WithName("tmpbans")
            .WithDescription("List temporary bans currently active")
            .Build(),
    };


    internal async Task SlashCommandExecuted(SocketSlashCommand command)
    {
        switch (command.Data.Name)
        {
            case "tmpban":
                await DoTmpBan(command);
                break;
            case "tmpbans":
                await ListTmpBans(command);
                break;
        }
    }

    private async Task DoTmpBan(SocketSlashCommand command)
    {
        if (command.Channel is not IGuildChannel channel || command.User is not SocketGuildUser sender)
        {
            return;
        }
        if (!sender.Roles.Any(role => role.Id == MODERATOR_ROLE))
        {
            await command.RespondAsync("noh >:(", ephemeral: true);
            return;
        }

        var options = command.Data.Options.ToArray();
        var bannee = (SocketGuildUser)options[0].Value;
        await bannee.BanAsync(0, "temp banned by slash command");
        var days = (int)options[1].Value;
        var newTime = DateTimeOffset.UtcNow.AddDays(days);
        string message;
        if (BanData.Data.TryGetValue(bannee.Id, out var existingTimeInt))
        {
            var existingTime = DateTimeOffset.FromUnixTimeSeconds(existingTimeInt);
            message = $"{bannee.Mention} was already banned until {existingTime}, but that has been updated to {newTime}";
        }
        else
        {
            message = $"{bannee.Mention} has been banned until (the UTC midnight after) {newTime}";
        }
        await command.RespondAsync(message, ephemeral: true);
        var modchannel = await channel.Guild.GetTextChannelAsync(ACEGIKMO_MOD_LOUNGE);
        await modchannel.SendMessageAsync(message + $" by {command.User.Mention}");
        BanData.Data[bannee.Id] = newTime.ToUnixTimeSeconds();
        BanData.Save();
    }

    private async Task ListTmpBans(SocketSlashCommand command)
    {
        if (command.User is not SocketGuildUser sender)
        {
            return;
        }
        if (!sender.Roles.Any(role => role.Id == MODERATOR_ROLE))
        {
            await command.RespondAsync("noh >:(", ephemeral: true);
            return;
        }

        var messageList = BanData.Data.Select(kvp => $"{MentionUtils.MentionUser(kvp.Key)} until {DateTimeOffset.FromUnixTimeSeconds(kvp.Value)}");
        var message = string.Join(", ", messageList);
        if (message.Length == 0)
        {
            await command.RespondAsync("Nobody's temp banned!", ephemeral: true);
        }
        else
        {
            await command.RespondAsync(message, ephemeral: true);
        }
    }

    public async Task Unban(SocketGuild acegikmo)
    {
        var modchannel = acegikmo.GetTextChannel(ACEGIKMO_MOD_LOUNGE);
        var now = DateTimeOffset.UtcNow;
        var unbannees = BanData.Data.Where(kvp => DateTimeOffset.FromUnixTimeSeconds(kvp.Value) < now).Select(kvp => kvp.Key).ToList();
        foreach (var unbannee in unbannees)
        {
            await modchannel.SendMessageAsync($"{MentionUtils.MentionUser(unbannee)} has been unbanned");
            BanData.Data.Remove(unbannee);
            await acegikmo.RemoveBanAsync(unbannee);
        }
        if (unbannees.Count > 0)
        {
            BanData.Save();
        }
    }
}
