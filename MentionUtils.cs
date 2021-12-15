namespace AcegikmoDiscordBot; 

public class MentionUtils {
	public static string MentionUser(ulong userId) {
		return $"<@{userId}>";
	}

	public static string MentionChannel(ulong channelId) {
		return $"<#{channelId}>";
	}
}