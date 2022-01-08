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

using MediaInfoLib;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.GenreConverter;
using MediaPortal.Extensions.OnlineLibraries;
using MediaPortal.Plugins.SlimTv.Interfaces.Aspects;
using MediaPortal.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Serialization;
using MediaPortal.Common.Settings;
using SlimTv.TvMosaicProvider.Settings;
using TvMosaic.API;

namespace MediaPortal.Extensions.MetadataExtractors
{
  /// <summary>
  /// MediaPortal 2 metadata extractor for TvMosaic recordings.
  /// </summary>
  public class TvMosaicRecordingMetadataExtractor : IMetadataExtractor
  {
    #region Constants

    /// <summary>
    /// GUID string for the Tve3Recording metadata extractor.
    /// </summary>
    public const string METADATAEXTRACTOR_ID_STR = "C40A622C-7DB8-4EFD-860A-4B19AC7A9D43";

    /// <summary>
    /// Tve3 metadata extractor GUID.
    /// </summary>
    public static Guid METADATAEXTRACTOR_ID = new Guid(METADATAEXTRACTOR_ID_STR);

    #endregion

    #region Protected fields and classes

    protected static IList<MediaCategory> MEDIA_CATEGORIES = new List<MediaCategory>();
    protected MetadataExtractorMetadata _metadata;

    protected static Regex _yearMatcher = new Regex(@"\d{4}$", RegexOptions.Multiline);
    protected readonly string _host;
    protected readonly HttpDataProvider _dvbLink;
    protected readonly RecordingSettings _recordingSettings;

    #endregion

    #region Ctor

    static TvMosaicRecordingMetadataExtractor()
    {
      MEDIA_CATEGORIES.Add(DefaultMediaCategories.Audio);
      MEDIA_CATEGORIES.Add(DefaultMediaCategories.Video);

      // All non-default media item aspects must be registered
      IMediaItemAspectTypeRegistration miatr = ServiceRegistration.Get<IMediaItemAspectTypeRegistration>();
      miatr.RegisterLocallyKnownMediaItemAspectTypeAsync(RecordingAspect.Metadata);
    }

    public TvMosaicRecordingMetadataExtractor()
    {
      _metadata = new MetadataExtractorMetadata(METADATAEXTRACTOR_ID, "TvMosaic recordings metadata extractor", MetadataExtractorPriority.Extended, false,
          MEDIA_CATEGORIES, new MediaItemAspectMetadata[]
              {
                MediaAspect.Metadata,
                VideoAspect.Metadata,
                RecordingAspect.Metadata,
              });

      var settings = ServiceRegistration.Get<ISettingsManager>().Load<TvMosaicProviderSettings>();
      _host = settings.Host;
      _dvbLink = new HttpDataProvider(_host, 9270, settings.Username ?? string.Empty, settings.Password ?? string.Empty);
      _recordingSettings = _dvbLink.GetRecordingSettings(new RecordingSettingsRequest()).Result.Result;
    }

    #endregion

    #region IMetadataExtractor implementation

    public MetadataExtractorMetadata Metadata
    {
      get { return _metadata; }
    }

    public virtual Task<bool> TryExtractMetadataAsync(IResourceAccessor mediaItemAccessor, IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData, bool forceQuickMode)
    {
      try
      {
        IResourceAccessor metaFileAccessor;
        if (!CanExtract(mediaItemAccessor, extractedAspectData, out metaFileAccessor))
          return Task.FromResult(false);

        //Assign all tags to the aspects for both tv and radio recordings
        string value;
        MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_ISVIRTUAL, false);

        string filePath = mediaItemAccessor.CanonicalLocalResourcePath.ToString();

        value = Path.GetFileNameWithoutExtension(filePath);
        if (value.Equals("manual", StringComparison.InvariantCultureIgnoreCase))
          value = ResourcePathHelper.GetFileNameWithoutExtension(metaFileAccessor.Path);

        MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_TITLE, value);
        MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_SORT_TITLE, BaseInfo.GetSortTitle(value));

        //if (TryGet(tags, TAG_CHANNEL, out value))
        // TODO: read recording info from API and fill properties here
        MediaItemAspect.SetAttribute(extractedAspectData, RecordingAspect.ATTR_CHANNEL, "TvMosaic");

        // Recording date formatted: 2011-11-04 20:55
        //DateTime tmpValue;
        //DateTime? recordingStart = null;
        //DateTime? recordingEnd = null;
        //DateTime? programStart = null;
        //DateTime? programEnd = null;

        //// First try to read program start and end times, they will be preferred.
        //if (TryGet(tags, TAG_PROGRAMSTARTTIME, out value) && DateTime.TryParse(value, out tmpValue))
        //  programStart = tmpValue;

        //if (TryGet(tags, TAG_PROGRAMENDTIME, out value) && DateTime.TryParse(value, out tmpValue))
        //  programEnd = tmpValue;

