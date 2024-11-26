using Fika.Core.Coop.Components;
using LiteNetLib.Utils;
using System;

namespace Fika.Core.Networking
{
	public interface IFikaNetworkManager
	{
		public CoopHandler CoopHandler { get; set; }
		public void RegisterPacket<T>(Action<T> handle) where T : INetSerializable, new();
		public void RegisterPacket<T, TUserData>(Action<T, TUserData> handle) where T : INetSerializable, new();
		public void PrintStatistics();
	}
}
