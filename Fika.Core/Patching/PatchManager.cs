using BepInEx;
using BepInEx.Logging;
using Fika.Core.Main.Utils;
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
        /// Retrieves a list of types from the given assembly that inherit from <see cref="FikaPatch"/>, <br/>
        /// excluding those marked with <see cref="IgnoreAutoPatchAttribute"/> and, in non-debug builds, <br/>
        /// excluding those marked with <see cref="DebugPatchAttribute"/>.
        /// </summary>
        /// <param name="assembly">The assembly to scan for patch types.</param>
        /// <returns>
        /// A list of types that inherit from <see cref="FikaPatch"/> and meet the filtering criteria.
        /// </returns>
        private List<Type> GetPatches(Assembly assembly)
        {
            List<Type> fikaPatches = [];

            Type baseType = typeof(FikaPatch);
            Type ignoreAttrType = typeof(IgnoreAutoPatchAttribute);
#if !DEBUG
            Type debugAttrType = typeof(DebugPatchAttribute);
#endif

            foreach (Type t in assembly.GetTypes())
            {
                if (t.BaseType != baseType)
                {
                    continue;
                }

                if (t.IsDefined(ignoreAttrType, inherit: false))
                {
                    continue;
                }

#if !DEBUG
                if (t.IsDefined(debugAttrType, inherit: false))
                {
                    continue;
                }
#endif

                fikaPatches.Add(t);
            }

            return fikaPatches;
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
                List<Type> patches = GetPatches(Assembly.GetCallingAssembly());

                if (patches.Count == 0)
                {
                    throw new ArgumentException("Could not find any patches defined in the assembly during auto patching");
                }

                foreach (Type type in patches)
                {
                    try
                    {
                        ((FikaPatch)Activator.CreateInstance(type)).Enable(_harmony);
                    }
                    catch (Exception ex)
                    {
                        FikaGlobals.LogFatal($"Failed to init [{type.Name}]: {ex.Message}");
                    }
                }

                _logger.LogInfo($"Enabled {patches.Count} patches");
                return;
            }

            if (_patches.Count == 0)
            {
                throw new ArgumentOutOfRangeException("There were no patches to enable");
            }

            for (int i = 0; i < _patches.Count; i++)
            {
                try
                {
                    _patches[i].Enable(_harmony);
                }
                catch (Exception ex)
                {
                    FikaGlobals.LogFatal($"Failed to init [{_patches[i].GetType().Name}]: {ex.Message}");
                }
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
                List<Type> patches = GetPatches(Assembly.GetCallingAssembly());

                if (patches.Count == 0)
                {
                    throw new ArgumentException("Could not find any patches defined in the assembly during auto patching");
                }

                foreach (Type type in patches)
                {
                    try
                    {
                        ((FikaPatch)Activator.CreateInstance(type)).Disable(_harmony);
                    }
                    catch (Exception ex)
                    {
                        FikaGlobals.LogFatal($"Failed to disable [{type.Name}]: {ex.Message}");
                    }
                }

                _logger.LogInfo($"Disabled {patches.Count} patches");
                return;
            }

            if (_patches.Count == 0)
            {
                throw new ArgumentOutOfRangeException("There were no patches to disable");
            }

            for (int i = 0; i < _patches.Count; i++)
            {
                try
                {
                    _patches[i].Disable(_harmony);
                }
                catch (Exception ex)
                {
                    FikaGlobals.LogFatal($"Failed to disable [{_patches[i].GetType().Name}]: {ex.Message}");
                }
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
