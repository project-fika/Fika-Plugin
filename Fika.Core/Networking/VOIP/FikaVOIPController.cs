using Dissonance;
using EFT;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using System;
using System.Collections.Generic;
using System.Threading;
using static EFT.Player;

namespace Fika.Core.Networking.VOIP
{
    class FikaVOIPController : IPlayerVoipController, IDisposable
    {
        private static readonly TimeSpan _hearingDetectionTime = TimeSpan.FromSeconds(2.0);

        public FikaVOIPController(FikaPlayer localPlayer, SoundSettingsControllerClass soundSettings)
        {
            EVoipState voipState = localPlayer.VoipState;
            LocalPlayer = localPlayer;
            SoundSettings = soundSettings;
            PushToTalkSettingsClass pushToTalkSettings = FikaGlobals.VOIPHandler.PushToTalkSettings;
            BlockingTime = TimeSpan.FromSeconds(pushToTalkSettings.BlockingTime);
            SpeakingSecondsInterval = TimeSpan.FromSeconds(pushToTalkSettings.SpeakingSecondsInterval);
            SpeakingSecondsLimit = TimeSpan.FromSeconds(pushToTalkSettings.SpeakingSecondsLimit);
            SpeakDelayBetweenLimit = SpeakingSecondsInterval - SpeakingSecondsLimit;

            if (DissonanceComms == null)
            {
                currentState = new MicrophoneFailState();
            }
            else
            {
                checker = LimitChecker.Create(pushToTalkSettings.ActivationsLimit,
                    pushToTalkSettings.ActivationsInterval, pushToTalkSettings.SpeakingSecondsInterval);
                offState = new();
                readyState = new();
                talkingState = new();
                limitedState = new();
                blockedState = new();
                bannedState = BannedState.Create(this);
                switch (voipState)
                {
                    case EVoipState.Available:
                        currentState = readyState;
                        break;
                    case EVoipState.Off:
                        currentState = offState;
                        break;
                    case EVoipState.Banned:
                        currentState = bannedState;
                        break;
                    default:
                        FikaGlobals.LogError($"Invalid VOIP state during initialization: {voipState}");
                        currentState = offState;
                        break;
                }

                Status = new();
                hasInteraction = new();
                talkDetected = new();
                currentState.Controller = this;
                currentState.vmethod_0();
                if (currentState.Status != EVoipControllerStatus.MicrophoneFail)
                {
                    try
                    {
                        compositeDisposableClass.BindState(soundSettings.VoipEnabled, ToggleVOIP);
                    }
                    catch (Exception ex)
                    {
                        FikaGlobals.LogError("Failed to bind soundsettings.VoipEnabled");
                        FikaGlobals.LogFatal(ex.Message);
                    }
                    try
                    {
                        compositeDisposableClass.BindState(soundSettings.VoipDevice, ChangeDevice);
                    }
                    catch (Exception ex)
                    {
                        FikaGlobals.LogError("Failed to bind soundsettings.VoipDevice");
                        FikaGlobals.LogFatal(ex.Message);
                    }
                }
            }
        }

        private void ChangeDevice(string device)
        {
            if (device == "Settings/UnavailablePressType")
            {
                ToggleVOIP(false);
                return;
            }
            if (device == "Default")
            {
                SetDefaultMicrophone();
                return;
            }
            if (SoundSettingsControllerClass.IsValidMicrophone(device))
            {
                FikaGlobals.LogInfo($"VoipMicrophone set device: {device}");
                DissonanceComms.MicrophoneName = device;
                return;
            }
            FikaGlobals.LogError($"Invalid microphone device name: {device}, trying setting as default");
            SetDefaultMicrophone();
        }

        private void SetDefaultMicrophone()
        {
            string defaultMicrophone = SoundSettingsControllerClass.DefaultMicrophone;
            if (defaultMicrophone != null)
            {
                FikaGlobals.LogInfo($"VoipMicrophone set default: {defaultMicrophone}");
                DissonanceComms.MicrophoneName = defaultMicrophone;
                return;
            }
            ToggleVOIP(false);
        }

