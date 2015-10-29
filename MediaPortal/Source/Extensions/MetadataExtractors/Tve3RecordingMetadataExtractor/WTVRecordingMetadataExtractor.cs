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
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using MCEBuddy.MetaData;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Extensions.MetadataExtractors.Aspects;
using MediaPortal.Utilities;
using MediaPortal.Extensions.OnlineLibraries;

namespace MediaPortal.Extensions.MetadataExtractors
{
  /// <summary>
  /// MediaPortal 2 metadata extractor for MCE WTV recordings which does an online lookup for series info.
  /// </summary>
  public class WTVRecordingSeriesMetadataExtractor : WTVRecordingMetadataExtractor
  {
    /// <summary>
    /// GUID string for the WTV Recording metadata extractor.
    /// </summary>
    private const string METADATAEXTRACTOR_ID_STR = "2E1493A6-4898-429C-AF56-D6D7EA2AFEF3";

    /// <summary>
    /// Tve3 metadata extractor GUID.
    /// </summary>
    public new static Guid METADATAEXTRACTOR_ID = new Guid(METADATAEXTRACTOR_ID_STR);

    protected static IList<MediaCategory> SERIES_MEDIA_CATEGORIES = new List<MediaCategory>();

    static WTVRecordingSeriesMetadataExtractor()
    {
      MediaCategory seriesCategory;
      IMediaAccessor mediaAccessor = ServiceRegistration.Get<IMediaAccessor>();
      if (!mediaAccessor.MediaCategories.TryGetValue(MEDIA_CATEGORY_NAME_SERIES, out seriesCategory))
        seriesCategory = mediaAccessor.RegisterMediaCategory(MEDIA_CATEGORY_NAME_SERIES, new List<MediaCategory> { DefaultMediaCategories.Video });
      SERIES_MEDIA_CATEGORIES.Add(seriesCategory);
    }

    public WTVRecordingSeriesMetadataExtractor()
    {
      _metadata = new MetadataExtractorMetadata(METADATAEXTRACTOR_ID, "WTV series recordings metadata extractor", MetadataExtractorPriority.Extended, false,
          SERIES_MEDIA_CATEGORIES, new[] { SeriesAspect.Metadata });
    }

    public SeriesInfo GetSeriesFromTags(IDictionary metadata)
    {
      SeriesInfo seriesInfo = new SeriesInfo();
      string tmpString;

      if (TryGet(metadata, TAG_TITLE, out tmpString))
        seriesInfo.Series = tmpString;

      if (TryGet(metadata, TAG_EPISODENAME, out tmpString))
        seriesInfo.Episode = tmpString;

      return seriesInfo;
    }

    protected override bool ExtractMetadata(ILocalFsResourceAccessor lfsra, IDictionary<Guid, MediaItemAspect> extractedAspectData, bool forceQuickMode)
    {
      if (!CanExtract(lfsra, extractedAspectData) || forceQuickMode)
        return false;

      using (var rec = new MCRecMetadataEditor(lfsra.LocalFileSystemPath))
      {
        // Handle series information
        IDictionary tags = rec.GetAttributes();
        SeriesInfo seriesInfo = GetSeriesFromTags(tags);

        if (SeriesTvDbMatcher.Instance.FindAndUpdateSeries(seriesInfo))
          seriesInfo.SetMetadata(extractedAspectData);
      }
      return true;
    }
  }

  /// <summary>
  /// MediaPortal 2 metadata extractor for MCE WTV recordings.
  /// </summary>
  public class WTVRecordingMetadataExtractor : IMetadataExtractor
  {
    #region Constants

    /// <summary>
    /// GUID string for the Tve3Recording metadata extractor.
    /// </summary>
    private const string METADATAEXTRACTOR_ID_STR = "8FB55236-C567-4233-ABFF-754F5A0BBD1C";

    /// <summary>
    /// Tve3 metadata extractor GUID.
    /// </summary>
    public static Guid METADATAEXTRACTOR_ID = new Guid(METADATAEXTRACTOR_ID_STR);

    public const string MEDIA_CATEGORY_NAME_SERIES = "Series";

    protected const string TAG_TITLE = "Title";
    protected const string TAG_PLOT = "WM/SubTitleDescription";
    protected const string TAG_ORIGINAL_TIME = "WM/MediaOriginalBroadcastDateTime";
    protected const string TAG_GENRE = "WM/Genre";
    protected const string TAG_CHANNEL = "WM/MediaStationName";
    protected const string TAG_EPISODENAME = "WM/SubTitle";
    protected const string TAG_STARTTIME = "WM/WMRVEncodeTime";
    protected const string TAG_ENDTIME = "WM/WMRVEndTime";

    #endregion

    #region Protected fields and classes

    protected static IList<MediaCategory> MEDIA_CATEGORIES = new List<MediaCategory>();
    protected MetadataExtractorMetadata _metadata;

    protected static Regex _yearMatcher = new Regex(@"\d{4}$", RegexOptions.Multiline);

    #endregion

    #region Ctor

