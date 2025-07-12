using EFT;
using Fika.Core.Coop.Components;
using System.Collections.Generic;

namespace Fika.Core.Coop.ObservedClasses
{
    public sealed class ObservedViewFilter : ViewFilter
    {
        public static readonly ObservedViewFilter Default = new();

        public override HashSet<EBodyModelPart> AllowedParts
        {
            get
            {
                return [EBodyModelPart.Body, EBodyModelPart.Feet, EBodyModelPart.Head];
            }
        }
    }
}
