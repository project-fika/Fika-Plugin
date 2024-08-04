using System.Runtime.Serialization;

namespace Fika.Core.Networking.Http.Models
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

        public RegisterPlayerRequest(int crc, string profileId, int variantId)
        {
            Crc = crc;
            LocationId = profileId;
            VariantId = variantId;
        }
    }
}