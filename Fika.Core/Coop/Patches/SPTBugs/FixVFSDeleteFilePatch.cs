using SPT.Reflection.Patching;
using System.Reflection;
using SPT.Common.Utils;

namespace Fika.Core.Coop.Patches.SPTBugs
{
	// This patch fixes a bug in SPT 3.10.0 where SPT's git history was out of date thus causing it to run this code early.
	// It will be fixed in SPT 3.10.1, but to keep backwards compatibility keep this patch in until 3.11 for players that refuse to update their SPT version.
	internal class FixVFSDeleteFilePatch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return typeof(VFS).GetMethod(nameof(VFS.DeleteFile));
		}

		[PatchPrefix]
		public static bool Prefix(string filepath)
		{
			if (VFS.Exists(filepath))
			{
				// Run the orginal method.
				return true;
			}

			// Skip method
			return false;
		}
	}
}
