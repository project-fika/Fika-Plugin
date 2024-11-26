using System.Runtime.Serialization;

namespace Fika.Core.Networking.Http
{
	[DataContract]
	public struct RegisterPlayerRequest
	{
		[DataMember(Name = "crc")]
		public int Crc;

		[DataMember(Name = "locationId")]
		public string LocationId;

		[DataMember(Name = "variantId")]
		public int VariantId;

		public RegisterPlayerRequest(int crc, string locationId, int variantId)
		{
			Crc = crc;
			LocationId = locationId;
			VariantId = variantId;
		}
	}
}