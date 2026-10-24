// © 2026 Lacyway All Rights Reserved

using Diz.Utils;
using EFT;
using EFT.InputSystem;
using EFT.UI;
using Fika.Core.Main.Utils;
using HarmonyLib;
using JsonType;
using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using static EFT.UI.PreloaderUI;

namespace Fika.Core.UI;

/// <summary>
/// Provides global UI utilities and helpers
/// </summary>
public static class FikaUIGlobals
{
    /// <summary>
    /// Dictionary mapping <see cref="EColor"/> values to their corresponding hex color codes.
    /// </summary>
    private static readonly Dictionary<EColor, string> _keyValuePairs = new()
    {
        { EColor.WHITE, "ffffff" },
        { EColor.BLACK, "000000" },
        { EColor.GREEN, "32a852" },
        { EColor.BROWN, "a87332" },
        { EColor.BLUE, "51c6db" },
        { EColor.RED, "a83232" }
    };

    /// <summary>
    /// Creates an overlay text element in the UI with the specified text.
    /// </summary>
    /// <param name="overlayText">The text to display in the overlay.</param>
    /// <returns>The created <see cref="TextMeshProUGUI"/> component.</returns>
    public static TextMeshProUGUI CreateOverlayText(string overlayText, Vector4 margin = default)
    {
        var obj = GameObject.Find("/Preloader UI/Preloader UI/Watermark");
        var labelObj = GameObject.Find("/Preloader UI/Preloader UI/Watermark/Label");

        if (labelObj != null)
        {
            UnityEngine.Object.Destroy(labelObj);
        }

        var watermarkText = obj.GetComponent<ClientWatermark>();
        if (watermarkText != null)
        {
            UnityEngine.Object.Destroy(watermarkText);
        }

        obj.SetActive(true);
        if (!obj.TryGetComponent<TextMeshProUGUI>(out var text))
        {
            text = obj.AddComponent<TextMeshProUGUI>();
        }
        text.horizontalAlignment = HorizontalAlignmentOptions.Center;
        text.verticalAlignment = VerticalAlignmentOptions.Bottom;
        text.margin = margin == default ? new Vector4(0f, 0f, 0f, -350f) : margin;
        text.SetText(overlayText);

        return text;
    }

    /// <summary>
    /// Shows a Fika message using the <see cref="PreloaderUI"/> error screen.
    /// </summary>
    /// <param name="preloaderUI">The preloader UI instance.</param>
    /// <param name="header">The header text for the message.</param>
    /// <param name="message">The message body text.</param>
    /// <param name="buttonType">The type of button to display.</param>
    /// <param name="waitingTime">The time to wait before auto-closing the message.</param>
    /// <param name="acceptCallback">Callback to invoke when the message is accepted.</param>
    /// <param name="endTimeCallback">Callback to invoke when the waiting time ends.</param>
    /// <returns>The context object for the displayed message.</returns>
    public static ErrorWindowContext ShowFikaMessage(this PreloaderUI preloaderUI, string header, string message,
        ErrorScreen.EButtonType buttonType, float waitingTime, Action acceptCallback, Action endTimeCallback)
    {
        if (!AsyncWorker.CheckIsMainThread())
        {
            FikaGlobals.LogError("You are trying to show error screen from non-main thread!");
            return new ErrorWindowContext();
        }

        var errorScreenContainer = preloaderUI._criticalErrorScreenContainer;
        var errorScreen = UnityEngine.Object.Instantiate(preloaderUI._criticalErrorScreenTemplate, errorScreenContainer.transform, false);
        errorScreenContainer.AddChildNode(errorScreen);
        var context = errorScreen.ShowFikaMessage(header, message, acceptCallback, waitingTime, endTimeCallback, buttonType, true);

        Action onClose = null;
        onClose = () =>
        {
            UnityEngine.Object.Destroy(errorScreen.gameObject);
            errorScreenContainer.RemoveChildNode(errorScreen);
            context.OnClose -= onClose;
        };
        context.OnClose += onClose;
        return context;
    }

