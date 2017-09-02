#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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
using MediaPortal.Plugins.Transcoding.Interfaces.Helpers;
using MediaPortal.Plugins.Transcoding.Interfaces.Metadata.Streams;

namespace MediaPortal.Plugins.Transcoding.Interfaces.Transcoding
{
  public class VideoTranscoding : BaseTranscoding
  {
    //Source info
    public int SourceVideoStreamIndex = -1;
    public int SourceAudioStreamIndex = -1;
    public TimeSpan SourceDuration = new TimeSpan(0);
    public VideoContainer SourceVideoContainer = VideoContainer.Unknown;
    public VideoCodec SourceVideoCodec = VideoCodec.Unknown;
    public AudioCodec SourceAudioCodec = AudioCodec.Unknown;
    public PixelFormat SourcePixelFormat = PixelFormat.Yuv420;
    public long SourceVideoBitrate = -1;
    public long SourceAudioBitrate = -1;
    public int SourceAudioChannels = -1;
    public long SourceAudioFrequency = -1;
    public float SourceVideoAspectRatio = -1;
    public float SourceVideoPixelAspectRatio = -1;
    public float SourceFrameRate = -1;
    public int SourceVideoHeight = -1;
    public int SourceVideoWidth = -1;
    public bool SourceSubtitleAvailable = false;
    public int SourceSubtitleStreamIndex = Subtitles.AUTO_SUBTITLE;
    public List<SubtitleStream> SourceSubtitles = new List<SubtitleStream>();

    //Target info
    public VideoContainer TargetVideoContainer = VideoContainer.Unknown;
    public VideoCodec TargetVideoCodec = VideoCodec.Unknown;
    public AudioCodec TargetAudioCodec = AudioCodec.Unknown;
    public int TargetVideoMaxHeight = -1;
    public long TargetAudioFrequency = -1;
    public float TargetVideoAspectRatio = -1;
    public long TargetVideoBitrate = -1;
    public long TargetAudioBitrate = -1;
    public QualityMode TargetVideoQuality = QualityMode.Default;
    public int TargetVideoQualityFactor = -1;
    public int TargetQualityFactor = -1;
    public EncodingPreset TargetPreset = EncodingPreset.Default;
    public EncodingProfile TargetProfile = EncodingProfile.Baseline;
    public PixelFormat TargetPixelFormat = PixelFormat.Yuv420;
    public float TargetLevel = -1;
    public Coder TargetCoder = Coder.Default;
    public bool TargetForceVideoTranscoding = false;
    public bool TargetForceAudioStereo = false;
    public bool TargetForceVideoCopy = false;
    public bool TargetForceAudioCopy = false;
    public SubtitleSupport TargetSubtitleSupport = SubtitleSupport.None;
    public SubtitleCodec TargetSubtitleCodec = SubtitleCodec.Srt;
    public string TargetSubtitleLanguages = "EN";
    public string TargetSubtitleMime = "text/srt";
    public bool TargetIsLive = false;

    public string Movflags = null;
    public string HlsBaseUrl = null;
  }
}
