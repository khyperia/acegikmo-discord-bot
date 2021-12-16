global using static AcegikmoDiscordBot.Util;
using System;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Rest.Core;
using Remora.Results;

namespace AcegikmoDiscordBot;

public static class Util {
	public static readonly Result Success = Result.FromSuccess();
	public static Snowflake Server => Program.Settings.Server;

	public static bool IsAshl(this IUser user) => user.ID == Program.Settings.Ashl;
	public static bool IsAcegikmo(this Snowflake server) => server == Program.Settings.Server;
	public static bool IsAcegikmo(this Optional<Snowflake> server) => server == Program.Settings.Server;
	public static string Mention(this IUser user) => MentionUtils.MentionUser(user.ID.Value);
	public static string MentionUser(this Snowflake user) => MentionUtils.MentionUser(user.Value);
	public static string MentionUser(this ulong user) => MentionUtils.MentionUser(user);

	public static T? Nullable<T>(this Optional<T> optional) where T: struct {
		return optional.HasValue ? optional.Value : null;
	}
	public static T? Nullsy<T>(this Optional<T> optional) where T: class {
		return optional.HasValue ? optional.Value : null;
	}

	public static bool Unpack<T>(this Result<T> result, out T data, out Result error) {
		if (!result.IsSuccess) error = Result.FromError(result);
		data = (result.IsSuccess ? result.Entity : default)!;
		return result.IsSuccess;
	}

	public static bool GetErrorResult(this IResult result, out Result errRes) {
		if(!result.IsSuccess) errRes = Result.FromError(result.Error);
		return !result.IsSuccess;
	}


}