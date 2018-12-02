#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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

      // Register reimport support
      miatr.RegisterLocallySupportedReimportMediaItemAspectTypeAsync(SeriesAspect.Metadata);
      miatr.RegisterLocallySupportedReimportMediaItemAspectTypeAsync(EpisodeAspect.Metadata);
      miatr.RegisterLocallySupportedReimportMediaItemAspectTypeAsync(VideoAspect.Metadata);
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

      bool isReimport = extractedAspectData.ContainsKey(ReimportAspect.ASPECT_ID);

      EpisodeInfo episodeInfo = new EpisodeInfo();
      episodeInfo.FromMetadata(extractedAspectData);

      if (!isReimport) //Ignore file based information for reimports because they might be the cause of the wrong match
      {
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
          catch (Exception ex)
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

      if (episodeInfo.EpisodeName.IsEmpty)
      {
        if (episodeInfo.EpisodeNumbers.Any())
          episodeInfo.EpisodeName = $"E{episodeInfo.EpisodeNumbers.First().ToString("000")}";
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

    public async Task<IList<MediaItemSearchResult>> SearchForMatchesAsync(IDictionary<Guid, IList<MediaItemAspect>> searchAspectData, ICollection<string> searchCategories)
    {
      try
      {
        if (!(searchCategories?.Contains(MEDIA_CATEGORY_NAME_SERIES) ?? true))
          return null;

        string searchData = null;
        var reimportAspect = MediaItemAspect.GetAspect(searchAspectData, ReimportAspect.Metadata);
        if (reimportAspect != null)
          searchData = reimportAspect.GetAttributeValue<string>(ReimportAspect.ATTR_SEARCH);

        ServiceRegistration.Get<ILogger>().Debug("SeriesMetadataExtractor: Search aspects to use: '{0}'", string.Join(",", searchAspectData.Keys));

        //Prepare search info
        EpisodeInfo episodeSearchinfo = null;
        SeriesInfo seriesSearchinfo = null;
        List<MediaItemSearchResult> searchResults = new List<MediaItemSearchResult>();
        if (!string.IsNullOrEmpty(searchData))
        {
          if (searchAspectData.ContainsKey(VideoAspect.ASPECT_ID))
          {
            EpisodeInfo episode = new EpisodeInfo();
            episode.FromMetadata(searchAspectData);

            episodeSearchinfo = new EpisodeInfo();
            episodeSearchinfo.SeasonNumber = episode.SeasonNumber;
            episodeSearchinfo.EpisodeNumbers = episode.EpisodeNumbers.ToList();
            if (searchData.StartsWith("tt", StringComparison.InvariantCultureIgnoreCase) && !searchData.Contains(" ") && int.TryParse(searchData.Substring(2), out int id))
              episodeSearchinfo.SeriesImdbId = searchData;
            else if (!searchData.Contains(" ") && int.TryParse(searchData, out int tvDbSeriesId))
              episodeSearchinfo.SeriesTvdbId = tvDbSeriesId;
            else //Fallabck to name search
            {
              searchData = searchData.Trim();
              SeriesMatcher seriesMatcher = new SeriesMatcher();
              //Add extension to simulate a file name which the matcher expects
              EpisodeInfo tempEpisodeInfo = new EpisodeInfo();
              tempEpisodeInfo.SeasonNumber = episode.SeasonNumber;
              if (seriesMatcher.MatchSeries(searchData + ".ext", tempEpisodeInfo))
              {
                episodeSearchinfo.SeriesName = tempEpisodeInfo.SeriesName;
                episodeSearchinfo.SeasonNumber = tempEpisodeInfo.SeasonNumber;
                episodeSearchinfo.EpisodeNumbers = tempEpisodeInfo.EpisodeNumbers.ToList();
              }
            }

            ServiceRegistration.Get<ILogger>().Debug("SeriesMetadataExtractor: Searching for episode matches on search: '{0}'", searchData);
          }
          else if (searchAspectData.ContainsKey(SeriesAspect.ASPECT_ID))
          {
            seriesSearchinfo = new SeriesInfo();
            if (searchData.StartsWith("tt", StringComparison.InvariantCultureIgnoreCase) && !searchData.Contains(" ") && int.TryParse(searchData.Substring(2), out int id))
              seriesSearchinfo.ImdbId = searchData;
            else if (!searchData.Contains(" ") && int.TryParse(searchData, out int tvDbSeriesId))
              seriesSearchinfo.TvdbId = tvDbSeriesId;
            else //Fallabck to name search
            {
              searchData = searchData.Trim();
              EpisodeInfo tempEpisodeInfo = new EpisodeInfo();
              tempEpisodeInfo.SeasonNumber = 1;
              SeriesMatcher seriesMatcher = new SeriesMatcher();
              //Add extension to simulate a file name which the matcher expects
              seriesMatcher.MatchSeries(searchData + " S01E01.ext", tempEpisodeInfo);
              if (tempEpisodeInfo.SeriesFirstAired.HasValue)
              {
                seriesSearchinfo.SeriesName = tempEpisodeInfo.SeriesName;
                seriesSearchinfo.FirstAired = tempEpisodeInfo.SeriesFirstAired;
              }
              else
              {
                seriesSearchinfo.SeriesName = searchData;
              }

              ServiceRegistration.Get<ILogger>().Debug("SeriesMetadataExtractor: Searching for series matches on search: '{0}'", searchData);
            }
          }
        }
        else
        {
          if (searchAspectData.ContainsKey(VideoAspect.ASPECT_ID))
          {
            episodeSearchinfo = new EpisodeInfo();
            episodeSearchinfo.FromMetadata(searchAspectData);

            ServiceRegistration.Get<ILogger>().Debug("SeriesMetadataExtractor: Searching for episode matches on aspects");
          }
          else if (searchAspectData.ContainsKey(SeriesAspect.ASPECT_ID))
          {
            seriesSearchinfo = new SeriesInfo();
            seriesSearchinfo.FromMetadata(searchAspectData);

            ServiceRegistration.Get<ILogger>().Debug("SeriesMetadataExtractor: Searching for series matches on aspects");
          }
        }

        //Perform online search
        if (episodeSearchinfo != null)
        {
          List<int> epNos = new List<int>(episodeSearchinfo.EpisodeNumbers.OrderBy(e => e));
          var matches = await OnlineMatcherService.Instance.FindMatchingEpisodesAsync(episodeSearchinfo).ConfigureAwait(false);
          ServiceRegistration.Get<ILogger>().Debug("SeriesMetadataExtractor: Episode search returned {0} matches", matches.Count());
          if (epNos.Count > 1)
          {
            //Check if double episode is in the search results
            if(!matches.Any(e => e.EpisodeNumbers.SequenceEqual(epNos)))
            {
              //Add a double episode if it's not
              var potentialEpisodes = new Dictionary<int, List<EpisodeInfo>>();
              foreach(var episodeNo in epNos)
                potentialEpisodes[episodeNo] = matches.Where(e => e.FirstEpisodeNumber == episodeNo && e.EpisodeNumbers.Count == 1).ToList();
              //Merge fitting episodes
              var mergedEpisodes = new List<EpisodeInfo>();
              foreach (var episodeNo in epNos)
              {
                if (episodeNo == episodeSearchinfo.FirstEpisodeNumber)
                {
                  foreach (var episode in potentialEpisodes[episodeNo])
                  {
                    mergedEpisodes.Add(episode.Clone());
                  }
                }
                else
                {
                  foreach(var mergedEpisode in mergedEpisodes)
                  {
                    var nextEpisode = potentialEpisodes[episodeNo].FirstOrDefault(e => e.SeriesTvdbId > 0 && e.SeriesTvdbId == mergedEpisode.SeriesTvdbId && 
                      e.SeasonNumber == mergedEpisode.SeasonNumber);
                    if (nextEpisode == null)
                      nextEpisode = potentialEpisodes[episodeNo].FirstOrDefault(e => !string.IsNullOrEmpty(e.SeriesImdbId) && e.SeriesImdbId.Equals(mergedEpisode.SeriesImdbId, StringComparison.InvariantCultureIgnoreCase) && 
                        e.SeasonNumber == mergedEpisode.SeasonNumber);
                    if (nextEpisode == null)
                      nextEpisode = potentialEpisodes[episodeNo].FirstOrDefault(e => e.SeriesMovieDbId > 0 && e.SeriesMovieDbId == mergedEpisode.SeriesMovieDbId && 
                        e.SeasonNumber == mergedEpisode.SeasonNumber);
                    if (nextEpisode != null)
                      MergeEpisodeDetails(mergedEpisode, nextEpisode);
                  }
                }
              }
              //Add valid merged episodes to search result
              var list = matches.ToList();
              var validMergedEpisodes = mergedEpisodes.Where(e => e.EpisodeNumbers.SequenceEqual(epNos));
              list.AddRange(validMergedEpisodes);
              matches = list.AsEnumerable();

              if(validMergedEpisodes.Count() > 0)
                ServiceRegistration.Get<ILogger>().Debug("SeriesMetadataExtractor: Added {0} multi-episodes to matches", validMergedEpisodes.Count());
            }
          }
          foreach (var match in matches)
          {
            var result = new MediaItemSearchResult
            {
              Name = $"{match.SeriesName}{(match.SeriesFirstAired == null || match.SeriesName.Text.EndsWith($"({match.SeriesFirstAired.Value.Year})") ? "" : $" ({match.SeriesFirstAired.Value.Year})")}" +
                $" S{(match.SeasonNumber.HasValue ? match.SeasonNumber.Value.ToString("00") : "??")}{(match.EpisodeNumbers.Count > 0 ? string.Join("", match.EpisodeNumbers.Select(e => "E" + e.ToString("00"))) : "E??")}" +
                $"{(match.EpisodeName.IsEmpty ? "" : $": {match.EpisodeName.Text}")}",
              Description = match.Summary.IsEmpty ? "" : match.Summary.Text,
            };

            //Add external Ids
            if (match.TvdbId > 0)
              result.ExternalIds.Add("thetvdb.com", match.TvdbId.ToString());
            if (!string.IsNullOrEmpty(match.ImdbId))
              result.ExternalIds.Add("imdb.com", match.ImdbId);
            if (match.MovieDbId > 0)
              result.ExternalIds.Add("themoviedb.org", match.MovieDbId.ToString());

            //Assign aspects and remove unwanted aspects
            match.SetMetadata(result.AspectData, true);
            CleanReimportAspects(result.AspectData);

            searchResults.Add(result);
          }
          return searchResults;
        }
        else if (seriesSearchinfo != null)
        {
          var matches = await OnlineMatcherService.Instance.FindMatchingSeriesAsync(seriesSearchinfo).ConfigureAwait(false);
          ServiceRegistration.Get<ILogger>().Debug("SeriesMetadataExtractor: Series search returned {0} matches", matches.Count());
          foreach (var match in matches)
          {
            var result = new MediaItemSearchResult
            {
              Name = $"{match.SeriesName}{(match.FirstAired == null || match.SeriesName.Text.EndsWith($"({match.FirstAired.Value.Year})") ? "" : $" ({match.FirstAired.Value.Year})")}",
              Description = match.Description.IsEmpty ? "" : match.Description.Text,
            };

            //Add external Ids
            if (match.TvdbId > 0)
              result.ExternalIds.Add("thetvdb.com", match.TvdbId.ToString());
            if (!string.IsNullOrEmpty(match.ImdbId))
              result.ExternalIds.Add("imdb.com", match.ImdbId);
            if (match.MovieDbId > 0)
              result.ExternalIds.Add("themoviedb.org", match.MovieDbId.ToString());

            //Assign aspects and remove unwanted aspects
            match.SetMetadata(result.AspectData, true);
            CleanReimportAspects(result.AspectData);

            searchResults.Add(result);
          }
          return searchResults;
        }
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Info("SeriesMetadataExtractor: Exception searching for matches (Text: '{0}')", e.Message);
      }
      return null;
    }

    private void MergeEpisodeDetails(EpisodeInfo episodeInfo, EpisodeInfo mergeEpisodeInfo)
    {
      MetadataUpdater.SetOrUpdateId(ref episodeInfo.SeriesImdbId, mergeEpisodeInfo.SeriesImdbId);
      MetadataUpdater.SetOrUpdateId(ref episodeInfo.SeriesMovieDbId, mergeEpisodeInfo.SeriesMovieDbId);
      MetadataUpdater.SetOrUpdateId(ref episodeInfo.SeriesTvdbId, mergeEpisodeInfo.SeriesTvdbId);
      MetadataUpdater.SetOrUpdateId(ref episodeInfo.SeriesTvRageId, mergeEpisodeInfo.SeriesTvRageId);
      MetadataUpdater.SetOrUpdateId(ref episodeInfo.SeriesTvMazeId, mergeEpisodeInfo.SeriesTvMazeId);

      MetadataUpdater.SetOrUpdateValue(ref episodeInfo.SeriesFirstAired, mergeEpisodeInfo.SeriesFirstAired);
      MetadataUpdater.SetOrUpdateString(ref episodeInfo.SeriesName, mergeEpisodeInfo.SeriesName);

      episodeInfo.EpisodeNumbers = episodeInfo.EpisodeNumbers.Union(mergeEpisodeInfo.EpisodeNumbers).ToList();
      episodeInfo.DvdEpisodeNumbers = episodeInfo.DvdEpisodeNumbers.Union(mergeEpisodeInfo.DvdEpisodeNumbers).ToList();
      episodeInfo.EpisodeName.Text += "; " + mergeEpisodeInfo.EpisodeName.Text;
      episodeInfo.Summary.Text += "\r\n\r\n" + mergeEpisodeInfo.Summary.Text;

      episodeInfo.Genres = episodeInfo.Genres.Union(mergeEpisodeInfo.Genres).ToList();
    }

    public async Task<bool> AddMatchedAspectDetailsAsync(IDictionary<Guid, IList<MediaItemAspect>> matchedAspectData)
    {
      try
      {
        if (matchedAspectData.ContainsKey(EpisodeAspect.ASPECT_ID))
        {
          EpisodeInfo info = new EpisodeInfo();
          info.FromMetadata(matchedAspectData);
          await OnlineMatcherService.Instance.FindAndUpdateEpisodeAsync(info).ConfigureAwait(false);
          info.SetMetadata(matchedAspectData, true);
          CleanReimportAspects(matchedAspectData);
          return true;
        }
        else if (matchedAspectData.ContainsKey(SeriesAspect.ASPECT_ID))
        {
          SeriesInfo info = new SeriesInfo();
          info.FromMetadata(matchedAspectData);
          await OnlineMatcherService.Instance.UpdateSeriesAsync(info, false).ConfigureAwait(false);
          info.SetMetadata(matchedAspectData, true);
          CleanReimportAspects(matchedAspectData);
          return true;
        }
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Info("SeriesMetadataExtractor: Exception adding match details (Text: '{0}')", e.Message);
      }
      return false;
    }

    private void CleanReimportAspects(IDictionary<Guid, IList<MediaItemAspect>> aspectData)
    {
      IEnumerable<Guid> reimportAspects = new Guid[] { ExternalIdentifierAspect.ASPECT_ID, MediaAspect.ASPECT_ID,
        SeriesAspect.ASPECT_ID, EpisodeAspect.ASPECT_ID, VideoAspect.ASPECT_ID, ReimportAspect.ASPECT_ID, GenreAspect.ASPECT_ID };
      foreach (var aspect in aspectData.Where(a => !reimportAspects.Contains(a.Key)).ToList())
        aspectData.Remove(aspect.Key);
    }

    #endregion
  }
}
