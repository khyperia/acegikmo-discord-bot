using Discord;
using Discord.WebSocket;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AcegikmoDiscordBot;

internal static class EchoCommand
{
    public static readonly SlashCommandProperties[] Commands =
    {
        // cant do ApplicationCommandOptionType.Integer because it doesn't support 64 bit ints
        new SlashCommandBuilder()
            .WithName("snowflaketotime")
            .WithDescription("Convert snowflake to time")
            .AddOption("snowflake", ApplicationCommandOptionType.String, "The snowflake", isRequired: true)
            .Build(),
        new SlashCommandBuilder()
            .WithName("timetosnowflake")
            .AddOption("time", ApplicationCommandOptionType.String, "The time", isRequired: true)
            .WithDescription("Convert a time to a snowflake")
            .Build(),
    };

    internal static async Task SlashCommandExecuted(SocketSlashCommand command)
    {
        var options = command.Data.Options.ToArray();
        switch (command.Data.Name)
        {
            case "snowflaketotime":
                try
                {
                    var snowflake = Convert.ToUInt64(options[0].Value);
                    var time = SnowflakeUtils.FromSnowflake(snowflake);
                    await command.RespondAsync(time.DateTime.ToString("r"));
                }
                catch
                {
                    await command.RespondAsync("Invalid number");
                }

                break;
            case "timetosnowflake":
                if (DateTime.TryParse(Convert.ToString(options[0].Value), out var thingyTime))
                {
                    var theSnowflake = SnowflakeUtils.ToSnowflake(thingyTime);
                    await command.RespondAsync(theSnowflake.ToString());
                }
                else
                {
                    await command.RespondAsync("Invalid date");
                }

                break;
        }
    }
}
