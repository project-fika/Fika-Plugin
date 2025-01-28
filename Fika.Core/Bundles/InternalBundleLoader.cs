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

        private AssetBundle masterBundle;

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
                        masterBundle = assetBundle.assetBundle;
                    }
                    else
                    {
                        FikaPlugin.Instance.FikaLogger.LogFatal("Unknwon bundle loaded! Terminating...");
                        Application.Quit();
                    }
                }
            });

            Instance = this;
        }

        /// <summary>
        /// Loads a Fika asset from the Master Bundle
        /// </summary>
        /// <typeparam name="T">The <see cref="UnityEngine.Object"/> to load</typeparam>
        /// <param name="asset">The <see cref="EFikaAsset"/> to load</param>
        /// <returns></returns>
        /// <exception cref="NullReferenceException">Master Bundle could not be found</exception>
        /// <exception cref="ArgumentOutOfRangeException"><see cref="EFikaAsset"/> was out of range</exception>
        internal T GetFikaAsset<T>(EFikaAsset asset) where T : UnityEngine.Object
        {
            if (masterBundle == null)
            {
                throw new NullReferenceException("GetFikaAsset::MasterBundle did not exist!");
            }

            switch (asset)
            {
                case EFikaAsset.MainMenuUI:
                    return masterBundle.LoadAsset<T>("MainMenuUI");
                case EFikaAsset.MatchmakerUI:
                    return masterBundle.LoadAsset<T>("NewMatchMakerUI");
                case EFikaAsset.Ping:
                    return masterBundle.LoadAsset<T>("BasePingPrefab"); ;
                case EFikaAsset.PlayerUI:
                    return masterBundle.LoadAsset<T>("PlayerFriendlyUI");
                case EFikaAsset.SendItemMenu:
                    return masterBundle.LoadAsset<T>("SendItemMenu");
            }

            throw new ArgumentOutOfRangeException(nameof(asset), "Invalid type was given");
        }

        /// <summary>
        /// Loads all <see cref="EFikaSprite"/> into memory and caches them
        /// </summary>
        /// <returns>All sprites in a <see cref="Dictionary{TKey, TValue}"/></returns>
        internal Dictionary<EFikaSprite, Sprite> GetFikaSprites()
        {
            Dictionary<EFikaSprite, Sprite> sprites = [];

            sprites.Add(EFikaSprite.PingPoint, masterBundle.LoadAsset<Sprite>("PingPoint"));
            sprites.Add(EFikaSprite.PingPlayer, masterBundle.LoadAsset<Sprite>("PingPlayer"));
            sprites.Add(EFikaSprite.PingLootableContainer, masterBundle.LoadAsset<Sprite>("PingLootableContainer"));
            sprites.Add(EFikaSprite.PingDoor, masterBundle.LoadAsset<Sprite>("PingDoor"));
            sprites.Add(EFikaSprite.PingDeadBody, masterBundle.LoadAsset<Sprite>("PingDeadBody"));
            sprites.Add(EFikaSprite.PingLootItem, masterBundle.LoadAsset<Sprite>("PingLootItem"));

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
            SendItemMenu
        }
    }
}