        private void ToggleVOIP(bool enabled)
        {
            this.enabled = enabled;
            if (forceMute)
            {
                return;
            }
            if (enabled)
            {
                currentState.vmethod_3();
                return;
            }
            currentState.vmethod_4();
        }

        public abstract class VOIPState
        {
            public abstract EVoipControllerStatus Status { get; }
            public FikaVOIPController Controller { get; set; }
            public LimitChecker LimitChecker
            {
                get
                {
                    return Controller.checker;
                }
            }

            public bool Bool1
            {
                get
                {
                    return this == (Controller?.currentState);
                }
            }

            public DissonanceComms DissonanceComms
            {
                get
                {
                    return Controller.DissonanceComms;
                }
            }

            public abstract TimeSpan TimeSpan_0 { get; }

            public virtual EVoipControllerStatus vmethod_0()
            {
                return Status;
            }

            public virtual EVoipControllerStatus ToggleTalk()
            {
                return Status;
            }

            public virtual EVoipControllerStatus StopTalk()
            {
                return Status;
            }

            public virtual void vmethod_3()
            {
            }

            public virtual void vmethod_4()
            {
                Controller.method_3(Controller.offState);
            }

            public virtual void Update()
            {

            }
        }

        public abstract class AbstractOffState : VOIPState
        {
            public override EVoipControllerStatus vmethod_0()
            {
                DissonanceComms.IsMuted = true;
                return base.vmethod_0();
            }
        }

        public abstract class AbstractOffState2 : AbstractOffState
        {
            public override TimeSpan TimeSpan_0
            {
                get
                {
                    return struct510.TimeSpan_0;
                }
            }

            public override void Update()
            {
                if (struct510.Boolean_0)
                {
                    Controller.method_3(Controller.readyState);
                }
            }

            protected Struct510 struct510;
        }

        public class OffState : VOIPState
        {
            public override EVoipControllerStatus Status
            {
                get
                {
                    return EVoipControllerStatus.Off;
                }
            }

            public override TimeSpan TimeSpan_0
            {
                get
                {
                    return TimeSpan.Zero;
                }
            }

            public override void vmethod_3()
            {
                Controller.TalkDetected.Value = false;
                if (Controller.bannedState.Boolean_1)
                {
                    Controller.method_3(Controller.readyState);
                    return;
                }
                Controller.method_3(Controller.bannedState);
            }

            public override void Update()
            {
                Controller.method_12();
            }
        }

        public class ReadyState : AbstractOffState
        {
            public override EVoipControllerStatus Status
            {
                get
                {
                    return EVoipControllerStatus.Ready;
                }
            }

            public override TimeSpan TimeSpan_0
            {
                get
                {
                    return TimeSpan.Zero;
                }
            }

            public override EVoipControllerStatus vmethod_0()
            {
                Controller.TalkDetected.Value = false;
                if (Controller.HasInteraction)
                {
                    Controller.method_2();
                }
                return base.vmethod_0();
            }

            public override EVoipControllerStatus ToggleTalk()
            {
                if (!Controller.checker.method_2())
                {
                    return method_0();
                }
                return Controller.method_3(Controller.talkingState);
            }

            private EVoipControllerStatus method_0()
            {
                return Controller.method_3(Controller.limitedState);
            }
        }

        public class TalkingState : VOIPState
        {
            public override EVoipControllerStatus Status
            {
                get
                {
                    return EVoipControllerStatus.Talking;
                }
            }

            public override TimeSpan TimeSpan_0
            {
                get
                {
                    return struct510.TimeSpan_0;
                }
            }

            public override EVoipControllerStatus vmethod_0()
            {
                struct510 = Controller.method_8();
                if (struct510.Boolean_0)
                {
                    return Controller.method_3(Controller.limitedState);
                }
                DissonanceComms.IsMuted = false;
                Controller.nullable_0 = null;
                Controller.HasInteraction.Value = true;
                return base.vmethod_0();
            }

            public override EVoipControllerStatus ToggleTalk()
            {
                return StopTalk();
            }

            public override EVoipControllerStatus StopTalk()
            {
                LimitChecker.InsertTimeState(struct510.DateTime_1, EFTDateTimeClass.UtcNow);
                return Controller.method_3(Controller.readyState);
            }

