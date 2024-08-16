using Newtonsoft.Json;
using UnityEngine;

namespace Fika.Core.Coop.Airdrops.Models
{
	/// <summary>
	/// Created by: SPT team
	/// Link: https://dev.sp-tarkov.com/SPT/Modules/src/branch/master/project/SPT.Custom/Airdrops/Models
	/// </summary>
	public class FikaAirdropParametersModel
	{
		public FikaAirdropConfigModel Config;
		public bool AirdropAvailable;
		public bool PlaneSpawned;
		public bool BoxSpawned;
		public float DistanceTraveled;
		public float DistanceToTravel;
		public float DistanceToDrop;
		public float Timer;
		public int DropHeight;
		public int TimeToStart;
		public Vector3 StartPosition;
		public Vector3 SpawnPoint;
		public Vector3 LookPoint;

		[JsonIgnore]
		public Vector3 RandomAirdropPoint = Vector3.zero;
	}
}