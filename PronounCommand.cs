using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Discord.Commands.Services;
using Remora.Discord.Gateway.Responders;
using Remora.Rest.Core;
using Remora.Results;
using static AcegikmoDiscordBot.Program;

namespace AcegikmoDiscordBot
{
    internal class PronounCommand: CommandGroup, IResponder<IMessageCreate>, IResponder<IInteractionCreate>
    {
        private readonly IDiscordRestChannelAPI _channelApi;
        private readonly IDiscordRestGuildAPI _guildApi;
        private readonly GamesCommand _gamesCommand;
        private readonly ContextInjectionService _context;
        private readonly FeedbackService _feedback;
        private readonly IDiscordRestInteractionAPI _interactionApi;

        private static string pronounMenu = "pronoun_menu";

        public PronounCommand(IDiscordRestChannelAPI channelApi, IDiscordRestGuildAPI guildApi, 
                GamesCommand gamesCommand, ContextInjectionService  context, FeedbackService feedbackService,
                IDiscordRestInteractionAPI interactionApi) {
            _channelApi = channelApi;
            _guildApi = guildApi;
            _gamesCommand = gamesCommand;
            _context = context;
            _feedback = feedbackService;
            _interactionApi = interactionApi;
        }

        [Command("pronoun")]
        [Ephemeral]
        [Description("Lets you choose your pronoun roles")]
        public async Task<Result> SlashCommand() {
            if(_context.Context is not InteractionContext context) return Result.FromSuccess();
            var member = await _guildApi.GetGuildMemberAsync(context.GuildID.Value, context.User.ID);
            var userRoles = member.Entity.Roles;
            IReadOnlyList<IMessageComponent> components = GetMenuForRoles(userRoles);
            var message = await _interactionApi.CreateFollowupMessageAsync(
                context.ApplicationID, 
                context.Token, 
                "choose your pronoun roles!\n*(ping moderators if you want more pronouns to be added)*",
                components: new Optional<IReadOnlyList<IMessageComponent>>(components));//???
            if (!message.IsSuccess) {
                return Result.FromError(message.Error);
            }
            return Result.FromSuccess();
        }
        
        public async Task<Result> RespondAsync(IInteractionCreate interaction, CancellationToken ct = new()) {
            if(!interaction.Data.HasValue || !interaction.Member.HasValue) return Result.FromSuccess();
            var data = interaction.Data.Value;
            var member = interaction.Member.Value;
            if (data.CustomID != pronounMenu) return Result.FromSuccess();
            var memberRoles = (List<Snowflake>)member.Roles;
            foreach ((string name, ulong id) in Settings.pronouns) {
                var roleSf = new Snowflake(id);
                var hasRole = memberRoles.Contains(roleSf);
                var wantsRole = data.Values.Value.Contains(name);
                if (hasRole != wantsRole) {
                    if (wantsRole) {
                        await _guildApi.AddGuildMemberRoleAsync(Settings.Server, member.User.Value.ID, roleSf);
                        memberRoles.Add(roleSf);
                    } else {
                        await _guildApi.RemoveGuildMemberRoleAsync(Settings.Server, member.User.Value.ID, roleSf);
                        memberRoles.Remove(roleSf);
                    } 
                }
            }
            
            //ack
            var components = GetMenuForRoles(memberRoles);
            await _interactionApi.CreateInteractionResponseAsync(interaction.ID, interaction.Token, 
                new InteractionResponse(InteractionCallbackType.UpdateMessage, new InteractionCallbackData
                (Components: new Optional<IReadOnlyList<IMessageComponent>>(components))));
            return Result.FromSuccess();
        }

        public IReadOnlyList<IMessageComponent> GetMenuForRoles(IReadOnlyList<Snowflake> userRoles) {
            var components = new [] {
                new ActionRowComponent(new [] {
                    new SelectMenuComponent(pronounMenu, Settings.pronouns.Select(
                            pronoun => new SelectOption(pronoun.Key, pronoun.Key, default, default,
                                IsDefault: userRoles.Contains(new Snowflake(pronoun.Value)))).ToArray(), 
                        default, 0, Settings.pronouns.Count, false)
                }),
            };
            return components;
        }

        public async Task<Result> RespondAsync(IMessageCreate message, CancellationToken ct = new())
        {
            if (message.Content == "!pronoun" && message.GuildID.IsAcegikmo()) {
                await _channelApi.CreateMessageAsync(message.ChannelID,
                    "Available pronouns: she/her, he/him, they/them, him/her/they, fae/faer. Example:\n!pronoun she/her");
            }
            if (message.Content.StartsWith("!pronoun ") && message.GuildID.IsAcegikmo())
            {
                var role = message.Content["!pronoun ".Length..].ToLower().Trim();

                Snowflake? idNull = Settings.MatchPronoun(role);
                if (idNull != null) {
                    var member = await _guildApi.GetGuildMemberAsync(message.GuildID.Value, message.Author.ID);
                    var id = idNull.Value;
                    if (member.Entity.Roles.Contains(id))
                    {
                        await _guildApi.RemoveGuildMemberRoleAsync(message.GuildID.Value, message.Author.ID, id);
                        await _gamesCommand.CrossReact(message);
                    }
                    else
                    {
                        await _guildApi.AddGuildMemberRoleAsync(message.GuildID.Value, message.Author.ID, id);
                        await _gamesCommand.Checkmark(message);
                    }
                }
            }
            return Result.FromSuccess();
        }
    }
}
