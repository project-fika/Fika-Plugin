using EFT;
using Fika.Core.Coop.Components;
using System.Collections.Generic;

namespace Fika.Core.Coop.ClientClasses
{
    public sealed class ClientViewFilter : ViewFilter
    {
        public override HashSet<EBodyModelPart> AllowedParts
        {
            get
            {
                return [EBodyModelPart.Body, EBodyModelPart.Feet, EBodyModelPart.Head, EBodyModelPart.Hands];
            }
        }
    }
}
