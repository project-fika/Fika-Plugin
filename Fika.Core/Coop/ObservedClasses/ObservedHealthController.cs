// © 2024 Lacyway All Rights Reserved

using EFT;
using EFT.InventoryLogic;

namespace Fika.Core.Coop.ObservedClasses
{
    public sealed class ObservedHealthController(byte[] serializedState, InventoryControllerClass inventory, SkillManager skills) : NetworkHealthControllerAbstractClass(serializedState, inventory, skills)
    {
        public override bool ApplyItem(Item item, EBodyPart bodyPart, float? amount = null)
        {
            return false;
        }

        public override void CancelApplyingItem()
        {
            // Do nothing
        }
    }
}
