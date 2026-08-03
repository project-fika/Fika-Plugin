using System.Collections.Generic;
using EFT;

namespace Fika.Core.Main.Components;

public abstract class ViewFilter : ICustomizationFilter
{
    public abstract HashSet<EBodyModelPart> AllowedParts { get; }

    public BodyCustomization FilterCustomization(BodyCustomization customization)
    {
        BodyCustomization value = new(customization);
        for (var i = 0; i < EnumHelper<EBodyModelPart>.Values.Count; i++)
        {
            var bodyPart = EnumHelper<EBodyModelPart>.Values[i];
            if (!AllowedParts.Contains(bodyPart))
            {
                value.Remove(bodyPart);
            }
        }
        return value;
    }
}