            public override void Update()
            {
                if (struct510.Boolean_0)
                {
                    method_0();
                }
            }

            private void method_0()
            {
                DateTime utcNow = EFTDateTimeClass.UtcNow;
                LimitChecker.InsertTimeState(struct510.DateTime_1, utcNow);
                Controller.method_3(Controller.limitedState);
            }

            private Struct510 struct510;
        }

        public class LimitedState : AbstractOffState2
        {
            public override EVoipControllerStatus Status
            {
                get
                {
                    return EVoipControllerStatus.Limited;
                }
            }

            public override EVoipControllerStatus vmethod_0()
            {
                struct510 = Controller.method_9();
                return base.vmethod_0();
            }
        }

        public class BlockedState : AbstractOffState2
        {
            public override EVoipControllerStatus Status
            {
                get
                {
                    return EVoipControllerStatus.Blocked;
                }
            }

            public override EVoipControllerStatus vmethod_0()
            {
                struct510 = Controller.method_11();
                return base.vmethod_0();
            }
        }

        public class MicrophoneFailState : AbstractOffState2
        {
            public override EVoipControllerStatus Status
            {
                get
                {
                    return EVoipControllerStatus.MicrophoneFail;
                }
            }

            public override EVoipControllerStatus vmethod_0()
            {
                return Status;
            }

            public override TimeSpan TimeSpan_0
            {
                get
                {
                    return struct510.TimeSpan_0;
                }
            }

            public override void Update()
            {
                Controller.method_12();
            }
        }

        public class BannedState : AbstractOffState2
        {
            public static BannedState Create(FikaVOIPController controller)
            {
                return new()
                {
                    struct510 = controller.method_10()
                };
            }

            public override EVoipControllerStatus Status
            {
                get
                {
                    return EVoipControllerStatus.Banned;
                }
            }

            public override EVoipControllerStatus vmethod_0()
            {
                struct510 = Controller.method_10();
                Controller.method_2();
                Controller.HasInteraction.Value = true;
                return base.vmethod_0();
            }

            public bool Boolean_1
            {
                get
                {
                    return struct510.Boolean_0;
                }
            }

            public override TimeSpan TimeSpan_0
            {
                get
                {
                    return struct510.TimeSpan_0;
                }
            }
        }

        private Struct510 method_8()
        {
            int num = 0;
            DateTime utcNow = EFTDateTimeClass.UtcNow;
            DateTime dateTime = utcNow;
            DateTime dateTime2 = dateTime;
            TimeSpan timeSpan = TimeSpan.Zero;
            for (; ; )
            {
                num++;
                DateTime dateTime3 = dateTime - SpeakingSecondsInterval;
                TimeSpan timeSpan2 = checker.method_1(dateTime3);
                TimeSpan timeSpan3 = SpeakingSecondsLimit - timeSpan2;
                if (timeSpan3.TotalSeconds < 0.1)
                {
                    goto IL_00B4;
                }
                timeSpan3 -= timeSpan;
                if (timeSpan3 <= TimeSpan.Zero)
                {
                    goto IL_00B4;
                }
                dateTime2 += timeSpan3;
                if (dateTime2 - utcNow >= SpeakingSecondsLimit)
                {
                    break;
                }
                timeSpan += timeSpan3;
                dateTime = dateTime2;
                if (num >= 20)
                {
                    goto IL_00B4;
                }
            }
            dateTime2 = utcNow + SpeakingSecondsLimit;
            IL_00B4:
            Struct510 @struct = new(in dateTime2, in utcNow);
            return @struct;
        }

        private readonly LimitChecker checker;
        private bool enabled = true;
        private bool forceMute;

        private DateTime? nullable_0;


        public FikaPlayer LocalPlayer { get; set; }
        public SoundSettingsControllerClass SoundSettings { get; set; }

        public DissonanceComms DissonanceComms
        {
            get
            {
                return LocalPlayer.DissonanceComms;
            }
        }

