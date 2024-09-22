using UnityEngine;

namespace Fika.Core.Coop.ObservedClasses.Snapshotting
{
	public class SnapshotInterpolationSettings
	{
		public SnapshotInterpolationSettings(double bufferTimeMultiplier)
		{
			this.bufferTimeMultiplier = bufferTimeMultiplier;
		}

		#region Buffering
		[Tooltip("Local simulation is behind by sendInterval * multiplier seconds.\nThis guarantees that we always have enough snapshots in the buffer to mitigate lags & jitter.\nIncrease this if the simulation isn't smooth. By default, it should be around 2.")]
		public double bufferTimeMultiplier = 2;

		[Tooltip("If a client can't process snapshots fast enough, don't store too many.")]
		public int bufferLimit = 32;
		#endregion

		#region Catchup / Slowdown
		[Tooltip("Slowdown begins when the local timeline is moving too fast towards remote time. Threshold is in frames worth of snapshots.\nThis needs to be negative.\nDon't modify unless you know what you are doing.")]
		public float catchupNegativeThreshold = -1;

		[Tooltip("Catchup begins when the local timeline is moving too slow and getting too far away from remote time. Threshold is in frames worth of snapshots.\nThis needs to be positive.\nDon't modify unless you know what you are doing.")]
		public float catchupPositiveThreshold = 1;

		[Tooltip("Local timeline acceleration in % while catching up.")]
		[Range(0, 1)]
		public double catchupSpeed = 0.02f;

		[Tooltip("Local timeline slowdown in % while slowing down.")]
		[Range(0, 1)]
		public double slowdownSpeed = 0.04f;

		[Tooltip("Catchup/Slowdown is adjusted over n-second exponential moving average.")]
		public int driftEmaDuration = 1;
		#endregion

		#region Dynamic Adjustment
		[Tooltip("Automatically adjust bufferTimeMultiplier for smooth results.\nSets a low multiplier on stable connections, and a high multiplier on jittery connections.")]
		public bool dynamicAdjustment = true;

		[Tooltip("Safety buffer that is always added to the dynamic bufferTimeMultiplier adjustment.")]
		public float dynamicAdjustmentTolerance = 1;

		[Tooltip("Dynamic adjustment is computed over n-second exponential moving average standard deviation.")]
		public int deliveryTimeEmaDuration = 2;
		#endregion
	}
}
