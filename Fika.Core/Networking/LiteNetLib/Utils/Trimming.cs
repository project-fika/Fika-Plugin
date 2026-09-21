#if NET5_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;

namespace Fika.Core.Networking.LiteNetLib.Utils;

internal static class Trimming
{
    internal const DynamicallyAccessedMemberTypes SerializerMemberTypes =
        DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties;
}
#endif
