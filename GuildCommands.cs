using System;
using System.Threading;
using System.Threading.Tasks;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.Commands.Services;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace AcegikmoDiscordBot; 

public class GuildCommands: IResponder<IReady > {
	private readonly SlashService _slashService;

	public GuildCommands(SlashService slashService)
	{
		_slashService = slashService;
	}
	
	public async Task<Result> RespondAsync(IReady ready, CancellationToken ct = new ()) {
		Console.WriteLine("setup guild commands!");
		return await _slashService.UpdateSlashCommandsAsync(Program.Settings.Server, ct);
	}
}