        //if (TryGet(tags, TAG_STARTTIME, out value) && DateTime.TryParse(value, out tmpValue))
        //  recordingStart = tmpValue;

        //if (TryGet(tags, TAG_ENDTIME, out value) && DateTime.TryParse(value, out tmpValue))
        //  recordingEnd = tmpValue;

        //// Correct start time if recording started before the program (skip pre-recording offset)
        //if (programStart.HasValue && recordingStart.HasValue && programStart > recordingStart)
        //  recordingStart = programStart;

        //// Correct end time if recording ended after the program (skip the post-recording offset)
        //if (programEnd.HasValue && recordingEnd.HasValue && programEnd < recordingEnd)
        //  recordingEnd = programEnd;

        //if (recordingStart.HasValue)
        //{
        //  MediaItemAspect.SetAttribute(extractedAspectData, RecordingAspect.ATTR_STARTTIME, recordingStart.Value);
        //}
        //if (recordingEnd.HasValue)
        //{
        //  MediaItemAspect.SetAttribute(extractedAspectData, RecordingAspect.ATTR_ENDTIME, recordingEnd.Value);
        //  RecordingUtils.CheckAndPrepareAspectRefresh(extractedAspectData);
        //}

        //if (extractedAspectData.ContainsKey(VideoAspect.ASPECT_ID)) //Only add video information for actual video recordings
        //{
        //  // Force MimeType
        //  IList<MultipleMediaItemAspect> providerAspects;
        //  MediaItemAspect.TryGetAspects(extractedAspectData, ProviderResourceAspect.Metadata, out providerAspects);
        //  foreach (MultipleMediaItemAspect aspect in providerAspects)
        //  {
        //    aspect.SetAttribute(ProviderResourceAspect.ATTR_MIME_TYPE, "slimtv/video");
        //  }

        //  MediaItemAspect.SetAttribute(extractedAspectData, VideoAspect.ATTR_ISDVD, false);
        //  if (TryGet(tags, TAG_PLOT, out value))
        //  {
        //    MediaItemAspect.SetAttribute(extractedAspectData, VideoAspect.ATTR_STORYPLOT, value);
        //    Match yearMatch = _yearMatcher.Match(value);
        //    int guessedYear;
        //    if (int.TryParse(yearMatch.Value, out guessedYear))
        //      MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_RECORDINGTIME, new DateTime(guessedYear, 1, 1));
        //  }
        //  if (TryGet(tags, TAG_GENRE, out value) && !string.IsNullOrEmpty(value?.Trim()))
        //  {
        //    List<GenreInfo> genreList = new List<GenreInfo>(new GenreInfo[] { new GenreInfo { Name = value.Trim() } });
        //    IGenreConverter converter = ServiceRegistration.Get<IGenreConverter>();
        //    foreach (var genre in genreList)
        //    {
        //      if (!genre.Id.HasValue && converter.GetGenreId(genre.Name, GenreCategory.Movie, null, out int genreId))
        //        genre.Id = genreId;
        //    }
        //    MultipleMediaItemAspect genreAspect = MediaItemAspect.CreateAspect(extractedAspectData, GenreAspect.Metadata);
        //    genreAspect.SetAttribute(GenreAspect.ATTR_ID, genreList[0].Id);
        //    genreAspect.SetAttribute(GenreAspect.ATTR_GENRE, genreList[0].Name);
        //  }
        //}

        return Task.FromResult(true);
      }
      catch (Exception e)
      {
        // Only log at the info level here - And simply return false. This lets the caller know that we
        // couldn't perform our task here.
        ServiceRegistration.Get<ILogger>().Info("TvMosaicRecordingMetadataExtractor: Exception reading resource '{0}' (Text: '{1}')", mediaItemAccessor.CanonicalLocalResourcePath, e.Message);
      }
      return Task.FromResult(false);
    }

    protected bool CanExtract(IResourceAccessor mediaItemAccessor, IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData, out IResourceAccessor metaFileAccessor)
    {
      metaFileAccessor = null;
      IFileSystemResourceAccessor fsra = mediaItemAccessor as IFileSystemResourceAccessor;
      if (fsra == null || !fsra.IsFile)
        return false;

      if (_recordingSettings == null || string.IsNullOrEmpty(_recordingSettings.RecordingPath))
        return false;

      string filePath = mediaItemAccessor.CanonicalLocalResourcePath.ToString();
      string lowerExtension = StringUtils.TrimToEmpty(ProviderPathHelper.GetExtension(filePath)).ToLowerInvariant();
      var isTs = lowerExtension == ".ts";
      if (isTs && fsra.Path.StartsWith(_recordingSettings.RecordingPath, StringComparison.InvariantCultureIgnoreCase))
        return true;
      return false;
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

    #endregion
  }
}
