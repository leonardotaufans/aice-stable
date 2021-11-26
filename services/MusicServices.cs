using aice_stable.Services;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using DSharpPlus.Lavalink.EventArgs;
using Emzi0767;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace aice_stable.services
{
    public sealed class MusicServices
    {
        private LavalinkService Lavalink { get; }
        private SecureRandom RNG { get; }
        private RedisClient redis { get; }
        private ConcurrentDictionary<ulong, MusicPlayer> MusicData { get; }
        private DiscordClient discord { get; }
        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="lavalink"></param>
        /// <param name="rng"></param>
        public MusicServices(RedisClient redis, LavalinkService lavalink, SecureRandom rng, Aice aice)
        {
            this.redis = redis;
            Lavalink = lavalink;
            RNG = rng;
            MusicData = new ConcurrentDictionary<ulong, MusicPlayer>();
            discord = aice.discord;
            Lavalink.TrackExceptionThrown += Lavalink_TrackExceptionThrown;
        }

        /// <summary>
        /// Saves data for specified guild.
        /// </summary>
        /// <param name="guild">Guild to save data for.</param>
        /// <returns></returns>
        public Task SaveDataForAsync(DiscordGuild guild)
        {
            if (this.MusicData.TryGetValue(guild.Id, out var gmd))
                return gmd.SaveAsync();
            return Task.CompletedTask;
        }


        /// <summary>
        /// Gets or creates a dataset for specified guild.
        /// </summary>
        /// <param name="guild">Guild to get or create dataset for.</param>
        /// <returns>Resulting dataset.</returns>
        public async Task<MusicPlayer> GetOrCreateDataAsync(DiscordGuild guild)
        {
            if (this.MusicData.TryGetValue(guild.Id, out var gmd))
                return gmd;

            gmd = this.MusicData.AddOrUpdate(guild.Id, new MusicPlayer(guild, this.RNG, this.Lavalink, this.redis), (k, v) => v);
            await gmd.LoadAsync();

            return gmd;
        }


        /// <summary>
        /// Loads track from specified URL
        /// </summary>
        /// <param name="uri">Source URL</param>
        /// <returns>Loaded tracks</returns>
        public Task<LavalinkLoadResult> GetTracksAsync(Uri uri)
            => Lavalink.LavalinkNode.Rest.GetTracksAsync(uri);

        /// <summary>
        /// As the name suggests, this is to shuffle the song.
        /// </summary>
        /// <param name="tracks"></param>
        /// <returns></returns>
        public IEnumerable<LavalinkTrack> Shuffle(IEnumerable<LavalinkTrack> tracks)
            => tracks.OrderBy(x => RNG.Next());

        /// <summary>
        /// Exception data
        /// </summary>
        /// <param name="con"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        private async Task Lavalink_TrackExceptionThrown(LavalinkGuildConnection con, TrackExceptionEventArgs e)
        {
            if (e.Player?.Guild == null)
                return;

            if (!MusicData.TryGetValue(e.Player.Guild.Id, out var gmd))
                return;

            await gmd.CommandChannel.SendMessageAsync($"{DiscordEmoji.FromName(this.discord, ":frowning:")} A problem occured while playing {Formatter.Bold(Formatter.Sanitize(e.Track.Title))} by {Formatter.Bold(Formatter.Sanitize(e.Track.Author))}:\n{e.Error}");
        }
    }
}
