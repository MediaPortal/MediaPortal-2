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
using MediaPortal.Plugins.Transcoding.Service.Metadata;
using MediaPortal.Plugins.Transcoding.Service.Profiles.MediaInfo;
using MediaPortal.Plugins.Transcoding.Service.Profiles.MediaMatch;
using MediaPortal.Plugins.Transcoding.Service.Profiles.Setup.Settings;
using MediaPortal.Plugins.Transcoding.Service.Profiles.Setup.Targets;

namespace MediaPortal.Plugins.Transcoding.Service.Profiles.Setup
{
  public class TranscodingSetup
  {
    public VideoSettings VideoSettings = new VideoSettings();
    public ImageSettings ImageSettings = new ImageSettings();
    public AudioSettings AudioSettings = new AudioSettings();
    public SubtitleSettings SubtitleSettings = new SubtitleSettings();

    public List<VideoTranscodingTarget> VideoTargets = new List<VideoTranscodingTarget>();
    public List<AudioTranscodingTarget> AudioTargets = new List<AudioTranscodingTarget>();
    public List<ImageTranscodingTarget> ImageTargets = new List<ImageTranscodingTarget>();

    public VideoTranscodingTarget GetMatchingVideoTranscoding(MetadataContainer info, string preferredAudioLanguages, out VideoMatch matchedSource)
    {
      int iMatchedAudioStream = 0;
      if (string.IsNullOrEmpty(preferredAudioLanguages) == false)
      {
        List<string> valuesLangs = new List<string>(preferredAudioLanguages.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
        int currentPriority = -1;
        for (int iAudio = 0; iAudio < info.Audio.Count; iAudio++)
        {
          for (int iPriority = 0; iPriority < valuesLangs.Count; iPriority++)
          {
            if (valuesLangs[iPriority].Equals(info.Audio[iAudio].Language, StringComparison.InvariantCultureIgnoreCase) == true)
            {
              if (currentPriority == -1 || iPriority < currentPriority)
              {
                currentPriority = iPriority;
                iMatchedAudioStream = iAudio;
              }
            }
          }
        }
      }

      foreach (VideoTranscodingTarget tDef in VideoTargets)
      {
        foreach (VideoInfo src in tDef.Sources)
        {
          if (src.Matches(info, iMatchedAudioStream, VideoSettings.H264LevelCheckMethod))
          {
            matchedSource = new VideoMatch();
            matchedSource.MatchedAudioStream = iMatchedAudioStream;
            matchedSource.MatchedVideoSource = src;
            return tDef;
          }
        }
      }
      matchedSource = null;
      return null;
    }

    public AudioTranscodingTarget GetMatchingAudioTranscoding(MetadataContainer info, out AudioMatch matchedSource)
    {
      int iMatchedAudioStream = 0;
      foreach (AudioTranscodingTarget tDef in AudioTargets)
      {
        foreach (AudioInfo src in tDef.Sources)
        {
          if (src.Matches(info, iMatchedAudioStream))
          {
            matchedSource = new AudioMatch();
            matchedSource.MatchedAudioStream = iMatchedAudioStream;
            matchedSource.MatchedAudioSource = src;
            return tDef;
          }
        }
      }
      matchedSource = null;
      return null;
    }

    public ImageTranscodingTarget GetMatchingImageTranscoding(MetadataContainer info, out ImageMatch matchedSource)
    {
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
      matchedSource = null;
      return null;
    }
  }
}
