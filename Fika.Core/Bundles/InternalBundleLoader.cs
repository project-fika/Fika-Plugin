// © 2025 Lacyway All Rights Reserved

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Fika.Core.Bundles
{
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
            Assembly assembly = Assembly.GetExecutingAssembly();
            assembly.GetManifestResourceNames().ToList().ForEach(name =>
            {
                using Stream stream = assembly.GetManifestResourceStream(name);
                using MemoryStream memoryStream = new();
                {
                    string bundlename = name.Replace("Fika.Core.Bundles.Files.", "").Replace(".bundle", "");
                    if (bundlename == "masterbundle")
                    {
                        stream.CopyTo(memoryStream);
                        AssetBundleCreateRequest assetBundle = AssetBundle.LoadFromMemoryAsync(memoryStream.ToArray());
                        _masterBundle = assetBundle.assetBundle;
                    }
                    else
                    {
                        FikaPlugin.Instance.FikaLogger.LogFatal("Unknown bundle loaded! Terminating...");
                        Application.Quit();
                    }
                }
            });

            Instance = this;
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
                EFikaAsset.MainMenuUI => _masterBundle.LoadAsset<GameObject>("MainMenuUI"),
                EFikaAsset.MatchmakerUI => _masterBundle.LoadAsset<GameObject>("NewMatchMakerUI"),
                EFikaAsset.Ping => _masterBundle.LoadAsset<GameObject>("BasePingPrefab"),
                EFikaAsset.PlayerUI => _masterBundle.LoadAsset<GameObject>("PlayerFriendlyUI"),
                EFikaAsset.SendItemMenu => _masterBundle.LoadAsset<GameObject>("SendItemMenu"),
                EFikaAsset.FreecamUI => _masterBundle.LoadAsset<GameObject>("FreecamUI"),
                EFikaAsset.AdminUI => _masterBundle.LoadAsset<GameObject>("AdminSettingsUI"),
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

            sprites.Add(EFikaSprite.PingPoint, _masterBundle.LoadAsset<Sprite>("PingPoint"));
            sprites.Add(EFikaSprite.PingPlayer, _masterBundle.LoadAsset<Sprite>("PingPlayer"));
            sprites.Add(EFikaSprite.PingLootableContainer, _masterBundle.LoadAsset<Sprite>("PingLootableContainer"));
            sprites.Add(EFikaSprite.PingDoor, _masterBundle.LoadAsset<Sprite>("PingDoor"));
            sprites.Add(EFikaSprite.PingDeadBody, _masterBundle.LoadAsset<Sprite>("PingDeadBody"));
            sprites.Add(EFikaSprite.PingLootItem, _masterBundle.LoadAsset<Sprite>("PingLootItem"));

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
            MainMenuUI,
            MatchmakerUI,
            Ping,
            PlayerUI,
            SendItemMenu,
            FreecamUI,
            AdminUI
        }
    }
}