using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Services;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace AcegikmoDiscordBot; 

public class GuildCommands: IResponder<IReady> {
	private readonly SlashService _slashService;
	private readonly IDiscordRestApplicationAPI _applicationApi;
	private readonly ContextInjectionService _context;

	public GuildCommands(SlashService slashService, IDiscordRestApplicationAPI applicationApi, ContextInjectionService context) {
		_slashService = slashService;
		_applicationApi = applicationApi;
		_context = context;
	}
	
	public async Task<Result> RespondAsync(IReady ready, CancellationToken ct = new ()) {
		Console.WriteLine("setup guild commands!");
		await _slashService.UpdateSlashCommandsAsync(Program.Settings.Server, ct);
		
		//hack in permissions
		var commands = await _applicationApi.GetGuildApplicationCommandsAsync(ready.Application.ID.Value, 
			Program.Settings.Server);
		var adminCommands = commands.Entity.Where(command => command.DefaultPermission == false);
		foreach (var command in adminCommands) {
			await _applicationApi.EditApplicationCommandPermissionsAsync(command.ApplicationID, command.GuildID.Value, command
			.ID, new[] {
					new ApplicationCommandPermissions(Program.Settings.ModRole, ApplicationCommandPermissionType.Role, true),
				});
		}
		
		return Result.FromSuccess();
	}
}