        public TimeSpan BlockingTime { get; set; }
        public TimeSpan SpeakingSecondsInterval { get; set; }
        public TimeSpan SpeakingSecondsLimit { get; set; }
        public TimeSpan SpeakDelayBetweenLimit { get; set; }

        public BindableStateClass<EVoipControllerStatus> Status { get; }
        public BindableStateClass<bool> HasInteraction
        {
            get
            {
                return hasInteraction;
            }
        }
        public BindableStateClass<bool> TalkDetected
        {
            get
            {
                return talkDetected;
            }
        }
        public TimeSpan TimeToNextStatus
        {
            get
            {
                return currentState.TimeSpan_0;
            }
        }
        public event Action<string> AbuseNotification;

        private readonly OffState offState;
        private readonly ReadyState readyState;
        private readonly TalkingState talkingState;
        private readonly LimitedState limitedState;
        private readonly BlockedState blockedState;
        private readonly BannedState bannedState;
        private VOIPState currentState;
        private readonly CompositeDisposableClass compositeDisposableClass = new(2);

        private readonly BindableStateClass<bool> hasInteraction;
        private readonly BindableStateClass<bool> talkDetected;

        public void method_2()
        {
            nullable_0 = new DateTime?(EFTDateTimeClass.UtcNow);
        }

        public EVoipControllerStatus method_3(VOIPState state)
        {
            if (currentState == state)
            {
                return state.Status;
            }
            state.Controller = this;
            VOIPState @class = Interlocked.Exchange(ref currentState, state);
            if (@class != null)
            {
                @class.Controller = null;
            }
            method_4(state.Status);
            Status.Value = state.Status;
            if (currentState != state)
            {
                FikaGlobals.LogError("State changed on Status event!");
            }
            if (!state.Bool1)
            {
                return currentState.Status;
            }
            return state.vmethod_0();
        }

        public void method_4(EVoipControllerStatus status)
        {
            EVoipState evoipState = LocalPlayer.VoipState;
            if (status != EVoipControllerStatus.Off)
            {
                if (status != EVoipControllerStatus.Banned)
                {
                    if (status != EVoipControllerStatus.MicrophoneFail)
                    {
                        if (evoipState == EVoipState.Available)
                        {
                            return;
                        }
                        evoipState = EVoipState.Available;
                    }
                    else
                    {
                        evoipState = EVoipState.MicrophoneFail;
                    }
                }
                else
                {
                    evoipState = EVoipState.Banned;
                }
            }
            else
            {
                evoipState = EVoipState.Off;
            }
            LocalPlayer.VoipState = evoipState;
        }

        private Struct510 method_10()
        {
            VOIPBanDataClass ban = LocalPlayer.Profile.Info.GetBan(EBanType.Voip);
            if (ban == null)
            {
                return default;
            }

            DateTime banUntil = ban.BanUntil;
            return new(in banUntil);
        }

        private Struct510 method_11()
        {
            TimeSpan timeSpan = BlockingTime;
            return new(timeSpan);
        }

        private Struct510 method_9()
        {
            TimeSpan timeSpan = SpeakDelayBetweenLimit;
            return new(in timeSpan);
        }

        public void method_12()
        {
            TimeSpan timeSpan = EFTDateTimeClass.UtcNow - LocalPlayer.HearingDateTime;
            bool flag = timeSpan <= _hearingDetectionTime;
            TalkDetected.Value = flag;
            if (!flag)
            {
                return;
            }
            TimeSpan timeSpan2 = _hearingDetectionTime - timeSpan;
            nullable_0 = new DateTime?(EFTDateTimeClass.UtcNow + timeSpan2);
            HasInteraction.Value = true;
        }

        public class LimitChecker
        {
            public static LimitChecker Create(byte activationsLimit, float activationsInterval, float speakingInterval)
            {
                int maxActivations = Math.Max(10, (int)activationsLimit);
                return new LimitChecker()
                {
                    activationsInterval = TimeSpan.FromSeconds(activationsInterval),
                    timeStates = (activationsLimit > 0 ? new(maxActivations) : null),
                    activationsLimit = activationsLimit,
                    interval = TimeSpan.FromSeconds(Math.Max(activationsInterval, speakingInterval))
                };
            }

