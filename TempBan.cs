using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Conditions;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Services;
using Remora.Discord.Gateway.Responders;
using Remora.Rest.Core;
using Remora.Results;

namespace AcegikmoDiscordBot;


[DiscordDefaultPermission(false)]
public class TempBan: CommandGroup, IResponder<IInteractionCreate> {
	private readonly IDiscordRestChannelAPI _discordAPI;
	private readonly IDiscordRestGuildAPI _guildAPI;
	private readonly ContextInjectionService _commandContext;
	private readonly IDiscordRestInteractionAPI _interactionApi;
	private readonly IDiscordRestChannelAPI _channelApi;
	
	private readonly Json<Dictionary<ulong, (DateTime, string)>> _banList = new("banned.json");

	
	public TempBan(IDiscordRestChannelAPI discordApi, IDiscordRestGuildAPI guildApi, 
		ContextInjectionService commandContext, IDiscordRestInteractionAPI interactionApi, IDiscordRestChannelAPI channelApi) {
		_discordAPI = discordApi;
		_guildAPI = guildApi;
		_commandContext = commandContext;
		_interactionApi = interactionApi;
		_channelApi = channelApi;
	}
	
	[Command("yeet")]
	[Ephemeral]
	[CommandType(ApplicationCommandType.User)]
	[RequireDiscordPermission(DiscordPermission.BanMembers)]
	public async Task<Result> Yeet() {
		if(_commandContext.Context is not InteractionContext context) return Result.FromSuccess();
		if(!context.Member.HasValue) return Result.FromSuccess();
		if (!context.Member.Value.Permissions.Value.HasPermission(DiscordPermission.BanMembers)) {
			await _interactionApi.CreateFollowupMessageAsync(context.ApplicationID, context.Token, "You dont have the permission to ban people :V");
			return Result.FromSuccess();
		}
		
		var userToBan = context.Data.Resolved.Value.Users.Value.Keys.First();

		var components = new[] {
			new ActionRowComponent(new [] {
				new ButtonComponent(ButtonComponentStyle.Danger, "yeet", default, $"yeet|{userToBan.Value}")
			}),
		};

		await _interactionApi.CreateFollowupMessageAsync(context.ApplicationID, context.Token, 
			$"Are you sure you want to yeet {MentionUtils.MentionUser(userToBan.Value)} and delete their latest messages?", components:components);
		
		return Result.FromSuccess();
	}

	[Command("ban")]
	[Ephemeral]
	public async Task<Result> Ban(IUser user, int days, [Greedy]string reason) {
		if(_commandContext.Context is not InteractionContext context) return Result.FromSuccess();
		if(!context.Member.HasValue) return Result.FromSuccess();
		if (!context.Member.Value.Permissions.Value.HasPermission(DiscordPermission.BanMembers)) {
			await _interactionApi.CreateFollowupMessageAsync(context.ApplicationID, context.Token, "You dont have the permission to ban people :V");
			return Result.FromSuccess();
		}
		
		var components = new[] {
			new ActionRowComponent(new [] {
				new ButtonComponent(ButtonComponentStyle.Danger, "ban", default, $"ban|{user.ID.Value}|{days}|{reason}"),
			}),
		};
		
		await _interactionApi.CreateFollowupMessageAsync(context.ApplicationID, context.Token, 
			$"Are you sure you want to ban {MentionUtils.MentionUser(user.ID.Value)} for {days} days because of \"{reason}\"?", 
			components:components);
		
		return Result.FromSuccess();
	}

	public async Task<Result> RespondAsync(IInteractionCreate interaction, CancellationToken ct = new()) {
		if(!interaction.Data.HasValue || !interaction.Member.HasValue) return Result.FromSuccess();
		var data = interaction.Data.Value;
		var member = interaction.Member.Value;
		var matchYeet = Regex.Match(data.CustomID.Value, @"^yeet\|([0-9]+)$");
		
		if (matchYeet.Success) {
			if (!member.Permissions.Value.HasPermission(DiscordPermission.BanMembers)) {
				await _interactionApi.CreateFollowupMessageAsync(interaction.ApplicationID, interaction.Token, 
					"You dont have the permission to ban people (and how did you even access this menu?)");
				return Result.FromSuccess();
			}
			var victim = ulong.Parse(matchYeet.Groups[1].Value);
			await _guildAPI.CreateGuildBanAsync(Program.Settings.Server, new Snowflake(victim), 1, "spam");
			await _interactionApi.CreateInteractionResponseAsync(interaction.ID, interaction.Token,
				new InteractionResponse(InteractionCallbackType.ChannelMessageWithSource, 
					new InteractionCallbackData(Content: $"{MentionUtils.MentionUser(victim)} was removed and their last day of messages deleted")));
			return Result.FromSuccess();
		}

		var matchBan = Regex.Match(data.CustomID.Value, @"^ban\|([0-9]+)\|([0-9]+)\|(.*)$");
		if (matchBan.Success) {
			var user = ulong.Parse(matchBan.Groups[1].Value);
			var time = int.Parse(matchBan.Groups[2].Value);
			var reason = matchBan.Groups[3].Value;

			var bannedUntil = DateTime.Now.AddDays(time);
			
			_banList.Data[user] = (bannedUntil, reason);
			_banList.Save();
			
			await _guildAPI.CreateGuildBanAsync(Program.Settings.Server, new Snowflake(user), 
				0, "banned for {time} days because of: {reason}");
			
			await _interactionApi.CreateInteractionResponseAsync(interaction.ID, interaction.Token,
				new InteractionResponse(InteractionCallbackType.ChannelMessageWithSource, 
					new InteractionCallbackData(Content: $"{MentionUtils.MentionUser(user)} was banned for {time} days " +
					                                     $"because: {reason}")));
		}
		
		return Result.FromSuccess();
	}

	public async Task CheckBans() {
		var now = DateTime.Now;
		foreach ((ulong user, (DateTime unbanTime, string reason)) in _banList.Data) {
			if (unbanTime < now) {
				await _guildAPI.RemoveGuildBanAsync(Program.Settings.Server, new Snowflake(user), $"Temp ban for '{reason}' ended");
				await _channelApi.CreateMessageAsync(Program.Settings.DeletedMsgs,
					$"{MentionUtils.MentionUser(user)}, who was previously banned for '{reason}', was unbanned");
				_banList.Data.Remove(user);
				_banList.Save();
			}
		}
	}
}