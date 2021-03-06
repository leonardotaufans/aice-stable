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
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using aice_stable.commands;

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
            discord.VoiceStateUpdated += VoiceStateUpdated;
            /// The list of services used to run this bot
            Services = new ServiceCollection()
                .AddTransient<SecureRandom>()
                .AddSingleton<StandardCommandModule>()
                .AddSingleton<MusicServices>()
                .AddSingleton(new LavalinkService(cfg.Lavalink, discord))
                .AddSingleton(new YoutubeSearchProvider(cfg.Youtube))
                .AddSingleton(new RedisClient(cfg.Redis))
                .AddSingleton(this)
                .BuildServiceProvider(true);
            /// Request
            discord.MessageCreated += async (s, e) => 
            {
                ///todo: Use external files to deal with this mess if possible
                if (e.Message.Content.ToLower().Equals("pain")) 
                {
                    await e.Message.RespondAsync("Pain-peko https://cdn.discordapp.com/attachments/879304880471298099/933247325319618570/f3ff0bfe160d84d6f85bb53c06319406.png");
                }

                if (e.Message.Content.ToLower().Equals("konpeko")) 
                {
                    await e.Message.RespondAsync("https://c.tenor.com/KnFPnSlq_AkAAAAd/konpeko.gif");
                }

                if (e.Message.Content.ToLower().Equals("hey moona")) 
                {
                    await e.Message.RespondAsync("https://youtu.be/xAnQKzvdHus");
                }
                
                if (e.Message.Content.ToLower().Equals("ehe"))
                {
                    await e.Message.RespondAsync("https://c.tenor.com/cZHoFqQEgwkAAAAC/paimon.gif");
                }

				if (e.Message.Content.ToLower().Equals("heh"))
				{
					await e.Message.RespondAsync("https://media.discordapp.net/attachments/879304880471298099/970194205274173480/1651382710576.jpg?width=692&height=468");
				}
            };


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

        ///<summary>
        ///Checks if the Discord voice channel is empty
        ///and if it is, stopping the music.
        ///</summary>
        private async Task VoiceStateUpdated(DiscordClient client, VoiceStateUpdateEventArgs voiceState)
        {
            var music = this.Services.GetService<MusicServices>();
            var guildMusicData = await music.GetOrCreateDataAsync(voiceState.Guild);
            if (voiceState.User == this.discord.CurrentUser)
                return;
            
            var channel = guildMusicData.Channel;
            // if (channel == null || channel != voiceState.Before.Channel)
            //     return;

            var users = channel.Users;
            if (!users.Any(x => !x.IsBot))
            {
                await guildMusicData.SkipOrStopAsync();

                if (guildMusicData.CommandChannel != null)
                {
                    guildMusicData.EmptyQueue();
                    await guildMusicData.DestroyPlayerAsync();
                    await guildMusicData.CommandChannel.SendMessageAsync
                        ($"{DiscordEmoji.FromName(client, ":play_pause:")} All users left the channel. Stopping playback.");
                }
            }

            if (voiceState.After.Channel != null) {
                await guildMusicData.CommandChannel.SendMessageAsync($"[debug] voice state {voiceState.After?.Channel}");
            }
            /// This is to stop the music and destroy the player
            /// once the channel is empty.
            /// More testing required.
            /// Thanks, poor documentation :)
        }
    }
}
