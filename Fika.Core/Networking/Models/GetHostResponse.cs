using System.Runtime.Serialization;

namespace Fika.Core.Networking.Http
{
	[DataContract]
	public struct GetHostResponse
	{
		[DataMember(Name = "ips")]
		public string[] Ips;

		[DataMember(Name = "port")]
		public int Port;

		[DataMember(Name = "natPunch")]
		public bool NatPunch;

		[DataMember(Name = "isDedicated")]
		public bool IsDedicated;

		public GetHostResponse(string[] ips, int port, bool natPunch, bool isDedicated)
		{
			Ips = ips;
			Port = port;
			NatPunch = natPunch;
			IsDedicated = isDedicated;
		}
	}
}