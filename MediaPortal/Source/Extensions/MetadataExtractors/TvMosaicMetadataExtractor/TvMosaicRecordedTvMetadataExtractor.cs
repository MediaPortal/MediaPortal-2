#region Copyright (C) 2007-2021 Team MediaPortal

/*
    Copyright (C) 2007-2021 Team MediaPortal
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

using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Plugins.SlimTv.Interfaces.Aspects;
using MediaPortal.Plugins.SlimTv.Interfaces.LiveTvMediaItem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using TvMosaic.API;
using TvMosaic.Shared;
using TvMosaicMetadataExtractor.ResourceAccess;

namespace TvMosaicMetadataExtractor
{
  #region Internal classes

  public class CompressionWebClient : WebClient
  {
    protected override WebRequest GetWebRequest(Uri address)
    {
      Headers["Accept-Encoding"] = "gzip,deflate";
      HttpWebRequest request = base.GetWebRequest(address) as HttpWebRequest;
      if (request != null)
        request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
      return request;
    }
  }

  #endregion

  /// <summary>
  /// Implementation of <see cref="IMetadataExtractor"/> that extracts metadata from TvMosaic recorded tv described by a <see cref="TvMosaicResourceAccessor"/>.
  /// </summary>
  public class TvMosaicRecordedTvMetadataExtractor : IMetadataExtractor
  {
    public const string METADATAEXTRACTOR_ID_STR = "ACE47425-27EE-417A-9E26-989C5C17AD95";
    /// <summary>
    /// TvMosaic metadata extractor GUID.
    /// </summary>
    public static Guid METADATAEXTRACTOR_ID = new Guid(METADATAEXTRACTOR_ID_STR);

    protected static IList<MediaCategory> MEDIA_CATEGORIES = new List<MediaCategory>();
    protected MetadataExtractorMetadata _metadata;

    static TvMosaicRecordedTvMetadataExtractor()
    {
      MEDIA_CATEGORIES.Add(DefaultMediaCategories.Audio);
      MEDIA_CATEGORIES.Add(DefaultMediaCategories.Video);
    }

    public TvMosaicRecordedTvMetadataExtractor()
    {
      _metadata = new MetadataExtractorMetadata(METADATAEXTRACTOR_ID, "TvMosaic recordings metadata extractor", MetadataExtractorPriority.Extended, false,
          MEDIA_CATEGORIES, new MediaItemAspectMetadata[]
              {
                MediaAspect.Metadata,
                VideoAspect.Metadata,
                RecordingAspect.Metadata,
              });
    }

    public MetadataExtractorMetadata Metadata
    {
      get { return _metadata; }
    }

    protected async Task ExtractRecordedTvMetadata(TvMosaicResourceAccessor accessor, IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData, RecordedTV recordedTV)
    {
      //ToDo: Fill more attributes
      MultipleMediaItemAspect providerResourceAspect = MediaItemAspect.CreateAspect(extractedAspectData, ProviderResourceAspect.Metadata);
      providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_INDEX, 0);
      providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_TYPE, ProviderResourceAspect.TYPE_PRIMARY);
      providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_MIME_TYPE, LiveTvMediaItem.MIME_TYPE_RECORDED_TV_STREAM);
      providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH, accessor.CanonicalLocalResourcePath.Serialize());

      MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_TITLE, recordedTV.VideoInfo.Name);
      MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_SORT_TITLE, recordedTV.VideoInfo.Name);

      MediaItemAspect.SetAttribute(extractedAspectData, VideoAspect.ATTR_STORYPLOT, recordedTV.VideoInfo.ShortDesc);
      MultipleMediaItemAspect videoStreamAspect = MediaItemAspect.CreateAspect(extractedAspectData, VideoStreamAspect.Metadata);
      videoStreamAspect.SetAttribute(VideoStreamAspect.ATTR_RESOURCE_INDEX, 0);
      videoStreamAspect.SetAttribute(VideoStreamAspect.ATTR_STREAM_INDEX, 0);
      videoStreamAspect.SetAttribute(VideoStreamAspect.ATTR_DURATION, recordedTV.VideoInfo.Duration);

      DateTime startTime = recordedTV.VideoInfo.StartTime.FromUnixTime();
      MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_RECORDINGTIME, startTime);
      MediaItemAspect.SetAttribute(extractedAspectData, RecordingAspect.ATTR_STARTTIME, startTime);
      MediaItemAspect.SetAttribute(extractedAspectData, RecordingAspect.ATTR_ENDTIME, startTime.AddSeconds(recordedTV.VideoInfo.Duration));
      MediaItemAspect.SetAttribute(extractedAspectData, RecordingAspect.ATTR_CHANNEL, recordedTV.ChannelName);

      if (!string.IsNullOrEmpty(recordedTV.Thumbnail))
      {
        byte[] thumbnailData = await DownloadThumbnail(recordedTV.Thumbnail);
        if (thumbnailData != null && thumbnailData.Length > 0)
          MediaItemAspect.SetAttribute(extractedAspectData, ThumbnailLargeAspect.ATTR_THUMBNAIL, thumbnailData);
      }
    }

    protected async Task<byte[]> DownloadThumbnail(string thumbnailUrl)
    {
      byte[] thumbnailData;
      try
      {
        using (CompressionWebClient client = new CompressionWebClient())
          thumbnailData = await client.DownloadDataTaskAsync(thumbnailUrl);
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("TvMosaicRecordedTvMetadataExtractor: Error retrieving thumbnail from url '{0}'", ex, thumbnailUrl);
        thumbnailData = null;
      }

      return thumbnailData;
    }

    public async Task<bool> TryExtractMetadataAsync(IResourceAccessor mediaItemAccessor, IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData, bool forceQuickMode)
    {
      TvMosaicResourceAccessor ra = mediaItemAccessor as TvMosaicResourceAccessor;
      if (ra == null || !ra.IsFile)
        return false;

      string objectId = ra.TvMosaicObjectId;
      if (string.IsNullOrEmpty(objectId))
        return false;

      TvMosaicNavigator navigator = new TvMosaicNavigator();
      RecordedTV recordedTV = (await navigator.GetObjectResponseAsync(objectId, false))?.Items?.FirstOrDefault();
      if (recordedTV == null)
        return false;

      await ExtractRecordedTvMetadata(ra, extractedAspectData, recordedTV);
      
      return true;
    }

    public bool IsDirectorySingleResource(IResourceAccessor mediaItemAccessor)
    {
      return false;
    }

    public bool IsStubResource(IResourceAccessor mediaItemAccessor)
    {
      return false;
    }

    public bool TryExtractStubItems(IResourceAccessor mediaItemAccessor, ICollection<IDictionary<Guid, IList<MediaItemAspect>>> extractedStubAspectData)
    {
      return false;
    }

    public Task<IList<MediaItemSearchResult>> SearchForMatchesAsync(IDictionary<Guid, IList<MediaItemAspect>> searchAspectData, ICollection<string> searchCategories)
    {
      return Task.FromResult<IList<MediaItemSearchResult>>(null);
    }

    public Task<bool> AddMatchedAspectDetailsAsync(IDictionary<Guid, IList<MediaItemAspect>> matchedAspectData)
    {
      return Task.FromResult(false);
    }

    public Task<bool> DownloadMetadataAsync(Guid mediaItemId, IDictionary<Guid, IList<MediaItemAspect>> aspectData)
    {
      return Task.FromResult(false);
    }
  }
}
