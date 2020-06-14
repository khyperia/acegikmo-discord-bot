using Discord;
using Discord.WebSocket;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Immutable;
using System.Text;
using System.Threading.Tasks;
using static AcegikmoDiscordBot.Program;

namespace AcegikmoDiscordBot
{
    internal class Log : IDisposable
    {
        public struct MessageDb
        {
            public ulong MessageId;
            public ulong ChannelId;
            public ulong AuthorId;
            public string Message;

            public static MessageDb Read(SqliteDataReader row)
            {
                var result = new MessageDb();
                for (var i = 0; i < row.FieldCount; i++)
                {
                    switch (row.GetName(i))
                    {
                        case "message_id":
                            result.MessageId = Convert.ToUInt64(row.GetValue(i));
                            break;
                        case "channel_id":
                            result.ChannelId = Convert.ToUInt64(row.GetValue(i));
                            break;
                        case "author_id":
                            result.AuthorId = Convert.ToUInt64(row.GetValue(i));
                            break;
                        case "message":
                            result.Message = (string)row.GetValue(i);
                            break;
                    }
                }
                return result;
            }
        }

        private readonly SqliteConnection _sql;

        public Log()
        {
            _sql = new SqliteConnection("Data Source=log.db");
            _sql.Open();
            var cmd = _sql.CreateCommand();
            cmd.CommandText = "CREATE TABLE IF NOT EXISTS log(message_id INTEGER PRIMARY KEY NOT NULL, channel_id INTEGER NOT NULL, author_id INTEGER NOT NULL, message TEXT NOT NULL)";
            cmd.ExecuteNonQuery();
        }

        public void Dispose() => _sql.Dispose();

        public async Task MessageReceivedAsync(SocketMessage message)
        {
            if (message.Author.Id == ASHL &&
                message.Content.StartsWith("!sql "))
            {
                var thing = message.Content.Substring("!sql ".Length);
                var scaryCmd = _sql.CreateCommand();
                scaryCmd.CommandText = thing; // spook
                using var scaryResult = scaryCmd.ExecuteReader();
                var msg = new StringBuilder();
                while (scaryResult.Read())
                {
                    if (msg.Length != 0)
                    {
                        msg.Append('\n');
                    }
                    if (msg.Length > 1000)
                    {
                        msg.Append("Too many results.");
                        break;
                    }
                    for (var i = 0; i < scaryResult.FieldCount; i++)
                    {
                        if (i != 0)
                        {
                            msg.Append(' ');
                        }
                        msg.Append(scaryResult.GetName(i));
                        msg.Append('=');
                        msg.Append(scaryResult.GetValue(i));
                    }
                }
                if (msg.Length == 0)
                {
                    msg.Append("No results.");
                }
                await message.Channel.SendMessageAsync(msg.ToString());
            }
            LogMessage(message);
        }

        internal Task MessageUpdatedAsync(Cacheable<IMessage, ulong> original, SocketMessage newMessage, ISocketMessageChannel channel)
        {
            LogMessage(newMessage);
            return Task.CompletedTask;
        }

        private void LogMessage(SocketMessage message)
        {
            var cmd = _sql.CreateCommand();
            cmd.CommandText = "INSERT OR REPLACE INTO log VALUES(@message_id, @channel_id, @author_id, @message)";
            cmd.Parameters.AddWithValue("message_id", (long)message.Id);
            cmd.Parameters.AddWithValue("channel_id", (long)message.Channel.Id);
            cmd.Parameters.AddWithValue("author_id", (long)message.Author.Id);
            cmd.Parameters.AddWithValue("message", Format(message));
            cmd.ExecuteNonQuery();
        }

        public bool TryGetMessage(ulong messageId, out MessageDb message)
        {
            using var cmd = _sql.CreateCommand();
            cmd.CommandText = "SELECT * FROM log WHERE message_id = @message_id LIMIT 1";
            cmd.Parameters.AddWithValue("message_id", (long)messageId);
            var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                message = MessageDb.Read(reader);
                return true;
            }
            else
            {
                message = default;
                return false;
            }
        }

        public bool TryGetPreviousMessage(ulong messageId, out MessageDb message)
        {
            using var cmd = _sql.CreateCommand();
            cmd.CommandText = "SELECT * FROM log WHERE message_id < @message_id ORDER BY message_id DESC LIMIT 1";
            cmd.Parameters.AddWithValue("message_id", (long)messageId);
            var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                message = MessageDb.Read(reader);
                return true;
            }
            else
            {
                message = default;
                return false;
            }
        }

        private string Format(SocketMessage message)
        {
            var result = string.IsNullOrWhiteSpace(message.Content) ? "" : message.Content;
            if (message.Attachments != null && !(message.Attachments is ImmutableArray<Attachment> array && array.IsDefault))
            {
                foreach (var attachment in message.Attachments)
                {
                    result += "\n" + attachment.ProxyUrl;
                }
            }
            return result;
        }
    }
}
