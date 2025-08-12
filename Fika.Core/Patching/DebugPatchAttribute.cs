using System;

namespace Fika.Core.Patching;

/// <summary>
/// If added to a patch, it will only be enabled during debug builds
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class DebugPatchAttribute : Attribute
{

}
