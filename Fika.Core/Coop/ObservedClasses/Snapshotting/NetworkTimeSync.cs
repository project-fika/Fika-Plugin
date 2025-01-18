using System.Diagnostics;
using System.Threading.Tasks;

namespace Fika.Core.Coop.ObservedClasses.Snapshotting
{
    /// <summary>
    /// Used to sync snapshots for replication
    /// </summary>
    public static class NetworkTimeSync
    {
        private static readonly Stopwatch stopwatch = new();
        private static double Offset = 0;

        private static readonly object threadLock = new();

        /// <summary>
        /// Gets the current time in the game since start
        /// </summary>
        public static double Time
        {
            get
            {
                return stopwatch.Elapsed.TotalSeconds + Offset;
            }
        }

        /// <summary>
        /// Starts the time counter (with an optional offset from server)
        /// </summary>
        public static void Start(double serverOffset = 0)
        {
            lock (threadLock)
            {
                Offset = serverOffset;
                _ = Task.Run(StartInternal);
            }
        }

        /// <summary>
        /// Resets (clears and stops) the time counter
        /// </summary>
        public static void Reset()
        {
            lock (threadLock)
            {
                stopwatch.Reset();
                Offset = 0; 
            }
        }

        private static Task StartInternal()
        {
            lock (threadLock)
            {
                stopwatch.Restart();
            }
            return Task.CompletedTask;
        }
    }
}
