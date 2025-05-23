﻿using System.Runtime.CompilerServices;

namespace Fika.Core.Coop.ObservedClasses.Snapshotting
{
    /// <summary>
    /// Unity 2020 doesn't have Math.Clamp yet
    /// </summary>
    public static class Mathd
    {
        /// <summary>
        /// Clamps value between 0 and 1 and returns value
        /// </summary>
        /// <param name="value"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Clamp(double value, double min, double max)
        {
            if (value < min)
            {
                return min;
            }

            if (value > max)
            {
                return max;
            }

            return value;
        }

        /// <summary>
        /// Clamps value between 0 and 1 and returns value
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Clamp01(double value)
        {
            return Clamp(value, 0, 1);
        }

        /// <summary>
        /// Calculates the linear parameter t that produces the interpolant value within the range [a, b]
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double InverseLerp(double a, double b, double value)
        {
            return a != b ? Clamp01((value - a) / (b - a)) : 0;
        }

        /// <summary>
        /// Linearly interpolates between a and b by t with no limit to t
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double LerpUnclamped(double a, double b, double t)
        {
            return a + (b - a) * t;
        }
    }
}
