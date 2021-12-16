using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Services;
using Remora.Results;

namespace AcegikmoDiscordBot; 

[Group("opt-out")]
public class OptOut : CommandGroup {
	
	private readonly IDiscordRestChannelAPI _discordAPI;
	private readonly IDiscordRestGuildAPI _guildAPI;
	private readonly ContextInjectionService _commandContext;
	private readonly IDiscordRestInteractionAPI _interactionApi;
	private readonly IDiscordRestChannelAPI _channelApi;
	
	public OptOut(IDiscordRestChannelAPI discordApi, IDiscordRestGuildAPI guildApi, 
			ContextInjectionService commandContext, IDiscordRestInteractionAPI interactionApi, IDiscordRestChannelAPI channelApi) {
		_discordAPI = discordApi;
		_guildAPI = guildApi;
		_commandContext = commandContext;
		_interactionApi = interactionApi;
		_channelApi = channelApi;
	}

	[Command("hide")]
	[Ephemeral]
	[Description("bans you from seeing one channel")]
	public async Task<Result> Out(IChannel channel) {
		if(_commandContext.Context is not InteractionContext context) return Result.FromSuccess();
		if(!context.Member.HasValue) return Result.FromSuccess();
		var member = context.Member.Value;

		if (channel.Type != ChannelType.GuildText) {
			await _interactionApi.CreateFollowupMessageAsync(context.ApplicationID, context.Token, "Thats not a text channel.");
			return Result.FromSuccess();
		}

		if (channel.PermissionOverwrites.HasValue) {
			foreach (var permission in channel.PermissionOverwrites.Value) {
				if (permission.Type == PermissionOverwriteType.Member && 
				    permission.Deny.HasPermission(DiscordPermission.ViewChannel) && 
				    permission.ID == member.User.Value.ID) {
					await _interactionApi.CreateFollowupMessageAsync(context.ApplicationID, context.Token,
						"You're already blocked from viewing that channel");
					return Result.FromSuccess();
				}
			}
		}

		await _channelApi.EditChannelPermissionsAsync(channel.ID, member.User.Value.ID,
			deny: new DiscordPermissionSet(DiscordPermission.ViewChannel), type: PermissionOverwriteType.Member);
		await _interactionApi.CreateFollowupMessageAsync(context.ApplicationID, context.Token, $"You've been locked out of {channel.Name} for now");
		return Result.FromSuccess();
	}
	
	[Command("show")]
	[Ephemeral]
	[Description("unbans you from seeing one channel")]
	public async Task<Result> In(IChannel channel) {
		if(_commandContext.Context is not InteractionContext context) return Result.FromSuccess();
		if(!context.Member.HasValue) return Result.FromSuccess();
		var member = context.Member.Value;

		if (channel.Type != ChannelType.GuildText) {
			await _interactionApi.CreateFollowupMessageAsync(context.ApplicationID, context.Token, "Thats not a text channel.");
			return Result.FromSuccess();
		}

		var isBanned = false;
		if (channel.PermissionOverwrites.HasValue) {
			foreach (var permission in channel.PermissionOverwrites.Value) {
				if (permission.Type == PermissionOverwriteType.Member && 
				    permission.Deny.HasPermission(DiscordPermission.ViewChannel) && 
				    permission.ID == member.User.Value.ID) {
					isBanned = true;
					break;
				}
			}
		}

		if (!isBanned) {
			await _interactionApi.CreateFollowupMessageAsync(context.ApplicationID, context.Token,
				"You're not blocked from viewing that channel");
			return Result.FromSuccess();
		}

		await _channelApi.DeleteChannelPermissionAsync(channel.ID, member.User.Value.ID);
		await _interactionApi.CreateFollowupMessageAsync(context.ApplicationID, context.Token, $"You're no longer locked out of {channel.Name}");
		return Result.FromSuccess();
	}
	
	[Command("reset")]
	[Ephemeral]
	[Description("unbans you from all channels")]
	public async Task<Result> Reset() {
		if(_commandContext.Context is not InteractionContext context) return Result.FromSuccess();
		if(!context.Member.HasValue) return Result.FromSuccess();
		var member = context.Member.Value;

		var channels = await _guildAPI.GetGuildChannelsAsync(Program.Settings.Server);

		var channelNames = new List<string>();
		foreach (var channel in channels.Entity) {
			if(channel.Type != ChannelType.GuildText) continue;
			var isBanned = false;
			if (channel.PermissionOverwrites.HasValue) {
				foreach (var permission in channel.PermissionOverwrites.Value) {
					if (permission.Type == PermissionOverwriteType.Member && 
					    permission.Deny.HasPermission(DiscordPermission.ViewChannel) && 
					    permission.ID == member.User.Value.ID) {
						isBanned = true;
						break;
					}
				}
			}

			if (!isBanned) {
				continue;
			}

			await _channelApi.DeleteChannelPermissionAsync(channel.ID, member.User.Value.ID);
			channelNames.Add(channel.Name.Value);
		}

		if (channelNames.Count == 0) {
			await _interactionApi.CreateFollowupMessageAsync(context.ApplicationID, context.Token, $"There weren't any channels to unblock for you");
		} else {
			await _interactionApi.CreateFollowupMessageAsync(context.ApplicationID, context.Token, $"You're no longer locked out of {string.Join(", ", channelNames)}");	
		}
		return Result.FromSuccess();
	}
}