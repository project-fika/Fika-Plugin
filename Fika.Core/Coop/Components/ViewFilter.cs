using EFT;
using System.Collections.Generic;

namespace Fika.Core.Coop.Components
{
    public abstract class ViewFilter : IViewFilter
    {
        public abstract HashSet<EBodyModelPart> AllowedParts { get; }

        public GClass2030 FilterCustomization(GClass2030 customization)
        {
            GClass2030 value = new(customization);
            for (int i = 0; i < GClass864<EBodyModelPart>.Values.Count; i++)
            {
                EBodyModelPart bodyPart = GClass864<EBodyModelPart>.Values[i];
                if (!AllowedParts.Contains(bodyPart))
                {
                    value.Remove(bodyPart);
                }
            }
            return value;
        }
    }
}
