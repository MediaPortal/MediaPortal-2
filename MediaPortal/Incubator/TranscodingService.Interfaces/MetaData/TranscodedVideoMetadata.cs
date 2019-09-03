﻿#region Copyright (C) 2007-2017 Team MediaPortal

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

namespace MediaPortal.Extensions.TranscodingService.Interfaces.Metadata
{
  public class TranscodedVideoMetadata
  {
    public VideoContainer TargetVideoContainer = VideoContainer.Unknown;
    public VideoCodec TargetVideoCodec = VideoCodec.Unknown;
    public AudioCodec TargetAudioCodec = AudioCodec.Unknown;
    public int? TargetVideoMaxHeight = null;
    public int? TargetVideoMaxWidth = null;
    public long? TargetAudioFrequency = null;
    public float? TargetVideoAspectRatio = null;
    public long? TargetVideoBitrate = null;
    public long? TargetAudioBitrate = null;
    public int? TargetAudioChannels = null;
    public float? TargetVideoPixelAspectRatio = null;
    public EncodingPreset TargetPreset = EncodingPreset.Default;
    public EncodingProfile TargetProfile = EncodingProfile.Baseline;
    public PixelFormat TargetVideoPixelFormat = PixelFormat.Yuv420;
    public float? TargetLevel = null;
    public float? TargetVideoFrameRate = null;
    public Timestamp TargetVideoTimestamp = Timestamp.None;
  }
}
