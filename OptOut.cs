using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
	private readonly IDiscordRestGuildAPI _guildAPI;
	private readonly ContextInjectionService _commandContext;
	private readonly IDiscordRestInteractionAPI _interactionApi;
	private readonly IDiscordRestChannelAPI _channelApi;
	
	public OptOut(IDiscordRestGuildAPI guildApi, ContextInjectionService commandContext, 
			IDiscordRestInteractionAPI interactionApi, IDiscordRestChannelAPI channelApi) {
		_guildAPI = guildApi;
		_commandContext = commandContext;
		_interactionApi = interactionApi;
		_channelApi = channelApi;
	}

	[Command("hide")]
	[Ephemeral]
	[Description("bans you from seeing one channel")]
	public async Task<Result> Out(IChannel channel) {
		if(_commandContext.Context is not InteractionContext{
			   Member: { HasValue: true, Value: {User: {HasValue:true, Value: var user} } member },
		   } context) 
			return Success;

		if (channel is { Type: ChannelType.GuildText }) {
			await _interactionApi.CreateFollowupMessageAsync(context.ApplicationID, context.Token, "Thats not a text channel.");
			return Success;
		}
		
		if (channel.PermissionOverwrites is { HasValue: true, Value: var overwrites }) {
			foreach (var permission in overwrites) {
				if (permission is { Type: PermissionOverwriteType.Member } && 
				    permission.Deny.HasPermission(DiscordPermission.ViewChannel) &&
				    permission.ID == user.ID) {
					await _interactionApi.CreateFollowupMessageAsync(context.ApplicationID, context.Token,
						"You're already blocked from viewing that channel");
					return Success;
				}
			}
		}

		await _channelApi.EditChannelPermissionsAsync(channel.ID, user.ID,
			deny: new DiscordPermissionSet(DiscordPermission.ViewChannel), type: PermissionOverwriteType.Member);
		await _interactionApi.CreateFollowupMessageAsync(context.ApplicationID, context.Token, $"You've been locked out of {channel.Name} for now");
		return Success;
	}
	
	[Command("show")]
	[Ephemeral]
	[Description("unbans you from seeing one channel")]
	public async Task<Result> In(IChannel channel) {
		if(_commandContext.Context is not InteractionContext{
			   Member: { HasValue: true, Value: {User: {HasValue:true, Value: var user} } member },
		   } context) 
			return Success;

		if (channel.Type != ChannelType.GuildText) {
			await _interactionApi.CreateFollowupMessageAsync(context.ApplicationID, context.Token, "Thats not a text channel.");
			return Success;
		}

		var isBanned = channel.PermissionOverwrites.Nullsy()?
			.Any(permission => permission.Type == PermissionOverwriteType.Member && 
	                       permission.Deny.HasPermission(DiscordPermission.ViewChannel) && 
	                       permission.ID == user.ID);

		if (isBanned == false) {
			await _interactionApi.CreateFollowupMessageAsync(context.ApplicationID, context.Token,
				"You're not blocked from viewing that channel");
			return Success;
		}

		await _channelApi.DeleteChannelPermissionAsync(channel.ID, user.ID);
		await _interactionApi.CreateFollowupMessageAsync(context.ApplicationID, context.Token, $"You're no longer locked out of {channel.Name}");
		return Success;
	}
	
	[Command("reset")]
	[Ephemeral]
	[Description("unbans you from all channels")]
	public async Task<Result> Reset() {
		if(_commandContext.Context is not InteractionContext{
			   Member: { HasValue: true, Value: {User: {HasValue:true, Value: var user} } member },
		   } context) 
			return Success;

		var channels = await _guildAPI.GetGuildChannelsAsync(Program.Settings.Server);

		var channelNames = new List<string>();
		foreach (var channel in channels.Entity) {
			var isBanned = false;
			if (channel is {PermissionOverwrites: {HasValue:true, Value:var overwrites}}) {
				foreach (var permission in overwrites) {
					if (permission.Type == PermissionOverwriteType.Member && 
					    permission.Deny.HasPermission(DiscordPermission.ViewChannel) && 
					    permission.ID == user.ID) {
						isBanned = true;
						break;
					}
				}
			}

			if (!isBanned) {
				continue;
			}

			await _channelApi.DeleteChannelPermissionAsync(channel.ID, user.ID);
			channelNames.Add(channel.Name.Value);
		}

		if (channelNames.Count == 0) {
			await _interactionApi.CreateFollowupMessageAsync(context.ApplicationID, context.Token, $"There weren't any channels to unblock for you");
		} else {
			await _interactionApi.CreateFollowupMessageAsync(context.ApplicationID, context.Token, $"You're no longer locked out of {string.Join(", ", channelNames)}");	
		}
		return Success;
	}
}