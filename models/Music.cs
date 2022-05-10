using System;
using System.Collections.Generic;
using System.Text;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
// using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
namespace aice_stable.Models
{
    public struct MusicItem
    {
        /// <summary>
        /// Gets the track to be played.
        /// </summary>
        [JsonIgnore]
        public LavalinkTrack Track { get; }

        /// <summary>
        /// Gets the member who requested the track.
        /// </summary>
        [JsonIgnore]
        public DiscordMember RequestedBy { get; }

        /// <summary>
        /// Constructs a new music queue items.
        /// </summary>
        /// <param name="track">Track to play.</param>
        /// <param name="requester">Who requested the track.</param>
        public MusicItem(LavalinkTrack track, DiscordMember requester)
        {
            this.Track = track;
            this.RequestedBy = requester;
        }
    }

    public struct MusicItemSerializable
    {
        [JsonProperty("track")]
        public string Track { get; set; }

        [JsonProperty("member_id")]
        public ulong MemberId { get; set; }

        public MusicItemSerializable(MusicItem mi)
        {
            this.Track = mi.Track.TrackString;
            this.MemberId = mi.RequestedBy.Id;
        }
    }
}
