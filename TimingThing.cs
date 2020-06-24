using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace AcegikmoDiscordBot
{
    internal class TimingThing
    {
        private readonly Log _log;
        private readonly Config _config;
        private DateTime _nextUpdate;

        public TimingThing(Log log, Config config)
        {
            _log = log;
            _config = config;
        }

        private void SetNextUpdate()
        {
            _nextUpdate = DateTime.UtcNow.Date.AddDays(1);
        }

        public async Task MessageReceivedAsync(SocketMessage message)
        {
            if (DateTime.UtcNow > _nextUpdate && message.Channel is SocketTextChannel messageChannel && messageChannel.Guild.Id == _config.server)
            {
                SetNextUpdate();
                var modchannel = messageChannel.Guild.GetTextChannel(_config.channel);
                await MemberizerCommand.Memberizer(_log, modchannel, 50);
                Console.WriteLine("Trimming...");
                _log.Trim();
                Console.WriteLine("Done trimming.");
            }
        }
    }
}
