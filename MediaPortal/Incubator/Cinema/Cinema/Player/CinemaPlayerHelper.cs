#region Copyright (C) 2007-2023 Team MediaPortal

/*
    Copyright (C) 2007-2023 Team MediaPortal
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

using Cinema.Settings;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.Services.ResourceAccess.RawUrlResourceProvider;
using MediaPortal.Common.SystemResolver;
using MediaPortal.UiComponents.Media.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace Cinema.Player
{
  internal class CinemaPlayerHelper
  {
    public const string CINEMA_MIMETYPE = "cinema/stream";

    public static void PlayStream(Trailer trailer)
    {
      var mediaItem = CreateStreamMediaItem(trailer);
      PlayItemsModel.PlayItem(mediaItem);
    }

    /// <summary>
    /// Constructs a dynamic <see cref="MediaItem"/> that contains the URL for the given <paramref name="trailer"/>.
    /// </summary>
    /// <param name="trailer">Trailer.</param>
    private static MediaItem CreateStreamMediaItem(Trailer trailer)
    {
      ServiceRegistration.Get<ILogger>().Debug("Cinema: Play Trailer '{0}' - Url:{1}", trailer.Title, trailer.Url);
      IDictionary<Guid, IList<MediaItemAspect>> aspects = new Dictionary<Guid, IList<MediaItemAspect>>();

      MultipleMediaItemAspect providerResourceAspect = MediaItemAspect.CreateAspect(aspects, ProviderResourceAspect.Metadata);
      SingleMediaItemAspect mediaAspect = MediaItemAspect.GetOrCreateAspect(aspects, MediaAspect.Metadata);
      SingleMediaItemAspect audioAspect = MediaItemAspect.GetOrCreateAspect(aspects, VideoAspect.Metadata);
      var trailerUrl = TryGetDirectVideoUrl(trailer.Url).Result;
      providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_TYPE, ProviderResourceAspect.TYPE_PRIMARY);
      providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH, RawUrlResourceProvider.ToProviderResourcePath(trailerUrl.videoUrl).Serialize());
      providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_SYSTEM_ID, ServiceRegistration.Get<ISystemResolver>().LocalSystemId);
      providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_MIME_TYPE, CINEMA_MIMETYPE);

      mediaAspect.SetAttribute(MediaAspect.ATTR_TITLE, trailer.Title);

      var mediaItem = new CinemaMediaItem(Guid.Empty, aspects, trailerUrl.audioUrl);
      return mediaItem;
    }

    /// <summary>
    /// Attempts to get the direct url for the highest quality youtube video. If the url points to a muxed stream of video and audio
    /// then only videoUrl in the returned tuple will be populated and audioUrl will be <c>null</c>; else if the video and audio
    /// streams are separate then both videoUrl and audioUrl will be populated with the urls to the video and audio streams respectively.
    /// </summary>
    /// <param name="trailerUrl">The url to a youtube video.</param>
    /// <returns>A tuple containing links to either the muxed video stream or separate streams for video and audio.</returns>
    private static async Task<(string videoUrl, string audioUrl)> TryGetDirectVideoUrl(string trailerUrl)
    {
      var youtube = new YoutubeClient();

      var streamManifest = await youtube.Videos.Streams.GetManifestAsync(trailerUrl);

      // Try and get the highest quality video stream
      var videoStream = streamManifest.GetVideoStreams().TryGetWithHighestVideoQuality();
      if (videoStream == null)
        return (null, null);

      // If the stream is muxed and therefore contains audio, simply return it
      if (videoStream is MuxedStreamInfo)
        return (videoStream.Url, null);

      // else the video stream does not contain audio so try and get the highest quality audio stream.
      // WebM audio streams don't seem to work so limit the restults to Mp4 streams
      var audioStream = streamManifest.GetAudioOnlyStreams().Where(s => s.Container == Container.Mp4).TryGetWithHighestBitrate();
      return (videoStream.Url, audioStream.Url);
    }

    private static int Runtime(double bitrate, double size)
    {
      double kbs = bitrate / 8;
      return (int)(size / kbs);
    }
  }

  public class CinemaMediaItem : MediaItem
  {
    public CinemaMediaItem(Guid mediaItemId, IDictionary<Guid, IList<MediaItemAspect>> aspects, string audioUrl = null)
      : base(mediaItemId, aspects)
    {
      AudioUrl = audioUrl;
    }

    public string AudioUrl { get; }
  }
}
