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

using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.MediaManagement.TransientAspects;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.Settings;
using MediaPortal.Extensions.MetadataExtractors.SeriesMetadataExtractor.NameMatchers;
using MediaPortal.Extensions.MetadataExtractors.SeriesMetadataExtractor.Settings;
using MediaPortal.Extensions.OnlineLibraries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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

      // All non-default media item aspects must be registered
      IMediaItemAspectTypeRegistration miatr = ServiceRegistration.Get<IMediaItemAspectTypeRegistration>();
      miatr.RegisterLocallyKnownMediaItemAspectTypeAsync(TempSeriesAspect.Metadata);
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
    public static int MaximumActorCount { get; private set; }
    public static bool IncludeCharacterDetails { get; private set; }
    public static int MaximumCharacterCount { get; private set; }
    public static bool IncludeDirectorDetails { get; private set; }
    public static bool IncludeWriterDetails { get; private set; }
    public static bool IncludeProductionCompanyDetails { get; private set; }
    public static bool IncludeTVNetworkDetails { get; private set; }
    public static bool OnlyLocalMedia { get; private set; }

    private void LoadSettings()
    {
      SeriesMetadataExtractorSettings settings = _settingWatcher.Settings;
      SkipOnlineSearches = settings.SkipOnlineSearches;
      SkipFanArtDownload = settings.SkipFanArtDownload;
      CacheOfflineFanArt = settings.CacheOfflineFanArt;
      CacheLocalFanArt = settings.CacheLocalFanArt;
      IncludeActorDetails = settings.IncludeActorDetails;
      MaximumActorCount = settings.MaximumActorCount;
      IncludeCharacterDetails = settings.IncludeCharacterDetails;
      MaximumCharacterCount = settings.MaximumCharacterCount;
      IncludeDirectorDetails = settings.IncludeDirectorDetails;
      IncludeWriterDetails = settings.IncludeWriterDetails;
      IncludeProductionCompanyDetails = settings.IncludeProductionCompanyDetails;
      IncludeTVNetworkDetails = settings.IncludeTVNetworkDetails;
      OnlyLocalMedia = settings.OnlyLocalMedia;
    }

    private void SettingsChanged(object sender, EventArgs e)
    {
      LoadSettings();
    }

    #endregion

    #region Protected methods

    protected async Task<bool> ExtractSeriesDataAsync(ILocalFsResourceAccessor lfsra, IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData)
    {
      // VideoAspect must be present to be sure it is actually a video resource.
      if (!extractedAspectData.ContainsKey(VideoAspect.ASPECT_ID) && !extractedAspectData.ContainsKey(SubtitleAspect.ASPECT_ID))
        return false;

      EpisodeInfo episodeInfo = new EpisodeInfo();
      episodeInfo.FromMetadata(extractedAspectData);

      // If there was no complete match, yet, try to get extended information out of matroska files)
      if (!episodeInfo.IsBaseInfoPresent || !episodeInfo.HasExternalId)
      {
        try
        {
          MatroskaMatcher matroskaMatcher = new MatroskaMatcher();
          if (await matroskaMatcher.MatchSeriesAsync(lfsra, episodeInfo).ConfigureAwait(false))
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
            if (!string.IsNullOrEmpty(language) && !episodeInfo.Languages.Contains(language))
              episodeInfo.Languages.Add(language);
          }
        }
      }

      episodeInfo.AssignNameId();

      if (SkipOnlineSearches && !SkipFanArtDownload)
      {
        EpisodeInfo tempInfo = episodeInfo.Clone();
        await OnlineMatcherService.Instance.FindAndUpdateEpisodeAsync(tempInfo).ConfigureAwait(false);
        episodeInfo.CopyIdsFrom(tempInfo);
        episodeInfo.HasChanged = tempInfo.HasChanged;
      }
      else if (!SkipOnlineSearches)
      {
        await OnlineMatcherService.Instance.FindAndUpdateEpisodeAsync(episodeInfo).ConfigureAwait(false);
      }

      //Send it to the videos section
      if (!SkipOnlineSearches && !episodeInfo.HasExternalId)
        return false;

      if (episodeInfo.EpisodeNameSort.IsEmpty)
      {
        if (!episodeInfo.SeriesName.IsEmpty && episodeInfo.SeasonNumber.HasValue && episodeInfo.DvdEpisodeNumbers.Any())
          episodeInfo.EpisodeNameSort = $"{episodeInfo.SeriesName.Text} S{episodeInfo.SeasonNumber.Value.ToString("00")}E{episodeInfo.DvdEpisodeNumbers.First().ToString("000.000")}";
        if (!episodeInfo.SeriesName.IsEmpty && episodeInfo.SeasonNumber.HasValue && episodeInfo.EpisodeNumbers.Any())
          episodeInfo.EpisodeNameSort = $"{episodeInfo.SeriesName.Text} S{episodeInfo.SeasonNumber.Value.ToString("00")}E{episodeInfo.EpisodeNumbers.First().ToString("000")}";
        else if (!episodeInfo.EpisodeName.IsEmpty)
          episodeInfo.EpisodeNameSort = BaseInfo.GetSortTitle(episodeInfo.EpisodeName.Text);
      }
      episodeInfo.SetMetadata(extractedAspectData);

      return episodeInfo.IsBaseInfoPresent;
    }

    #endregion

    #region IMetadataExtractor implementation

    public MetadataExtractorMetadata Metadata
    {
      get { return _metadata; }
    }

    public async Task<bool> TryExtractMetadataAsync(IResourceAccessor mediaItemAccessor, IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData, bool forceQuickMode)
    {
      try
      {
        if (forceQuickMode)
          return false;

        if (!(mediaItemAccessor is IFileSystemResourceAccessor))
          return false;

        using (LocalFsResourceAccessorHelper rah = new LocalFsResourceAccessorHelper(mediaItemAccessor))
          return await ExtractSeriesDataAsync(rah.LocalFsResourceAccessor, extractedAspectData).ConfigureAwait(false);
      }
      catch (Exception e)
      {
        // Only log at the info level here - And simply return false. This lets the caller know that we
        // couldn't perform our task here.
        ServiceRegistration.Get<ILogger>().Info("SeriesMetadataExtractor: Exception reading resource '{0}' (Text: '{1}')", mediaItemAccessor.CanonicalLocalResourcePath, e.Message);
      }
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

    public Task<bool> TryClearCachedMetadataAsync(IDictionary<Guid, IList<MediaItemAspect>> currentAspectData)
    {
      if(currentAspectData.ContainsKey(EpisodeAspect.ASPECT_ID))
      {
        EpisodeInfo info = new EpisodeInfo();
        info.FromMetadata(currentAspectData);
        return OnlineMatcherService.Instance.ClearEpisodeMatchAsync(info);
      }
      return Task.FromResult(false);
    }

    #endregion
  }
}
