using LiteNetLib.Utils;

namespace Fika.Core.Networking
{
	public struct UsableItemPacket(int netId) : INetSerializable
	{
		public int NetId = netId;
		public bool HasCompassState;
		public bool CompassState;
		public bool ExamineWeapon;
		public bool HasAim;
		public bool AimState;

		public void Deserialize(NetDataReader reader)
		{
			NetId = reader.GetInt();
			HasCompassState = reader.GetBool();
			if (HasCompassState)
			{
				CompassState = reader.GetBool();
			}
			ExamineWeapon = reader.GetBool();
			HasAim = reader.GetBool();
			if (HasAim)
			{
				AimState = reader.GetBool();
			}
		}

		public void Serialize(NetDataWriter writer)
		{
			writer.Put(NetId);
			writer.Put(HasCompassState);
			if (HasCompassState)
			{
				writer.Put(CompassState);
			}
			writer.Put(ExamineWeapon);
			writer.Put(HasAim);
			if (HasAim)
			{
				writer.Put(AimState);
			}
		}
	}
}
