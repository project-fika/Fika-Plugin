namespace Fika.Core.Networking.Packets
{
    /// <summary>
    /// Represents common sub-packet types for general player actions.
    /// </summary>
    public enum ECommonSubPacketType : byte
    {
        /// <summary>
        /// Player phrase.
        /// </summary>
        Phrase,

        /// <summary>
        /// Interaction with the world environment.
        /// </summary>
        WorldInteraction,

        /// <summary>
        /// Interaction with containers.
        /// </summary>
        ContainerInteraction,

        /// <summary>
        /// Proceed action.
        /// </summary>
        Proceed,

        /// <summary>
        /// Headlights toggled or updated.
        /// </summary>
        HeadLights,

        /// <summary>
        /// Inventory contents changed.
        /// </summary>
        InventoryChanged,

        /// <summary>
        /// Item dropped.
        /// </summary>
        Drop,

        /// <summary>
        /// Stationary interaction.
        /// </summary>
        Stationary,

        /// <summary>
        /// Vault or climbing action.
        /// </summary>
        Vault,

        /// <summary>
        /// Generic interaction event.
        /// </summary>
        Interaction,

        /// <summary>
        /// Mounting or dismounting action.
        /// </summary>
        Mounting
    }
}
