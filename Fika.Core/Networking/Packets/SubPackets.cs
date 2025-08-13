// © 2025 Lacyway All Rights Reserved

using EFT;
using System;

namespace Fika.Core.Networking.Packets;

/// <summary>
/// Class containing several static methods to serialize/deserialize sub-packages
/// </summary>
public class SubPackets
{
    

    public struct DeathInfoPacket
    {
        public string AccountId;
        public string ProfileId;
        public string Nickname;
        public string KillerAccountId;
        public string KillerProfileId;
        public string KillerName;
        public string Status;
        public string WeaponName;
        public string GroupId;

        public EPlayerSide Side;
        public int Level;
        public DateTime Time;
    }
}
