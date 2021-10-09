using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using aice_stable.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Immutable;
using aice_stable.services;
using DSharpPlus.Interactivity.Extensions;
using System.Net;
using System.Globalization;
using aice_stable.models;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;


namespace aice_stable.commands
{
    [ModuleLifespan(ModuleLifespan.Transient)]
public class MusicCommandModule : BaseCommandModule
    {
        private static ImmutableDictionary<int, DiscordEmoji> NumberMappings { get; }
        private static ImmutableDictionary<DiscordEmoji, int> NumberMappingsReverse { get; }
        private static ImmutableArray<DiscordEmoji> Numbers { get; }

        private MusicServices Music { get; }
        private MusicPlayer MusicPlayer { get; set; }
        private YoutubeSearchProvider Youtube { get; }
        public MusicCommandModule(MusicServices music, YoutubeSearchProvider youtube)
        {
            Music = music;
            Youtube = youtube;
        }

        static MusicCommandModule()
        {
            var iab = ImmutableArray.CreateBuilder<DiscordEmoji>();
            iab.Add(DiscordEmoji.FromUnicode("1\u20e3"));
            iab.Add(DiscordEmoji.FromUnicode("2\u20e3"));
            iab.Add(DiscordEmoji.FromUnicode("3\u20e3"));
            iab.Add(DiscordEmoji.FromUnicode("4\u20e3"));
            iab.Add(DiscordEmoji.FromUnicode("5\u20e3"));
            iab.Add(DiscordEmoji.FromUnicode("\u274c"));
            Numbers = iab.ToImmutable();

            var idb = ImmutableDictionary.CreateBuilder<int, DiscordEmoji>();
            idb.Add(1, DiscordEmoji.FromUnicode("1\u20e3"));
            idb.Add(2, DiscordEmoji.FromUnicode("2\u20e3"));
            idb.Add(3, DiscordEmoji.FromUnicode("3\u20e3"));
            idb.Add(4, DiscordEmoji.FromUnicode("4\u20e3"));
            idb.Add(5, DiscordEmoji.FromUnicode("5\u20e3"));
            idb.Add(-1, DiscordEmoji.FromUnicode("\u274c"));
            NumberMappings = idb.ToImmutable();
            var idb2 = ImmutableDictionary.CreateBuilder<DiscordEmoji, int>();
            idb2.AddRange(NumberMappings.ToDictionary(x => x.Value, x => x.Key));
            NumberMappingsReverse = idb2.ToImmutable();
        }

