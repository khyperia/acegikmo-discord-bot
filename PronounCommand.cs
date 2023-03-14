using Discord;
using Discord.WebSocket;
using System.Linq;
using System.Threading.Tasks;

namespace AcegikmoDiscordBot;

internal static class PronounCommand
{
    private const ulong SheHer = 506469351713538070U;
    private const ulong HeHim = 506469615841443840U;
    private const ulong TheyThem = 506469646602600459U;
    private const ulong HimHerThey = 583033959378583562U;
    private const ulong FaeFaer = 881143654973075477U;

    public static readonly SlashCommandProperties[] Commands =
    {
        new SlashCommandBuilder()
            .WithName("pronoun")
            .WithDescription("Set your pronoun role")
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("role")
                .WithDescription("Pronouns to set")
                .WithRequired(true)
                .AddChannelType(ChannelType.Text)
                .AddChoice("she/her", "she/her")
                .AddChoice("he/him", "he/him")
                .AddChoice("they/them", "they/them")
                .AddChoice("him/her/they", "him/her/they")
                .AddChoice("fae/faer", "fae/faer")
                .WithType(ApplicationCommandOptionType.String)
            )
            .Build()
    };

    internal static async Task SlashCommandExecuted(SocketSlashCommand command)
    {
        if (command.Data.Name != "pronoun" || command.User is not IGuildUser author ||
            command.Channel is not SocketTextChannel channel)
        {
            return;
        }

        var role = (string)command.Data.Options.First();
        ulong? idNull = role switch
        {
            "she/her" => SheHer,
            "he/him" => HeHim,
            "they/them" => TheyThem,
            "him/her/they" => HimHerThey,
            "fae/faer" => FaeFaer,
            _ => null,
        };
        if (idNull != null)
        {
            var id = idNull.Value;
            if (author.RoleIds.Contains(id))
            {
                await author.RemoveRoleAsync(channel.Guild.GetRole(id));
                await command.RespondAsync("Pronoun role removed", ephemeral: true);
            }
            else
            {
                await author.AddRoleAsync(channel.Guild.GetRole(id));
                await command.RespondAsync("Pronoun role added", ephemeral: true);
            }
        }
    }
}
