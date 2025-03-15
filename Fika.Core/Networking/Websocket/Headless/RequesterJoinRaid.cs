using Newtonsoft.Json;

namespace Fika.Core.Networking.Websocket.Headless
{
    public class RequesterJoinRaid
    {
        [JsonProperty("type")]
        public EFikaHeadlessWSMessageTypes Type;
        [JsonProperty("matchId")]
        public string MatchId;
    }
}
