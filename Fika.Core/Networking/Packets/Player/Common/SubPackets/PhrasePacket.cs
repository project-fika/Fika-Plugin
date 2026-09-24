using Fika.Core.Main.Players;

namespace Fika.Core.Networking.Packets.Player.Common.SubPackets;

public readonly struct PhrasePacket
{
    public PhrasePacket(EPhraseTrigger trigger, int index)
    {
        PhraseTrigger = trigger;
        PhraseIndex = index;
    }

    public readonly EPhraseTrigger PhraseTrigger;
    public readonly int PhraseIndex;

    public readonly void Execute(FikaPlayer player)
    {
        if (player.gameObject.activeSelf && player.HealthController.IsAlive)
        {
            player.Speaker.PlayDirect(PhraseTrigger, PhraseIndex);
        }
    }
}
