// © 2024 Lacyway All Rights Reserved

using EFT.UI;
using TMPro;
using UnityEngine;

namespace Fika.Core.UI
{
    internal class FikaUIUtils
    {
        public static TextMeshProUGUI CreateOverlayText(string overlayText)
        {
            GameObject obj = GameObject.Find("/Preloader UI/Preloader UI/Watermark");
            GameObject labelObj = GameObject.Find("/Preloader UI/Preloader UI/Watermark/Label");

            if (labelObj != null)
            {
                Object.Destroy(labelObj);
            }

            ClientWatermark watermarkText = obj.GetComponent<ClientWatermark>();
            if (watermarkText != null)
            {
                Object.Destroy(watermarkText);
            }

            obj.active = true;
            TextMeshProUGUI text = obj.AddComponent<TextMeshProUGUI>();
            text.horizontalAlignment = HorizontalAlignmentOptions.Center;
            text.verticalAlignment = VerticalAlignmentOptions.Bottom;
            text.margin = new Vector4(0, 0, 0, -350);
            text.text = overlayText;

            return text;
        }
    }
}
