using Aki.Reflection.Patching;
using EFT.UI;
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace Fika.Core.Coop.FreeCamera
{
    internal class FadeBlackScreen_Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => typeof(PreloaderUI).GetMethod(nameof(PreloaderUI.FadeBlackScreen));

        [PatchPrefix]
        public static bool Prefix()
        {
            return false;
        }
    }

    internal class StartBlackScreenShow_Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => typeof(PreloaderUI).GetMethod(nameof(PreloaderUI.StartBlackScreenShow));

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

    internal class SetBlackImageAlpha_Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => typeof(PreloaderUI).GetMethod(nameof(PreloaderUI.SetBlackImageAlpha));

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
