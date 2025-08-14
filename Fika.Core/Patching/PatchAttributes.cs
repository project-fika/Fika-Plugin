using System;

namespace Fika.Core.Patching;

[AttributeUsage(AttributeTargets.Method)]
public class PatchPostfixAttribute : Attribute;

[AttributeUsage(AttributeTargets.Method)]
public class PatchPrefixAttribute : Attribute;

/// <summary>
/// If added to a patch, it will not be used during auto patching
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class IgnoreAutoPatchAttribute : Attribute;

[AttributeUsage(AttributeTargets.Method)]
public class PatchTranspilerAttribute : Attribute;

/// <summary>
/// If added to a patch, it will only be enabled during debug builds
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class DebugPatchAttribute : Attribute;

[AttributeUsage(AttributeTargets.Method)]
public class PatchILManipulatorAttribute : Attribute;

[AttributeUsage(AttributeTargets.Method)]
public class PatchFinalizerAttribute : Attribute;