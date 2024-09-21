using Comfort.Common;
using EFT;
using Fika.Core.Coop.Players;
using Fika.Core.Coop.Utils;
using Fika.Core.Networking;
using System.Collections.Generic;
using UnityEngine;

namespace Fika.Core.Coop.ObservedClasses.Snapshotter
{
	public class FikaSnapshotter : MonoBehaviour
	{
		private readonly SortedList<double, PlayerStatePacket> buffer = [];
		private double localTimeline;
		private double localTimeScale = Time.timeScale;
		private readonly SnapshotInterpolationSettings interpolationSettings = new();
		private ExponentialMovingAverage driftEma;
		private ExponentialMovingAverage deliveryTimeEma;
		private ObservedCoopPlayer player;
		private int sendRate;
		private float sendInterval;

		public static FikaSnapshotter Create(ObservedCoopPlayer player)
		{
			FikaSnapshotter snapshotter = player.gameObject.AddComponent<FikaSnapshotter>();
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
				player.Interpolate(toSnapshot, fromSnapshot, ratio);
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
