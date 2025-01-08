// © 2025 Lacyway All Rights Reserved

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
    public class InternalBundleLoader
    {
        public Dictionary<string, AssetBundleCreateRequest> _loadedBundles;
        public static InternalBundleLoader Instance { get; private set; }

        public void Create()
        {
            Instance = this;
            Awake();
        }

        private void Awake()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            _loadedBundles = [];

            assembly.GetManifestResourceNames().ToList().ForEach(name =>
            {
                using Stream stream = assembly.GetManifestResourceStream(name);
                using MemoryStream memoryStream = new();
                {
                    string bundlename = name.Replace("Fika.Core.Bundles.Files.", "").Replace(".bundle", "");
                    stream.CopyTo(memoryStream);
                    _loadedBundles.Add(bundlename, AssetBundle.LoadFromMemoryAsync(memoryStream.ToArray()));
                }
            });
        }

        public AssetBundle GetAssetBundle(string bundleName)
        {
            if (_loadedBundles.TryGetValue(bundleName, out AssetBundleCreateRequest request) && request.isDone)
            {
                return request.assetBundle;
            }

            return null;
        }
    }
}