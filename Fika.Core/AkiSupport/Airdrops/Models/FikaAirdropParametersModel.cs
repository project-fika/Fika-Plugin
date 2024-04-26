using Aki.Custom.Airdrops.Models;
using Newtonsoft.Json;
using UnityEngine;

namespace Fika.Core.AkiSupport.Airdrops.Models
{
    /// <summary>
    /// Created by: SPT-Aki team
    /// Link: https://dev.sp-tarkov.com/SPT-AKI/Modules/src/branch/master/project/Aki.Custom/Airdrops/Models
    /// Paulov: Property instead of Fields so I can easily Json the Model
    /// </summary>
    public class FikaAirdropParametersModel
    {
        public FikaAirdropConfigModel Config { get; set; }
        public bool AirdropAvailable { get; set; }
        public bool PlaneSpawned { get; set; }
        public bool BoxSpawned { get; set; }
        public float DistanceTraveled { get; set; }
        public float DistanceToTravel { get; set; }
        public float DistanceToDrop { get; set; }
        public float Timer { get; set; }
        public int DropHeight { get; set; }
        public int TimeToStart { get; set; }
        public Vector3 StartPosition { get; set; }
        public Vector3 SpawnPoint { get; set; }
        public Vector3 LookPoint { get; set; }

        [JsonIgnore]
        public Vector3 RandomAirdropPoint { get; set; } = Vector3.zero;

        public float RandomAirdropPointX { get { return RandomAirdropPoint.x; } set { RandomAirdropPoint = new Vector3(value, RandomAirdropPoint.y, RandomAirdropPoint.z); } }
        public float RandomAirdropPointY { get { return RandomAirdropPoint.y; } set { RandomAirdropPoint = new Vector3(RandomAirdropPoint.x, value, RandomAirdropPoint.z); } }
        public float RandomAirdropPointZ { get { return RandomAirdropPoint.z; } set { RandomAirdropPoint = new Vector3(RandomAirdropPoint.x, RandomAirdropPoint.y, value); } }
    }
}