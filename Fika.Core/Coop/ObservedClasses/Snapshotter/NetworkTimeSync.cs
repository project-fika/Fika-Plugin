using System.Diagnostics;

namespace Fika.Core.Coop.ObservedClasses.Snapshotter
{
	/// <summary>
	/// Used to sync snapshots for replication
	/// </summary>
	public static class NetworkTimeSync
	{
		private static readonly Stopwatch stopwatch = new();

		/// <summary>
		/// Gets the current time in the game since start
		/// </summary>
		public static double Time
		{
			get
			{
				return stopwatch.Elapsed.TotalSeconds;
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
