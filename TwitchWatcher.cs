using Discord.WebSocket;
using System;
using System.Collections.Generic;
using TwitchLib.Api;
using TwitchLib.Api.Services;
using TwitchLib.Api.Services.Events;
using TwitchLib.Api.Services.Events.LiveStreamMonitor;

namespace AcegikmoDiscordBot
{
    class TwitchWatcher
    {
        private readonly DiscordSocketClient _client;
        private readonly HashSet<string> _islive = new HashSet<string>();
        private readonly bool _watching = false;

        public TwitchWatcher(DiscordSocketClient client)
        {
            _client = client;
            Console.WriteLine("Booting twitch");
            var api = new TwitchAPI();
            api.Settings.ClientId = Program.Config.twitchclientid;
            api.Settings.Secret = Program.Config.twitchsecret;
            var monitor = new LiveStreamMonitorService(api);
            monitor.SetChannelsByName(new List<string> { "acegikmo" });

            monitor.OnStreamOnline += Monitor_OnStreamOnline;
            monitor.OnStreamOffline += Monitor_OnStreamOffline;
            monitor.OnStreamUpdate += Monitor_OnStreamUpdate;

            monitor.OnServiceStarted += Monitor_OnServiceStarted;
            monitor.OnChannelsSet += Monitor_OnChannelsSet;

            monitor.Start();

            monitor.UpdateLiveStreamersAsync().Wait();

            _watching = true;
        }

        private void Send(string message)
        {
            if (_watching)
            {
                _client.GetGuild(Program.ACEGIKMO_SERVER).GetTextChannel(Program.ACEGIKMO_DELETED_MESSAGES).SendMessageAsync(message);
            }
            else
            {
                Console.WriteLine($"Twitch watching=false message: {message}");
            }
        }

        private void Monitor_OnStreamOnline(object? sender, OnStreamOnlineArgs e)
        {
            if (_islive.Add(e.Channel))
            {
                Send($"Stream online: channel = {e.Channel} title = {e.Stream.Title} type = {e.Stream.Type}");
            }
        }

        private void Monitor_OnStreamUpdate(object? sender, OnStreamUpdateArgs e)
        {
            Send($"Stream update: channel = {e.Channel} title = {e.Stream.Title} type = {e.Stream.Type}");
        }

        private void Monitor_OnStreamOffline(object? sender, OnStreamOfflineArgs e)
        {
            if (_islive.Remove(e.Channel))
            {
                Send($"Stream offline: channel = {e.Channel} title = {e.Stream.Title} type = {e.Stream.Type}");
            }
        }

        private void Monitor_OnChannelsSet(object? sender, OnChannelsSetArgs e)
        {
            Console.WriteLine($"Twitch channels set: {string.Join(", ", e.Channels)}");
        }

        private void Monitor_OnServiceStarted(object? sender, OnServiceStartedArgs e)
        {
            Console.WriteLine("Twitch service started");
        }
    }
}
