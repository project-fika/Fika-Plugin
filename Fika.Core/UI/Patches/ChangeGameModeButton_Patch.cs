﻿using EFT.UI;
using Fika.Core.Patching;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Fika.Core.UI.FikaUIGlobals;

namespace Fika.Core.UI.Patches
{
    public class ChangeGameModeButton_Patch : FikaPatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(ChangeGameModeButton).GetMethod(nameof(ChangeGameModeButton.Show));
        }

        [PatchPrefix]
        private static bool PrefixChange(TextMeshProUGUI ____buttonLabel, TextMeshProUGUI ____buttonDescription, Image ____buttonDescriptionIcon,
            GameObject ____availableState)
        {
            ____buttonLabel.text = "PvE";
            ____buttonDescription.text = $"Fika will always be {ColorizeText(EColor.BLUE, "PvE")}";
            ____buttonDescriptionIcon.gameObject.SetActive(false);
            ____availableState.SetActive(true);
            return false;
        }
    }
}