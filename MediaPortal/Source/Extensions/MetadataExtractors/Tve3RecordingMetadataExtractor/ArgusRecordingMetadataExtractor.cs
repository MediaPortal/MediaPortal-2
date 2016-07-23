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
  /// MediaPortal 2 metadata extractor for Argus recordings which does an online lookup for series info.
  /// </summary>
  public class ArgusRecordingSeriesMetadataExtractor : ArgusRecordingMetadataExtractor
  {
    /// <summary>
    /// GUID string for the Argus Recording metadata extractor.
    /// </summary>
    private const string METADATAEXTRACTOR_ID_STR = "4BFEB0CC-B977-4C55-8B94-0634C5454EC6";

    /// <summary>
    /// Argus metadata extractor GUID.
    /// </summary>
    public new static Guid METADATAEXTRACTOR_ID = new Guid(METADATAEXTRACTOR_ID_STR);

    protected static IList<MediaCategory> SERIES_MEDIA_CATEGORIES = new List<MediaCategory>();

    static ArgusRecordingSeriesMetadataExtractor()
    {
      MediaCategory seriesCategory;
      IMediaAccessor mediaAccessor = ServiceRegistration.Get<IMediaAccessor>();
      if (!mediaAccessor.MediaCategories.TryGetValue(MEDIA_CATEGORY_NAME_SERIES, out seriesCategory))
        seriesCategory = mediaAccessor.RegisterMediaCategory(MEDIA_CATEGORY_NAME_SERIES, new List<MediaCategory> { DefaultMediaCategories.Video });
      SERIES_MEDIA_CATEGORIES.Add(seriesCategory);
    }

    public ArgusRecordingSeriesMetadataExtractor()
    {
      _metadata = new MetadataExtractorMetadata(METADATAEXTRACTOR_ID, "Argus recordings series metadata extractor", MetadataExtractorPriority.Extended, false,
        SERIES_MEDIA_CATEGORIES, new[] { SeriesAspect.Metadata });
    }

    public override bool TryExtractMetadata(IResourceAccessor mediaItemAccessor, IDictionary<Guid, MediaItemAspect> extractedAspectData, bool forceQuickMode)
    {
      try
      {
        IResourceAccessor metaFileAccessor;
        if (!CanExtract(mediaItemAccessor, extractedAspectData, out metaFileAccessor)) return false;

        Argus.Recording recording;
        using (metaFileAccessor)
        {
          using (Stream metaStream = ((IFileSystemResourceAccessor)metaFileAccessor).OpenRead())
            recording = (Argus.Recording)GetTagsXmlSerializer().Deserialize(metaStream);
        }

        // Handle series information
        SeriesInfo seriesInfo = GetSeriesFromTags(recording);
        if (seriesInfo.IsCompleteMatch)
        {
          if (!forceQuickMode)
            SeriesTvDbMatcher.Instance.FindAndUpdateSeries(seriesInfo);

          seriesInfo.SetMetadata(extractedAspectData);
        }
        return true;
      }
      catch (Exception e)
      {
        // Only log at the info level here - And simply return false. This lets the caller know that we
        // couldn't perform our task here.
        ServiceRegistration.Get<ILogger>().Info("ArgusRecordingSeriesMetadataExtractor: Exception reading resource '{0}' (Text: '{1}')", mediaItemAccessor.CanonicalLocalResourcePath, e.Message);
      }
      return false;
    }
  }

  /// <summary>
  /// MediaPortal 2 metadata extractor for Argus recordings.
  /// </summary>
  public class ArgusRecordingMetadataExtractor : IMetadataExtractor
  {

    #region Constants

    /// <summary>
    /// GUID string for the Argus Recording metadata extractor.
    /// </summary>
    private const string METADATAEXTRACTOR_ID_STR = "BA7E2983-4836-4F37-A613-19EE46BED7B7";

    /// <summary>
    /// Argus metadata extractor GUID.
    /// </summary>
    public static Guid METADATAEXTRACTOR_ID = new Guid(METADATAEXTRACTOR_ID_STR);

    public const string MEDIA_CATEGORY_NAME_SERIES = "Series";

    #endregion

    #region Protected fields and classes

    protected static IList<MediaCategory> MEDIA_CATEGORIES = new List<MediaCategory>();
    protected MetadataExtractorMetadata _metadata;
    protected static XmlSerializer _xmlSerializer = null; // Lazy initialized

    protected static Regex _yearMatcher = new Regex(@"\d{4}$", RegexOptions.Multiline);

    #endregion

    #region Ctor

    static ArgusRecordingMetadataExtractor()
    {
      MEDIA_CATEGORIES.Add(DefaultMediaCategories.Video);

      // All non-default media item aspects must be registered
      IMediaItemAspectTypeRegistration miatr = ServiceRegistration.Get<IMediaItemAspectTypeRegistration>();
      miatr.RegisterLocallyKnownMediaItemAspectType(RecordingAspect.Metadata);
    }

    public ArgusRecordingMetadataExtractor()
    {
      _metadata = new MetadataExtractorMetadata(METADATAEXTRACTOR_ID, "Argus recordings metadata extractor", MetadataExtractorPriority.Extended, false,
          MEDIA_CATEGORIES, new[]
              {
                MediaAspect.Metadata,
                VideoAspect.Metadata,
                RecordingAspect.Metadata,
              });
    }

    #endregion

    protected XmlSerializer GetTagsXmlSerializer()
    {
      return _xmlSerializer ?? (_xmlSerializer = new XmlSerializer(typeof(Argus.Recording)));
    }

    public SeriesInfo GetSeriesFromTags(Argus.Recording recording)
    {
      SeriesInfo seriesInfo = new SeriesInfo { Series = recording.Title };

      if (recording.SeriesNumber.HasValue)
        seriesInfo.SeasonNumber = recording.SeriesNumber.Value;

      if (recording.EpisodeNumber.HasValue)
        seriesInfo.EpisodeNumbers.Add(recording.EpisodeNumber.Value);

      if (!seriesInfo.IsCompleteMatch)
      {
        // Check for formatted display value, i.e.:
        // <EpisodeNumberDisplay>1.4</EpisodeNumberDisplay>
        if (!string.IsNullOrWhiteSpace(recording.EpisodeNumberDisplay))
        {
          var parts = recording.EpisodeNumberDisplay.Split('.');
          if (parts.Length == 2)
          {
            int val;
            if (int.TryParse(parts[0], out val))
              seriesInfo.SeasonNumber = val;
            if (int.TryParse(parts[1], out val))
              seriesInfo.EpisodeNumbers.Add(val);
          }
        }
      }
      return seriesInfo;
    }

    #region IMetadataExtractor implementation

    public MetadataExtractorMetadata Metadata
    {
      get { return _metadata; }
    }

    public virtual bool TryExtractMetadata(IResourceAccessor mediaItemAccessor, IDictionary<Guid, MediaItemAspect> extractedAspectData, bool forceQuickMode)
    {
      try
      {
        IResourceAccessor metaFileAccessor;
        if (!CanExtract(mediaItemAccessor, extractedAspectData, out metaFileAccessor)) return false;

        Argus.Recording recording;
        using (metaFileAccessor)
        {
          using (Stream metaStream = ((IFileSystemResourceAccessor)metaFileAccessor).OpenRead())
            recording = (Argus.Recording)GetTagsXmlSerializer().Deserialize(metaStream);
        }

        MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_TITLE, recording.Title);

        MediaItemAspect.SetCollectionAttribute(extractedAspectData, VideoAspect.ATTR_GENRES, new[] { recording.Category });

        MediaItemAspect.SetAttribute(extractedAspectData, VideoAspect.ATTR_STORYPLOT, recording.Description);

        Match yearMatch = _yearMatcher.Match(recording.Description);
        int guessedYear;
        if (int.TryParse(yearMatch.Value, out guessedYear))
          MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_RECORDINGTIME, new DateTime(guessedYear, 1, 1));

        MediaItemAspect.SetAttribute(extractedAspectData, RecordingAspect.ATTR_CHANNEL, recording.ChannelDisplayName);

        MediaItemAspect.SetAttribute(extractedAspectData, RecordingAspect.ATTR_STARTTIME, recording.ProgramStartTime);

        MediaItemAspect.SetAttribute(extractedAspectData, RecordingAspect.ATTR_ENDTIME, recording.ProgramStopTime);

        if (!string.IsNullOrWhiteSpace(recording.Director))
          MediaItemAspect.SetCollectionAttribute(extractedAspectData, VideoAspect.ATTR_DIRECTORS, recording.Director.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));

        if (!string.IsNullOrWhiteSpace(recording.Actors))
          MediaItemAspect.SetCollectionAttribute(extractedAspectData, VideoAspect.ATTR_ACTORS, recording.Actors.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));

        return true;
      }
      catch (Exception e)
      {
        // Only log at the info level here - And simply return false. This lets the caller know that we
        // couldn't perform our task here.
        ServiceRegistration.Get<ILogger>().Info("ArgusRecordingMetadataExtractor: Exception reading resource '{0}' (Text: '{1}')", mediaItemAccessor.CanonicalLocalResourcePath, e.Message);
      }
      return false;
    }

    protected static bool CanExtract(IResourceAccessor mediaItemAccessor, IDictionary<Guid, MediaItemAspect> extractedAspectData, out IResourceAccessor metaFileAccessor)
    {
      metaFileAccessor = null;
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
      string metaFilePath = ProviderPathHelper.ChangeExtension(filePath, ".arg");
      if (!ResourcePath.Deserialize(metaFilePath).TryCreateLocalResourceAccessor(out metaFileAccessor))
        return false;
      return true;
    }

    #endregion
  }
}
