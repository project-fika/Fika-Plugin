using Comfort.Common;
using Fika.Core.Coop.Players;
using Fika.Core.Coop.Utils;
using Fika.Core.Networking;
using System.Collections.Generic;
using System.Security.Policy;
using UnityEngine;

namespace Fika.Core.Coop.ObservedClasses.Snapshotter
{
	public class FikaSnapshotter : MonoBehaviour
	{
		private readonly SortedList<double, PlayerStatePacket> buffer = [];
		private double localTimeline;
		private double localTimeScale = 1;
		private readonly SnapshotInterpolationSettings interpolationSettings = new();
		private ExponentialMovingAverage driftEma;
		private ExponentialMovingAverage deliveryTimeEma;
		private ObservedCoopPlayer player;
		private int sendRate;

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
				return 30 * interpolationSettings.bufferTimeMultiplier;
			}
		}

		private void Awake()
		{
			driftEma = new ExponentialMovingAverage(sendRate * interpolationSettings.driftEmaDuration);
			deliveryTimeEma = new ExponentialMovingAverage(sendRate * interpolationSettings.deliveryTimeEmaDuration);

			if (FikaBackendUtils.IsServer)
			{
				sendRate = Singleton<FikaServer>.Instance.SendRate;
			}
			else
			{
				sendRate = Singleton<FikaClient>.Instance.SendRate;
			}
		}

		private void Update()
		{
			if (buffer.Count > 0)
			{
				SnapshotInterpolation.Step(buffer, Time.deltaTime, ref localTimeline, localTimeScale, out PlayerStatePacket fromSnapshot,
					out PlayerStatePacket toSnapshot, out double ratio);

				player.Interpolate(fromSnapshot, toSnapshot, ratio);
			}
		}

		public void Insert(PlayerStatePacket snapshot)
		{
			snapshot.LocalTime = Time.time;

			SnapshotInterpolation.InsertAndAdjust(buffer, interpolationSettings.bufferLimit, snapshot, ref localTimeline, ref localTimeScale,
				sendRate, BufferTime, interpolationSettings.catchupSpeed, interpolationSettings.slowdownSpeed, ref driftEma,
				interpolationSettings.catchupNegativeThreshold, interpolationSettings.catchupPositiveThreshold, ref deliveryTimeEma);
		}
	}
}
