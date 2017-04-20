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

using System;
using System.Collections.Generic;
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Extensions.MetadataExtractors.SeriesMetadataExtractor.NameMatchers;
using MediaPortal.Extensions.OnlineLibraries;
using MediaPortal.Common.Services.Settings;
using MediaPortal.Extensions.MetadataExtractors.SeriesMetadataExtractor.Settings;
using MediaPortal.Common.Messaging;
using System.Threading;

namespace MediaPortal.Extensions.MetadataExtractors.SeriesMetadataExtractor
{
  /// <summary>
  /// MediaPortal 2 metadata extractor implementation for Series.
  /// </summary>
  public class SeriesMetadataExtractor : IMetadataExtractor, IDisposable
  {
    #region Constants

    /// <summary>
    /// GUID string for the video metadata extractor.
    /// </summary>
    public const string METADATAEXTRACTOR_ID_STR = "A2D018D4-97E9-4B37-A7C3-31FD270277D0";

    /// <summary>
    /// Video metadata extractor GUID.
    /// </summary>
    public static Guid METADATAEXTRACTOR_ID = new Guid(METADATAEXTRACTOR_ID_STR);

    public const string MEDIA_CATEGORY_NAME_SERIES = "Series";
    public const double MINIMUM_HOUR_AGE_BEFORE_UPDATE = 0.5;

    #endregion

    #region Protected fields and classes

    protected static ICollection<MediaCategory> MEDIA_CATEGORIES = new List<MediaCategory>();
    protected static ICollection<string> VIDEO_FILE_EXTENSIONS = new List<string>();

    protected MetadataExtractorMetadata _metadata;
    protected AsynchronousMessageQueue _messageQueue;
    protected int _importerCount;
    protected SettingsChangeWatcher<SeriesMetadataExtractorSettings> _settingWatcher;

    #endregion

    #region Ctor

    static SeriesMetadataExtractor()
    {
      MediaCategory seriesCategory;
      IMediaAccessor mediaAccessor = ServiceRegistration.Get<IMediaAccessor>();
      if (!mediaAccessor.MediaCategories.TryGetValue(MEDIA_CATEGORY_NAME_SERIES, out seriesCategory))
        seriesCategory = mediaAccessor.RegisterMediaCategory(MEDIA_CATEGORY_NAME_SERIES, new List<MediaCategory> { DefaultMediaCategories.Video });
      MEDIA_CATEGORIES.Add(seriesCategory);
    }

    public SeriesMetadataExtractor()
    {
      _metadata = new MetadataExtractorMetadata(METADATAEXTRACTOR_ID, "Series metadata extractor", MetadataExtractorPriority.External, true,
          MEDIA_CATEGORIES, new MediaItemAspectMetadata[]
              {
                MediaAspect.Metadata,
                EpisodeAspect.Metadata
              });

      _messageQueue = new AsynchronousMessageQueue(this, new string[]
        {
            ImporterWorkerMessaging.CHANNEL,
        });
      _messageQueue.MessageReceived += OnMessageReceived;
      _messageQueue.Start();

      _settingWatcher = new SettingsChangeWatcher<SeriesMetadataExtractorSettings>();
      _settingWatcher.SettingsChanged += SettingsChanged;

      LoadSettings();
    }

    public void Dispose()
    {
      _messageQueue.Shutdown();
    }

