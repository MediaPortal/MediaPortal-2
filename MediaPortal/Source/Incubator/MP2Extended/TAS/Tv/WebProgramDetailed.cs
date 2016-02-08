using System;

namespace MediaPortal.Plugins.MP2Extended.TAS.Tv
{
    public class WebProgramDetailed : WebProgramBasic
    {
        public string Classification { get; set; }
        public string EpisodeName { get; set; }
        public string EpisodeNum { get; set; }
        public string EpisodeNumber { get; set; }
        public string EpisodePart { get; set; }
        public string Genre { get; set; }
        public bool HasConflict { get; set; }
        public bool IsChanged { get; set; }
        public bool IsPartialRecordingSeriesPending { get; set; }
        public bool IsRecording { get; set; }
        public bool IsRecordingManual { get; set; }
        public bool IsRecordingOnce { get; set; }
        public bool IsRecordingOncePending { get; set; }
        public bool IsRecordingSeries { get; set; }
        public bool IsRecordingSeriesPending { get; set; }
        public bool Notify { get; set; }
        public DateTime OriginalAirDate { get; set; }
        public int ParentalRating { get; set; }
        public string SeriesNum { get; set; }
        public int StarRating { get; set; }
    }
}
