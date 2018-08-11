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

namespace MediaPortal.Extensions.TranscodingService.Interfaces.Transcoding
{
  public class AudioTranscoding : BaseTranscoding
  {
    //Source info
    public TimeSpan? SourceDuration = null;
    public AudioContainer SourceAudioContainer = AudioContainer.Unknown;
    public AudioCodec SourceAudioCodec = AudioCodec.Unknown;
    public long? SourceAudioBitrate = null;
    public int? SourceAudioChannels = null;
    public long? SourceAudioFrequency = null;

    //Target info
    public AudioContainer TargetAudioContainer = AudioContainer.Unknown;
    public AudioCodec TargetAudioCodec = AudioCodec.Unknown;
    public long? TargetAudioFrequency = null;
    public long? TargetAudioBitrate = null;
    public bool TargetForceAudioStereo = false;
    public bool TargetForceCopy = false;
    public Coder TargetCoder = Coder.Default;
    public bool TargetIsLive = false;
  }
}
