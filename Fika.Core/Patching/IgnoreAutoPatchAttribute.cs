using System;

namespace Fika.Core.Patching;

/// <summary>
/// If added to a patch, it will not be used during auto patching
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class IgnoreAutoPatchAttribute : Attribute
{

}
