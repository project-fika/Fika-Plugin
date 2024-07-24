using System.Reflection;
using Aki.Reflection.Patching;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Fika.Headless.Patches {
    // https://github.com/Unity-Technologies/UnityCsReference/blob/77b37cd9f002e27b45be07d6e3667ee53985ec82/Runtime/Export/Graphics/Texture.cs#L696
    public class ValidateFormatPatch1 : ModulePatch {
        protected override MethodBase GetTargetMethod()
        {
            var methods = typeof(Texture).GetMethods(BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var method in methods)
            {
                if (method.Name == "ValidateFormat" && method.GetParameters().Length == 1 && method.GetParameters()[0].ParameterType == typeof(RenderTextureFormat))
                {
                    return method;
                }
            }

            return null;
        }

        [PatchPostfix]
        static void Postfix(ref bool __result)
        {
            __result = true;
        }
    }
}