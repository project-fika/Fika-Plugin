using Comfort.Common;
using EFT.Interactive;
using EFT.Interactive.SecretExfiltrations;
using Fika.Core.Main.Components;
using Fika.Core.Main.GameMode;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Networking.Pooling;
using LiteNetLib.Utils;

namespace Fika.Core.Networking.Packets.World;

public class ExfilCountdown : IPoolSubPacket
{
    public string ExfilName;
    public float ExfilStartTime;

    private ExfilCountdown() { }

    public static ExfilCountdown CreateInstance()
    {
        return new ExfilCountdown();
    }

    public static ExfilCountdown FromValue(string exfilName, float exfilStartTime)
    {
        ExfilCountdown packet = GenericSubPacketPoolManager.Instance.GetPacket<ExfilCountdown>(EGenericSubPacketType.ExfilCountdown);
        packet.ExfilName = exfilName;
        packet.ExfilStartTime = exfilStartTime;
        return packet;
    }

    public void Execute(FikaPlayer player = null)
    {
        CoopHandler coopHandler = Singleton<IFikaNetworkManager>.Instance.CoopHandler;
        if (coopHandler == null)
        {
            FikaPlugin.Instance.FikaLogger.LogError("ClientExtract: CoopHandler was null!");
            return;
        }

        if (ExfiltrationControllerClass.Instance != null)
        {
            IFikaGame fikaGame = Singleton<IFikaGame>.Instance;
            if (fikaGame == null)
            {
                FikaGlobals.LogError("ExfilCountdown: FikaGame was null");
                return;
            }

            ExfiltrationControllerClass exfilController = ExfiltrationControllerClass.Instance;
            foreach (ExfiltrationPoint exfiltrationPoint in exfilController.ExfiltrationPoints)
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
                foreach (SecretExfiltrationPoint secretExfiltration in exfilController.SecretExfiltrationPoints)
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

            FikaPlugin.Instance.FikaLogger.LogError("ExfilCountdown: Could not find ExfiltrationPoint: " + ExfilName);
        }
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(ExfilName);
        writer.Put(ExfilStartTime);
    }

    public void Deserialize(NetDataReader reader)
    {
        ExfilName = reader.GetString();
        ExfilStartTime = reader.GetFloat();
    }

    public void Dispose()
    {
        ExfilName = null;
        ExfilStartTime = 0f;
    }
}
