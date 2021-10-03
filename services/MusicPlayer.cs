using aice_stable.models;
using aice_stable.Models;
using aice_stable.services;
using aice_stable.Services;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using DSharpPlus.Lavalink.EventArgs;
using Emzi0767;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace aice_stable.services
{
    /// <summary>
    /// Yeah, I'm copying most of these codes. 
    /// </summary>
    public sealed class MusicPlayer : IIdentifiable
    {
        public string Identifier { get; }
        /// <summary>
        /// Gets the repeat mode set for this guild.
        /// </summary>
        public RepeatMode RepeatMode { get; private set; } = RepeatMode.None;

        /// <summary>
        /// Gets whether the queue for this guild is shuffled.
        /// </summary>
        public bool IsShuffled { get; private set; } = false;

        /// <summary>
        /// Gets whether a track is currently playing.
        /// </summary>
        public bool IsPlaying { get; private set; } = false;

        /// <summary>
        /// Gets the playback volume for this guild.
        /// </summary>
        public int Volume { get; private set; } = 100;

        /// <summary>
        /// Gets the current music queue.
        /// </summary>
        public IReadOnlyCollection<MusicItem> Queue { get; }

        /// <summary>
        /// Gets the currently playing item.
        /// </summary>
        public MusicItem NowPlaying { get; private set; } = default;

        /// <summary>
        /// Gets the channel in which the music is played.
        /// </summary>
        public DiscordChannel Channel => this.Player?.Channel;

        /// <summary>
        /// Gets or sets the channel in which commands are executed.
        /// </summary>
        public DiscordChannel CommandChannel { get; set; }
        private List<MusicItem> QueueInternal { get; }
        private SemaphoreSlim QueueInternalLock { get; }
        private string QueueSerialized { get; set; }
        private DiscordGuild Guild { get; }
        private SecureRandom RNG { get; }
        private RedisClient redis { get; }
        public LavalinkService Lavalink { get; }
        private LavalinkGuildConnection Player { get; set; }
        ///private RedisClient Redis { get; }
        ///
        /// <summary>
        /// Creates a new instance of playback data.
        /// </summary>
        /// <param name="guild">Guild to track data for.</param>
        /// <param name="rng">Cryptographically-secure random number generator implementation.</param>
        /// <param name="lavalink">Lavalink service.</param>
        /// <param name="redis">Redis service.</param>
        public MusicPlayer(DiscordGuild guild, SecureRandom rng, LavalinkService lavalink, RedisClient redis)
        {
            this.redis = redis;
            Guild = guild;
            RNG = rng;
            Lavalink = lavalink;
            Identifier = Guild.Id.ToString(CultureInfo.InvariantCulture);
            QueueInternalLock = new SemaphoreSlim(1, 1);
            QueueInternal = new List<MusicItem>();
            Queue = new ReadOnlyCollection<MusicItem>(QueueInternal);
        }

        /// <summary>
        /// Asynchronously saves data from this data tracker to specified redis instance.
        /// </summary>
        /// <returns></returns>
        internal async Task SaveAsync()
        {
            lock (this.QueueInternal)
            {
                this.QueueSerialized = JsonConvert.SerializeObject(this.QueueInternal.Select(x => new MusicItemSerializable(x)));
            }

            await this.redis.SetValueForAsync(this, x => x.RepeatMode);
            await this.redis.SetValueForAsync(this, x => x.IsShuffled);
            await this.redis.SetValueForAsync(this, x => x.Volume);
            await this.redis.SetValueForAsync(this, x => x.QueueSerialized);

            this.QueueSerialized = null;
        }

        /// <summary>
        /// Asynchronously loads data for this data tracker from specified redis instance.
        /// </summary>
        /// <returns></returns>
        internal async Task LoadAsync()
        {
            await this.redis.GetValueForAsync(this, x => x.RepeatMode, this.RepeatMode);
            await this.redis.GetValueForAsync(this, x => x.IsShuffled, this.IsShuffled);
            await this.redis.GetValueForAsync(this, x => x.Volume, this.Volume);
            await this.redis.GetValueForAsync(this, x => x.QueueSerialized, "[]");

            var rawQueue = JsonConvert.DeserializeObject<IEnumerable<MusicItemSerializable>>(this.QueueSerialized);
            this.QueueSerialized = null;

            var mbrs = await Task.WhenAll(rawQueue.Select(x => x.MemberId).Distinct().Select(x => this.Guild.GetMemberAsync(x)));
            var members = mbrs.ToDictionary(x => x.Id, x => x);

            lock (this.QueueInternal)
            {
                this.QueueInternal.Clear();
                foreach (var rawItem in rawQueue)
                    this.QueueInternal.Add(new MusicItem(LavalinkUtilities.DecodeTrack(rawItem.Track), members[rawItem.MemberId]));
            }
        }

        public async Task PlayAsync()
        {
            if (Player == null || !Player.IsConnected)
                return;

            if (NowPlaying.Track?.TrackString == null)
                await PlayHandlerAsync();
        }

        /// <summary>
        /// Stops the playback.
        /// </summary>
        public async Task StopAsync()
        {
            if (this.Player == null || !this.Player.IsConnected)
                return;

            this.NowPlaying = default;
            await this.Player.StopAsync();
        }

        /// <summary>
        /// Pauses the playback.
        /// </summary>
        public async Task PauseAsync()
        {
            if (Player == null || !Player.IsConnected)
                return;

            IsPlaying = false;
            await Player.PauseAsync();
        }

        /// <summary>
        /// Resumes the playback.
        /// </summary>
        public async Task ResumeAsync()
        {
            if (Player == null || !Player.IsConnected)
                return;

            IsPlaying = true;
            await Player.ResumeAsync();
        }

        /// <summary>
        /// Restarts current track.
        /// </summary>
        public async Task RestartAsync()
        {
            if (this.Player == null || !this.Player.IsConnected)
                return;

            if (this.NowPlaying.Track.TrackString == null)
                return;

            await this.QueueInternalLock.WaitAsync();
            try
            {
                this.QueueInternal.Insert(0, this.NowPlaying);
                await this.Player.StopAsync();
            }
            finally
            {
                this.QueueInternalLock.Release();
            }
        }

        /// <summary>
        /// Seeks the currently-playing track.
        /// </summary>
        /// <param name="target">Where or how much to seek by.</param>
        /// <param name="relative">Whether the seek is relative.</param>
        public async Task SeekAsync(TimeSpan target, bool relative)
        {
            if (this.Player == null || !this.Player.IsConnected)
                return;

            if (!relative)
                await this.Player.SeekAsync(target);
            else
                await this.Player.SeekAsync(this.Player.CurrentState.PlaybackPosition + target);
        }

        /// <summary>
        /// Empties the playback queue.
        /// </summary>
        /// <returns>Number of cleared items.</returns>
        public int EmptyQueue()
        {
            lock (QueueInternal)
            {
                var itemCount = QueueInternal.Count;
                QueueInternal.Clear();
                return itemCount;
            }
        }

        /// <summary>
        /// Returns number of queue in playlist.
        /// </summary>
        /// <returns> Number of items in playlist </returns>
        public int PlaylistQueue()
        {
            lock (QueueInternal)
            {
                return QueueInternal.Count;
            }
        }

        /// <summary>
        /// Shuffles the playback queue.
        /// </summary>
        public void Shuffle()
        {
            if (IsShuffled)
                return;

            IsShuffled = true;
            Reshuffle();
        }

        /// <summary>
        /// Reshuffles the playback queue.
        /// </summary>
        public void Reshuffle()
        {
            lock (QueueInternal)
            {
                QueueInternal.Sort(new Shuffler<MusicItem>(RNG));
            }
        }

        /// <summary>
        /// Causes the queue to no longer be shuffled.
        /// </summary>
        public void StopShuffle()
        {
            IsShuffled = false;
        }

        /// <summary>
        /// Sets the queue's repeat mode.
        /// </summary>
        public void SetRepeatMode(RepeatMode mode)
        {
            var pMode = RepeatMode;
            RepeatMode = mode;

            if (NowPlaying.Track.TrackString != null)
            {
                if (mode == RepeatMode.Single && mode != pMode)
                {
                    lock (QueueInternal)
                    {
                        QueueInternal.Insert(0, this.NowPlaying);
                    }
                }
                else if (mode != RepeatMode.Single && pMode == RepeatMode.Single)
                {
                    lock (QueueInternal)
                    {
                        QueueInternal.RemoveAt(0);
                    }
                }
            }
        }

        /// <summary>
        /// Enqueues a music track for playback.
        /// </summary>
        /// <param name="item">Music track to enqueue.</param>
        public void Enqueue(MusicItem item)
        {
            lock (this.QueueInternal)
            {
                if (this.RepeatMode == RepeatMode.All && this.QueueInternal.Count == 1)
                {
                    this.QueueInternal.Insert(0, item);
                }
                else if (!this.IsShuffled || !this.QueueInternal.Any())
                {
                    this.QueueInternal.Add(item);
                }
                else if (this.IsShuffled)
                {
                    var index = this.RNG.Next(0, this.QueueInternal.Count);
                    this.QueueInternal.Insert(index, item);
                }
            }
        }

        /// <summary>
        /// Dequeues next music item for playback.
        /// </summary>
        /// <returns>Dequeued item, or null if dequeueing fails.</returns>
        public MusicItem? Dequeue()
        {
            lock (this.QueueInternal)
            {
                if (this.QueueInternal.Count == 0)
                    return null;

                if (this.RepeatMode == RepeatMode.None)
                {
                    var item = this.QueueInternal[0];
                    this.QueueInternal.RemoveAt(0);
                    return item;
                }

                if (this.RepeatMode == RepeatMode.Single)
                {
                    var item = this.QueueInternal[0];
                    return item;
                }

                if (this.RepeatMode == RepeatMode.All)
                {
                    var item = this.QueueInternal[0];
                    this.QueueInternal.RemoveAt(0);
                    this.QueueInternal.Add(item);
                    return item;
                }
            }

            return null;
        }

        /// <summary>
        /// Removes a track from the queue.
        /// </summary>
        /// <param name="index">Index of the track to remove.</param>
        public MusicItem? Remove(int index)
        {
            lock (this.QueueInternal)
            {
                if (index < 0 || index >= this.QueueInternal.Count)
                    return null;

                var item = this.QueueInternal[index];
                this.QueueInternal.RemoveAt(index);
                return item;
            }
        }

        /// <summary>
        /// Creates a player for this guild.
        /// </summary>
        /// <returns></returns>
        public async Task CreatePlayerAsync(DiscordChannel channel)
        {
            if (this.Player != null && this.Player.IsConnected)
                return;

            this.Player = await this.Lavalink.LavalinkNode.ConnectAsync(channel);
            if (this.Volume != 100)
                await this.Player.SetVolumeAsync(this.Volume);
            this.Player.PlaybackFinished += this.Player_PlaybackFinished;
        }

        /// <summary>
        /// Destroys a player for this guild.
        /// </summary>
        /// <returns></returns>
        public async Task DestroyPlayerAsync()
        {
            if (this.Player == null)
                return;

            if (this.Player.IsConnected)
                await this.Player.DisconnectAsync();

            this.Player = null;
        }

        /// <summary>
        /// Gets the current position in the track.
        /// </summary>
        /// <returns>Position in the track.</returns>
        public TimeSpan GetCurrentPosition()
        {
            if (this.NowPlaying.Track.TrackString == null)
                return TimeSpan.Zero;

            return this.Player.CurrentState.PlaybackPosition;
        }

        private async Task Player_PlaybackFinished(LavalinkGuildConnection con, TrackFinishEventArgs e)
        {
            await Task.Delay(500);
            this.IsPlaying = false;
            await this.PlayHandlerAsync();
        }

        private async Task PlayHandlerAsync()
        {
            var itemN = this.Dequeue();
            if (itemN == null)
            {
                this.NowPlaying = default;
                return;
            }

            var item = itemN.Value;
            this.NowPlaying = item;
            this.IsPlaying = true;
            await this.Player.PlayAsync(item.Track);
        }
    }
}
