using aice_stable.Models;
using DSharpPlus;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace aice_stable.services
{
    public static class AiceUtilities
    {
        /// <summary>
        /// Gets the properly-configured UTF8 encoder.
        /// </summary>
        public static UTF8Encoding UTF8 { get; } = new UTF8Encoding(false);

        /// <summary>
        /// Converts the string to a fixed-width string.
        /// </summary>
        /// <param name="s">String to fix the width of.</param>
        /// <param name="targetLength">Length that the string should be.</param>
        /// <returns>Adjusted string.</returns>
        public static string ToFixedWidth(this string s, int targetLength)
        {
            if (s == null)
                throw new NullReferenceException();

            if (s.Length < targetLength)
                return s.PadRight(targetLength, ' ');

            if (s.Length > targetLength)
                return s.Substring(0, targetLength);

            return s;
        }

        /// <summary>
        /// Gets the version of the bot's assembly.
        /// </summary>
        /// <returns>Bot version.</returns>
        public static string GetBotVersion()
        {
            var a = Assembly.GetExecutingAssembly();
            var av = a.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            return av.InformationalVersion;
        }

        /// <summary>
        /// Converts given <see cref="TimeSpan"/> to a duration string.
        /// </summary>
        /// <param name="ts">Time span to convert.</param>
        /// <returns>Duration string.</returns>
        public static string ToDurationString(this TimeSpan ts)
        {
            if (ts.TotalHours >= 1)
                return ts.ToString(@"h\:mm\:ss");
            return ts.ToString(@"m\:ss");
        }

        /// <summary>
        /// Converts given <see cref="MusicItem"/> to a track string.
        /// </summary>
        /// <param name="mi">Music item to convert.</param>
        /// <returns>Track string.</returns>
        public static string ToTrackString(this MusicItem x)
        {
            return $"{Formatter.Bold(Formatter.Sanitize(x.Track.Title))} by {Formatter.Bold(Formatter.Sanitize(x.Track.Author))} [{x.Track.Length.ToDurationString()}] (added by {x.RequestedBy.DisplayName})";
        }
    }
}
