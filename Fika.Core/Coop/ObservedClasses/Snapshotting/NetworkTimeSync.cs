using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Fika.Core.Coop.ObservedClasses.Snapshotting
{
    /// <summary>
    /// Used to sync snapshots for replication
    /// </summary>
    public static class NetworkTimeSync
    {
        /// <summary>
        /// Gets the current time in the game since start as a <see cref="double"/>
        /// </summary>        
        public static double NetworkTime
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return Time.unscaledTimeAsDouble;
            }
        }
    }
}
