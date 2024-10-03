using System.Diagnostics;

namespace Fika.Core.Coop.ObservedClasses.Snapshotting
{
	/// <summary>
	/// Used to sync snapshots for replication
	/// </summary>
	public static class NetworkTimeSync
	{
		private static readonly Stopwatch stopwatch = new();
		private static double Offset = 0;

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
		/// Starts the time counter
		/// </summary>
		public static void Start()
		{
			stopwatch.Restart();
		}

		/// <summary>
		/// Resets (clears and stops) the time counter
		/// </summary>
		public static void Reset()
		{
			stopwatch.Reset();
		}
	}
}
