using EFT.CameraControl;
using Fika.Core.Patching;
using System.Reflection;

namespace Fika.Core.Main.Patches.Camera
{
    public class OpticRetrice_UpdateTransform_Patch : FikaPatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(OpticRetrice).GetMethod(nameof(OpticRetrice.UpdateTransform));
        }

        [PatchPrefix]
        public static bool Prefix(OpticSight opticSight, SkinnedMeshRenderer ____renderer)
        {
            return opticSight.ScopeData != null && opticSight.ScopeData.Reticle != null && ____renderer != null;
        }
    }
}
