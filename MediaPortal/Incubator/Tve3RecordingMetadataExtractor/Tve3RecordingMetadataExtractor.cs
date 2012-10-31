#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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
using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
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
  /// MediaPortal 2 metadata extractor for TVE3 recordings.
  /// </summary>
  public class Tve3RecordingMetadataExtractor : IMetadataExtractor
  {
    #region Helper classes for simple XML deserialization

    [XmlRoot(ElementName = "SimpleTag")]
    public class SimpleTag
    {
      [XmlElement(ElementName = "name")]
      public string Name { get; set; }
      [XmlElement(ElementName = "value")]
      public string Value { get; set; }
      public override string ToString()
      {
        return string.Format("{0}: {1}", Name, Value);
      }
    }

    [XmlRoot(ElementName = "tags")]
    public class Tags
    {
      [XmlArray(ElementName = "tag")]
      public List<SimpleTag> Tag;
    }

    #endregion

    #region Constants

    /// <summary>
    /// GUID string for the Tve3Recording metadata extractor.
    /// </summary>
    public const string METADATAEXTRACTOR_ID_STR = "C7080745-8EAE-459E-8A9A-25D87DF8565F";

    /// <summary>
    /// Tve3 metadata extractor GUID.
    /// </summary>
    public static Guid METADATAEXTRACTOR_ID = new Guid(METADATAEXTRACTOR_ID_STR);

    public const string MEDIA_CATEGORY_NAME_SERIES = "Series";

    const string TAG_TITLE = "TITLE";
    const string TAG_PLOT = "COMMENT";
    const string TAG_GENRE = "GENRE";
    const string TAG_CHANNEL = "CHANNEL_NAME";
    const string TAG_EPISODENAME = "EPISODENAME";
    const string TAG_SERIESNUM = "SERIESNUM";
    const string TAG_EPISODENUM = "EPISODENUM";
    const string TAG_STARTTIME = "STARTTIME";
    const string TAG_ENDTIME = "ENDTIME";

    #endregion

    #region Protected fields and classes

    protected static IList<MediaCategory> MEDIA_CATEGORIES = new List<MediaCategory>();
    protected MetadataExtractorMetadata _metadata;
    protected static XmlSerializer _xmlSerializer = null; // Lazy initialized

    protected static Regex _yearMatcher = new Regex(@"\d{4}$", RegexOptions.Multiline);

    #endregion

    #region Ctor

    static Tve3RecordingMetadataExtractor()
    {
      MEDIA_CATEGORIES.Add(DefaultMediaCategories.Video);

      MediaCategory seriesCategory;
      IMediaAccessor mediaAccessor = ServiceRegistration.Get<IMediaAccessor>();
      if (!mediaAccessor.MediaCategories.TryGetValue(MEDIA_CATEGORY_NAME_SERIES, out seriesCategory))
        seriesCategory = mediaAccessor.RegisterMediaCategory(MEDIA_CATEGORY_NAME_SERIES, new List<MediaCategory> { DefaultMediaCategories.Video });
      MEDIA_CATEGORIES.Add(seriesCategory);

      // All non-default media item aspects must be registered
      IMediaItemAspectTypeRegistration miatr = ServiceRegistration.Get<IMediaItemAspectTypeRegistration>();
      miatr.RegisterLocallyKnownMediaItemAspectType(RecordingAspect.Metadata);
    }

    public Tve3RecordingMetadataExtractor()
    {
      _metadata = new MetadataExtractorMetadata(METADATAEXTRACTOR_ID, "TVEngine3 recordings metadata extractor", MetadataExtractorPriority.Extended, false,
          MEDIA_CATEGORIES, new[]
              {
                MediaAspect.Metadata,
                VideoAspect.Metadata,
                RecordingAspect.Metadata,
                SeriesAspect.Metadata
              });
    }

    #endregion

    #region IMetadataExtractor implementation

    public MetadataExtractorMetadata Metadata
    {
      get { return _metadata; }
    }

    public bool TryExtractMetadata(IResourceAccessor mediaItemAccessor, IDictionary<Guid, MediaItemAspect> extractedAspectData, bool forceQuickMode)
    {
      try
      {
        IFileSystemResourceAccessor fsra = mediaItemAccessor as IFileSystemResourceAccessor;
        if (fsra == null || !fsra.IsFile)
          return false;

        string title;
        if (!MediaItemAspect.TryGetAttribute(extractedAspectData, MediaAspect.ATTR_TITLE, out title) || string.IsNullOrEmpty(title))
          return false;

        string filePath = mediaItemAccessor.CanonicalLocalResourcePath.ToString();
        string lowerExtension = StringUtils.TrimToEmpty(ProviderPathHelper.GetExtension(filePath)).ToLowerInvariant();
        if (lowerExtension != ".ts")
          return false;
        string metaFilePath = ProviderPathHelper.ChangeExtension(filePath, ".xml");
        IResourceAccessor metaFileAccessor;
        if (!ResourcePath.Deserialize(metaFilePath).TryCreateLocalResourceAccessor(out metaFileAccessor))
          return false;

        Tags tags;
        using (metaFileAccessor)
        {
          using (Stream metaStream = ((IFileSystemResourceAccessor) metaFileAccessor).OpenRead())
            tags = (Tags) GetTagsXmlSerializer().Deserialize(metaStream);
        }

        // Handle series information
        SeriesInfo seriesInfo = GetSeriesFromTags(tags);
        if (seriesInfo.IsCompleteMatch)
        {
          if (!forceQuickMode)
            SeriesTvDbMatcher.Instance.FindAndUpdateSeries(seriesInfo);

          seriesInfo.SetMetadata(extractedAspectData);
        }

        string value;
        if (TryGet(tags, TAG_TITLE, out value) && !string.IsNullOrEmpty(value))
          MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_TITLE, value);

        if (TryGet(tags, TAG_GENRE, out value))
          MediaItemAspect.SetCollectionAttribute(extractedAspectData, VideoAspect.ATTR_GENRES, new List<String> { value });

        if (TryGet(tags, TAG_PLOT, out value))
        {
          MediaItemAspect.SetAttribute(extractedAspectData, VideoAspect.ATTR_STORYPLOT, value);
          Match yearMatch = _yearMatcher.Match(value);
          int guessedYear;
          if (int.TryParse(yearMatch.Value, out guessedYear))
            MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_RECORDINGTIME, new DateTime(guessedYear, 1, 1));
        }

        if (TryGet(tags, TAG_CHANNEL, out value))
          MediaItemAspect.SetAttribute(extractedAspectData, RecordingAspect.ATTR_CHANNEL, value);

        // Recording date formatted: 2011-11-04 20:55
        DateTime recordingStart;
        DateTime recordingEnd;
        if (TryGet(tags, TAG_STARTTIME, out value) && DateTime.TryParse(value, out recordingStart))
          MediaItemAspect.SetAttribute(extractedAspectData, RecordingAspect.ATTR_STARTTIME, recordingStart);

        if (TryGet(tags, TAG_ENDTIME, out value) && DateTime.TryParse(value, out recordingEnd))
          MediaItemAspect.SetAttribute(extractedAspectData, RecordingAspect.ATTR_ENDTIME, recordingEnd);

        return true;
      }
      catch (Exception e)
      {
        // Only log at the info level here - And simply return false. This lets the caller know that we
        // couldn't perform our task here.
        ServiceRegistration.Get<ILogger>().Info("Tve3RecordingMetadataExtractor: Exception reading resource '{0}' (Text: '{1}')", mediaItemAccessor.CanonicalLocalResourcePath, e.Message);
      }
      return false;
    }

    protected XmlSerializer GetTagsXmlSerializer()
    {
      return _xmlSerializer ?? (_xmlSerializer = new XmlSerializer(typeof(Tags)));
    }

    public SeriesInfo GetSeriesFromTags(Tags extractedTags)
    {
      SeriesInfo seriesInfo = new SeriesInfo();
      string tmpString;

      if (TryGet(extractedTags, TAG_TITLE, out tmpString))
        seriesInfo.Series = tmpString;

      if (TryGet(extractedTags, TAG_EPISODENAME, out tmpString))
        seriesInfo.Episode = tmpString;

      if (TryGet(extractedTags, TAG_SERIESNUM, out tmpString))
        int.TryParse(tmpString, out seriesInfo.SeasonNumber);

      if (TryGet(extractedTags, TAG_EPISODENUM, out tmpString))
      {
        int episodeNum;
        if (int.TryParse(tmpString, out episodeNum))
          seriesInfo.EpisodeNumbers.Add(episodeNum);
      }
      return seriesInfo;
    }

    private static bool TryGet(Tags tags, string key, out string value)
    {
      value = null;
      SimpleTag tag = tags.Tag.Find(t => t.Name == key);
      if (tag == null || tag.Value == null)
        return false;

      value = tag.Value.Trim();
      return !string.IsNullOrEmpty(value);
    }

    #endregion
  }
}
