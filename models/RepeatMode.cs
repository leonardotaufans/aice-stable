using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace aice_stable.models
{

    /// Yes, this is stolen from https://github.com/Emzi0767/Discord-Music-Turret-Bot/blob/master/Emzi0767.MusicTurret/Data/RepeatMode.cs
    /// <summary>
    /// Represents music repeat mode.
    /// </summary>
    public enum RepeatMode
    {
        /// <summary>
        /// Defines that items are not to be repeated.
        /// </summary>
        None,

        /// <summary>
        /// Defines that a single item is to be repeated.
        /// </summary>
        Single,

        /// <summary>
        /// Defines that the entire queue is to be looped.
        /// </summary>
        All,

        /// <summary>
        /// Defines unknown mode. This indicates failed conversion.
        /// </summary>
        Unknown
    }

    /// <summary>
    /// Converts between <see cref="RepeatMode"/> and <see cref="string"/>.
    /// </summary>
    public sealed class RepeatModeConverter
    {
        private ImmutableDictionary<string, RepeatMode> Mapping { get; }

        /// <summary>
        /// Creates a new instance of this converter.
        /// </summary>
        public RepeatModeConverter()
        {
            var vals = Enum.GetValues(typeof(RepeatMode))
                .Cast<RepeatMode>()
                .Select(x => new { str = x.ToString().ToLowerInvariant(), @enum = x });

            var idb = ImmutableDictionary.CreateBuilder<string, RepeatMode>();
            foreach (var a in vals)
                idb.Add(a.str, a.@enum);

            this.Mapping = idb.ToImmutable();
        }

        /// <summary>
        /// Attempts to convert given string to <see cref="RepeatMode" />.
        /// </summary>
        /// <param name="value">String to convert.</param>
        /// <param name="mode">Converted value or <see cref="RepeatMode.Unknown"/> if conversion fails.</param>
        /// <returns>Whether the conversion succeeded.</returns>
        public bool TryFromString(string value, out RepeatMode mode)
        {
            mode = RepeatMode.Unknown;
            if (value == null)
                return false;

            value = value.ToLowerInvariant();
            if (!this.Mapping.ContainsKey(value))
                return false;

            mode = this.Mapping[value];
            return true;
        }

        /// <summary>
        /// Converts the given string from string to <see cref="RepeatMode"/>.
        /// </summary>
        /// <param name="value">String to convert.</param>
        /// <returns>Converted value.</returns>
        /// <exception cref="ArgumentException">Invalid value supplied.</exception>
        public RepeatMode FromString(string value)
        {
            if (!this.TryFromString(value, out var mode))
                throw new ArgumentException("Invalid value specified.", nameof(value));

            return mode;
        }

        /// <summary>
        /// Converts the given <see cref="RepeatMode"/> to string.
        /// </summary>
        /// <param name="mode"><see cref="RepeatMode"/> to convert.</param>
        /// <returns>Converted string or <see cref="null"/> if the conversion fails.</returns>
        public string ToString(RepeatMode mode)
            => mode != RepeatMode.Unknown ? mode.ToString().ToLowerInvariant() : null;
    }
}