    /// <summary>
    /// Shows a Fika message using the <see cref="ErrorScreen"/> UI.
    /// </summary>
    /// <param name="errorScreen">The error screen instance.</param>
    /// <param name="title">The title of the error message.</param>
    /// <param name="message">The message body text.</param>
    /// <param name="closeManuallyCallback">Callback to invoke when the message is closed manually.</param>
    /// <param name="waitingTime">The time to wait before auto-closing the message.</param>
    /// <param name="timeOutCallback">Callback to invoke when the waiting time ends.</param>
    /// <param name="buttonType">The type of button to display.</param>
    /// <param name="removeHtml">Whether to remove HTML tags from the message.</param>
    /// <returns>The context object for the displayed message.</returns>
    public static ErrorWindowContext ShowFikaMessage(this ErrorScreen errorScreen, string title, string message,
        Action closeManuallyCallback = null, float waitingTime = 0f, Action timeOutCallback = null,
        ErrorScreen.EButtonType buttonType = ErrorScreen.EButtonType.OkButton, bool removeHtml = true)
    {
        var context = errorScreen.Show(title, message, waitingTime, buttonType, removeHtml);
        var onClose = timeOutCallback ?? closeManuallyCallback;
        if (onClose != null)
        {
            context.OnAccept += onClose;
        }
        context.OnDecline += Application.Quit;

        var text = buttonType switch
        {
            ErrorScreen.EButtonType.OkButton => "I UNDERSTAND",
            ErrorScreen.EButtonType.CancelButton => "CANCEL",
            ErrorScreen.EButtonType.QuitButton => "I DECLINE",
            _ => throw new ArgumentOutOfRangeException()
        };
        errorScreen._exitButton.SetHeaderText(text, errorScreen._exitButton.HeaderSize);
        return context;
    }

    /// <summary>
    /// Gets the hex color code for the specified <see cref="EColor"/>.
    /// </summary>
    /// <param name="color">The color enum value.</param>
    /// <returns>The hex color code as a string.</returns>
    private static string GetHexByColor(EColor color)
    {
        return _keyValuePairs.TryGetValue(color, out var value) ? value : "ffffff";
    }

    /// <summary>
    /// Utility used to color text within a <see cref="CustomTextMeshProUGUI"/>.
    /// </summary>
    /// <param name="color">The color to return the text in.</param>
    /// <param name="text">The original text.</param>
    /// <returns>Text in color.</returns>
    public static string ColorizeText(EColor color, string text)
    {
        return $"<color=#{GetHexByColor(color)}>{text}</color>";
    }

    /// <summary>
    /// Utility used to make text bold within a <see cref="CustomTextMeshProUGUI"/>.
    /// </summary>
    /// <param name="text">The original text.</param>
    /// <returns>Text in bold.</returns>
    public static string BoldText(string text)
    {
        return $"<b>{text}</b>";
    }

    /// <summary>
    /// Gets a static date and time value used for certain UI displays.
    /// </summary>
    public static DateTime StaticTime
    {
        get
        {
            return new DateTime(2016, 8, 4, 15, 28, 0);
        }
    }

    /// <summary>
    /// Returns a formatted time string based on the specified <see cref="EDateTime"/> and static time flag.
    /// </summary>
    /// <param name="time">The time type to format (current or previous).</param>
    /// <param name="staticTime">Whether to use the static time or backend time.</param>
    /// <returns>The formatted time string, or an empty string if unavailable.</returns>
    public static string FormattedTime(EDateTime time, bool staticTime)
    {
        if (TarkovApplication.Exist(out var tarkovApplication))
        {
            if (tarkovApplication.Session != null)
            {
                var session = tarkovApplication.Session;

                if (staticTime)
                {
                    var staticDate = StaticTime;
                    return time == EDateTime.CURR ? staticDate.ToString("HH:mm:ss") : staticDate.AddHours(-12).ToString("HH:mm:ss");
                }

                var backendTime = session.GetCurrentLocationTime;
                if (backendTime.ToManaged() == DateTime.MinValue)
                {
                    return "";
                }
                return time == EDateTime.CURR ? backendTime.ToString("HH:mm:ss") : backendTime.AddHours(-12).ToString("HH:mm:ss");
            }
        }
        return "";
    }

    /// <summary>
    /// Represents the presence state of a Fika player.
    /// </summary>
    public enum EFikaPlayerPresence
    {
        /// <summary>
        /// Player is in the menu.
        /// </summary>
        IN_MENU,
        /// <summary>
        /// Player is in a raid.
        /// </summary>
        IN_RAID,
        /// <summary>
        /// Player is in the stash.
        /// </summary>
        IN_STASH,
        /// <summary>
        /// Player is in the hideout.
        /// </summary>
        IN_HIDEOUT,
        /// <summary>
        /// Player is in the flea market.
        /// </summary>
        IN_FLEA
    }

    /// <summary>
    /// Enum used for <see cref="ColorizeText"/> to specify text color.
    /// </summary>
    public enum EColor
    {
        /// <summary>
        /// White color.
        /// </summary>
        WHITE,
        /// <summary>
        /// Black color.
        /// </summary>
        BLACK,
        /// <summary>
        /// Green color.
        /// </summary>
        GREEN,
        /// <summary>
        /// Brown color.
        /// </summary>
        BROWN,
        /// <summary>
        /// Blue color.
        /// </summary>
        BLUE,
        /// <summary>
        /// Red color.
        /// </summary>
        RED
    }
}
