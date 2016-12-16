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

using MediaPortal.Common.Localization;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UiComponents.Media.General;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

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
      SingleMediaItemAspect videoAspect;
      if (MediaItemAspect.TryGetAspect(mediaItem.Aspects, VideoAspect.Metadata, out videoAspect))
        StoryPlot = (string)videoAspect[VideoAspect.ATTR_STORYPLOT];

      VideoStreams = new ItemsList();
      AudioStreams = new ItemsList();
      Subtitles = new ItemsList();
      IList<MultipleMediaItemAspect> videoStreamAspects;
      IList<MultipleMediaItemAspect> audioStreamAspects;
      IList<MultipleMediaItemAspect> subtitleAspects;
      Dictionary<int, VideoStreamItem> videoStreams = new Dictionary<int, VideoStreamItem>();
      Dictionary<int, List<VideoAudioStreamItem>> videoAudioStreams = new Dictionary<int, List<VideoAudioStreamItem>>();
      Dictionary<int, List<SubtitleItem>> subtitleStreams = new Dictionary<int, List<SubtitleItem>>();
      MediaItemAspect.TryGetAspects(mediaItem.Aspects, SubtitleAspect.Metadata, out subtitleAspects);
      if (MediaItemAspect.TryGetAspects(mediaItem.Aspects, VideoStreamAspect.Metadata, out videoStreamAspects) &&
        MediaItemAspect.TryGetAspects(mediaItem.Aspects, VideoAudioStreamAspect.Metadata, out audioStreamAspects))
      {
        List<string> videoEncodings = new List<string>();
        List<string> audioEncodings = new List<string>();
        foreach (MultipleMediaItemAspect videoStreamAspect in videoStreamAspects)
        {
          int? part = (int?)videoStreamAspect[VideoStreamAspect.ATTR_VIDEO_PART];
          int? partSet = (int?)videoStreamAspect[VideoStreamAspect.ATTR_VIDEO_PART_SET];
          string partSetName = (string)videoStreamAspect[VideoStreamAspect.ATTR_VIDEO_PART_SET_NAME];
          if (partSet.HasValue)
          {
            if (!videoStreams.ContainsKey(partSet.Value))
            {
              videoStreams.Add(partSet.Value, new VideoStreamItem());
              videoStreams[partSet.Value].Set = partSet.Value;
              videoStreams[partSet.Value].SetName = partSetName;
              videoAudioStreams.Add(partSet.Value, new List<VideoAudioStreamItem>());
              subtitleStreams.Add(partSet.Value, new List<SubtitleItem>());
            }
            else
              continue;
          }
          long? dur = null;
          float? aspectRatio = null;
          string type = null;
          long? bitrate = null;
          float? fps = null;
          int? height = null;
          int? width = null;
          string encoding = null;
          int? parts = null;
          string language = null;
          int? channels = null;
          bool? isForced = null;
          bool? isDefault = null;
          long? samplerate = null;
          if (!part.HasValue || part.Value < 0)
          {
            dur = (long?)videoStreamAspect[VideoStreamAspect.ATTR_DURATION];
            aspectRatio = (float?)videoStreamAspect[VideoStreamAspect.ATTR_ASPECTRATIO];
            type = (string)videoStreamAspect[VideoStreamAspect.ATTR_VIDEO_TYPE];
            bitrate = (long?)videoStreamAspect[VideoStreamAspect.ATTR_VIDEOBITRATE];
            fps = (float?)videoStreamAspect[VideoStreamAspect.ATTR_FPS];
            height = (int?)videoStreamAspect[VideoStreamAspect.ATTR_HEIGHT];
            width = (int?)videoStreamAspect[VideoStreamAspect.ATTR_WIDTH];
            encoding = (string)videoStreamAspect[VideoStreamAspect.ATTR_VIDEOENCODING];
            parts = 0;
          }
          else if (partSet.HasValue)
          {
            IEnumerable<MultipleMediaItemAspect> aspectList = videoStreamAspects.Where(a => (int?)a[VideoStreamAspect.ATTR_VIDEO_PART_SET] == partSet &&
              a[VideoStreamAspect.ATTR_DURATION] != null);
            if (aspectList.Any())
              dur = aspectList.Sum(a => (long)a[VideoStreamAspect.ATTR_DURATION]);

            aspectList = videoStreamAspects.Where(a => (int?)a[VideoStreamAspect.ATTR_VIDEO_PART_SET] == partSet &&
              a[VideoStreamAspect.ATTR_ASPECTRATIO] != null);
            if (aspectList.Any())
              aspectRatio = aspectList.Max(a => (float)a[VideoStreamAspect.ATTR_ASPECTRATIO]);

            aspectList = videoStreamAspects.Where(a => (int?)a[VideoStreamAspect.ATTR_VIDEO_PART_SET] == partSet &&
              a[VideoStreamAspect.ATTR_VIDEOBITRATE] != null);
            if (aspectList.Any())
              bitrate = aspectList.Max(a => (long)a[VideoStreamAspect.ATTR_VIDEOBITRATE]);

            aspectList = videoStreamAspects.Where(a => (int?)a[VideoStreamAspect.ATTR_VIDEO_PART_SET] == partSet &&
              a[VideoStreamAspect.ATTR_FPS] != null);
            if (aspectList.Any())
              fps = aspectList.Max(a => (float)a[VideoStreamAspect.ATTR_FPS]);

            aspectList = videoStreamAspects.Where(a => (int?)a[VideoStreamAspect.ATTR_VIDEO_PART_SET] == partSet &&
              a[VideoStreamAspect.ATTR_HEIGHT] != null);
            if (aspectList.Any())
              height = aspectList.Max(a => (int)a[VideoStreamAspect.ATTR_HEIGHT]);

            aspectList = videoStreamAspects.Where(a => (int?)a[VideoStreamAspect.ATTR_VIDEO_PART_SET] == partSet &&
              a[VideoStreamAspect.ATTR_WIDTH] != null);
            if (aspectList.Any())
              width = aspectList.Max(a => (int)a[VideoStreamAspect.ATTR_WIDTH]);

            aspectList = videoStreamAspects.Where(a => (int?)a[VideoStreamAspect.ATTR_VIDEO_PART_SET] == partSet &&
              a[VideoStreamAspect.ATTR_VIDEO_PART] != null);
            if (aspectList.Any())
              parts = aspectList.Max(a => (int)a[VideoStreamAspect.ATTR_VIDEO_PART]);

            encoding = videoStreamAspects.Where(a => (int?)a[VideoStreamAspect.ATTR_VIDEO_PART_SET] == partSet &&
              a[VideoStreamAspect.ATTR_VIDEOENCODING] != null).Select(a => (string)a[VideoStreamAspect.ATTR_VIDEOENCODING]).FirstOrDefault();
            type = videoStreamAspects.Where(a => (int?)a[VideoStreamAspect.ATTR_VIDEO_PART_SET] == partSet &&
              a[VideoStreamAspect.ATTR_VIDEO_TYPE] != null).Select(a => (string)a[VideoStreamAspect.ATTR_VIDEO_TYPE]).FirstOrDefault();
          }

          videoStreams[partSet.Value].Duration = dur.HasValue ? FormattingUtils.FormatMediaDuration(TimeSpan.FromSeconds((int)dur.Value)) : string.Empty;
          videoStreams[partSet.Value].Format = type;
          if (aspectRatio.HasValue)
          {
            if (aspectRatio.Value < 1.4)
              videoStreams[partSet.Value].AspectRatio = "4:3";
            else if (aspectRatio.Value < 1.8)
              videoStreams[partSet.Value].AspectRatio = "16:9";
            else
              videoStreams[partSet.Value].AspectRatio = "21:9";
          }
          if (bitrate.HasValue)
            videoStreams[partSet.Value].BitRate = bitrate.ToString() + " kbps";
          if (fps.HasValue)
          {
            float validFrameRate = 23.976F;
            if (fps.Value < 23.989999999999998D)
              validFrameRate = 23.976F;
            else if (fps.Value < 24.100000000000001D)
              validFrameRate = 24;
            else if (fps.Value < 25.100000000000001D)
              validFrameRate = 25;
            else if (fps.Value < 29.989999999999998D)
              validFrameRate = 29.97F;
            else if (fps.Value < 30.100000000000001D)
              validFrameRate = 30;
            else if (fps.Value < 50.100000000000001D)
              validFrameRate = 50;
            else if (fps.Value < 59.990000000000002D)
              validFrameRate = 59.94F;
            else if (fps.Value < 60.100000000000001D)
              validFrameRate = 60;
            else if (fps.Value < 120.100000000000001D)
              validFrameRate = 120;
            videoStreams[partSet.Value].FPS = validFrameRate.ToString("0.###");
          }
          if(height.HasValue)
            videoStreams[partSet.Value].Height = height.Value;
          if(width.HasValue)
            videoStreams[partSet.Value].Width = width.Value;
          videoStreams[partSet.Value].Parts = parts;
          videoStreams[partSet.Value].VideoEncoding = encoding;

          string videoEnc = (string)videoStreamAspect[VideoStreamAspect.ATTR_VIDEOENCODING];
          if (!string.IsNullOrEmpty(videoEnc))
            if (!videoEncodings.Contains(videoEnc))
              videoEncodings.Add(videoEnc);

          foreach (MultipleMediaItemAspect audioStreamAspect in audioStreamAspects)
          {
            if ((int)videoStreamAspect[VideoStreamAspect.ATTR_RESOURCE_INDEX] == (int)audioStreamAspect[VideoAudioStreamAspect.ATTR_RESOURCE_INDEX])
            {
              VideoAudioStreamItem audioItem = new VideoAudioStreamItem();
              audioItem.Set = partSet.Value;
              channels = null;
              language = null;
              bitrate = null;
              encoding = null;
              samplerate = null;
              if (!part.HasValue || part.Value < 0)
              {
                channels = (int?)audioStreamAspect[VideoAudioStreamAspect.ATTR_AUDIOCHANNELS];
                language = (string)audioStreamAspect[VideoAudioStreamAspect.ATTR_AUDIOLANGUAGE];
                bitrate = (long?)audioStreamAspect[VideoAudioStreamAspect.ATTR_AUDIOBITRATE];
                encoding = (string)audioStreamAspect[VideoAudioStreamAspect.ATTR_AUDIOENCODING];
                samplerate = (long?)audioStreamAspect[VideoAudioStreamAspect.ATTR_AUDIOSAMPLERATE];
              }
              else if (partSet.HasValue)
              {
                IEnumerable<MultipleMediaItemAspect> aspectList = audioStreamAspects.Where(a => (int)a[VideoAudioStreamAspect.ATTR_RESOURCE_INDEX] == (int)videoStreamAspect[VideoStreamAspect.ATTR_RESOURCE_INDEX] &&
                  a[VideoAudioStreamAspect.ATTR_AUDIOCHANNELS] != null);
                if (aspectList.Any())
                  channels = aspectList.Max(a => (int)a[VideoAudioStreamAspect.ATTR_AUDIOCHANNELS]);

                aspectList = audioStreamAspects.Where(a => (int)a[VideoAudioStreamAspect.ATTR_RESOURCE_INDEX] == (int)videoStreamAspect[VideoStreamAspect.ATTR_RESOURCE_INDEX] &&
                  a[VideoAudioStreamAspect.ATTR_AUDIOSAMPLERATE] != null);
                if (aspectList.Any())
                  samplerate = aspectList.Max(a => (long)a[VideoAudioStreamAspect.ATTR_AUDIOSAMPLERATE]);

                aspectList = audioStreamAspects.Where(a => (int)a[VideoAudioStreamAspect.ATTR_RESOURCE_INDEX] == (int)videoStreamAspect[VideoStreamAspect.ATTR_RESOURCE_INDEX] &&
                  a[VideoAudioStreamAspect.ATTR_AUDIOBITRATE] != null);
                if (aspectList.Any())
                  bitrate = aspectList.Max(a => (long)a[VideoAudioStreamAspect.ATTR_AUDIOBITRATE]);

                language = audioStreamAspects.Where(a => (int)a[VideoAudioStreamAspect.ATTR_RESOURCE_INDEX] == (int)videoStreamAspect[VideoStreamAspect.ATTR_RESOURCE_INDEX] &&
                  a[VideoAudioStreamAspect.ATTR_AUDIOLANGUAGE] != null).Select(a => (string)a[VideoAudioStreamAspect.ATTR_AUDIOLANGUAGE]).FirstOrDefault();
                encoding = audioStreamAspects.Where(a => (int)a[VideoAudioStreamAspect.ATTR_RESOURCE_INDEX] == (int)videoStreamAspect[VideoStreamAspect.ATTR_RESOURCE_INDEX] &&
                  a[VideoAudioStreamAspect.ATTR_AUDIOENCODING] != null).Select(a => (string)a[VideoAudioStreamAspect.ATTR_AUDIOENCODING]).FirstOrDefault();
              }

              audioItem.AudioEncoding = encoding;
              if (bitrate.HasValue)
                audioItem.BitRate = bitrate.Value.ToString() + " kbps";
              if (samplerate.HasValue)
                audioItem.SampleRate = (samplerate.Value / 1000.0).ToString("0.0") + " kHz";

              if (channels.HasValue)
              {
                if (channels.Value == 1)
                  audioItem.Channels = "1.0";
                else if (channels.Value == 2)
                  audioItem.Channels = "2.0";
                else if (channels.Value == 5)
                  audioItem.Channels = "5.0";
                else if (channels.Value == 6)
                  audioItem.Channels = "5.1";
                else if (channels.Value == 7)
                  audioItem.Channels = "7.0";
                else if (channels.Value == 8)
                  audioItem.Channels = "7.1";
                else if (channels.Value == 10)
                  audioItem.Channels = "10.0";
                else if (channels.Value == 12)
                  audioItem.Channels = "10.2";
              }
              audioItem.Language = language;
              videoAudioStreams[partSet.Value].Add(audioItem);

              string audioEnc = (string)audioStreamAspect[VideoAudioStreamAspect.ATTR_AUDIOENCODING];
              if (!string.IsNullOrEmpty(audioEnc))
                if (!audioEncodings.Contains(audioEnc))
                  audioEncodings.Add(audioEnc);
            }
          }

          if (subtitleAspects != null)
          {
            foreach (MultipleMediaItemAspect subtitleAspect in subtitleAspects)
            {
              if ((int)videoStreamAspect[VideoStreamAspect.ATTR_RESOURCE_INDEX] == (int)subtitleAspect[SubtitleAspect.ATTR_VIDEO_RESOURCE_INDEX])
              {
                SubtitleItem subtitleItem = new SubtitleItem();
                subtitleItem.Set = partSet.Value;

                language = null;
                encoding = null;
                type = null;
                isDefault = null;
                isForced = null;
                if (!part.HasValue || part.Value < 0)
                {
                  isDefault = (bool?)subtitleAspect[SubtitleAspect.ATTR_DEFAULT];
                  isForced = (bool?)subtitleAspect[SubtitleAspect.ATTR_FORCED];
                  type = (string)subtitleAspect[SubtitleAspect.ATTR_SUBTITLE_FORMAT];
                  language = (string)subtitleAspect[SubtitleAspect.ATTR_SUBTITLE_LANGUAGE];
                }
                else if (partSet.HasValue)
                {
                  isDefault = subtitleAspects.Where(a => (int)a[SubtitleAspect.ATTR_RESOURCE_INDEX] == (int)videoStreamAspect[VideoStreamAspect.ATTR_RESOURCE_INDEX] &&
                    a[SubtitleAspect.ATTR_DEFAULT] != null).Select(a => (bool)a[SubtitleAspect.ATTR_DEFAULT]).FirstOrDefault();
                  isForced = subtitleAspects.Where(a => (int)a[SubtitleAspect.ATTR_RESOURCE_INDEX] == (int)videoStreamAspect[VideoStreamAspect.ATTR_RESOURCE_INDEX] &&
                    a[SubtitleAspect.ATTR_FORCED] != null).Select(a => (bool)a[SubtitleAspect.ATTR_FORCED]).FirstOrDefault();
                  type = subtitleAspects.Where(a => (int)a[SubtitleAspect.ATTR_RESOURCE_INDEX] == (int)videoStreamAspect[VideoStreamAspect.ATTR_RESOURCE_INDEX] &&
                    a[SubtitleAspect.ATTR_SUBTITLE_FORMAT] != null).Select(a => (string)a[SubtitleAspect.ATTR_SUBTITLE_FORMAT]).FirstOrDefault();
                  language = subtitleAspects.Where(a => (int)a[SubtitleAspect.ATTR_RESOURCE_INDEX] == (int)videoStreamAspect[VideoStreamAspect.ATTR_RESOURCE_INDEX] &&
                    a[SubtitleAspect.ATTR_SUBTITLE_LANGUAGE] != null).Select(a => (string)a[SubtitleAspect.ATTR_SUBTITLE_LANGUAGE]).FirstOrDefault();
                }

                subtitleItem.Default = isDefault;
                subtitleItem.Forced = isForced;
                subtitleItem.Format = type;
                subtitleItem.Language = language;
                subtitleStreams[partSet.Value].Add(subtitleItem);
              }
            }
          }
        }

        Duration = videoStreams.Count > 0 ? videoStreams.First().Value.Duration : string.Empty;
        AudioEncoding = audioEncodings.Count > 0 ? string.Join(", ", audioEncodings) : string.Empty;
        VideoEncoding = videoEncodings.Count > 0 ? string.Join(", ", videoEncodings) : string.Empty;
        VideoStreams.Clear();
        foreach (VideoStreamItem v in videoStreams.Values)
          VideoStreams.Add(v);
        AudioStreams.Clear();
        foreach (List<VideoAudioStreamItem> aList in videoAudioStreams.Values)
          foreach (VideoAudioStreamItem a in aList)
            AudioStreams.Add(a);
        Subtitles.Clear();
        foreach (List<SubtitleItem> sList in subtitleStreams.Values)
          foreach (SubtitleItem s in sList)
            Subtitles.Add(s);
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

    public string StoryPlot
    {
      get { return this[Consts.KEY_STORY_PLOT]; }
      set { SetLabel(Consts.KEY_STORY_PLOT, value); }
    }

    public ItemsList Subtitles { get; set; }

    public ItemsList VideoStreams { get; set; }

    public ItemsList AudioStreams { get; set; }
  }
}
