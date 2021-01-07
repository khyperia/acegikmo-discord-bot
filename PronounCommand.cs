using Discord;
using Discord.WebSocket;
using System.Linq;
using System.Threading.Tasks;
using static AcegikmoDiscordBot.Program;

namespace AcegikmoDiscordBot
{
    internal class PronounCommand
    {
        private static readonly ulong SheHer = 506469351713538070U;
        private static readonly ulong HeHim = 506469615841443840U;
        private static readonly ulong TheyThem = 506469646602600459U;
        private static readonly ulong HimHerThey = 583033959378583562U;

        public async Task MessageReceivedAsync(SocketMessage message)
        {
            if (message.Content == "!pronoun" &&
                message.Channel is SocketTextChannel ch &&
                ch.Guild.Id == ACEGIKMO_SERVER)
            {
                await message.Channel.SendMessageAsync("Available pronouns: she/her, he/him, they/them, him/her/they. Example:\n!pronoun she/her");
            }
            if (message.Content.StartsWith("!pronoun ") &&
                message.Author is IGuildUser author &&
                message.Channel is SocketTextChannel channel &&
                channel.Guild.Id == ACEGIKMO_SERVER)
            {
                var role = message.Content.Substring("!pronoun ".Length).ToLower();
                ulong? idNull = role switch
                {
                    "she/her" => SheHer,
                    "he/him" => HeHim,
                    "they/them" => TheyThem,
                    "him/her/they" => HimHerThey,
                    _ => null,
                };
                if (idNull != null)
                {
                    var id = idNull.Value;
                    if (author.RoleIds.Contains(id))
                    {
                        await author.RemoveRoleAsync(channel.Guild.GetRole(id));
                        await GamesCommand.CrossReact(message);
                    }
                    else
                    {
                        await author.AddRoleAsync(channel.Guild.GetRole(id));
                        await GamesCommand.Checkmark(message);
                    }
                }
            }
        }
    }
}
