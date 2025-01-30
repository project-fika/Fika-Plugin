using Fika.Core.Networking.Http;
using Newtonsoft.Json;

namespace Fika.Core.Networking.Websocket.Headless
{
    public class StartRaid
    {
        [JsonProperty("type")]
        public EFikaHeadlessWSMessageTypes Type;
        [JsonProperty("startRequest")]
        public StartHeadlessRequest StartRequest;
    }
}