            private TimeSpan activationsInterval;
            private List<TimeState> timeStates;
            private byte activationsLimit;
            private TimeSpan interval;

            public void InsertTimeState(DateTime from, DateTime to)
            {
                if (timeStates == null)
                {
                    return;
                }

                DateTime dateTime = EFTDateTimeClass.UtcNow - interval;
                int num = timeStates.Count - 1;
                while (num >= 0 && !(timeStates[num].To >= dateTime))
                {
                    timeStates.RemoveAt(num);
                    num--;
                }
                timeStates.Insert(0, new()
                {
                    From = from,
                    To = to
                });
            }

            public TimeSpan method_1(DateTime dateTime)
            {
                if (timeStates == null)
                {
                    return TimeSpan.Zero;
                }
                TimeSpan timeSpan = TimeSpan.Zero;
                int i = 0;
                while (i < timeStates.Count)
                {
                    TimeState state = timeStates[i];
                    if (!(state.To <= dateTime))
                    {
                        if (!(state.From <= dateTime))
                        {
                            timeSpan += state.Duration;
                            i++;
                            continue;
                        }
                        timeSpan += state.To - dateTime;
                    }
                    return timeSpan;
                }
                return timeSpan;
            }

            public bool method_2()
            {
                if (timeStates == null)
                {
                    return true;
                }
                if (timeStates.Count < activationsLimit)
                {
                    return true;
                }
                DateTime dateTime = EFTDateTimeClass.UtcNow - activationsInterval;
                DateTime from = timeStates[(activationsLimit - 1)].From;
                return dateTime >= from;
            }

            public struct TimeState
            {
                public readonly TimeSpan Duration
                {
                    get
                    {
                        return To - From;
                    }
                }

                public DateTime From;
                public DateTime To;
            }
        }

        public struct Struct510
        {
            public Struct510(in DateTime dateTime, in DateTime created)
            {
                DateTime_1 = created;
                Created = dateTime;
            }

            public Struct510(in DateTime dateTime)
            {
                DateTime_1 = EFTDateTimeClass.UtcNow;
                Created = dateTime;
            }

            public Struct510(in TimeSpan timeSpan)
            {
                DateTime_1 = EFTDateTimeClass.UtcNow;
                Created = DateTime_1 + timeSpan;
            }

            public bool Boolean_0
            {
                get
                {
                    return TimeSpan_0 <= TimeSpan.Zero;
                }
            }

            // Created
            public DateTime Created;
            public DateTime DateTime_1;

            public readonly TimeSpan TimeSpan_0
            {
                get
                {
                    TimeSpan timeSpan = Created - EFTDateTimeClass.UtcNow;
                    if (!(timeSpan > TimeSpan.Zero))
                    {
                        return TimeSpan.Zero;
                    }
                    return timeSpan;
                }
            }
        }

        public void Dispose()
        {
            compositeDisposableClass.Dispose();
            if (LocalPlayer != null)
            {
                LocalPlayer.VoipController = null;
            }
        }

        public void Update()
        {
            if (forceMute)
            {
                return;
            }
            currentState.Update();
            method_1();
        }

        public void method_1()
        {
            if (nullable_0 != null && !(nullable_0.Value > EFTDateTimeClass.UtcNow))
            {
                nullable_0 = null;
                hasInteraction.Value = false;
            }
        }

        public void ForceMuteVoIP(bool enable)
        {
            forceMute = enable;
            if (forceMute)
            {
                if (currentState.Status != EVoipControllerStatus.MicrophoneFail)
                {
                    currentState.vmethod_4();
                    return;
                }
            }
            else if (enabled && currentState.Status != EVoipControllerStatus.MicrophoneFail)
            {
                currentState.vmethod_3();
            }
        }

        public void ReportAbuse()
        {
            // Do nothing
        }

        public EVoipControllerStatus StopTalk()
        {
            return currentState.StopTalk();
        }

        public EVoipControllerStatus ToggleTalk()
        {
            return currentState.ToggleTalk();
        }

        public void ReceiveAbuseNotification(string reporterId)
        {
            AbuseNotification?.Invoke(reporterId);
        }
    }
}
