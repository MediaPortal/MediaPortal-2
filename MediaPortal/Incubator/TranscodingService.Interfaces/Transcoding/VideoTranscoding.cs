#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using MediaPortal.Extensions.TranscodingService.Interfaces.Metadata.Streams;

namespace MediaPortal.Extensions.TranscodingService.Interfaces.Transcoding
{
  public class VideoTranscoding : BaseTranscoding
  {
    //Source info
    public Dictionary<int, VideoStream> SourceVideoStreams = new Dictionary<int, VideoStream>();
    public Dictionary<int, List<AudioStream>> SourceAudioStreams = new Dictionary<int, List<AudioStream>>();
    public Dictionary<int, VideoContainer> SourceVideoContainers = new Dictionary<int, VideoContainer>();
    public Dictionary<int, List<SubtitleStream>> SourceSubtitles = new Dictionary<int, List<SubtitleStream>>();
    public Dictionary<int, List<SubtitleStream>> PreferredSourceSubtitles = new Dictionary<int, List<SubtitleStream>>();

    public VideoStream FirstSourceVideoStream => SourceVideoStreams.FirstOrDefault().Value;
    public List<AudioStream> FirstSourceVideoAudioStreams => SourceAudioStreams.FirstOrDefault().Value;
    public AudioStream FirstSourceAudioStream => SourceAudioStreams.FirstOrDefault().Value?.FirstOrDefault();
    public VideoContainer FirstSourceVideoContainer => SourceVideoContainers.FirstOrDefault().Value;
    public int FirstAudioStreamIndex => SourceAudioStreams.Any() ? SourceAudioStreams.First().Key : -1;
    public SubtitleStream FirstPreferredSourceSubtitle => PreferredSourceSubtitles?.FirstOrDefault().Value.FirstOrDefault();

    //Target info
    public VideoContainer TargetVideoContainer = VideoContainer.Unknown;
    public VideoCodec TargetVideoCodec = VideoCodec.Unknown;
    public AudioCodec TargetAudioCodec = AudioCodec.Unknown;
    public int? TargetVideoMaxHeight = null;
    public long? TargetAudioFrequency = null;
    public float? TargetVideoAspectRatio = null;
    public long? TargetVideoBitrate = null;
    public long? TargetAudioBitrate = null;
    public QualityMode TargetVideoQuality = QualityMode.Default;
    public int? TargetVideoQualityFactor = null;
    public int? TargetQualityFactor = null;
    public EncodingPreset TargetPreset = EncodingPreset.Default;
    public EncodingProfile TargetProfile = EncodingProfile.Baseline;
    public PixelFormat TargetPixelFormat = PixelFormat.Yuv420;
    public float? TargetLevel = null;
    public Coder TargetCoder = Coder.Default;
    public bool TargetForceVideoTranscoding = false;
    public bool TargetForceAudioStereo = false;
    public bool TargetForceVideoCopy = false;
    public bool TargetForceAudioCopy = false;
    public bool TargetAudioMultiTrackSupport = false;
    public SubtitleSupport TargetSubtitleSupport = SubtitleSupport.None;
    public SubtitleCodec TargetSubtitleCodec = SubtitleCodec.Srt;
    public string TargetSubtitleCharacterEncoding = null;
    public IEnumerable<string> TargetSubtitleLanguages = new List<string>() { "EN" };
    public string TargetSubtitleMime = "text/srt";
    public string TargetSubtitleColor = null;
    public string TargetSubtitleFontSize = null;
    public bool TargetSubtitleBox = false;
    public string TargetSubtitleFont = null;
    public bool TargetIsLive = false;

    public string Movflags = null;
    public string HlsBaseUrl = null;
  }
}
