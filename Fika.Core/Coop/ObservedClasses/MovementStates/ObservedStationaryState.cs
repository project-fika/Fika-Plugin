using EFT;

namespace Fika.Core.Coop.ObservedClasses
{
    public class ObservedStationaryState(MovementContext movementContext) : StationaryStateClass(movementContext)
    {
        public override bool OutOfOperationRange
        {
            get
            {
                return false;
            }
        }
    }
}
