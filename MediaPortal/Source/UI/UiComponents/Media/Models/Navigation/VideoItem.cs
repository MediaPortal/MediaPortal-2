#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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
using MediaPortal.Common.Localization;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.UiComponents.Media.General;
using System.Collections.Generic;

namespace MediaPortal.UiComponents.Media.Models.Navigation
{
  public class VideoItem : PlayableMediaItem
  {
    public VideoItem(MediaItem mediaItem)
      : base(mediaItem)
    {
    }

    public override void Update(MediaItem mediaItem)
    {
      base.Update(mediaItem);
      SimpleTitle = Title;
      IList<MultipleMediaItemAspect> videoAspects;
      IList<MultipleMediaItemAspect> audioAspects;
      if (MediaItemAspect.TryGetAspects(mediaItem.Aspects, VideoStreamAspect.Metadata, out videoAspects) && 
        MediaItemAspect.TryGetAspects(mediaItem.Aspects, VideoAudioStreamAspect.Metadata, out audioAspects))
      {
        //TODO: Handle that videos might be from different versions of the movie
        long? duration = null;
        List<string> videoEncodings = new List<string>();
        foreach (MultipleMediaItemAspect videoAspect in videoAspects)
        { 
          duration += (long?)videoAspect[VideoStreamAspect.ATTR_DURATION];
          string videoEnc = (string)videoAspect[VideoStreamAspect.ATTR_VIDEOENCODING];
          if (!string.IsNullOrEmpty(videoEnc))
            if (!videoEncodings.Contains(videoEnc))
              videoEncodings.Add(videoEnc);
        }
        List<string> audioEncodings = new List<string>();
        foreach (MultipleMediaItemAspect audioAspect in audioAspects)
        {
          string audioEnc = (string)audioAspect[VideoAudioStreamAspect.ATTR_AUDIOENCODING];
          if (!string.IsNullOrEmpty(audioEnc))
            if (!audioEncodings.Contains(audioEnc))
              audioEncodings.Add(audioEnc);
        }
        Duration = duration.HasValue ? FormattingUtils.FormatMediaDuration(TimeSpan.FromSeconds((int)duration.Value)) : string.Empty;
        AudioEncoding = audioEncodings.Count > 0 ? string.Join(", ", audioEncodings) : string.Empty;
        VideoEncoding = videoEncodings.Count > 0 ? string.Join(", ", videoEncodings) : string.Empty;
      }
      FireChange();
    }

    public string AudioEncoding
    {
      get { return this[Consts.KEY_AUDIO_ENCODING]; }
      set { SetLabel(Consts.KEY_AUDIO_ENCODING, value); }
    }

    public string VideoEncoding
    {
      get { return this[Consts.KEY_VIDEO_ENCODING]; }
      set { SetLabel(Consts.KEY_VIDEO_ENCODING, value); }
    }
  }
}