    static WTVRecordingMetadataExtractor()
    {
      MEDIA_CATEGORIES.Add(DefaultMediaCategories.Video);

      // All non-default media item aspects must be registered
      IMediaItemAspectTypeRegistration miatr = ServiceRegistration.Get<IMediaItemAspectTypeRegistration>();
      miatr.RegisterLocallyKnownMediaItemAspectType(RecordingAspect.Metadata);
    }

    public WTVRecordingMetadataExtractor()
    {
      _metadata = new MetadataExtractorMetadata(METADATAEXTRACTOR_ID, "WTV recordings metadata extractor", MetadataExtractorPriority.Extended, false,
          MEDIA_CATEGORIES, new[]
              {
                MediaAspect.Metadata,
                VideoAspect.Metadata,
                RecordingAspect.Metadata,
              });
    }

    #endregion

    public static DateTime FromMCEFileTime(long encodeTime)
    {
      return DateTime.FromFileTime(encodeTime).AddYears(-1601);
    }

    public static bool TryGet<TE>(IDictionary dict, string key, out TE value)
    {
      value = default(TE);
      if (!dict.Contains(key))
        return false;

      var entryValue = dict[key] is MetadataItem ? (MetadataItem)dict[key] : null;
      if (entryValue != null && entryValue.Value is TE)
      {
        value = (TE)entryValue.Value;
        return true;
      }
      return false;
    }

    #region IMetadataExtractor implementation

    public MetadataExtractorMetadata Metadata
    {
      get { return _metadata; }
    }

    public bool TryExtractMetadata(IResourceAccessor mediaItemAccessor, IDictionary<Guid, MediaItemAspect> extractedAspectData, bool forceQuickMode)
    {
      try
      {
        if (!(mediaItemAccessor is IFileSystemResourceAccessor))
          return false;
        using (LocalFsResourceAccessorHelper rah = new LocalFsResourceAccessorHelper(mediaItemAccessor))
          return ExtractMetadata(rah.LocalFsResourceAccessor, extractedAspectData, forceQuickMode);

      }
      catch (Exception e)
      {
        // Only log at the info level here - And simply return false. This lets the caller know that we
        // couldn't perform our task here.
        ServiceRegistration.Get<ILogger>().Info("WTVRecordingMetadataExtractor: Exception reading resource '{0}' (Text: '{1}')", mediaItemAccessor.CanonicalLocalResourcePath, e.Message);
      }
      return false;
    }

    protected virtual bool ExtractMetadata(ILocalFsResourceAccessor lfsra, IDictionary<Guid, MediaItemAspect> extractedAspectData, bool forceQuickMode)
    {
      if (!CanExtract(lfsra, extractedAspectData))
        return false;

      using (var rec = new MCRecMetadataEditor(lfsra.LocalFileSystemPath))
      {
        // Handle series information
        IDictionary tags = rec.GetAttributes();

        // Force MimeType
        MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_MIME_TYPE, "slimtv/wtv");

        string value;
        if (TryGet(tags, TAG_TITLE, out value) && !string.IsNullOrEmpty(value))
          MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_TITLE, value);

        if (TryGet(tags, TAG_GENRE, out value))
          MediaItemAspect.SetCollectionAttribute(extractedAspectData, VideoAspect.ATTR_GENRES, new List<String>(value.Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries)));

        if (TryGet(tags, TAG_PLOT, out value))
        {
          MediaItemAspect.SetAttribute(extractedAspectData, VideoAspect.ATTR_STORYPLOT, value);
        }

        if (TryGet(tags, TAG_ORIGINAL_TIME, out value))
        {
          DateTime origTime;
          if (DateTime.TryParse(value, out origTime))
            MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_RECORDINGTIME, origTime);
        }

        if (TryGet(tags, TAG_CHANNEL, out value))
          MediaItemAspect.SetAttribute(extractedAspectData, RecordingAspect.ATTR_CHANNEL, value);

        long lValue;
        if (TryGet(tags, TAG_STARTTIME, out lValue))
          MediaItemAspect.SetAttribute(extractedAspectData, RecordingAspect.ATTR_STARTTIME, FromMCEFileTime(lValue));
        if (TryGet(tags, TAG_ENDTIME, out lValue))
          MediaItemAspect.SetAttribute(extractedAspectData, RecordingAspect.ATTR_ENDTIME, FromMCEFileTime(lValue));
      }
      return true;
    }

    protected static bool CanExtract(ILocalFsResourceAccessor lfsra, IDictionary<Guid, MediaItemAspect> extractedAspectData)
    {
      if (lfsra == null || !lfsra.IsFile)
        return false;

      string title;
      if (!MediaItemAspect.TryGetAttribute(extractedAspectData, MediaAspect.ATTR_TITLE, out title) || string.IsNullOrEmpty(title))
        return false;

      string filePath = lfsra.CanonicalLocalResourcePath.ToString();
      string lowerExtension = StringUtils.TrimToEmpty(ProviderPathHelper.GetExtension(filePath)).ToLowerInvariant();
      if (lowerExtension != ".wtv" && lowerExtension != ".dvr-ms")
        return false;
      return true;
    }

    #endregion
  }
}
