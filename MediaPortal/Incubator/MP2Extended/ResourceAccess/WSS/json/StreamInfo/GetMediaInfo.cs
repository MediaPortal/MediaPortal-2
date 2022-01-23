#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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
using System.Globalization;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.MP2Extended.Utils;
using MediaPortal.Plugins.MP2Extended.WSS.StreamInfo;
using MediaPortal.Plugins.MP2Extended.Common;
using MP2Extended.Extensions;
using System.Linq;
using MediaPortal.Extensions.TranscodingService.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.LiveTvMediaItem;
using MediaPortal.Extensions.TranscodingService.Interfaces.Metadata;
using System.Threading.Tasks;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.Profiles;
using Microsoft.Owin;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.json.StreamInfo
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "itemId", Type = typeof(string), Nullable = false)]
  [ApiFunctionParam(Name = "type", Type = typeof(int), Nullable = false)]
  internal class GetMediaInfo
  {
    private const string UNDEFINED = "?";

    public static async Task<WebMediaInfo> ProcessAsync(IOwinContext context, string itemId, WebMediaType type)
    {
      if (itemId == null)
        throw new BadRequestException("GetMediaInfo: itemId is null");

      Guid mediaItemId;
      MediaItem item;
      long duration = 0;
      string container = string.Empty;
      MetadataContainer info;
      List<WebVideoStream> webVideoStreams = new List<WebVideoStream>();
      List<WebAudioStream> webAudioStreams = new List<WebAudioStream>();
      List<WebSubtitleStream> webSubtitleStreams = new List<WebSubtitleStream>();

      if (type == WebMediaType.TV || type == WebMediaType.Radio)
      {
        int channelIdInt;
        if (int.TryParse(itemId, out channelIdInt))
        {
          item = new LiveTvMediaItem(Guid.Empty);
          info = MediaAnalyzer.ParseChannelStreamAsync(channelIdInt, (LiveTvMediaItem)item).Result;
          if (info == null)
          {
            throw new BadRequestException(String.Format("GetMediaInfo: Channel {0} stream not available", itemId));
          }
        }
        else
        {
          throw new BadRequestException(String.Format("GetMediaInfo: Channel {0} not found", itemId));
        }
      }
      else if (Guid.TryParse(itemId, out mediaItemId) == true)
      {
        ISet<Guid> necessaryMIATypes = new HashSet<Guid>();
        necessaryMIATypes.Add(MediaAspect.ASPECT_ID);
        necessaryMIATypes.Add(ProviderResourceAspect.ASPECT_ID);
        necessaryMIATypes.Add(ImporterAspect.ASPECT_ID);

        ISet<Guid> optionalMIATypes = new HashSet<Guid>();
        optionalMIATypes.Add(VideoAspect.ASPECT_ID);
        optionalMIATypes.Add(VideoStreamAspect.ASPECT_ID);
        optionalMIATypes.Add(VideoAudioStreamAspect.ASPECT_ID);
        optionalMIATypes.Add(AudioAspect.ASPECT_ID);
        optionalMIATypes.Add(ImageAspect.ASPECT_ID);

        item = MediaLibraryAccess.GetMediaItemById(context, itemId, necessaryMIATypes, optionalMIATypes);

        if (item == null)
          throw new BadRequestException(String.Format("GetMediaInfo: No MediaItem found with id: {0}", itemId));
      }
      else
      {
        throw new BadRequestException(String.Format("GetMediaInfo: Media not found with id: {0}", itemId));
      }

      // decide which type of media item we have
      info = await MediaAnalyzer.ParseMediaItemAsync(item);
      if (item.Aspects.ContainsKey(VideoAspect.ASPECT_ID))
      {
        var videoAspect = item.GetAspect(VideoAspect.Metadata);
        var videoStreamAspect = item.GetAspect(VideoStreamAspect.Metadata);
        duration = Convert.ToInt64(videoStreamAspect.GetAttributeValue<long?>(VideoStreamAspect.ATTR_DURATION) ?? 0);
        bool interlaced = (videoStreamAspect.GetAttributeValue<string>(VideoStreamAspect.ATTR_VIDEO_TYPE) ?? "").ToLowerInvariant().Contains("interlaced");

        foreach (var data in info.Metadata)
        {
          int edition = data.Key;
          int editionOffset = 0;
          if (edition >= 0)
            editionOffset = (edition + 1) * ProfileMediaItem.EDITION_OFFSET;

          if (info.IsVideo(edition))
          {
            // Video
            WebVideoStream webVideoStream = new WebVideoStream();
            webVideoStream.Codec = Convert.ToString(info.Video[edition].Codec);
            webVideoStream.DisplayAspectRatio = Convert.ToDecimal(info.Video[edition].AspectRatio ?? 0);
            webVideoStream.DisplayAspectRatioString = AspectRatioHelper.AspectRatioToString(Convert.ToDecimal(info.Video[edition].AspectRatio ?? 0));
            webVideoStream.Height = Convert.ToInt32(info.Video[edition].Height ?? 0);
            webVideoStream.Width = Convert.ToInt32(info.Video[edition].Width ?? 0);
            webVideoStreams.Add(webVideoStream);
            webVideoStream.ID = info.Video[edition].StreamIndex + editionOffset;
            webVideoStream.Index = 0;
            webVideoStream.Interlaced = interlaced;

            container = info.Metadata[edition].VideoContainerType.ToString();

            // Audio
            for (int i = 0; i < info.Audio[edition].Count; i++)
            {
              WebAudioStream webAudioStream = new WebAudioStream();
              if (info.Audio[edition][i].Channels != null)
                webAudioStream.Channels = info.Audio[edition][i].Channels.Value;

              webAudioStream.Codec = info.Audio[edition][i].Codec.ToString();
              webAudioStream.ID = info.Audio[edition][i].StreamIndex + editionOffset;
              webAudioStream.Index = i;
              if (info.Audio[edition][i].Language != null)
              {
                string language = info.Audio[edition][i].Language == string.Empty ? UNDEFINED : info.Audio[edition][i].Language;
                webAudioStream.Language = language;
                if (language != UNDEFINED)
                {
                  webAudioStream.LanguageFull = new CultureInfo(language).EnglishName;
                  if (item.HasEditions && item.Editions.Any(e => e.Value.SetNo == edition))
                    webAudioStream.Title = item.Editions.First(e => e.Key == edition).Value.Name;
                  else if (item.GetPlayData(out var mime, out var mediaName))
                    webAudioStream.Title = mediaName;
                  else
                    webAudioStream.Title = "?";
                  if (string.IsNullOrEmpty(webAudioStream.Codec) == false)
                    webAudioStream.Title += webAudioStream.Title?.Length > 0 ? $" ({webAudioStream.Codec.ToUpperInvariant()})" : webAudioStream.Codec.ToUpperInvariant();
                }
              }
              webAudioStreams.Add(webAudioStream);
            }

            // Subtitles
            if (info.Subtitles[edition].Any())
            {
              int firstMediaIdx = info.Subtitles[edition].Keys.First();
              for (int i = 0; i < info.Subtitles[edition][firstMediaIdx].Count; i++)
              {
                WebSubtitleStream webSubtitleStream = new WebSubtitleStream();
                webSubtitleStream.Filename = info.Subtitles[edition][firstMediaIdx][i].IsEmbedded ? "Embedded" : info.Subtitles[edition][firstMediaIdx][i].SourcePath;
                webSubtitleStream.ID = info.Subtitles[edition][firstMediaIdx][i].StreamIndex + editionOffset;
                webSubtitleStream.Index = i;
                if (info.Subtitles[edition][firstMediaIdx][i].Language != null)
                {
                  string language = info.Subtitles[edition][firstMediaIdx][i].Language == string.Empty ? UNDEFINED : info.Subtitles[edition][firstMediaIdx][i].Language;
                  webSubtitleStream.Language = language;
                  webSubtitleStream.LanguageFull = language;
                  if (language != UNDEFINED)
                    webSubtitleStream.LanguageFull = new CultureInfo(language).EnglishName;
                }
                webSubtitleStreams.Add(webSubtitleStream);
              }
            }
          }
        }
      }
      else if (item.Aspects.ContainsKey(AudioAspect.ASPECT_ID))
      {
        int edition = info.Metadata.Min(d => d.Key);
        if (info.IsAudio(edition))
        {
          var audioAspect = item.GetAspect(AudioAspect.Metadata);
          duration = (long)audioAspect[AudioAspect.ATTR_DURATION];
          container = info.Metadata[edition].AudioContainerType.ToString();
        }
      }
      else if (item.Aspects.ContainsKey(ImageAspect.ASPECT_ID))
      {
        int edition = info.Metadata.Min(d => d.Key);
        if (info.IsImage(edition))
          container = info.Metadata[edition].ImageContainerType.ToString();
      }

      WebMediaInfo webMediaInfo = new WebMediaInfo
      {
        Duration = duration * 1000,
        Container = container,
        VideoStreams = webVideoStreams,
        AudioStreams = webAudioStreams,
        SubtitleStreams = webSubtitleStreams
      };

      return webMediaInfo;
    }

    internal static IMediaConverter MediaConverter
    {
      get { return ServiceRegistration.Get<IMediaConverter>(); }
    }

    internal static IMediaAnalyzer MediaAnalyzer
    {
      get { return ServiceRegistration.Get<IMediaAnalyzer>(); }
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
