using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Gateway.Responders;
using Remora.Rest.Core;
using Remora.Results;
using static AcegikmoDiscordBot.Program;

namespace AcegikmoDiscordBot {
    internal class Log : IDisposable, IResponder<IMessageCreate>, IResponder<IMessageUpdate> {
        public struct MessageDb {
            public ulong MessageId;
            public ulong ChannelId;
            public ulong AuthorId;
            public string Message;

            public static bool Read(SqliteDataReader row, out MessageDb result) {
                bool message = false, channel = false, author = false, content = false;
                result = default;
                for (var i = 0; i < row.FieldCount; i++) {
                    switch (row.GetName(i)) {
                        case "message_id":
                            result.MessageId = Convert.ToUInt64(row.GetValue(i));
                            message = true;
                            break;
                        case "channel_id":
                            result.ChannelId = Convert.ToUInt64(row.GetValue(i));
                            channel = true;
                            break;
                        case "author_id":
                            result.AuthorId = Convert.ToUInt64(row.GetValue(i));
                            author = true;
                            break;
                        case "message":
                            result.Message = (string)row.GetValue(i);
                            content = true;
                            break;
                    }
                }

                return message && channel && author && content;
            }
        }

        private readonly SqliteConnection _sql;
        private readonly IDiscordRestChannelAPI _channelAPI;

        public Log(IDiscordRestChannelAPI channelAPI) {
            _channelAPI = channelAPI;
            
            _sql = new SqliteConnection("Data Source=log.db");
            _sql.Open();
            var cmd = _sql.CreateCommand();
            cmd.CommandText =
                "CREATE TABLE IF NOT EXISTS log(message_id INTEGER PRIMARY KEY NOT NULL, channel_id INTEGER NOT NULL, author_id INTEGER NOT NULL, message TEXT NOT NULL)";
            cmd.ExecuteNonQuery();
        }

        public void Dispose() => _sql.Dispose();

        public void Trim() {
            using var cmd = _sql.CreateCommand();
            cmd.CommandText = "DELETE FROM log WHERE message_id < @message_id";
            var cutoff = Snowflake.CreateTimestampSnowflake(DateTime.UtcNow.AddDays(-7));
            cmd.Parameters.AddWithValue("message_id", (long)cutoff.Value);
            cmd.ExecuteNonQuery();
        }

        public async Task<Result> RespondAsync(IMessageCreate message, CancellationToken ct = new()) {
            if (message.Author.ID.Value == ASHL && message.Content.StartsWith("!sql ")) {
                var thing = message.Content["!sql ".Length..];
                var scaryCmd = _sql.CreateCommand();
                scaryCmd.CommandText = thing; // spook
                using var scaryResult = scaryCmd.ExecuteReader();
                var msg = new StringBuilder();
                while (scaryResult.Read()) {
                    if (msg.Length != 0) {
                        msg.Append('\n');
                    }

                    if (msg.Length > 1000) {
                        msg.Append("Too many results.");
                        break;
                    }

                    for (var i = 0; i < scaryResult.FieldCount; i++) {
                        if (i != 0) {
                            msg.Append(' ');
                        }

                        msg.Append(scaryResult.GetName(i));
                        msg.Append('=');
                        msg.Append(scaryResult.GetValue(i));
                    }
                }

                if (msg.Length == 0) {
                    msg.Append("No results.");
                }

                await _channelAPI.CreateMessageAsync(message.ChannelID, msg.ToString(), ct: ct);
            }

            await LogMessage(message);
            return Result.FromSuccess();
        }

        public async Task<Result> RespondAsync(IMessageUpdate update, CancellationToken ct = new CancellationToken()) {
            await LogMessage(update);
            return Result.FromSuccess();
        }

        private async Task LogMessage(IPartialMessage message) {
            if (message.GuildID.Value.Value != ACEGIKMO_SERVER) {
                return;
            }

            var content = Format(message);
            if (content == "" && message.Author.Value.ID.Value == 0) {
                Console.WriteLine(
                    $"Empty content/author ID message ({message.GetType().FullName}, " +
                    $"type {(message as IMessage)?.Type}, " +
                    $"source {message.Author.Value.ID.Value}): " +
                    $"https://discordapp.com/channels/{message.GuildID.Value.Value}/{message.ChannelID.Value.Value}/{message.ID.Value.Value}");
                return;
            }

            using var cmd = _sql.CreateCommand();
            cmd.CommandText = "INSERT OR REPLACE INTO log VALUES(@message_id, @channel_id, @author_id, @message)";
            cmd.Parameters.AddWithValue("message_id", (long)message.ID.Value.Value);
            cmd.Parameters.AddWithValue("channel_id", (long)message.ChannelID.Value.Value);
            cmd.Parameters.AddWithValue("author_id", (long)message.Author.Value.ID.Value);
            cmd.Parameters.AddWithValue("message", content);
            cmd.ExecuteNonQuery();
        }

        public bool TryGetMessage(ulong messageId, out MessageDb message) {
            using var cmd = _sql.CreateCommand();
            cmd.CommandText = "SELECT * FROM log WHERE message_id = @message_id LIMIT 1";
            cmd.Parameters.AddWithValue("message_id", (long)messageId);
            var reader = cmd.ExecuteReader();
            if (reader.Read()) {
                return MessageDb.Read(reader, out message);
            } else {
                message = default;
                return false;
            }
        }

        public bool TryGetPreviousMessage(ulong messageId, ulong channelId, out MessageDb message) {
            using var cmd = _sql.CreateCommand();
            cmd.CommandText =
                "SELECT * FROM log WHERE channel_id = @channel_id AND message_id < @message_id ORDER BY message_id DESC LIMIT 1";
            cmd.Parameters.AddWithValue("channel_id", (long)channelId);
            cmd.Parameters.AddWithValue("message_id", (long)messageId);
            var reader = cmd.ExecuteReader();
            if (reader.Read()) {
                return MessageDb.Read(reader, out message);
            } else {
                message = default;
                return false;
            }
        }

        public IEnumerable<(ulong authorId, ulong count)> MessageCounts(IEnumerable<ulong> userIds, ulong limit) {
            using var cmd = _sql.CreateCommand();
            cmd.CommandText =
                $"SELECT author_id, COUNT(*) as message_count FROM log WHERE author_id IN ({string.Join(",", userIds)}) GROUP BY author_id HAVING COUNT(*) > {limit} ORDER BY message_count DESC";
            var reader = cmd.ExecuteReader();
            while (reader.Read()) {
                ulong author = 0;
                ulong count = 0;
                for (var col = 0; col < reader.FieldCount; col++) {
                    switch (reader.GetName(col)) {
                        case "author_id":
                            author = Convert.ToUInt64(reader.GetValue(col));
                            break;
                        case "message_count":
                            count = Convert.ToUInt64(reader.GetValue(col));
                            break;
                    }
                }

                if (author != 0 && count != 0) {
                    yield return (author, count);
                }
            }
        }

        private static string Format(IPartialMessage message) {
            var result = string.IsNullOrWhiteSpace(message.Content.Value) ? "" : message.Content.Value;
            if (message.Attachments.HasValue) {
                foreach (var attachment in message.Attachments.Value) {
                    result += "\n" + attachment.ProxyUrl;
                }
            }

            return result;
        }
    }
}
