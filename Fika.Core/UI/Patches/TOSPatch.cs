using Aki.Reflection.Patching;
using Comfort.Common;
using EFT;
using EFT.UI;
using System;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace Fika.Core.UI.Patches
{
    public class TOSPatch : ModulePatch
    {
        protected const string str_1 = "V2VsY29tZSB0byBNUFQhCgpNUFQgaXMgYSBjby1vcCBtb2QgZm9yIFNQVCwgYWxsb3dpbmcgeW91IHRvIHBsYXkgd2l0aCB5b3VyIGZyaWVuZHMuIE1QVCBpcyBhbmQgd2lsbCBhbHdheXMgYmUgZnJlZSwgaWYgeW91IHBhaWQgZm9yIGl0IHlvdSBnb3Qgc2NhbW1lZC4gWW91IGFyZSBhbHNvIG5vdCBhbGxvd2VkIHRvIGhvc3QgcHVibGljIHNlcnZlcnMgd2l0aCBtb25ldGl6YXRpb24gb3IgZG9uYXRpb25zLgoKV2FpdCBmb3IgdGhpcyBtZXNzYWdlIHRvIGZhZGUgdG8gYWNjZXB0IG91ciBUZXJtcyBvZiBTZXJ2aWNlLgoKWW91IGNhbiBqb2luIG91ciBEaXNjb3JkIGhlcmU6IGh0dHBzOi8vZGlzY29yZC5nZy9GOWpIajhKekF3";
        protected const string str_2 = "V2VsY29tZSB0byBNUFQhCgpNUFQgaXMgYSBjby1vcCBtb2QgZm9yIFNQVCwgYWxsb3dpbmcgeW91IHRvIHBsYXkgd2l0aCB5b3VyIGZyaWVuZHMuIE1QVCBpcyBhbmQgd2lsbCBhbHdheXMgYmUgZnJlZSwgaWYgeW91IHBhaWQgZm9yIGl0IHlvdSBnb3Qgc2NhbW1lZC4gWW91IGFyZSBhbHNvIG5vdCBhbGxvd2VkIHRvIGhvc3QgcHVibGljIHNlcnZlcnMgd2l0aCBtb25ldGl6YXRpb24gb3IgZG9uYXRpb25zLgoKWW91IGNhbiBqb2luIG91ciBEaXNjb3JkIGhlcmU6IGh0dHBzOi8vZGlzY29yZC5nZy9GOWpIajhKekF3";

        private static bool HasShown = false;

        protected override MethodBase GetTargetMethod() => typeof(TarkovApplication).GetMethod(nameof(TarkovApplication.method_23));

        [PatchPostfix]
        public static void PostFix()
        {
            if (HasShown)
            {
                return;
            }

            HasShown = true;

            if (!FikaPlugin.AcceptedTOS.Value)
            {
                byte[] str_1_b = Convert.FromBase64String(str_1);
                string str_1_d = Encoding.UTF8.GetString(str_1_b);
                Singleton<PreloaderUI>.Instance.ShowCriticalErrorScreen("Fika", str_1_d, ErrorScreen.EButtonType.QuitButton, 30f,
                    Application.Quit,
                    () => { FikaPlugin.AcceptedTOS.Value = true; });
            }
            else
            {
                byte[] str_2_b = Convert.FromBase64String(str_2);
                string str_2_d = Encoding.UTF8.GetString(str_2_b);
                Singleton<PreloaderUI>.Instance.ShowCriticalErrorScreen("Fika", str_2_d, ErrorScreen.EButtonType.OkButton, 0f,
                    null,
                    null);
            }
        }
    }
}
