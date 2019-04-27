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

using System;
using System.Collections.Generic;
using MediaPortal.Extensions.TranscodingService.Interfaces.Metadata;
using MediaPortal.Extensions.TranscodingService.Interfaces.Profiles.MediaInfo;
using MediaPortal.Extensions.TranscodingService.Interfaces.Profiles.MediaMatch;
using MediaPortal.Extensions.TranscodingService.Interfaces.Profiles.Setup.Settings;
using MediaPortal.Extensions.TranscodingService.Interfaces.Profiles.Setup.Targets;

namespace MediaPortal.Extensions.TranscodingService.Interfaces.Profiles.Setup
{
  public class TranscodingSetup
  {
    public VideoSettings VideoSettings = new VideoSettings();
    public ImageSettings ImageSettings = new ImageSettings();
    public AudioSettings AudioSettings = new AudioSettings();
    public SubtitleSettings SubtitleSettings = new SubtitleSettings();

    public List<VideoTranscodingTarget> GenericVideoTargets = new List<VideoTranscodingTarget>();
    public List<AudioTranscodingTarget> GenericAudioTargets = new List<AudioTranscodingTarget>();
    public List<ImageTranscodingTarget> GenericImageTargets = new List<ImageTranscodingTarget>();

    public List<VideoTranscodingTarget> VideoTargets = new List<VideoTranscodingTarget>();
    public List<AudioTranscodingTarget> AudioTargets = new List<AudioTranscodingTarget>();
    public List<ImageTranscodingTarget> ImageTargets = new List<ImageTranscodingTarget>();

    public VideoTranscodingTarget GetMatchingVideoTranscoding(MetadataContainer info, int matchedAudioStream, out VideoMatch matchedSource)
    {
      matchedSource = null;

      if (info == null)
        return null;

      foreach (VideoTranscodingTarget tDef in VideoTargets)
      {
        foreach (VideoInfo src in tDef.Sources)
        {
          if (src.Matches(info, matchedAudioStream, VideoSettings.H264LevelCheckMethod))
          {
            matchedSource = new VideoMatch();
            matchedSource.MatchedAudioStream = matchedAudioStream;
            matchedSource.MatchedVideoSource = src;
            return tDef;
          }
        }
      }
      return null;
    }

    public AudioTranscodingTarget GetMatchingAudioTranscoding(MetadataContainer info, out AudioMatch matchedSource)
    {
      matchedSource = null;
      if (info == null) return null;

      int matchedAudioStream = info.FirstAudioStream.StreamIndex;
      foreach (AudioTranscodingTarget tDef in AudioTargets)
      {
        foreach (AudioInfo src in tDef.Sources)
        {
          if (src.Matches(info, matchedAudioStream))
          {
            matchedSource = new AudioMatch();
            matchedSource.MatchedAudioStream = matchedAudioStream;
            matchedSource.MatchedAudioSource = src;
            return tDef;
          }
        }
      }
      return null;
    }

    public ImageTranscodingTarget GetMatchingImageTranscoding(MetadataContainer info, out ImageMatch matchedSource)
    {
      matchedSource = null;
      if (info == null) return null;

      foreach (ImageTranscodingTarget tDef in ImageTargets)
      {
        foreach (ImageInfo src in tDef.Sources)
        {
          if (src.Matches(info))
          {
            matchedSource = new ImageMatch();
            matchedSource.MatchedImageSource = src;
            return tDef;
          }
        }
      }
      if (info.Image.Height > ImageSettings.MaxHeight ||
        info.Image.Width > ImageSettings.MaxWidth ||
        (info.Image.Orientation > 1 && ImageSettings.AutoRotate == true))
      {
        matchedSource = new ImageMatch();
        matchedSource.MatchedImageSource = new ImageInfo();

        ImageTranscodingTarget tDef = new ImageTranscodingTarget();
        tDef.Target = new ImageInfo();
        tDef.Target.ImageContainerType = info.Metadata.ImageContainerType;
        tDef.Target.PixelFormatType = info.Image.PixelFormatType;
        tDef.Target.QualityType = ImageSettings.Quality;
        return tDef;
      }
      return null;
    }
  }
}
