using System;

namespace MediaPortal.Plugins.MP2Extended.TAS.Misc
{
    public class WebVirtualCard
    {
        public int BitRateMode { get; set; }
        public string ChannelName { get; set; }
        public string Device { get; set; }
        public bool Enabled { get; set; }
        public int GetTimeshiftStoppedReason { get; set; }
        public bool GrabTeletext { get; set; }
        public bool HasTeletext { get; set; }
        public int Id { get; set; }
        public int ChannelId { get; set; }
        public bool IsGrabbingEpg { get; set; }
        public bool IsRecording { get; set; }
        public bool IsScanning { get; set; }
        public bool IsScrambled { get; set; }
        public bool IsTimeShifting { get; set; }
        public bool IsTunerLocked { get; set; }
        public int MaxChannel { get; set; }
        public int MinChannel { get; set; }
        public string Name { get; set; }
        public int QualityType { get; set; }
        public string RecordingFileName { get; set; }
        public string RecordingFolder { get; set; }
        public int RecordingFormat { get; set; }
        public int RecordingScheduleId { get; set; }
        public DateTime RecordingStarted { get; set; }
        public string RemoteServer { get; set; }
        public string RTSPUrl { get; set; }
        public int SignalLevel { get; set; }
        public int SignalQuality { get; set; }
        public string TimeShiftFileName { get; set; }
        public string TimeShiftFolder { get; set; }
        public DateTime TimeShiftStarted { get; set; }
        public WebCardType Type { get; set; }
        public WebUser User { get; set; }
    }
}
