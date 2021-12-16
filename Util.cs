using Remora.Discord.API.Abstractions.Objects;
using Remora.Rest.Core;

namespace AcegikmoDiscordBot; 

public static class Util {
	public static bool IsAshl(this IUser user) => user.ID == Program.Settings.Ashl;
	public static bool IsAcegikmo(this Snowflake server) => server == Program.Settings.Server;
	public static bool IsAcegikmo(this Optional<Snowflake> server) => server == Program.Settings.Server;
	
}