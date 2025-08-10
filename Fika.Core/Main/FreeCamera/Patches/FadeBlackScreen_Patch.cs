using EFT.UI;
using Fika.Core.Patching;
using System;
using System.Reflection;
using UnityEngine.UI;

namespace Fika.Core.Main.FreeCamera.Patches
{
    [IgnoreAutoPatch]
    internal class FadeBlackScreen_Patch : FikaPatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(PreloaderUI)
                .GetMethod(nameof(PreloaderUI.FadeBlackScreen));
        }

        [PatchPrefix]
        public static bool Prefix()
        {
            return false;
        }
    }

    [IgnoreAutoPatch]
    internal class StartBlackScreenShow_Patch : FikaPatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(PreloaderUI).GetMethod(nameof(PreloaderUI.StartBlackScreenShow));
        }

        [PatchPrefix]
        public static bool Prefix()
        {
            return false;
        }

        [PatchPostfix]
        public static void Postfix(Action callback)
        {
            callback?.Invoke();
        }
    }

    [IgnoreAutoPatch]
    internal class SetBlackImageAlpha_Patch : FikaPatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(PreloaderUI).GetMethod(nameof(PreloaderUI.SetBlackImageAlpha));
        }

        [PatchPrefix]
        public static bool Prefix()
        {
            return false;
        }

        [PatchPostfix]
        public static void Postfix(float alpha, Image ____overlapBlackImage)
        {
            ____overlapBlackImage.gameObject.SetActive(value: true);
            ____overlapBlackImage.color = new Color(0f, 0f, 0f, 0f);
        }
    }
}
