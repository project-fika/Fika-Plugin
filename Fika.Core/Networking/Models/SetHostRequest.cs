using Fika.Core.Coop.Components;
using System.Runtime.Serialization;

namespace Fika.Core.Networking.Http
{
	[DataContract]
	public struct SetHostRequest
	{
		[DataMember(Name = "serverId")]
		public string ServerId;

		[DataMember(Name = "ips")]
		public string[] Ips;

		[DataMember(Name = "port")]
		public int Port;

		[DataMember(Name = "natPunch")]
		public bool NatPunch;

		[DataMember(Name = "isDedicated")]
		public bool IsDedicated;

		public SetHostRequest(string[] ips, int port, bool natPunch, bool isDedicated)
		{
			ServerId = CoopHandler.GetServerId();
			Ips = ips;
			Port = port;
			NatPunch = natPunch;
			IsDedicated = isDedicated;
		}
	}
}