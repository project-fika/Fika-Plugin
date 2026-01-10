// © 2026 Lacyway All Rights Reserved

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Diz.Utils;
using Fika.Core.Main.Utils;

namespace Fika.Core.Bundles;

/// <summary>
/// Created by Nexus / pandahhcorp <br/>
/// Refactored by Lacyway to load bundles directly from memory
/// </summary>
internal class InternalBundleLoader
{
    public static InternalBundleLoader Instance { get; private set; }

    private AssetBundle _masterBundle;

    public InternalBundleLoader()
    {
        Task.Run(LoadBundles);
        Instance = this;
    }

    public async Task LoadBundles()
    {
        var assembly = Assembly.GetExecutingAssembly();
        foreach (var name in assembly.GetManifestResourceNames())
        {
            await using var stream = assembly.GetManifestResourceStream(name);
            await using MemoryStream memoryStream = new();

            var bundleName = name.Replace("Fika.Core.Bundles.Files.", "")
                .Replace(".bundle", "");

            if (bundleName == "masterbundle")
            {
                await stream.CopyToAsync(memoryStream);
                var assetBundle = AssetBundle.LoadFromMemoryAsync(memoryStream.ToArray());
                while (!assetBundle.isDone)
                {
                    await Task.Yield();
                }

                _masterBundle = assetBundle.assetBundle;
            }
            else
            {
                FikaGlobals.LogFatal("Unknown bundle loaded! Terminating...");
                AsyncWorker.RunInMainTread(Application.Quit);
            }
        }
    }

    /// <summary>
    /// Loads a Fika asset from the Master Bundle
    /// </summary>
    /// <param name="asset">The <see cref="EFikaAsset"/> type to load</param>
    /// <returns></returns>
    /// <exception cref="NullReferenceException">Master Bundle could not be found</exception>
    /// <exception cref="ArgumentOutOfRangeException"><see cref="EFikaAsset"/> was out of range</exception>
    internal GameObject GetFikaAsset(EFikaAsset asset)
    {
        if (_masterBundle == null)
        {
            throw new NullReferenceException("GetFikaAsset::MasterBundle did not exist!");
        }

        return asset switch
        {
            EFikaAsset.Ping => _masterBundle.LoadAsset<GameObject>("BasePingPrefab.prefab"),
            EFikaAsset.SendItemMenu => _masterBundle.LoadAsset<GameObject>("SendItemMenu.prefab"),
            EFikaAsset.MainMenuUI => _masterBundle.LoadAsset<GameObject>("MainMenuUI.prefab"),
            EFikaAsset.MatchmakerUI => _masterBundle.LoadAsset<GameObject>("NewMatchMakerUI.prefab"),
            EFikaAsset.PlayerUI => _masterBundle.LoadAsset<GameObject>("PlayerFriendlyUI.prefab"),
            EFikaAsset.FreecamUI => _masterBundle.LoadAsset<GameObject>("FreecamUI.prefab"),
            EFikaAsset.AdminUI => _masterBundle.LoadAsset<GameObject>("AdminSettingsUI.prefab"),
            EFikaAsset.FikaChatUI => _masterBundle.LoadAsset<GameObject>("FikaChatUI.prefab"),
            EFikaAsset.RaidAdminUI => _masterBundle.LoadAsset<GameObject>("RaidAdminUI.prefab"),
            EFikaAsset.LoadingScreenUI => _masterBundle.LoadAsset<GameObject>("LoadingScreenUI.prefab"),
            EFikaAsset.DebugUI => _masterBundle.LoadAsset<GameObject>("DebugUI.prefab"),
            _ => throw new ArgumentOutOfRangeException(nameof(asset), "Invalid type was given")
        };
    }

    /// <summary>
    /// Loads all <see cref="EFikaSprite"/> into memory and caches them
    /// </summary>
    /// <returns>All sprites in a <see cref="Dictionary{TKey, TValue}"/></returns>
    internal Dictionary<EFikaSprite, Sprite> GetFikaSprites()
    {
        Dictionary<EFikaSprite, Sprite> sprites = [];

        sprites.Add(EFikaSprite.PingPoint, _masterBundle.LoadAsset<Sprite>("PingPoint.png"));
        sprites.Add(EFikaSprite.PingPlayer, _masterBundle.LoadAsset<Sprite>("PingPlayer.png"));
        sprites.Add(EFikaSprite.PingLootableContainer, _masterBundle.LoadAsset<Sprite>("PingLootableContainer.png"));
        sprites.Add(EFikaSprite.PingDoor, _masterBundle.LoadAsset<Sprite>("PingDoor.png"));
        sprites.Add(EFikaSprite.PingDeadBody, _masterBundle.LoadAsset<Sprite>("PingDeadBody.png"));
        sprites.Add(EFikaSprite.PingLootItem, _masterBundle.LoadAsset<Sprite>("PingLootItem.png"));

        return sprites;
    }

    public enum EFikaSprite
    {
        PingPoint,
        PingPlayer,
        PingLootableContainer,
        PingDoor,
        PingDeadBody,
        PingLootItem
    }

    public enum EFikaAsset
    {
        Ping,
        SendItemMenu,
        MainMenuUI,
        MatchmakerUI,
        PlayerUI,
        FreecamUI,
        AdminUI,
        FikaChatUI,
        RaidAdminUI,
        LoadingScreenUI,
        DebugUI
    }
}