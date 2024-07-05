using System.Reflection;
using System.Text;
using Fika.Core.Models;
using Newtonsoft.Json.Linq;
using Aki.Reflection.Patching;
using EFT.UI;

namespace Fika.Headless.Patches
{
    // Token: 0x0200000D RID: 13
    public class WebSocketReceivePatch : ModulePatch
    {
        // Token: 0x06000026 RID: 38 RVA: 0x00002544 File Offset: 0x00000744
        protected override MethodBase GetTargetMethod()
        {
            return typeof(NotificationManagerClass).GetMethod(nameof(NotificationManagerClass.method_6));
        }

        // Token: 0x06000027 RID: 39 RVA: 0x0000256C File Offset: 0x0000076C
        [PatchPrefix]
        private static bool Prefix(byte[] bytes)
        {
            string text = Encoding.UTF8.GetString(bytes);

            JObject jsonObject = JObject.Parse(text);
            bool flag = !jsonObject.ContainsKey("type");
            bool flag2;
            if (flag)
            {
                flag2 = true;
            }
            else
            {
                string type = jsonObject.Value<string>("type");
                jsonObject.Remove("type");
                string text2 = type;
                string text3 = text2;
                if (!(text3 == "fikaDedicatedStartRaid"))
                {
                    flag2 = true;
                }
                else
                {
                    StartDedicatedRequest request = jsonObject.ToObject<StartDedicatedRequest>();
                    FikaDedicatedPlugin.Instance.OnFikaStartRaid(request);
                    flag2 = false;
                }
            }
            return flag2;
        }
    }
}