        /// <summary>
        /// Checks if user is already in a voice channel, and starts the Redis database
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        public override async Task BeforeExecutionAsync(CommandContext ctx)
        {
            var voiceState = ctx.Member.VoiceState;     /// Getting the requesting member's voice state
            var channel = voiceState?.Channel;          /// And getting its channel

            if (channel == null)
            {
                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msraisedhand:")} " +
                    $"You need to be in a voice channel.");
                return;
            }
            var member = ctx.Guild.CurrentMember?.VoiceState?.Channel;  /// This is to stop the requesting member
            if (member != null && channel != member)                    /// to command the bot without even 
            {                                                           /// joining the same channel.
                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msraisedhand:")} " +
                    $"You need to be in the same voice channel.");
                return;
            }
            this.MusicPlayer = await this.Music.GetOrCreateDataAsync(ctx.Guild);
            this.MusicPlayer.CommandChannel = ctx.Channel;              /// Still haven't figured out its uses.
            await base.BeforeExecutionAsync(ctx);               
        }

        /// <summary>
        /// Plays the song from a URL.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="uri"></param>
        /// <returns></returns>
        [Command("play"), Description("Plays a song from URL or search."), Aliases("p"), Priority(1)]
        public async Task PlayAsync(CommandContext ctx, [Description("URL to play from.")] Uri uri)
        {
            if (uri == null)
            {
                await ctx.RespondAsync("You must provide a URL to play from.");
                return;
            }
            var trackLoad = await Music.GetTracksAsync(uri);
            var tracks = trackLoad.Tracks;
            if (trackLoad.LoadResultType == LavalinkLoadResultType.LoadFailed || !tracks.Any())
            {
                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msfrown:")} No tracks were found at specified URL.");
                return;
            }

            if (MusicPlayer.IsShuffled)
            {
                tracks = Music.Shuffle(tracks);
            }

            else if (trackLoad.LoadResultType == LavalinkLoadResultType.PlaylistLoaded
                && trackLoad.PlaylistInfo.SelectedTrack > 0)
            {
                var index = trackLoad.PlaylistInfo.SelectedTrack;
                tracks = tracks.Skip(index).Concat(tracks.Take(index));
            }

            var trackCount = tracks.Count();
            foreach (var track in tracks)
                MusicPlayer.Enqueue(new MusicItem(track, ctx.Member));

            var voiceState = ctx.Member.VoiceState;
            var channel = voiceState.Channel;
            await MusicPlayer.CreatePlayerAsync(channel);
            await MusicPlayer.PlayAsync();

            if (trackCount > 1)
                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":ok_hand:")} " +
                    $"Added {trackCount:#,##0} tracks to playback queue.");
            else
            {
                var track = tracks.First();
                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":ok_hand:")} " +
                    $"Added {Formatter.Bold(Formatter.Sanitize(track.Title))} by {Formatter.Bold(Formatter.Sanitize(track.Author))} to the playback queue.");
            }

        }
        /// <summary>
        /// Plays the song using search
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        [Command("play"), Priority(0)]
        public async Task PlayAsync(CommandContext ctx,
            [RemainingText, Description("Search query")] string query)
        {
            if (query == null)
            {
                await ctx.RespondAsync("You must provide a text to play from.");
                return;
            }
            var interactivity = ctx.Client.GetInteractivity();
            var results = await Youtube.SearchAsync(query);

            if (!results.Any())
            {
                await ctx.RespondAsync(($"{DiscordEmoji.FromName(ctx.Client, ":msfrown:")} Nothing was found."));
                return;
            }

            var msgContent = string.Join("\n",
                results.Select((x, i) => $"{NumberMappings[i + 1]} {Formatter.Bold(Formatter.Sanitize(WebUtility.HtmlDecode(x.Title)))} by {Formatter.Bold(Formatter.Sanitize(WebUtility.HtmlDecode(x.Author)))}"));
            msgContent = $"{msgContent}\n\nType a number 1-{results.Count()} to queue a track. To cancel, type cancel or {Numbers.Last()}.";
            var message = await ctx.RespondAsync(msgContent);
            var answer = await interactivity.WaitForMessageAsync(x => x.Author == ctx.User, TimeSpan.FromSeconds(30));
            if (answer.TimedOut || answer.Result == null)
            {
                await message.ModifyAsync($"{DiscordEmoji.FromName(ctx.Client, ":msfrown:")} No choice was made.");
                return;
            }
            
            var resultIndex = answer.Result.Content.Trim();
            if (!int.TryParse(resultIndex, NumberStyles.Integer, CultureInfo.InvariantCulture, out var elInd))
            {
                if (resultIndex.ToLowerInvariant() == "cancel")
                {
                    elInd = -1;
                }
                else
                {
                    var em = DiscordEmoji.FromUnicode(resultIndex);
                    if (!NumberMappingsReverse.ContainsKey(em))
                    {
                        await message.ModifyAsync($"{DiscordEmoji.FromName(ctx.Client, ":msraisedhand:")} Invalid choice was made.");
                        return;
                    }

                    elInd = NumberMappingsReverse[em];
                }
            }
            else if (elInd < 1)
            {
                await message.ModifyAsync($"{DiscordEmoji.FromName(ctx.Client, ":msraisedhand:")} Invalid choice was made.");
                return;
            }

            if (!NumberMappings.ContainsKey(elInd))
            {
                await message.ModifyAsync($"{DiscordEmoji.FromName(ctx.Client, ":msraisedhand:")} Invalid choice was made.");
                return;
            }

            if (elInd == -1)
            {
                await message.ModifyAsync($"{DiscordEmoji.FromName(ctx.Client, ":ok_hand:")} Choice cancelled.");
                return;
            }

            var element = results.ElementAt(elInd - 1);
            var url = new Uri($"https://youtu.be/{element.Id}");

            var trackLoad = await Music.GetTracksAsync(url);
            var tracks = trackLoad.Tracks;

            if (trackLoad.LoadResultType == LavalinkLoadResultType.LoadFailed || !tracks.Any())
            {
                await message.ModifyAsync($"{DiscordEmoji.FromName(ctx.Client, ":msfrown:")} No tracks were found at specified link.");
                return;
            }

            if (this.MusicPlayer.IsShuffled)
                tracks = Music.Shuffle(tracks);
            var trackCount = tracks.Count();
            foreach (var track in tracks)
                MusicPlayer.Enqueue(new MusicItem(track, ctx.Member));
            var voiceState = ctx.Member.VoiceState;
            var channel = voiceState.Channel;
            await MusicPlayer.CreatePlayerAsync(channel);
            await MusicPlayer.PlayAsync();

            if (trackCount > 1)
            {
                await message.ModifyAsync($"{DiscordEmoji.FromName(ctx.Client, ":ok_hand:")} Added {trackCount:#,##0} tracks to playback queue.");
            }
            else
            {
                var track = tracks.First();
                await message.ModifyAsync($"{DiscordEmoji.FromName(ctx.Client, ":ok_hand:")} Added {Formatter.Bold(Formatter.Sanitize(track.Title))} by {Formatter.Bold(Formatter.Sanitize(track.Author))} to the playback queue.");
            }
        }
        
        /// <summary>
        /// Stops the song
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        [Command("stop"), Description("Stops playback and quits the voice channel.")]
        public async Task StopAsync(CommandContext ctx)
        {
            int queue = this.MusicPlayer.PlaylistQueue();
            this.MusicPlayer.EmptyQueue();
            await this.MusicPlayer.StopAsync();
            await this.MusicPlayer.DestroyPlayerAsync();
            await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":ok_hand:")} Removed {queue+1:#,##0} tracks from the queue.");
        }

        [Command("pause"), Description("Pauses playback.")]
        public async Task PauseAsync(CommandContext ctx)
        {
            await this.MusicPlayer.PauseAsync();
            await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":ok_hand:")} Playback paused. Use {Formatter.InlineCode($"{ctx.Prefix}resume")} to resume playback.");
        }

        [Command("resume"), Description("Resumes playback."), Aliases("unpause")]
        public async Task ResumeAsync(CommandContext ctx)
        {
            await this.MusicPlayer.ResumeAsync();
            await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":ok_hand:")} Playback resumed.");
        }

        [Command("skip"), Description("Skips current track."), Aliases("next")]
        public async Task SkipAsync(CommandContext ctx)
        {
            var track = this.MusicPlayer.NowPlaying;
            await this.MusicPlayer.StopAsync();
            await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":ok_hand:")} {Formatter.Bold(Formatter.Sanitize(track.Track.Title))} by {Formatter.Bold(Formatter.Sanitize(track.Track.Author))} skipped.");
        }

        [Command("seek"), Description("Seeks to specified time in current track.")]
        public async Task SeekAsync(CommandContext ctx,
            [RemainingText, Description("Which time point to seek to.")] TimeSpan position)
        {
            await this.MusicPlayer.SeekAsync(position, false);
        }

        [Command("forward"), Description("Forwards the track by specified amount of time.")]
        public async Task ForwardAsync(CommandContext ctx,
            [RemainingText, Description("By how much to forward.")] TimeSpan offset)
        {
            await this.MusicPlayer.SeekAsync(offset, true);
        }

        [Command("rewind"), Description("Rewinds the track by specified amount of time.")]
        public async Task RewindAsync(CommandContext ctx,
            [RemainingText, Description("By how much to rewind.")] TimeSpan offset)
        {
            await this.MusicPlayer.SeekAsync(-offset, true);
        }

        [Command("restart"), Description("Restarts the playback of the current track.")]
        public async Task RestartAsync(CommandContext ctx)
        {
            var track = this.MusicPlayer.NowPlaying;
            await this.MusicPlayer.RestartAsync();
            await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":ok_hand:")} {Formatter.Bold(Formatter.Sanitize(track.Track.Title))} by {Formatter.Bold(Formatter.Sanitize(track.Track.Author))} restarted.");
        }

        [Command("repeat"), Description("Changes repeat mode of the queue."), Aliases("loop")]
        public async Task RepeatAsync(CommandContext ctx,
            [Description("Repeat mode. Can be all, single, or none.")] string mode = null)
        {
            if (mode == null)
            {
                await ctx.RespondAsync($"Repeat options: \n```all | single | none```" + 
                                        $"\nCurrent mode: {this.MusicPlayer.RepeatMode}");
                var repeatModeConverter = new RepeatModeConverter();
                repeatModeConverter.ToString(this.MusicPlayer.RepeatMode);
                return;
            }

            var rmc = new RepeatModeConverter();
            if (!rmc.TryFromString(mode, out var rm))
            {
                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msraisedhand:")} Invalid repeat mode specified.");
                return;
            }

            this.MusicPlayer.SetRepeatMode(rm);
            await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":ok_hand:")} Repeat mode set to {rm}.");
        }

        [Command("shuffle"), Description("Toggles shuffle mode.")]
        public async Task ShuffleAsync(CommandContext ctx)
        {
            if (this.MusicPlayer.IsShuffled)
            {
                this.MusicPlayer.StopShuffle();
                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":ok_hand:")} Queue is no longer shuffled.");
            }
            else
            {
                this.MusicPlayer.Shuffle();
                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":ok_hand:")} Queue is now shuffled.");
            }
        }

        [Command("reshuffle"), Description("Reshuffles the queue. If queue is not shuffled, it won't enable shuffle mode.")]
        public async Task ReshuffleAsync(CommandContext ctx)
        {
            this.MusicPlayer.Reshuffle();
            await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":ok_hand:")} Queue reshuffled.");
        }

        [Command("remove"), Description("Removes a track from playback queue."), Aliases("del", "rm")]
        public async Task RemoveAsync(CommandContext ctx,
            [Description("Which track to remove.")] int index)
        {
            var itemN = this.MusicPlayer.Remove(index - 1);
            if (itemN == null)
            {
                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msraisedhand:")} No such track.");
                return;
            }

            var track = itemN.Value;
            await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":ok_hand:")} {Formatter.Bold(Formatter.Sanitize(track.Track.Title))} by {Formatter.Bold(Formatter.Sanitize(track.Track.Author))} removed.");
        }

        [Command("queue"), Description("Displays current playback queue."), Aliases("q")]
        public async Task QueueAsync(CommandContext ctx)
        {
            var interactivity = ctx.Client.GetInteractivity();

            if (this.MusicPlayer.RepeatMode == RepeatMode.Single)
            {
                var track = this.MusicPlayer.NowPlaying;
                await ctx.RespondAsync($"Queue repeats {Formatter.Bold(Formatter.Sanitize(track.Track.Title))} by {Formatter.Bold(Formatter.Sanitize(track.Track.Author))}.");
                return;
            }

            var pageCount = this.MusicPlayer.Queue.Count / 10 + 1;
            if (this.MusicPlayer.Queue.Count % 10 == 0) pageCount--;
            var pages = this.MusicPlayer.Queue.Select(x => x.ToTrackString())
                .Select((s, i) => new { str = s, index = i })
                .GroupBy(x => x.index / 10)
                .Select(xg => new Page($"Now playing: {this.MusicPlayer.NowPlaying.ToTrackString()}\n\n{string.Join("\n", xg.Select(xa => $"`{xa.index + 1:00}` {xa.str}"))}\n\n{(this.MusicPlayer.RepeatMode == RepeatMode.All ? "The entire queue is repeated.\n\n" : "")}Page {xg.Key + 1}/{pageCount}"))
                .ToArray();

            var trk = this.MusicPlayer.NowPlaying;
            if (!pages.Any())
            {
                if (trk.Track?.TrackString == null)
                    await ctx.RespondAsync("Queue is empty!");
                else
                    await ctx.RespondAsync($"Now playing: {this.MusicPlayer.NowPlaying.ToTrackString()}");

                return;
            }

            var ems = new PaginationEmojis
            {
                SkipLeft = null,
                SkipRight = null,
                Stop = DiscordEmoji.FromUnicode("⏹"),
                Left = DiscordEmoji.FromUnicode("◀"),
                Right = DiscordEmoji.FromUnicode("▶")
            };
            await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, pages, ems, PaginationBehaviour.Ignore, PaginationDeletion.KeepEmojis, TimeSpan.FromMinutes(2));
        }

        [Command("nowplaying"), Description("Displays information about currently-played track."), Aliases("np")]
        public async Task NowPlayingAsync(CommandContext ctx)
        {
            var track = this.MusicPlayer.NowPlaying;
            if (this.MusicPlayer.NowPlaying.Track?.TrackString == null)
            {
                await ctx.RespondAsync($"Not playing.");
            }
            else
            {
                await ctx.RespondAsync($"Now playing: {Formatter.Bold(Formatter.Sanitize(track.Track.Title))} by {Formatter.Bold(Formatter.Sanitize(track.Track.Author))} [{this.MusicPlayer.GetCurrentPosition().ToDurationString()}/{this.MusicPlayer.NowPlaying.Track.Length.ToDurationString()}] requested by {Formatter.Bold(Formatter.Sanitize(this.MusicPlayer.NowPlaying.RequestedBy.DisplayName))}.");
            }
        }

        [Command("playerinfo"), Description("Displays information about current player."), Aliases("pinfo", "pinf"), Hidden]
        public async Task PlayerInfoAsync(CommandContext ctx)
        {
            await ctx.RespondAsync($"Queue length: {this.MusicPlayer.Queue.Count}\nIs shuffled? {(this.MusicPlayer.IsShuffled ? "Yes" : "No")}\nRepeat mode: {this.MusicPlayer.RepeatMode}\nVolume: {this.MusicPlayer.Volume}%");
        }
    }
}