using EFT.InventoryLogic;

namespace Fika.Core.Coop.InventoryLogic
{
	public class FikaItemAddress : GClass3010
	{
		public Item Item
		{
			get
			{
				return Grid.GetItemAt(LocationInGrid);
			}
		}

		public FikaItemAddress(StashGridClass grid, LocationInGrid locationInGrid) : base(grid, locationInGrid)
		{
		}
	}
}
