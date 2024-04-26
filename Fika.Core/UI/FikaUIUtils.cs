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
            var obj = GameObject.Find("/Preloader UI/Preloader UI/Watermark");
            var labelObj = GameObject.Find("/Preloader UI/Preloader UI/Watermark/Label");

            if (labelObj != null)
            {
                Object.Destroy(labelObj);
            }

            var watermarkText = obj.GetComponent<ClientWatermark>();
            if (watermarkText != null)
            {
                Object.Destroy(watermarkText);
            }

            obj.active = true;
            var text = obj.AddComponent<TextMeshProUGUI>();
            text.horizontalAlignment = HorizontalAlignmentOptions.Center;
            text.verticalAlignment = VerticalAlignmentOptions.Bottom;
            text.margin = new Vector4(0, 0, 0, -350);
            text.text = overlayText;

            return text;
        }
    }
}
