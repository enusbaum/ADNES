﻿using System.Runtime.CompilerServices;

namespace ADNES.Common.Extensions
{
    /// <summary>
    ///     Extension Methods for typical functions performed on int values in ADNES
    /// </summary>
    internal static class IntExtensions
    {
        /// <summary>
        ///     Returns if the specified bit was set
        /// </summary>
        /// <param name="b"></param>
        /// <param name="bitMask"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsFlagSet(this int b, int bitMask) => (b & bitMask) != 0;
    }
}