    private void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == ImporterWorkerMessaging.CHANNEL)
      {
        ImporterWorkerMessaging.MessageType messageType = (ImporterWorkerMessaging.MessageType)message.MessageType;
        switch (messageType)
        {
          case ImporterWorkerMessaging.MessageType.ImportStarted:
            if (Interlocked.Increment(ref _importerCount) == 1)
            {
              IMediaFanArtHandler fanartHandler;
              if (ServiceRegistration.Get<IMediaAccessor>().LocalFanArtHandlers.TryGetValue(SeriesFanArtHandler.FANARTHANDLER_ID, out fanartHandler))
                fanartHandler.ClearCache();
            }
            break;
          case ImporterWorkerMessaging.MessageType.ImportCompleted:
            if (Interlocked.Decrement(ref _importerCount) == 0)
            {
              IRelationshipExtractor relationshipExtractor;
              if (ServiceRegistration.Get<IMediaAccessor>().LocalRelationshipExtractors.TryGetValue(SeriesRelationshipExtractor.METADATAEXTRACTOR_ID, out relationshipExtractor))
                foreach (ISeriesRelationshipExtractor extractor in relationshipExtractor.RoleExtractors)
                  extractor.ClearCache();
            }
            break;
        }
      }
    }

    #endregion

    #region Settings

    public static bool SkipOnlineSearches { get; private set; }
    public static bool SkipFanArtDownload { get; private set; }
    public static bool CacheOfflineFanArt { get; private set; }
    public static bool CacheLocalFanArt { get; private set; }
    public static bool IncludeActorDetails { get; private set; }
    public static bool IncludeCharacterDetails { get; private set; }
    public static bool IncludeDirectorDetails { get; private set; }
    public static bool IncludeWriterDetails { get; private set; }
    public static bool IncludeProductionCompanyDetails { get; private set; }
    public static bool IncludeTVNetworkDetails { get; private set; }
    public static bool OnlyLocalMedia { get; private set; }

    private void LoadSettings()
    {
      SkipOnlineSearches = _settingWatcher.Settings.SkipOnlineSearches;
      SkipFanArtDownload = _settingWatcher.Settings.SkipFanArtDownload;
      CacheOfflineFanArt = _settingWatcher.Settings.CacheOfflineFanArt;
      CacheLocalFanArt = _settingWatcher.Settings.CacheLocalFanArt;
      IncludeActorDetails = _settingWatcher.Settings.IncludeActorDetails;
      IncludeCharacterDetails = _settingWatcher.Settings.IncludeCharacterDetails;
      IncludeDirectorDetails = _settingWatcher.Settings.IncludeDirectorDetails;
      IncludeWriterDetails = _settingWatcher.Settings.IncludeWriterDetails;
      IncludeProductionCompanyDetails = _settingWatcher.Settings.IncludeProductionCompanyDetails;
      IncludeTVNetworkDetails = _settingWatcher.Settings.IncludeTVNetworkDetails;
      OnlyLocalMedia = _settingWatcher.Settings.OnlyLocalMedia;
    }

    private void SettingsChanged(object sender, EventArgs e)
    {
      LoadSettings();
    }

    #endregion

    #region Protected methods

    protected bool ExtractSeriesData(ILocalFsResourceAccessor lfsra, IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData, bool importOnly)
    {
      // VideoAspect must be present to be sure it is actually a video resource.
      if (!extractedAspectData.ContainsKey(VideoStreamAspect.ASPECT_ID) && !extractedAspectData.ContainsKey(SubtitleAspect.ASPECT_ID))
        return false;

      if (extractedAspectData.ContainsKey(SubtitleAspect.ASPECT_ID) && !importOnly)
        return false; //Subtitles can only be imported not refreshed

      bool refresh = false;
      if (extractedAspectData.ContainsKey(EpisodeAspect.ASPECT_ID))
        refresh = true;

      EpisodeInfo episodeInfo = new EpisodeInfo();
      if (refresh)
      {
        episodeInfo.FromMetadata(extractedAspectData);
      }
      ISeriesRelationshipExtractor.UpdateEpisodeSeries(extractedAspectData, episodeInfo);
      if (!episodeInfo.IsBaseInfoPresent)
      {
        string title = null;
        int seasonNumber;
        SingleMediaItemAspect episodeAspect;
        MediaItemAspect.TryGetAspect(extractedAspectData, EpisodeAspect.Metadata, out episodeAspect);
        IEnumerable<int> episodeNumbers;
        if (MediaItemAspect.TryGetAttribute(extractedAspectData, EpisodeAspect.ATTR_SERIES_NAME, out title) &&
            MediaItemAspect.TryGetAttribute(extractedAspectData, EpisodeAspect.ATTR_SEASON, out seasonNumber) &&
            (episodeNumbers = episodeAspect.GetCollectionAttribute<int>(EpisodeAspect.ATTR_EPISODE)) != null)
        {
          episodeInfo.SeriesName = title;
          episodeInfo.SeasonNumber = seasonNumber;
          episodeInfo.EpisodeNumbers.Clear();
          episodeNumbers.ToList().ForEach(n => episodeInfo.EpisodeNumbers.Add(n));
        }
      }

      // If there was no complete match, yet, try to get extended information out of matroska files)
      if (!episodeInfo.IsBaseInfoPresent || !episodeInfo.HasExternalId)
      {
        try
        {
          MatroskaMatcher matroskaMatcher = new MatroskaMatcher();
          if (matroskaMatcher.MatchSeries(lfsra, episodeInfo))
          {
            ServiceRegistration.Get<ILogger>().Debug("ExtractSeriesData: Found EpisodeInfo by MatroskaMatcher for {0}, IMDB {1}, TVDB {2}, TMDB {3}, AreReqiredFieldsFilled {4}",
              episodeInfo.SeriesName, episodeInfo.SeriesImdbId, episodeInfo.SeriesTvdbId, episodeInfo.SeriesMovieDbId, episodeInfo.IsBaseInfoPresent);
          }
        }
        catch(Exception ex)
        {
          ServiceRegistration.Get<ILogger>().Debug("ExtractSeriesData: Exception reading matroska tags for '{0}'", ex, lfsra.CanonicalLocalResourcePath);
        }
      }

      // If no information was found before, try name matching
      if (!episodeInfo.IsBaseInfoPresent)
      {
        // Try to match series from folder and file naming
        SeriesMatcher seriesMatcher = new SeriesMatcher();
        seriesMatcher.MatchSeries(lfsra, episodeInfo);
      }

      //Prepare online search improvements
      if (episodeInfo.SeriesFirstAired == null)
      {
        EpisodeInfo tempEpisodeInfo = new EpisodeInfo();
        SeriesMatcher seriesMatcher = new SeriesMatcher();
        seriesMatcher.MatchSeries(lfsra, tempEpisodeInfo);
        if (tempEpisodeInfo.SeriesFirstAired.HasValue)
          episodeInfo.SeriesFirstAired = tempEpisodeInfo.SeriesFirstAired;
      }
      if (string.IsNullOrEmpty(episodeInfo.SeriesAlternateName))
      {
        var mediaItemPath = lfsra.CanonicalLocalResourcePath;
        var seriesMediaItemDirectoryPath = ResourcePathHelper.Combine(mediaItemPath, "../../");
        episodeInfo.SeriesAlternateName = seriesMediaItemDirectoryPath.FileName;
      }

      if (episodeInfo.Languages.Count == 0)
      {
        IList<MultipleMediaItemAspect> audioAspects;
        if (MediaItemAspect.TryGetAspects(extractedAspectData, VideoAudioStreamAspect.Metadata, out audioAspects))
        {
          foreach (MultipleMediaItemAspect aspect in audioAspects)
          {
            string language = (string)aspect.GetAttributeValue(VideoAudioStreamAspect.ATTR_AUDIOLANGUAGE);
            if (!string.IsNullOrEmpty(language))
              episodeInfo.Languages.Add(language);
          }
        }
      }

      episodeInfo.AssignNameId();

      if (SkipOnlineSearches && !SkipFanArtDownload)
      {
        EpisodeInfo tempInfo = episodeInfo.Clone();
        OnlineMatcherService.Instance.FindAndUpdateEpisode(tempInfo, importOnly);
        episodeInfo.CopyIdsFrom(tempInfo);
        episodeInfo.HasChanged = tempInfo.HasChanged;
      }
      else if (!SkipOnlineSearches)
      {
        OnlineMatcherService.Instance.FindAndUpdateEpisode(episodeInfo, importOnly);
      }

      //Send it to the videos section
      if (!SkipOnlineSearches && !episodeInfo.HasExternalId)
        return false;

      if(refresh)
      {
        if((IncludeActorDetails && !BaseInfo.HasRelationship(extractedAspectData, PersonAspect.ROLE_ACTOR) && episodeInfo.Actors.Count > 0) ||
          (IncludeCharacterDetails && !BaseInfo.HasRelationship(extractedAspectData, CharacterAspect.ROLE_CHARACTER) && episodeInfo.Characters.Count > 0) ||
          (IncludeDirectorDetails && !BaseInfo.HasRelationship(extractedAspectData, PersonAspect.ROLE_DIRECTOR) && episodeInfo.Directors.Count > 0) ||
          (IncludeWriterDetails && !BaseInfo.HasRelationship(extractedAspectData, PersonAspect.ROLE_WRITER) && episodeInfo.Writers.Count > 0) ||
          (!BaseInfo.HasRelationship(extractedAspectData, SeriesAspect.ROLE_SERIES) && !episodeInfo.SeriesName.IsEmpty) ||
          (!BaseInfo.HasRelationship(extractedAspectData, SeasonAspect.ROLE_SEASON) && episodeInfo.SeasonNumber.HasValue))
        {
          episodeInfo.HasChanged = true;
        }
      }

      if (!episodeInfo.HasChanged && !importOnly)
        return false;

      episodeInfo.SetMetadata(extractedAspectData);

      return episodeInfo.IsBaseInfoPresent;
    }

    #endregion

    #region IMetadataExtractor implementation

    public MetadataExtractorMetadata Metadata
    {
      get { return _metadata; }
    }

    public bool TryExtractMetadata(IResourceAccessor mediaItemAccessor, IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData, bool importOnly)
    {
      try
      {
        if (!(mediaItemAccessor is IFileSystemResourceAccessor))
          return false;

        using (LocalFsResourceAccessorHelper rah = new LocalFsResourceAccessorHelper(mediaItemAccessor))
          return ExtractSeriesData(rah.LocalFsResourceAccessor, extractedAspectData, importOnly);
      }
      catch (Exception e)
      {
        // Only log at the info level here - And simply return false. This lets the caller know that we
        // couldn't perform our task here.
        ServiceRegistration.Get<ILogger>().Info("SeriesMetadataExtractor: Exception reading resource '{0}' (Text: '{1}')", mediaItemAccessor.CanonicalLocalResourcePath, e.Message);
      }
      return false;
    }

    #endregion
  }
}
