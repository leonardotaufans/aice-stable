using aice_stable.Models;
using Emzi0767;
using System.Collections.Generic;

/// <summary>
/// Using code completely from https://github.com/Emzi0767/Discord-Music-Turret-Bot/blob/master/Emzi0767.MusicTurret/Shuffler.cs
/// with some modifications to rid of some useless stuff and more commentary on the futility of life.
/// Cause I ain't dealin with this shit.
/// </summary>
namespace aice_stable.services
{
    /// <summary>
    /// Comparer implementation which uses a cryptographically-secure random number generator to shuffle items.
    /// </summary>
    /// <typeparam name="T">Type of the item.</typeparam>
    public class Shuffler<T> : IComparer<T>
    {
        public SecureRandom RNG { get; }

        /// <summary>
        /// Creates a new shuffler.
        /// </summary>
        /// <param name="rng">Cryptographically-secure random number generator.</param>
        public Shuffler(SecureRandom rng)
        {
            this.RNG = rng;
        }

        /// <summary>
        /// Returns a random order for supplied items.
        /// </summary>
        /// <param name="x">First item.</param>
        /// <param name="y">Second item.</param>
        /// <returns>Random order for the items.</returns>
        public int Compare(T x, T y)
        {
            var val1 = RNG.Next();
            var val2 = RNG.Next();

            if (val1 > val2)
                return 1;
            if (val1 < val2)
                return -1;
            return 0;
        }
    }
}