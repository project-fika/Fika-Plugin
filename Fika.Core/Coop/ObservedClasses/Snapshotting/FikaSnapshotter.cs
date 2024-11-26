using Comfort.Common;
using Fika.Core.Coop.Players;
using Fika.Core.Coop.Utils;
using Fika.Core.Networking;
using System.Collections.Generic;
using UnityEngine;

namespace Fika.Core.Coop.ObservedClasses.Snapshotting
{
	public class FikaSnapshotter : MonoBehaviour
	{
		private SortedList<double, PlayerStatePacket> buffer;
		private double localTimeline;
		private double localTimeScale;
		private SnapshotInterpolationSettings interpolationSettings;
		private ExponentialMovingAverage driftEma;
		private ExponentialMovingAverage deliveryTimeEma;
		private ObservedCoopPlayer player;
		private int sendRate;
		private float sendInterval;

		public static FikaSnapshotter Create(ObservedCoopPlayer player)
		{
			FikaSnapshotter snapshotter = player.gameObject.AddComponent<FikaSnapshotter>();
			snapshotter.buffer = [];
			snapshotter.localTimeScale = Time.timeScale;
			snapshotter.player = player;
			return snapshotter;
		}

		private double BufferTime
		{
			get
			{
				return sendInterval * interpolationSettings.bufferTimeMultiplier;
			}
		}

		private void Awake()
		{
			double smoothingRate = FikaPlugin.SmoothingRate.Value switch
			{
				FikaPlugin.ESmoothingRate.Low => 1.5,
				FikaPlugin.ESmoothingRate.Medium => 2,
				FikaPlugin.ESmoothingRate.High => 2.5,
				_ => 2,
			};
			interpolationSettings = new(smoothingRate);
			driftEma = new(sendRate * interpolationSettings.driftEmaDuration);
			deliveryTimeEma = new(sendRate * interpolationSettings.deliveryTimeEmaDuration);
			if (FikaBackendUtils.IsServer)
			{
				sendRate = Singleton<FikaServer>.Instance.SendRate;
			}
			else
			{
				sendRate = Singleton<FikaClient>.Instance.SendRate;
			}
			sendInterval = 1f / sendRate;
		}

		private void Update()
		{
			if (buffer.Count > 0)
			{
				SnapshotInterpolation.Step(buffer, Time.unscaledDeltaTime, ref localTimeline, localTimeScale, out PlayerStatePacket fromSnapshot,
					out PlayerStatePacket toSnapshot, out double ratio);
				player.Interpolate(ref toSnapshot, ref fromSnapshot, ratio);
			}
		}

		public void Insert(PlayerStatePacket snapshot)
		{
			snapshot.LocalTime = NetworkTimeSync.Time;
			SnapshotInterpolation.InsertAndAdjust(buffer, interpolationSettings.bufferLimit, snapshot, ref localTimeline, ref localTimeScale,
				sendInterval, BufferTime, interpolationSettings.catchupSpeed, interpolationSettings.slowdownSpeed, ref driftEma,
				interpolationSettings.catchupNegativeThreshold, interpolationSettings.catchupPositiveThreshold, ref deliveryTimeEma);
		}
	}
}
