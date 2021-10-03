using aice_stable.services;
using aice_stable.Services;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Lavalink;
using Emzi0767;
using Emzi0767.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace aice_stable
{
    public sealed class Aice
    {
        public DiscordClient discord;
        public LavalinkExtension Lavalink;
        /// <summary>
        /// Gets the shard ID. Not that useful considering we only have 1 shards at the moment.
        /// </summary>
        public int ShardId { get; }
        /// <summary>
        /// Configuration data
        /// </summary>
        public AiceConfigurationData configuration { get; }
        public InteractivityExtension Interactivity { get; }
        public CommandsNextExtension CommandsNext { get; }
        private AsyncExecutor AsyncExecutor { get; }
        private Timer GameTimer { get; set; } = null;
        private IServiceProvider Services { get; }
        private readonly object _logLock = new object();
        public string BotVersion { get; }
        public Aice(AiceConfigurationData cfg, int shardId, AsyncExecutor async)
        {
            ShardId = shardId;
            configuration = cfg;
            AsyncExecutor = async;
            BotVersion = AiceUtilities.GetBotVersion();
            /// <summary>
            /// Here are some information regarding these configuration data:
            /// Token: API token required to access Discord API.
            /// TokenType: Bearer if we're using oAuth token. Bot if we're using Discord Bot token. 
            /// DiscordIntents: <see cref="https://discord.com/developers/docs/topics/gateway#gateway-intents"/> AllUnprivileged is all intents except GUILD_MEMBERS and GUILD_PRESENCE
            /// AutoReconnect & ReconnectIndefinitely: If the connection drops, the code will attempt to reconnect indefinitely.
            /// GatewayCompressionLevel: Choose whether to compress in Payload level, entire Stream level, or none.
            /// LargeThreshold: How many guild members are the limit to make the gateway stop sending offline members in the list.
            /// </summary>
            discord = new DiscordClient(
                new DiscordConfiguration()
                {
                    Token = cfg.Discord.Token, 
                    TokenType = TokenType.Bot, 
                    Intents = DiscordIntents.AllUnprivileged,
                    AutoReconnect = true,
                    ReconnectIndefinitely = true,
                    GatewayCompressionLevel = GatewayCompressionLevel.Stream, /// To reduce bandwidth usage in the server, we compress the entire stream
                    LargeThreshold = 250
                });
            discord.Ready += DiscordReady;

            /// The list of services used to run this bot
            Services = new ServiceCollection()
                .AddTransient<SecureRandom>()
                .AddSingleton<MusicServices>()
                .AddSingleton(new LavalinkService(cfg.Lavalink, discord))
                .AddSingleton(new YoutubeSearchProvider(cfg.Youtube))
                .AddSingleton(new RedisClient(cfg.Redis))
                .AddSingleton(this)
                .BuildServiceProvider(true);

            /// Use the CommandsNext plugin
            var commands = discord.UseCommandsNext(new CommandsNextConfiguration()
            {
                StringPrefixes = cfg.Discord.DefaultPrefixes, /// Prefix: !
                EnableMentionPrefix = cfg.Discord.EnableMentionPrefix, /// Allow mention as prefix
                Services = this.Services 
            });
            commands.SetHelpFormatter<AiceHelpFormatter>();
            commands.RegisterCommands(Assembly.GetExecutingAssembly());
            /// So we can interact with the respond
            Interactivity = discord.UseInteractivity(new InteractivityConfiguration()
            {
                Timeout = TimeSpan.FromSeconds(30)
            });

            Lavalink = discord.UseLavalink();
        }

        public Task StartAsync()
        {
            return this.discord.ConnectAsync();
        }
        
        /// <summary>
        /// on Discord ready
        /// </summary>
        /// <param name="sender">DiscordClient of the bot</param>
        /// <param name="e"></param>
        /// <returns></returns>
        private Task DiscordReady(DiscordClient sender, ReadyEventArgs e)
        {
            if (this.GameTimer == null && !string.IsNullOrWhiteSpace(this.configuration.Discord.Game))
                this.GameTimer = new Timer(this.GameTimerCallback, sender, TimeSpan.Zero, TimeSpan.FromHours(1));

            return Task.CompletedTask;
        }

        /// <summary>
        /// Sets the Discord status
        /// </summary>
        /// <param name="state">DiscordClient data as an object</param>
        private void GameTimerCallback(object state)
        {
            
            var client = state as DiscordClient;
            try
            {
                /// UpdateStatusAsync takes a DiscordActivity, UserStatus, and time since idle.
                AsyncExecutor.Execute(client.UpdateStatusAsync(
                    /// DiscordActivity takes the string name, and the activity type
                    new DiscordActivity(this.configuration.Discord.Game, ActivityType.Watching), UserStatus.Online, null));
            } catch (Exception e)
            {
                client.Logger.LogError("GameTimerCallback", $"Could not update presence ({e.GetType()}: {e.Message})");
            }
        }
    }
}
