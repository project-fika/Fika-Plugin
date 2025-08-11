namespace Fika.Core.Networking.Packets
{
    /// <summary>
    /// Represents the type of context-sensitive interaction (proceed action).
    /// </summary>
    public enum EProceedType : byte
    {
        /// <summary>
        /// No item held (empty hands).
        /// </summary>
        EmptyHands,

        /// <summary>
        /// Food class item.
        /// </summary>
        FoodClass,

        /// <summary>
        /// Grenade class item.
        /// </summary>
        GrenadeClass,

        /// <summary>
        /// Meds class item.
        /// </summary>
        MedsClass,

        /// <summary>
        /// Quick grenade throw action.
        /// </summary>
        QuickGrenadeThrow,

        /// <summary>
        /// Quick knife kick action.
        /// </summary>
        QuickKnifeKick,

        /// <summary>
        /// Quick use action of an item.
        /// </summary>
        QuickUse,

        /// <summary>
        /// Usable item interaction.
        /// </summary>
        UsableItem,

        /// <summary>
        /// Weapon held.
        /// </summary>
        Weapon,

        /// <summary>
        /// Stationary held (turret).
        /// </summary>
        Stationary,

        /// <summary>
        /// Knife held.
        /// </summary>
        Knife,

        /// <summary>
        /// Attempt to proceed action.
        /// </summary>
        TryProceed
    }
}
