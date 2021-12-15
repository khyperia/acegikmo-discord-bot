using Remora.Discord.API.Abstractions.Objects;

namespace AcegikmoDiscordBot; 

public static class Util {
	public static bool IsAshl(this IUser user) => user.ID.Value == Program.ASHL;
}