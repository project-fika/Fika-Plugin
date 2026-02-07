using System;
using System.Collections.Generic;
using System.Threading;
using Dissonance;
using EFT;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using static EFT.Player;

namespace Fika.Core.Networking.VOIP;

class FikaVOIPController : IPlayerVoipController, IDisposable
{
    private static readonly TimeSpan _hearingDetectionTime = TimeSpan.FromSeconds(2.0);

    public FikaVOIPController(FikaPlayer localPlayer, SoundSettingsControllerClass soundSettings)
    {
        var voipState = localPlayer.VoipState;
        LocalPlayer = localPlayer;
        SoundSettings = soundSettings;
        var pushToTalkSettings = FikaGlobals.VOIPHandler.PushToTalkSettings;
        BlockingTime = TimeSpan.FromSeconds(pushToTalkSettings.BlockingTime);
        SpeakingSecondsInterval = TimeSpan.FromSeconds(pushToTalkSettings.SpeakingSecondsInterval);
        SpeakingSecondsLimit = TimeSpan.FromSeconds(pushToTalkSettings.SpeakingSecondsLimit);
        SpeakDelayBetweenLimit = SpeakingSecondsInterval - SpeakingSecondsLimit;

        if (DissonanceComms == null)
        {
            _currentState = new MicrophoneFailState();
        }
        else
        {
            _checker = LimitChecker.Create(pushToTalkSettings.ActivationsLimit,
                pushToTalkSettings.ActivationsInterval, pushToTalkSettings.SpeakingSecondsInterval);
            _offState = new();
            _readyState = new();
            _talkingState = new();
            _limitedState = new();
            _blockedState = new();
            _bannedState = BannedState.Create(this);
            switch (voipState)
            {
                case EVoipState.Available:
                    _currentState = _readyState;
                    break;
                case EVoipState.Off:
                    _currentState = _offState;
                    break;
                case EVoipState.Banned:
                    _currentState = _bannedState;
                    break;
                default:
                    FikaGlobals.LogError($"Invalid VOIP state during initialization: {voipState}");
                    _currentState = _offState;
                    break;
            }

            Status = new();
            HasInteraction = new();
            TalkDetected = new();
            _currentState.Controller = this;
            _currentState.vmethod_0();
            if (_currentState.Status != EVoipControllerStatus.MicrophoneFail)
            {
                try
                {
                    _compositeDisposableClass.BindState(soundSettings.VoipEnabled, ToggleVOIP);
                }
                catch (Exception ex)
                {
                    FikaGlobals.LogError("Failed to bind soundsettings.VoipEnabled");
                    FikaGlobals.LogFatal(ex.Message);
                }
                try
                {
                    _compositeDisposableClass.BindState(soundSettings.VoipDevice, ChangeDevice);
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
        var defaultMicrophone = SoundSettingsControllerClass.DefaultMicrophone;
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
        _enabled = enabled;
        if (_forceMute)
        {
            return;
        }
        if (enabled)
        {
            _currentState.vmethod_3();
            return;
        }
        _currentState.vmethod_4();
    }

    public abstract class VOIPState
    {
        public abstract EVoipControllerStatus Status { get; }
        public FikaVOIPController Controller { get; set; }
        public LimitChecker LimitChecker
        {
            get
            {
                return Controller._checker;
            }
        }

        public bool Bool1
        {
            get
            {
                return this == (Controller?._currentState);
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
            Controller.method_3(Controller._offState);
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
                Controller.method_3(Controller._readyState);
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
            if (Controller._bannedState.Boolean_1)
            {
                Controller.method_3(Controller._readyState);
                return;
            }
            Controller.method_3(Controller._bannedState);
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
            if (!Controller._checker.method_2())
            {
                return method_0();
            }
            return Controller.method_3(Controller._talkingState);
        }

        private EVoipControllerStatus method_0()
        {
            return Controller.method_3(Controller._limitedState);
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
                return Controller.method_3(Controller._limitedState);
            }
            DissonanceComms.IsMuted = false;
            Controller._nullable_0 = null;
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
            return Controller.method_3(Controller._readyState);
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
            var utcNow = EFTDateTimeClass.UtcNow;
            LimitChecker.InsertTimeState(struct510.DateTime_1, utcNow);
            Controller.method_3(Controller._limitedState);
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
        var num = 0;
        var utcNow = EFTDateTimeClass.UtcNow;
        var dateTime = utcNow;
        var dateTime2 = dateTime;
        var timeSpan = TimeSpan.Zero;
        for (; ; )
        {
            num++;
            var dateTime3 = dateTime - SpeakingSecondsInterval;
            var timeSpan2 = _checker.method_1(dateTime3);
            var timeSpan3 = SpeakingSecondsLimit - timeSpan2;
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

    private readonly LimitChecker _checker;
    private bool _enabled = true;
    private bool _forceMute;

    private DateTime? _nullable_0;

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
    public BindableStateClass<bool> HasInteraction { get; }
    public BindableStateClass<bool> TalkDetected { get; }
    public TimeSpan TimeToNextStatus
    {
        get
        {
            return _currentState.TimeSpan_0;
        }
    }
    public event Action<string> AbuseNotification;

    private readonly OffState _offState;
    private readonly ReadyState _readyState;
    private readonly TalkingState _talkingState;
    private readonly LimitedState _limitedState;
    private readonly BlockedState _blockedState;
    private readonly BannedState _bannedState;
    private VOIPState _currentState;
    private readonly CompositeDisposableClass _compositeDisposableClass = new(2);

    public void method_2()
    {
        _nullable_0 = new DateTime?(EFTDateTimeClass.UtcNow);
    }

    public EVoipControllerStatus method_3(VOIPState state)
    {
        if (_currentState == state)
        {
            return state.Status;
        }
        state.Controller = this;
        var @class = Interlocked.Exchange(ref _currentState, state);
        if (@class != null)
        {
            @class.Controller = null;
        }
        method_4(state.Status);
        Status.Value = state.Status;
        if (_currentState != state)
        {
            FikaGlobals.LogError("State changed on Status event!");
        }
        if (!state.Bool1)
        {
            return _currentState.Status;
        }
        return state.vmethod_0();
    }

    public void method_4(EVoipControllerStatus status)
    {
        var evoipState = LocalPlayer.VoipState;
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
        var ban = LocalPlayer.Profile.Info.GetBan(EBanType.Voip);
        if (ban == null)
        {
            return default;
        }

        var banUntil = ban.BanUntil;
        return new(in banUntil);
    }

    private Struct510 method_11()
    {
        var timeSpan = BlockingTime;
        return new(timeSpan);
    }

    private Struct510 method_9()
    {
        var timeSpan = SpeakDelayBetweenLimit;
        return new(in timeSpan);
    }

    public void method_12()
    {
        var timeSpan = EFTDateTimeClass.UtcNow - LocalPlayer.HearingDateTime;
        var flag = timeSpan <= _hearingDetectionTime;
        TalkDetected.Value = flag;
        if (!flag)
        {
            return;
        }
        var timeSpan2 = _hearingDetectionTime - timeSpan;
        _nullable_0 = new DateTime?(EFTDateTimeClass.UtcNow + timeSpan2);
        HasInteraction.Value = true;
    }

    public class LimitChecker
    {
        public static LimitChecker Create(byte activationsLimit, float activationsInterval, float speakingInterval)
        {
            var maxActivations = Math.Max(10, (int)activationsLimit);
            return new LimitChecker()
            {
                _activationsInterval = TimeSpan.FromSeconds(activationsInterval),
                _timeStates = (activationsLimit > 0 ? new(maxActivations) : null),
                _activationsLimit = activationsLimit,
                _interval = TimeSpan.FromSeconds(Math.Max(activationsInterval, speakingInterval))
            };
        }

        private TimeSpan _activationsInterval;
        private List<TimeState> _timeStates;
        private byte _activationsLimit;
        private TimeSpan _interval;

        public void InsertTimeState(DateTime from, DateTime to)
        {
            if (_timeStates == null)
            {
                return;
            }

            var dateTime = EFTDateTimeClass.UtcNow - _interval;
            var num = _timeStates.Count - 1;
            while (num >= 0 && !(_timeStates[num].To >= dateTime))
            {
                _timeStates.RemoveAt(num);
                num--;
            }
            _timeStates.Insert(0, new()
            {
                From = from,
                To = to
            });
        }

        public TimeSpan method_1(DateTime dateTime)
        {
            if (_timeStates == null)
            {
                return TimeSpan.Zero;
            }
            var timeSpan = TimeSpan.Zero;
            var i = 0;
            while (i < _timeStates.Count)
            {
                var state = _timeStates[i];
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
            if (_timeStates == null)
            {
                return true;
            }
            if (_timeStates.Count < _activationsLimit)
            {
                return true;
            }
            var dateTime = EFTDateTimeClass.UtcNow - _activationsInterval;
            var from = _timeStates[(_activationsLimit - 1)].From;
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

        public readonly bool Boolean_0
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
                var timeSpan = Created - EFTDateTimeClass.UtcNow;
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
        _compositeDisposableClass.Dispose();
        if (LocalPlayer != null)
        {
            LocalPlayer.VoipController = null;
        }
    }

    public void Update()
    {
        if (_forceMute)
        {
            return;
        }
        _currentState.Update();
        method_1();
    }

    public void method_1()
    {
        if (_nullable_0 != null && !(_nullable_0.Value > EFTDateTimeClass.UtcNow))
        {
            _nullable_0 = null;
            HasInteraction.Value = false;
        }
    }

    public void ForceMuteVoIP(bool enable)
    {
        _forceMute = enable;
        if (_forceMute)
        {
            if (_currentState.Status != EVoipControllerStatus.MicrophoneFail)
            {
                _currentState.vmethod_4();
            }
        }
        else if (_enabled && _currentState.Status != EVoipControllerStatus.MicrophoneFail)
        {
            _currentState.vmethod_3();
        }
    }

    public void ReportAbuse()
    {
        // Do nothing
    }

    public EVoipControllerStatus StopTalk()
    {
        return _currentState.StopTalk();
    }

    public EVoipControllerStatus ToggleTalk()
    {
        return _currentState.ToggleTalk();
    }

    public void ReceiveAbuseNotification(string reporterId)
    {
        AbuseNotification?.Invoke(reporterId);
    }
}
