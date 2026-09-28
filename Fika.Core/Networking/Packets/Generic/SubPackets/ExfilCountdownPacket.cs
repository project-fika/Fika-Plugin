using Comfort.Common;
using CommonAssets.Scripts.Game;
using EFT.Interactive;
using Fika.Core.Main.GameMode;
using Fika.Core.Main.Utils;

namespace Fika.Core.Networking.Packets.Generic.SubPackets;

public readonly struct ExfilCountdownPacket : IGenericPacket
{
    public EGenericPacketType Type => EGenericPacketType.ExfilCountdown;

    public ExfilCountdownPacket(string exfilName, float exfilStartTime)
    {
        ExfilName = exfilName;
        ExfilStartTime = exfilStartTime;
    }

    public ExfilCountdownPacket(NetDataReader reader)
    {
        ExfilName = reader.GetString();
        ExfilStartTime = reader.GetFloat();
    }

    public readonly string ExfilName;
    public readonly float ExfilStartTime;

    public readonly void Execute()
    {
        var coopHandler = Singleton<IFikaNetworkManager>.Instance.CoopHandler;
        if (coopHandler == null)
        {
            FikaGlobals.LogError("ClientExtract: CoopHandler was null!");
            return;
        }

        if (ExfiltrationController.Instance != null)
        {
            var fikaGame = Singleton<IFikaGame>.Instance;
            if (fikaGame == null)
            {
                FikaGlobals.LogError("ExfilCountdown: FikaGame was null");
                return;
            }

            var exfilController = ExfiltrationController.Instance;
            foreach (var exfiltrationPoint in exfilController.ExfiltrationPoints)
            {
                if (exfiltrationPoint.Settings.Name == ExfilName)
                {
                    exfiltrationPoint.ExfiltrationStartTime = fikaGame != null ? fikaGame.GameController.GameInstance.PastTime : ExfilStartTime;

                    if (exfiltrationPoint.Status != EExfiltrationStatus.Countdown)
                    {
                        exfiltrationPoint.Status = EExfiltrationStatus.Countdown;
                    }
                    return;
                }
            }

            if (exfilController.SecretExfiltrationPoints != null)
            {
                foreach (var secretExfiltration in exfilController.SecretExfiltrationPoints)
                {
                    if (secretExfiltration.Settings.Name == ExfilName)
                    {
                        secretExfiltration.ExfiltrationStartTime = fikaGame != null ? fikaGame.GameController.GameInstance.PastTime : ExfilStartTime;

                        if (secretExfiltration.Status != EExfiltrationStatus.Countdown)
                        {
                            secretExfiltration.Status = EExfiltrationStatus.Countdown;
                        }
                        return;
                    }
                }
            }

            FikaGlobals.LogError("ExfilCountdown: Could not find ExfiltrationPoint: " + ExfilName);
        }
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.Put(ExfilName);
        writer.Put(ExfilStartTime);
    }
}
