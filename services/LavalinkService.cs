using aice_stable.services;
using DSharpPlus;
using DSharpPlus.EventArgs;
using DSharpPlus.Lavalink;
using DSharpPlus.Lavalink.EventArgs;
using DSharpPlus.Net;
using Emzi0767.Utilities;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace aice_stable.Services
{
    /// <summary>
    /// Lavalink service which maintains a Lavalink node connection.
    /// </summary>
    public sealed class LavalinkService
    {
        /// <summary>
        /// Sets the log event
        /// </summary>
        public static EventId LogEvent { get; } = new EventId(1001, "Aice-bot");

        /// <summary>
        /// Gets the Lavalink node connection.
        /// </summary>
        public LavalinkNodeConnection LavalinkNode { get; private set; }
        /// <summary>
        /// Loads the configuration data
        /// </summary>
        private AiceConfigurationLavalink Configuration { get; }
        /// <summary>
        /// Gets the Discord client
        /// </summary>
        private DiscordClient Discord { get; }

        /// <summary>
        /// Sets the async handler for track exception
        /// </summary>
        private readonly AsyncEvent<LavalinkGuildConnection, TrackExceptionEventArgs> _trackException;

        /// <summary>
        /// Creates a new Lavalink service with specified configuration options.
        /// </summary>
        /// <param name="cfg">Lavalink configuration.</param>
        /// <param name="client">Discord client to which the Lavalink will be attached.</param>
        public LavalinkService(AiceConfigurationLavalink cfg, DiscordClient client)
        {
            this.Configuration = cfg;
            this.Discord = client;
            this.Discord.Ready += this.onClientReady;
            this._trackException = new AsyncEvent<LavalinkGuildConnection, TrackExceptionEventArgs>("LAVALINK_TRACK_EXCEPTION", TimeSpan.Zero, this.EventExceptionHandler);
        }

        private Task onClientReady(DiscordClient sender, ReadyEventArgs e)
        {
            if (this.LavalinkNode == null)
                Task.Run(async () =>
                {
                    var lava = sender.GetLavalink();
                    this.LavalinkNode = await lava.ConnectAsync(new LavalinkConfiguration   /// LavalinkConfiguration is from the library
                    {
                        Password = this.Configuration.Password,
                        SocketEndpoint = new ConnectionEndpoint(this.Configuration.Hostname, this.Configuration.Port),
                        RestEndpoint = new ConnectionEndpoint(this.Configuration.Hostname, this.Configuration.Port)
                    });

                    this.LavalinkNode.TrackException += this.LavalinkNode_TrackException;
                });
            return Task.CompletedTask;
        }

        private async Task LavalinkNode_TrackException(LavalinkGuildConnection lavaConnection, TrackExceptionEventArgs e)
        {
            await this._trackException.InvokeAsync(lavaConnection, e);
        }

        public event AsyncEventHandler<LavalinkGuildConnection, TrackExceptionEventArgs> TrackExceptionThrown
        {
            add => this._trackException.Register(value);
            remove => this._trackException.Unregister(value);
        }

        private void EventExceptionHandler(
            AsyncEvent<LavalinkGuildConnection, TrackExceptionEventArgs> asyncEvent,
            Exception exception,
            AsyncEventHandler<LavalinkGuildConnection, TrackExceptionEventArgs> handler,
            LavalinkGuildConnection sender,
            TrackExceptionEventArgs eventArgs)
            => this.Discord.Logger.LogError(LogEvent, exception, "Exception occured during track playback");
    }
}