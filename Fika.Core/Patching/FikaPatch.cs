using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Fika.Core.Patching
{
    public abstract class FikaPatch
    {
        private readonly List<HarmonyMethod> _prefixList;
        private readonly List<HarmonyMethod> _postfixList;
        private readonly List<HarmonyMethod> _transpilerList;
        private readonly List<HarmonyMethod> _finalizerList;
        private readonly List<HarmonyMethod> _ilmanipulatorList;

        protected static ManualLogSource Logger { get; private set; }

        protected FikaPatch() : this(null)
        {
            if (Logger == null)
            {
                Logger = BepInEx.Logging.Logger.CreateLogSource(nameof(FikaPatch));
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name">Name</param>
        protected FikaPatch(string name = null)
        {
            _prefixList = GetPatchMethods(typeof(PatchPrefixAttribute));
            _postfixList = GetPatchMethods(typeof(PatchPostfixAttribute));
            _transpilerList = GetPatchMethods(typeof(PatchTranspilerAttribute));
            _finalizerList = GetPatchMethods(typeof(PatchFinalizerAttribute));
            _ilmanipulatorList = GetPatchMethods(typeof(PatchILManipulatorAttribute));

            if (_prefixList.Count == 0
                && _postfixList.Count == 0
                && _transpilerList.Count == 0
                && _finalizerList.Count == 0
                && _ilmanipulatorList.Count == 0)
            {
                throw new Exception(message: $"{GetType().Name}: At least one of the patch methods must be specified");
            }
        }

        /// <summary>
        /// Get original method
        /// </summary>
        /// <returns>Method</returns>
        protected abstract MethodBase GetTargetMethod();

        /// <summary>
        /// Get HarmonyMethod from string
        /// </summary>
        /// <param name="attributeType">Attribute type</param>
        /// <returns>Method</returns>
        private List<HarmonyMethod> GetPatchMethods(Type attributeType)
        {
            Type T = GetType();
            List<HarmonyMethod> methods = new List<HarmonyMethod>();

            foreach (MethodInfo method in T.GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                if (method.GetCustomAttribute(attributeType) != null)
                {
                    methods.Add(new HarmonyMethod(method));
                }
            }

            return methods;
        }

        /// <summary>
        /// Apply patch to target
        /// </summary>
        public void Enable(Harmony harmony)
        {
            MethodBase target = GetTargetMethod();

            if (target == null)
            {
                throw new InvalidOperationException($"{GetType().Name}: TargetMethod is null");
            }

            try
            {
                foreach (HarmonyMethod prefix in _prefixList)
                {
                    harmony.Patch(target, prefix: prefix);
                }

                foreach (HarmonyMethod postfix in _postfixList)
                {
                    harmony.Patch(target, postfix: postfix);
                }

                foreach (HarmonyMethod transpiler in _transpilerList)
                {
                    harmony.Patch(target, transpiler: transpiler);
                }

                foreach (HarmonyMethod finalizer in _finalizerList)
                {
                    harmony.Patch(target, finalizer: finalizer);
                }

                foreach (HarmonyMethod ilmanipulator in _ilmanipulatorList)
                {
                    harmony.Patch(target, ilmanipulator: ilmanipulator);
                }

                Logger.LogInfo($"Enabled patch {GetType().Name}");
            }
            catch (Exception ex)
            {
                Logger.LogError($"{GetType().Name}: {ex}");
                throw new Exception($"{GetType().Name}:", ex);
            }
        }

        /// <summary>
        /// Remove applied patch from target
        /// </summary>
        public void Disable(Harmony harmony)
        {
            MethodBase target = GetTargetMethod();

            if (target == null)
            {
                throw new InvalidOperationException($"{GetType().Name}: TargetMethod is null");
            }

            try
            {
                harmony.Unpatch(target, HarmonyPatchType.All, harmony.Id);
                Logger.LogInfo($"Disabled patch {GetType().Name}");
            }
            catch (Exception ex)
            {
                Logger.LogError($"{GetType().Name}: {ex}");
                throw new Exception($"{GetType().Name}:", ex);
            }
        }
    }
}
