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
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.Extractors;
using MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.NfoReaders;
using MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.Utilities;
using MediaPortal.Utilities;
using MediaPortal.Utilities.SystemAPI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors
{
  /// <summary>
  /// MediaPortal 2 metadata extractor for series reading from local nfo-files.
  /// </summary>
  public class NfoSeriesMetadataExtractor : NfoSeriesExtractorBase, IMetadataExtractor
  {
    #region Constants / Static fields

    /// <summary>
    /// GUID of the NfoMetadataExtractors plugin
    /// </summary>
    public const string PLUGIN_ID_STR = "2505C495-28AA-4D1C-BDEE-CA4A3A89B0D5";
    public static readonly Guid PLUGIN_ID = new Guid(PLUGIN_ID_STR);

    /// <summary>
    /// GUID for the NfoSeriesMetadataExtractor
    /// </summary>
    public const string METADATAEXTRACTOR_ID_STR = "45070E52-7CA1-473C-AE10-B08FB8243CC3";
    public static readonly Guid METADATAEXTRACTOR_ID = new Guid(METADATAEXTRACTOR_ID_STR);

    /// <summary>
    /// MediaCategories this MetadataExtractor is applied to
    /// </summary>
    private const string MEDIA_CATEGORY_NAME_SERIES = "Series";
    private readonly static ICollection<MediaCategory> MEDIA_CATEGORIES = new List<MediaCategory>();

    #endregion

    #region Private fields

    /// <summary>
    /// Metadata of this MetadataExtractor
    /// </summary>
    private readonly MetadataExtractorMetadata _metadata;

    #endregion

    #region Ctor

    /// <summary>
    /// Initializes <see cref="MEDIA_CATEGORIES"/> and, if necessary, registers the "Series" <see cref="MediaCategory"/>
    /// </summary>
    static NfoSeriesMetadataExtractor()
    {
      MediaCategory seriesCategory;
      var mediaAccessor = ServiceRegistration.Get<IMediaAccessor>();
      if (!mediaAccessor.MediaCategories.TryGetValue(MEDIA_CATEGORY_NAME_SERIES, out seriesCategory))
        seriesCategory = mediaAccessor.RegisterMediaCategory(MEDIA_CATEGORY_NAME_SERIES, new List<MediaCategory> { DefaultMediaCategories.Video });
      MEDIA_CATEGORIES.Add(seriesCategory);

      // All non-default media item aspects must be registered
      IMediaItemAspectTypeRegistration miatr = ServiceRegistration.Get<IMediaItemAspectTypeRegistration>();
      miatr.RegisterLocallyKnownMediaItemAspectTypeAsync(TempSeriesAspect.Metadata);
      miatr.RegisterLocallyKnownMediaItemAspectTypeAsync(TempActorAspect.Metadata);
    }

    /// <summary>
    /// Instantiates a new <see cref="NfoSeriesMetadataExtractor"/> object
    /// </summary>
    public NfoSeriesMetadataExtractor()
    {
      // The metadataExtractorPriority is intentionally set wrong to "Extended" although, depending on the
      // content of the nfo-file, it may download thumbs from the internet (and should therefore be
      // "External"). This is a temporary workaround for performance purposes. It ensures that this 
      // MetadataExtractor is applied before the VideoThumbnailer (which is intentionally set to "External"
      // although it only uses local files). Creating thumbs with the VideoThumbnailer takes much longer
      // than downloading them from the internet.
      // ToDo: Correct this once we have a better priority system
      _metadata = new MetadataExtractorMetadata(
        metadataExtractorId: METADATAEXTRACTOR_ID,
        name: "Nfo series metadata extractor",
        metadataExtractorPriority: MetadataExtractorPriority.Extended,
        processesNonFiles: true,
        shareCategories: MEDIA_CATEGORIES,
        extractedAspectTypes: new MediaItemAspectMetadata[]
        {
          MediaAspect.Metadata,
          EpisodeAspect.Metadata,
          ThumbnailLargeAspect.Metadata
        });
    }

    #endregion

    #region Settings

    public static bool IncludeActorDetails { get; private set; }
    public static bool IncludeCharacterDetails { get; private set; }

    protected override void LoadSettings()
    {
      IncludeActorDetails = _settingWatcher.Settings.IncludeActorDetails;
      IncludeCharacterDetails = _settingWatcher.Settings.IncludeCharacterDetails;
    }

    #endregion

    #region Private methods

    #region Metadata extraction

    /// <summary>
    /// Asynchronously tries to extract episode metadata for the given <param name="mediaItemAccessor"></param>
    /// </summary>
    /// <param name="mediaItemAccessor">Points to the resource for which we try to extract metadata</param>
    /// <param name="extractedAspectData">Dictionary of <see cref="MediaItemAspect"/>s with the extracted metadata</param>
    /// <param name="forceQuickMode">If <c>true</c>, nothing is downloaded from the internet</param>
    /// <returns><c>true</c> if metadata was found and stored into <param name="extractedAspectData"></param>, else <c>false</c></returns>
    private async Task<bool> TryExtractEpsiodeMetadataAsync(IResourceAccessor mediaItemAccessor, IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData, bool forceQuickMode)
    {
      // Get a unique number for this call to TryExtractMetadataAsync. We use this to make reading the debug log easier.
      // This MetadataExtractor is called in parallel for multiple MediaItems so that the respective debug log entries
      // for one call are not contained one after another in debug log. We therefore prepend this number before every log entry.
      var miNumber = Interlocked.Increment(ref _lastMediaItemNumber);
      bool isStub = extractedAspectData.ContainsKey(StubAspect.ASPECT_ID);
      try
      {
        _debugLogger.Info("[#{0}]: Start extracting metadata for resource '{1}' (forceQuickMode: {2})", miNumber, mediaItemAccessor, forceQuickMode);

        // This MetadataExtractor only works for MediaItems accessible by an IFileSystemResourceAccessor.
        // Otherwise it is not possible to find a nfo-file in the MediaItem's directory or parent directory.
        if (!(mediaItemAccessor is IFileSystemResourceAccessor))
        {
          _debugLogger.Info("[#{0}]: Cannot extract metadata; mediaItemAccessor is not an IFileSystemResourceAccessor", miNumber);
          return false;
        }

        // We only extract metadata with this MetadataExtractor, if another MetadataExtractor that was applied before
        // has identified this MediaItem as a video and therefore added a VideoAspect.
        if (!extractedAspectData.ContainsKey(VideoAspect.ASPECT_ID))
        {
          _debugLogger.Info("[#{0}]: Cannot extract metadata; this resource is not a video", miNumber);
          return false;
        }

        // Here we try to find an IFileSystemResourceAccessor pointing to the episode nfo-file.
        // If we don't find one, we cannot extract any metadata.
        IFileSystemResourceAccessor episodeNfoFsra;
        NfoSeriesEpisodeReader episodeNfoReader = null;
        bool episodeDetailsFound = false;
        if (TryGetEpisodeNfoSResourceAccessor(miNumber, mediaItemAccessor as IFileSystemResourceAccessor, out episodeNfoFsra))
        {
          episodeDetailsFound = true;
          // Now we (asynchronously) extract the metadata into a stub object.
          // If no metadata was found, nothing can be stored in the MediaItemAspects.
          episodeNfoReader = new NfoSeriesEpisodeReader(_debugLogger, miNumber, forceQuickMode, isStub, _httpClient, _settings);
          using (episodeNfoFsra)
          {
            if (!await episodeNfoReader.TryReadMetadataAsync(episodeNfoFsra).ConfigureAwait(false))
            {
              _debugLogger.Warn("[#{0}]: No valid metadata found in episode nfo-file", miNumber);
              return false;
            }
          }
        }

        // Then we try to find an IFileSystemResourceAccessor pointing to the series nfo-file.
        IFileSystemResourceAccessor seriesNfoFsra;
        if (TryGetSeriesNfoSResourceAccessor(miNumber, mediaItemAccessor as IFileSystemResourceAccessor, out seriesNfoFsra))
        {
          // If we found one, we (asynchronously) extract the metadata into a stub object and, if metadata was found,
          // we store it into the episodeNfoReader so that the latter can store metadata from series and episode level into the MediaItemAspects.
          var seriesNfoReader = new NfoSeriesReader(_debugLogger, miNumber, forceQuickMode, !episodeDetailsFound, isStub, _httpClient, _settings);
          using (seriesNfoFsra)
          {
            if (await seriesNfoReader.TryReadMetadataAsync(seriesNfoFsra).ConfigureAwait(false))
            {
              //Check reimport
              if (extractedAspectData.ContainsKey(ReimportAspect.ASPECT_ID))
              {
                SeriesInfo reimport = new SeriesInfo();
                reimport.FromMetadata(extractedAspectData);
                if(!VerifySeriesReimport(seriesNfoReader, reimport))
                {
                  ServiceRegistration.Get<ILogger>().Info("NfoSeriesMetadataExtractor: Nfo series metadata from resource '{0}' ignored because it does not match reimport {1}", mediaItemAccessor, reimport);
                  return false;
                }
              }

              Stubs.SeriesStub series = seriesNfoReader.GetSeriesStubs().FirstOrDefault();

              // Check if episode should be found
              if (isStub || !episodeDetailsFound)
              {
                if (series != null && series.Episodes?.Count > 0)
                {
                  List<Stubs.SeriesEpisodeStub> episodeStubs = null;
                  if (extractedAspectData.ContainsKey(EpisodeAspect.ASPECT_ID))
                  {
                    int? seasonNo = 0;
                    IEnumerable episodes;
                    if (MediaItemAspect.TryGetAttribute(extractedAspectData, EpisodeAspect.ATTR_SEASON, out seasonNo) && MediaItemAspect.TryGetAttribute(extractedAspectData, EpisodeAspect.ATTR_EPISODE, out episodes))
                    {
                      List<int> episodeNos = new List<int>();
                      CollectionUtils.AddAll(episodeNos, episodes.Cast<int>());

                      if (seasonNo.HasValue && episodeNos.Count > 0)
                        episodeStubs = series.Episodes.Where(e => e.Season == seasonNo.Value && episodeNos.Intersect(e.Episodes).Any()).ToList();
                    }
                  }
                  else
                  {
                    string title = null;
                    if (MediaItemAspect.TryGetAttribute(extractedAspectData, MediaAspect.ATTR_TITLE, out title))
                    {
                      Regex regex = new Regex(@"(?<series>[^\\]+).S(?<seasonnum>\d+)[\s|\.|\-|_]{0,1}E((?<episodenum>\d+)_?)+(?<episode>.*)");
                      Match match = regex.Match(title);

                      if (match.Success && match.Groups["seasonnum"].Length > 0 && match.Groups["episodenum"].Length > 0)
                        episodeStubs = series.Episodes.Where(e => e.Season == Convert.ToInt32(match.Groups["seasonnum"].Value) && e.Episodes.Contains(Convert.ToInt32(match.Groups["episodenum"].Value))).ToList();
                    }
                  }
                  if (episodeStubs != null && episodeStubs.Count > 0)
                  {
                    Stubs.SeriesEpisodeStub mergedEpisode = null;
                    if (isStub)
                    {
                      if (episodeStubs.Count == 1)
                      {
                        mergedEpisode = episodeStubs.First();
                      }
                      else
                      {
                        Stubs.SeriesEpisodeStub episode = episodeStubs.First();
                        mergedEpisode = new Stubs.SeriesEpisodeStub();
                        mergedEpisode.Actors = episode.Actors;
                        mergedEpisode.Aired = episode.Aired;
                        mergedEpisode.Credits = episode.Credits;
                        mergedEpisode.Director = episode.Director;
                        mergedEpisode.DisplayEpisode = episode.DisplayEpisode;
                        mergedEpisode.DisplaySeason = episode.DisplaySeason;
                        mergedEpisode.EpBookmark = episode.EpBookmark;
                        mergedEpisode.FileInfo = episode.FileInfo;
                        mergedEpisode.LastPlayed = episode.LastPlayed;
                        mergedEpisode.Mpaa = episode.Mpaa;
                        mergedEpisode.PlayCount = episode.PlayCount;
                        mergedEpisode.Premiered = episode.Premiered;
                        mergedEpisode.ProductionCodeNumber = episode.ProductionCodeNumber;
                        mergedEpisode.ResumePosition = episode.ResumePosition;
                        mergedEpisode.Season = episode.Season;
                        mergedEpisode.Sets = episode.Sets;
                        mergedEpisode.ShowTitle = episode.ShowTitle;
                        mergedEpisode.Status = episode.Status;
                        mergedEpisode.Studio = episode.Studio;
                        mergedEpisode.Tagline = episode.Tagline;
                        mergedEpisode.Thumb = episode.Thumb;
                        mergedEpisode.Top250 = episode.Top250;
                        mergedEpisode.Trailer = episode.Trailer;
                        mergedEpisode.Watched = episode.Watched;
                        mergedEpisode.Year = episode.Year;
                        mergedEpisode.Id = episode.Id;
                        mergedEpisode.UniqueId = episode.UniqueId;

                        //Merge episodes
                        mergedEpisode.Title = string.Join("; ", episodeStubs.OrderBy(e => e.Episodes.First()).Select(e => e.Title).ToArray());
                        mergedEpisode.Rating = episodeStubs.Where(e => e.Rating.HasValue).Sum(e => e.Rating.Value) / episodeStubs.Where(e => e.Rating.HasValue).Count(); // Average rating
                        mergedEpisode.Votes = episodeStubs.Where(e => e.Votes.HasValue).Sum(e => e.Votes.Value) / episodeStubs.Where(e => e.Votes.HasValue).Count();
                        mergedEpisode.Runtime = TimeSpan.FromSeconds(episodeStubs.Where(e => e.Runtime.HasValue).Sum(e => e.Runtime.Value.TotalSeconds));
                        mergedEpisode.Plot = string.Join("\r\n\r\n", episodeStubs.OrderBy(e => e.Episodes.First()).
                          Select(e => string.Format("{0,02}) {1}", e.Episodes.First(), e.Plot)).ToArray());
                        mergedEpisode.Outline = string.Join("\r\n\r\n", episodeStubs.OrderBy(e => e.Episodes.First()).
                          Select(e => string.Format("{0,02}) {1}", e.Episodes.First(), e.Outline)).ToArray());
                        mergedEpisode.Episodes = new HashSet<int>(episodeStubs.SelectMany(x => x.Episodes).ToList());
                        mergedEpisode.DvdEpisodes = new HashSet<decimal>(episodeStubs.SelectMany(x => x.DvdEpisodes).ToList());
                      }

                      IList<MultipleMediaItemAspect> providerResourceAspects;
                      if (MediaItemAspect.TryGetAspects(extractedAspectData, ProviderResourceAspect.Metadata, out providerResourceAspects))
                      {
                        MultipleMediaItemAspect providerResourceAspect = providerResourceAspects.First(pa => pa.GetAttributeValue<int>(ProviderResourceAspect.ATTR_TYPE) == ProviderResourceAspect.TYPE_STUB);
                        string mime = null;
                        if (mergedEpisode.FileInfo != null && mergedEpisode.FileInfo.Count > 0)
                          mime = MimeTypeDetector.GetMimeTypeFromExtension("file" + mergedEpisode.FileInfo.First().Container);
                        if (mime != null)
                          providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_MIME_TYPE, mime);
                      }

                      MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_TITLE, string.Format("{0} S{1:00}{2} {3}", series.ShowTitle, mergedEpisode.Season, mergedEpisode.Episodes.Select(e => "E" + e.ToString("00")), mergedEpisode.Title));
                      MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_SORT_TITLE, BaseInfo.GetSortTitle(mergedEpisode.Title));
                      MediaItemAspect.SetAttribute(extractedAspectData, MediaAspect.ATTR_RECORDINGTIME, mergedEpisode.Premiered.HasValue ? mergedEpisode.Premiered.Value : mergedEpisode.Year.HasValue ? mergedEpisode.Year.Value : (DateTime?)null);

                      if (mergedEpisode.FileInfo != null && mergedEpisode.FileInfo.Count > 0)
                      {
                        extractedAspectData.Remove(VideoStreamAspect.ASPECT_ID);
                        extractedAspectData.Remove(VideoAudioStreamAspect.ASPECT_ID);
                        extractedAspectData.Remove(SubtitleAspect.ASPECT_ID);
                        StubParser.ParseFileInfo(extractedAspectData, mergedEpisode.FileInfo, mergedEpisode.Title);
                      }
                    }

                    episodeNfoReader = new NfoSeriesEpisodeReader(_debugLogger, miNumber, forceQuickMode, isStub, _httpClient, _settings);
                    episodeNfoReader.SetEpisodeStubs(new List<Stubs.SeriesEpisodeStub> { mergedEpisode });
                  }
                }
              }
              if (series != null)
              {
                if (episodeNfoReader != null)
                {
                  episodeNfoReader.SetSeriesStubs(new List<Stubs.SeriesStub> { series });

                  // Then we store the found metadata in the MediaItemAspects. If we only found metadata that is
                  // not (yet) supported by our MediaItemAspects, this MetadataExtractor returns false.
                  if (!episodeNfoReader.TryWriteMetadata(extractedAspectData))
                  {
                    _debugLogger.Warn("[#{0}]: No metadata was written into MediaItemsAspects", miNumber);
                    return false;
                  }
                }
                else
                {
                  EpisodeInfo episode = new EpisodeInfo();
                  if (series.Id.HasValue)
                    episode.SeriesTvdbId = series.Id.Value;
                  if (series.Premiered.HasValue)
                    episode.SeriesFirstAired = series.Premiered.Value;
                  episode.SeriesName = series.ShowTitle;
                  episode.SetMetadata(extractedAspectData);
                }
              }
            }
            else
              _debugLogger.Warn("[#{0}]: No valid metadata found in series nfo-file", miNumber);
          }
        }
        else if (episodeNfoReader != null)
        {
          //Check reimport
          if (extractedAspectData.ContainsKey(ReimportAspect.ASPECT_ID))
          {
            EpisodeInfo reimport = new EpisodeInfo();
            reimport.FromMetadata(extractedAspectData);
            if (!VerifyEpisodeReimport(episodeNfoReader, reimport))
            {
              ServiceRegistration.Get<ILogger>().Info("NfoSeriesMetadataExtractor: Nfo episode metadata from resource '{0}' ignored because it does not match reimport {1}", mediaItemAccessor, reimport);
              return false;
            }
          }

          // Then we store the found metadata in the MediaItemAspects. If we only found metadata that is
          // not (yet) supported by our MediaItemAspects, this MetadataExtractor returns false.
          if (!episodeNfoReader.TryWriteMetadata(extractedAspectData))
          {
            _debugLogger.Warn("[#{0}]: No metadata was written into MediaItemsAspects", miNumber);
            return false;
          }
        }

        _debugLogger.Info("[#{0}]: Successfully finished extracting metadata", miNumber);
        ServiceRegistration.Get<ILogger>().Debug("NfoSeriesMetadataExtractor: Assigned nfo episode metadata for resource '{0}'", mediaItemAccessor);
        return true;
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Warn("NfoSeriesMetadataExtractor: Exception while extracting metadata for resource '{0}'; enable debug logging for more details.", mediaItemAccessor);
        _debugLogger.Error("[#{0}]: Exception while extracting metadata", e, miNumber);
        return false;
      }
    }

    /// <summary>
    /// Asynchronously tries to extract series metadata for the given <param name="mediaItemAccessor"></param>
    /// </summary>
    /// <param name="mediaItemAccessor">Points to the resource for which we try to extract metadata</param>
    /// <param name="extractedAspectData">Dictionary of <see cref="MediaItemAspect"/>s with the extracted metadata</param>
    /// <param name="importOnly">If <c>true</c>, nothing is downloaded from the internet</param>
    /// <returns><c>true</c> if metadata was found and stored into <param name="extractedAspectData"></param>, else <c>false</c></returns>
    private async Task<bool> TryExtractSeriesMetadataAsync(IResourceAccessor mediaItemAccessor, IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData, bool importOnly, bool forceQuickMode)
    {
      // Get a unique number for this call to TryExtractMetadataAsync. We use this to make reading the debug log easier.
      // This MetadataExtractor is called in parallel for multiple MediaItems so that the respective debug log entries
      // for one call are not contained one after another in debug log. We therefore prepend this number before every log entry.
      var miNumber = Interlocked.Increment(ref _lastMediaItemNumber);
      bool isStub = extractedAspectData.ContainsKey(StubAspect.ASPECT_ID);
      try
      {
        _debugLogger.Info("[#{0}]: Start extracting metadata for resource '{1}' (importOnly: {2}, forceQuickMode: {3})", miNumber, mediaItemAccessor, importOnly, forceQuickMode);

        // This MetadataExtractor only works for MediaItems accessible by an IFileSystemResourceAccessor.
        // Otherwise it is not possible to find a nfo-file in the MediaItem's directory or parent directory.
        if (!(mediaItemAccessor is IFileSystemResourceAccessor))
        {
          _debugLogger.Info("[#{0}]: Cannot extract metadata; mediaItemAccessor is not an IFileSystemResourceAccessor", miNumber);
          return false;
        }

        // Then we try to find an IFileSystemResourceAccessor pointing to the series nfo-file.
        IFileSystemResourceAccessor seriesNfoFsra;
        if (TryGetSeriesNfoSResourceAccessor(miNumber, mediaItemAccessor as IFileSystemResourceAccessor, out seriesNfoFsra))
        {
          // If we found one, we (asynchronously) extract the metadata into a stub object and, if metadata was found,
          // we store it into the episodeNfoReader so that the latter can store metadata from series and episode level into the MediaItemAspects.
          var seriesNfoReader = new NfoSeriesReader(_debugLogger, miNumber, forceQuickMode, false, false, _httpClient, _settings);
          using (seriesNfoFsra)
          {
            if (await seriesNfoReader.TryReadMetadataAsync(seriesNfoFsra).ConfigureAwait(false))
            {
              // Then we store the found metadata in the MediaItemAspects. If we only found metadata that is
              // not (yet) supported by our MediaItemAspects, this MetadataExtractor returns false.
              if (!seriesNfoReader.TryWriteMetadata(extractedAspectData))
              {
                _debugLogger.Warn("[#{0}]: No metadata was written into series MediaItemsAspects", miNumber);
                return false;
              }
              else
              {
                _debugLogger.Warn("[#{0}]: No valid metadata found in series nfo-file", miNumber);
              }
            }
          }
        }

        _debugLogger.Info("[#{0}]: Successfully finished extracting series metadata", miNumber);
        ServiceRegistration.Get<ILogger>().Debug("NfoSeriesMetadataExtractor: Assigned nfo series metadata for resource '{0}'", mediaItemAccessor);
        return true;
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Warn("NfoSeriesMetadataExtractor: Exception while extracting series metadata for resource '{0}'; enable debug logging for more details.", mediaItemAccessor);
        _debugLogger.Error("[#{0}]: Exception while extracting metadata", e, miNumber);
        return false;
      }
    }

    #endregion

    #endregion

    #region Logging helpers

    /// <summary>
    /// Logs version and setting information into <see cref="_debugLogger"/>
    /// </summary>
    protected override void LogSettings()
    {
      _debugLogger.Info("-------------------------------------------------------------");
      _debugLogger.Info("NfoSeriesMetadataExtractor v{0} instantiated", ServiceRegistration.Get<IPluginManager>().AvailablePlugins[PLUGIN_ID].Metadata.PluginVersion);
      _debugLogger.Info("Setttings:");
      _debugLogger.Info("   EnableDebugLogging: {0}", _settings.EnableDebugLogging);
      _debugLogger.Info("   WriteRawNfoFileIntoDebugLog: {0}", _settings.WriteRawNfoFileIntoDebugLog);
      _debugLogger.Info("   WriteStubObjectIntoDebugLog: {0}", _settings.WriteStubObjectIntoDebugLog);
      _debugLogger.Info("   SeriesNfoFileNames: {0}", String.Join(";", _settings.SeriesNfoFileNames));
      _debugLogger.Info("   NfoFileNameExtensions: {0}", String.Join(" ", _settings.NfoFileNameExtensions));
      _debugLogger.Info("   SeparatorCharacters: {0}", String.Join(" ", _settings.SeparatorCharacters));
      _debugLogger.Info("   IgnoreStrings: {0}", String.Join(";", _settings.IgnoreStrings));
      _debugLogger.Info("-------------------------------------------------------------");
    }

    #endregion

    #region IMetadataExtractor implementation

    public MetadataExtractorMetadata Metadata
    {
      get { return _metadata; }
    }

    public Task<bool> TryExtractMetadataAsync(IResourceAccessor mediaItemAccessor, IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData, bool forceQuickMode)
    {
      //if (extractedAspectData.ContainsKey(EpisodeAspect.ASPECT_ID))
      //  return false;

      return TryExtractEpsiodeMetadataAsync(mediaItemAccessor, extractedAspectData, forceQuickMode);
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

    #endregion
  }
}
