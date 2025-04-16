using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Fika.Core.Patching
{
    public class PatchManager
    {
        private readonly string _patcherName;
        private readonly Harmony _harmony;
        private readonly bool _autoPatch;
        private readonly List<FikaPatch> _patches;
        private readonly ManualLogSource _logger;

        public PatchManager(BaseUnityPlugin unityPlugin)
        {
            _patcherName = unityPlugin.Info.Metadata.Name + " PatchManager";
            _harmony = new(unityPlugin.Info.Metadata.GUID);
            _patches = [];
            _logger = Logger.CreateLogSource(_patcherName);
        }

        public PatchManager(BaseUnityPlugin unityPlugin, bool autoPatch)
        {
            _patcherName = unityPlugin.Info.Metadata.Name + " PatchManager";
            _harmony = new(unityPlugin.Info.Metadata.GUID);
            _autoPatch = autoPatch;
            _patches = [];
            _logger = Logger.CreateLogSource(_patcherName);
        }

        public PatchManager(string guid, string pluginName)
        {
            _patcherName = pluginName + " PatchManager";
            _harmony = new(guid);
            _patches = [];
            _logger = Logger.CreateLogSource(_patcherName);
        }

        public PatchManager(string guid, string pluginName, bool autoPatch)
        {
            _patcherName = pluginName + " PatchManager";
            _harmony = new(guid);
            _autoPatch = autoPatch;
            _patches = [];
            _logger = Logger.CreateLogSource(_patcherName);
        }

        /// <summary>
        /// Adds a single patch
        /// </summary>
        /// <param name="patch"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public void AddPatch(FikaPatch patch)
        {
            if (_autoPatch)
            {
                throw new InvalidOperationException("You cannot manually add patches when using auto patching");
            }

            _patches.Add(patch);
        }

        /// <summary>
        /// Adds a list of patches
        /// </summary>
        /// <param name="patchList"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public void AddPatches(List<FikaPatch> patchList)
        {
            if (_autoPatch)
            {
                throw new InvalidOperationException("You cannot manually add patches when using auto patching");
            }

            _patches.AddRange(patchList);
        }

        /// <summary>
        /// Enables all patches, if <see cref="_autoPatch"/> is enabled it will find them automatically
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void EnablePatches()
        {
            if (_autoPatch)
            {
#if DEBUG
                List<Type> query = [.. Assembly.GetExecutingAssembly()
                    .GetTypes()
                    .Where(t => t.BaseType == typeof(FikaPatch)
                                && t.GetCustomAttribute<IgnoreAutoPatchAttribute>() == null)];
#else
                List<Type> query = [.. Assembly.GetExecutingAssembly()
                    .GetTypes()
                    .Where(t => t.BaseType == typeof(FikaPatch)
                                && t.GetCustomAttribute<IgnoreAutoPatchAttribute>() == null
                                && t.GetCustomAttribute<DebugPatchAttribute>() == null)];
#endif

                if (query.Count == 0)
                {
                    throw new ArgumentException("Could not find any patches defined in the assembly during auto patching");
                }

                foreach (Type type in query)
                {
                    ((FikaPatch)Activator.CreateInstance(type)).Enable(_harmony);
                }

                _logger.LogInfo($"Enabled {query.Count} patches");
                return;
            }

            if (_patches.Count == 0)
            {
                throw new ArgumentOutOfRangeException("There were no patches to enable");
            }

            for (int i = 0; i < _patches.Count; i++)
            {
                _patches[i].Enable(_harmony);
            }
        }

        /// <summary>
        /// Disables all patches, if <see cref="_autoPatch"/> is enabled it will find them automatically
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void DisablePatches()
        {
            if (_autoPatch)
            {
#if DEBUG
                List<Type> query = [.. Assembly.GetExecutingAssembly()
                    .GetTypes()
                    .Where(t => t.BaseType == typeof(FikaPatch)
                                && t.GetCustomAttribute<IgnoreAutoPatchAttribute>() == null)];
#else
                List<Type> query = [.. Assembly.GetExecutingAssembly()
                    .GetTypes()
                    .Where(t => t.BaseType == typeof(FikaPatch)
                                && t.GetCustomAttribute<IgnoreAutoPatchAttribute>() == null
                                && t.GetCustomAttribute<DebugPatchAttribute>() == null)];
#endif

                if (query.Count == 0)
                {
                    throw new ArgumentException("Could not find any patches defined in the assembly during auto patching");
                }

                foreach (Type type in query)
                {
                    ((FikaPatch)Activator.CreateInstance(type)).Disable(_harmony);
                }

                _logger.LogInfo($"Disabled {query.Count} patches");
                return;
            }

            if (_patches.Count == 0)
            {
                throw new ArgumentOutOfRangeException("There were no patches to disable");
            }

            for (int i = 0; i < _patches.Count; i++)
            {
                _patches[i].Disable(_harmony);
            }
        }

        /// <summary>
        /// Enables a single patch
        /// </summary>
        /// <param name="patch"></param>
        public void EnablePatch(FikaPatch patch)
        {
            patch.Enable(_harmony);
        }

        /// <summary>
        /// Disables a single patch
        /// </summary>
        /// <param name="patch"></param>
        public void DisablePatch(FikaPatch patch)
        {
            patch.Disable(_harmony);
        }
    }
}
