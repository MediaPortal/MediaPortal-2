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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.FanArt;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess;
using MediaPortal.Common.Services.Settings;
using MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.Extractors;
using MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.Settings;
using MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.NfoReaders;
using MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.Stubs;

namespace MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors
{
  public class SeriesFanArtHandler : BaseFanArtHandler
  {
    #region Constants

    private static readonly Guid[] FANART_ASPECTS = { EpisodeAspect.ASPECT_ID };
    private static readonly NfoSeriesExtractor SERIES_EXTRACTOR = new NfoSeriesExtractor();

    /// <summary>
    /// Folder within a series' folder that holds further backdrops, as the media managers name it.
    /// </summary>
    private const string EXTRA_FANART_FOLDER = "ExtraFanArt/";

    /// <summary>
    /// The series reader's settings, read once and kept up to date while the server runs.
    /// </summary>
    private SettingsChangeWatcher<NfoSeriesMetadataExtractorSettings> _settingsWatcher;

    /// <summary>
    /// GUID string for the nfo series FanArt handler.
    /// </summary>
    public const string FANARTHANDLER_ID_STR = "F3D7E786-2BBE-4234-9E92-1D9DA306924C";

    /// <summary>
    /// Nfo Episode FanArt handler GUID.
    /// </summary>
    public static Guid FANARTHANDLER_ID = new Guid(FANARTHANDLER_ID_STR);

    #endregion

    #region Constructor

    public SeriesFanArtHandler()
      : base(new FanArtHandlerMetadata(FANARTHANDLER_ID, "NFO Series FanArt handler"), FANART_ASPECTS)
    {
    }

    #endregion

    #region Base overrides

    public override async Task CollectFanArtAsync(Guid mediaItemId, IDictionary<Guid, IList<MediaItemAspect>> aspects)
    {
      IResourceLocator mediaItemLocator = null;
      if (!BaseInfo.IsVirtualResource(aspects))
        mediaItemLocator = GetResourceLocator(aspects);

      if (!aspects.ContainsKey(EpisodeAspect.ASPECT_ID) || mediaItemLocator == null)
        return;

      IFanArtCache fanArtCache = ServiceRegistration.Get<IFanArtCache>();
      using (IResourceAccessor mediaItemAccessor = mediaItemLocator.CreateAccessor())
      {
        EpisodeInfo episodeInfo = new EpisodeInfo();
        if (!episodeInfo.FromMetadata(aspects))
          return;

        //Episode fanart
        if (AddToCache(mediaItemId))
        {
          var existingThumbs = fanArtCache.GetFanArtFiles(mediaItemId, FanArtTypes.Thumbnail);
          int? season = episodeInfo.SeasonNumber;
          int? episode = episodeInfo.EpisodeNumbers != null && episodeInfo.EpisodeNumbers.Any() ? episodeInfo.EpisodeNumbers.First() : (int?)null;
          if (!existingThumbs.Any()) //Only get thumb if needed for better performance
          {
            NfoSeriesEpisodeReader episodeReader = await SERIES_EXTRACTOR.TryGetNfoSeriesEpisodeReaderAsync(mediaItemAccessor, season, episode, true).ConfigureAwait(false);
            if (episodeReader != null)
            {
              var stubs = episodeReader.GetEpisodeStubs();
              var mainStub = stubs?.FirstOrDefault();
              if (mainStub?.Thumb != null)
              {
                await fanArtCache.TrySaveFanArt(mediaItemId, episodeInfo.ToString(), FanArtTypes.Thumbnail, p => TrySaveFileImage(mainStub.Thumb, p, "Thumb", "Nfo.")).ConfigureAwait(false);
              }
            }
          }

          // The pictures a media manager filed next to the episode file. The reference reads them from that
          // folder before it asks anything online, and it names each kind by the file's own name, so an episode
          // called "s01e01.mkv" is allowed a "s01e01-clearart.png" beside it.
          if (CachesFolderPictures(mediaItemLocator))
            await SaveFolderPicturesAsync(mediaItemLocator.NativeSystemId,
                ResourcePathHelper.Combine(mediaItemLocator.NativeResourcePath, "../"), mediaItemId,
                episodeInfo.ToString(),
                ResourcePathHelper.GetFileNameWithoutExtension(mediaItemLocator.NativeResourcePath.ToString())
                    .ToLowerInvariant(), EXTRA_FANART_FOLDER).ConfigureAwait(false);
        }

        //Series fanart
        if (RelationshipExtractorUtils.TryGetLinkedId(SeriesAspect.ROLE_SERIES, aspects, out Guid seriesMediaItemId))
        {
          IList<Tuple<Guid, string>> actors = GetActors(aspects);
          RelationshipExtractorUtils.TryGetLinkedId(SeasonAspect.ROLE_SEASON, aspects, out Guid seasonMediaItemId);

          //Check if loading nfo is needed
          if ((actors?.All(a => IsInCache(a.Item1)) ?? true) && IsInCache(seriesMediaItemId) && (seasonMediaItemId == Guid.Empty || IsInCache(seasonMediaItemId)))
            return; //Everything was already saved

          // The show's own pictures and this season's, read from the folders they are filed in. Before the .nfo
          // file, because a shelf of episodes without one is common, and in the reference the walk does not
          // care whether the file is there either.
          if (CachesFolderPictures(mediaItemLocator))
            await CollectShowFolderPictures(mediaItemLocator, episodeInfo, seriesMediaItemId, seasonMediaItemId)
                .ConfigureAwait(false);

          NfoSeriesReader seriesNfoReader = await SERIES_EXTRACTOR.TryGetNfoSeriesReaderAsync(mediaItemAccessor, true).ConfigureAwait(false);
          if (seriesNfoReader != null)
          {
            var stubs = seriesNfoReader.GetSeriesStubs();
            var mainStub = stubs?.FirstOrDefault();
            if (AddToCache(seriesMediaItemId))
            {
              var series = episodeInfo.CloneBasicInstance<SeriesInfo>();
              if (mainStub?.Thumbs?.Count > 0)
                await TrySaveThumbStubs(fanArtCache, mainStub.Thumbs, null, seriesMediaItemId, series.ToString());
            }

            if (seasonMediaItemId != Guid.Empty && episodeInfo.SeasonNumber.HasValue && AddToCache(seasonMediaItemId))
            {
              var season = episodeInfo.CloneBasicInstance<SeasonInfo>();
              if (mainStub?.Thumbs?.Count > 0)
                await TrySaveThumbStubs(fanArtCache, mainStub.Thumbs, episodeInfo.SeasonNumber, seasonMediaItemId, season.ToString());
            }


            //Actor fanart
            //We only want the series actors because thumb loading is disabled on episode actors for performance reasons, so we might need to
            //load the series nfo multiple time before we have all actors depending on what actors are in the episode
            if (actors != null)
              foreach (var actor in actors)
              {
                if (!IsInCache(actor.Item1))
                {
                  var existingThumbs = fanArtCache.GetFanArtFiles(actor.Item1, FanArtTypes.Thumbnail);
                  var actorStub = mainStub?.Actors?.FirstOrDefault(a => string.Equals(a?.Name, actor.Item2, StringComparison.InvariantCultureIgnoreCase));
                  if (actorStub != null || existingThumbs.Any()) //We have a thumb already or no thumb is available, so no need to check again
                    AddToCache(actor.Item1);

                  if (actorStub?.Thumb != null)
                  {
                    await fanArtCache.TrySaveFanArt(actor.Item1, actor.Item2, FanArtTypes.Thumbnail, p => TrySaveFileImage(actorStub.Thumb, p, "Thumb", "Nfo.")).ConfigureAwait(false);
                  }
                }
              }
          }
        }
      }
    }

    #endregion

    #region Protected methods

    /// <summary>
    /// Whether the pictures lying next to an episode are to be copied into the cache.
    /// </summary>
    /// <param name="mediaItemLocator">Where the episode stands.</param>
    /// <returns><c>true</c> when the share the episode stands on is one the household has asked to be copied.</returns>
    protected bool CachesFolderPictures(IResourceLocator mediaItemLocator)
    {
      NfoSeriesMetadataExtractorSettings settings = GetSettings();
      return ShouldCacheLocalFanArt(mediaItemLocator.NativeResourcePath, settings.CacheLocalFanArt,
          settings.CacheOfflineFanArt);
    }

    /// <summary>
    /// The series reader's settings, watched, so that a switch thrown on the settings screen is seen by the next
    /// episode the importer reaches rather than waiting for the server to come back.
    /// </summary>
    protected NfoSeriesMetadataExtractorSettings GetSettings()
    {
      SettingsChangeWatcher<NfoSeriesMetadataExtractorSettings> watcher = _settingsWatcher;
      if (watcher == null)
      {
        watcher = new SettingsChangeWatcher<NfoSeriesMetadataExtractorSettings>();
        SettingsChangeWatcher<NfoSeriesMetadataExtractorSettings> existing =
            Interlocked.CompareExchange(ref _settingsWatcher, watcher, null);
        if (existing != null)
        {
          watcher.Dispose();
          watcher = existing;
        }
      }
      return watcher.Settings;
    }

    /// <summary>
    /// Stores the pictures filed for a series, and the ones filed for the season the episode lies in.
    /// </summary>
    /// <param name="mediaItemLocator">Where the episode file is.</param>
    /// <param name="episodeInfo">The episode, which carries the season number and the names to write beside
    /// the pictures.</param>
    /// <param name="seriesMediaItemId">The show the episode belongs to.</param>
    /// <param name="seasonMediaItemId">The season the episode belongs to, or <see cref="Guid.Empty"/> when the
    /// library does not know one.</param>
    /// <returns>A <see cref="Task"/> that completes when both folders have been walked.</returns>
    /// <remarks>
    /// <para>
    /// Which folder holds a show's own pictures is not the same question in every shelf: filed as
    /// <c>Show/Season 01/file.mkv</c> they lie one folder higher than filed as <c>Show/file.mkv</c>. The
    /// reference asks the folders themselves - a show's folder is one that holds season folders - and this asks
    /// the same question the same way.
    /// </para>
    /// <para>
    /// What is left out, and said out loud rather than silently: a media manager that files a season's art under
    /// the season's name in front of it - <c>specials-clearart.png</c> - is not read, only the plain names. The
    /// reference knows those prefixes too; our shelves had none of them when this was written, and a wrong
    /// picture is worse to a household than a missing one.
    /// </para>
    /// </remarks>
    protected async Task CollectShowFolderPictures(IResourceLocator mediaItemLocator, EpisodeInfo episodeInfo,
        Guid seriesMediaItemId, Guid seasonMediaItemId)
    {
      string nativeSystemId = mediaItemLocator.NativeSystemId;
      ResourcePath episodeDirectory = ResourcePathHelper.Combine(mediaItemLocator.NativeResourcePath, "../");
      ResourcePath aboveEpisodeDirectory = ResourcePathHelper.Combine(mediaItemLocator.NativeResourcePath, "../../");

      bool episodeFolderIsTheSeason;
      using (IResourceAccessor episodeAccessor = new ResourceLocator(nativeSystemId, episodeDirectory).CreateAccessor())
      using (IResourceAccessor aboveAccessor = new ResourceLocator(nativeSystemId, aboveEpisodeDirectory).CreateAccessor())
        episodeFolderIsTheSeason =
            !LocalFanartHelper.IsSeriesFolder(aboveAccessor as IFileSystemResourceAccessor, episodeInfo.SeasonNumber)
            && LocalFanartHelper.IsSeriesFolder(episodeAccessor as IFileSystemResourceAccessor, episodeInfo.SeasonNumber);

      ResourcePath showDirectory = episodeFolderIsTheSeason ? episodeDirectory : aboveEpisodeDirectory;
      await SaveFolderPicturesAsync(nativeSystemId, showDirectory, seriesMediaItemId,
          episodeInfo.CloneBasicInstance<SeriesInfo>().ToString(), null, EXTRA_FANART_FOLDER).ConfigureAwait(false);

      // A shelf filed flat has no folder of its own for a season, so there is nothing to read for one.
      if (episodeFolderIsTheSeason && seasonMediaItemId != Guid.Empty && episodeInfo.SeasonNumber.HasValue)
        await SaveFolderPicturesAsync(nativeSystemId, episodeDirectory, seasonMediaItemId,
            episodeInfo.CloneBasicInstance<SeasonInfo>().ToString()).ConfigureAwait(false);
    }

    protected IList<Tuple<Guid, string>> GetActors(IDictionary<Guid, IList<MediaItemAspect>> aspects)
    {
      IList<Tuple<Guid, string>> actors = null;
      if (MediaItemAspect.TryGetAspect(aspects, VideoAspect.Metadata, out SingleMediaItemAspect videoAspect))
      {
        var actorNames = videoAspect.GetCollectionAttribute<string>(VideoAspect.ATTR_ACTORS);
        if (actorNames != null)
          RelationshipExtractorUtils.TryGetMappedLinkedIds(PersonAspect.ROLE_ACTOR, aspects, actorNames.ToList(), out actors);
      }
      return actors;
    }

    protected async Task<bool> TrySaveThumbStubs(IFanArtCache fanArtCache, HashSet<SeriesThumbStub> thumbs, int? season, Guid mediaItemId, string mediaItemName)
    {
      if (thumbs == null || thumbs.Count == 0)
        return false;

      HashSet<byte[]> posters = new HashSet<byte[]>();
      HashSet<byte[]> banners = new HashSet<byte[]>();
      HashSet<byte[]> fanart = new HashSet<byte[]>();
      foreach (var thumbStub in thumbs)
      {
        if ((!season.HasValue && thumbStub.Season == null) || (season.HasValue && thumbStub.Season == season))
        {
          if (thumbStub.Aspect == SeriesThumbStub.ThumbAspect.Poster || !thumbStub.Aspect.HasValue)
            posters.Add(thumbStub.Thumb);
          else if (thumbStub.Aspect == SeriesThumbStub.ThumbAspect.Banner)
            banners.Add(thumbStub.Thumb);
          else if (thumbStub.Aspect == SeriesThumbStub.ThumbAspect.Fanart)
            fanart.Add(thumbStub.Thumb);
        }
      }

      await TrySaveFanArt(fanArtCache, FanArtTypes.Poster, "Poster", posters, mediaItemId, mediaItemName);
      await TrySaveFanArt(fanArtCache, FanArtTypes.Banner, "Banner", banners, mediaItemId, mediaItemName);
      await TrySaveFanArt(fanArtCache, FanArtTypes.FanArt, "FanArt", fanart, mediaItemId, mediaItemName);

      return true;
    }

    protected async Task<bool> TrySaveFanArt(IFanArtCache fanArtCache, string fanArtType, string fanArtName, HashSet<byte[]> fanArtData, Guid mediaItemId, string mediaName)
    {
      if (fanArtData == null || fanArtData.Count == 0)
        return false;

      bool addCount = fanArtData.Count > 1;
      int count = 0;
      foreach (var data in fanArtData)
        await fanArtCache.TrySaveFanArt(mediaItemId, mediaName, fanArtType, p => TrySaveFileImage(data, p, $"{fanArtName}{(addCount ? (count++).ToString() : "")}", "Nfo."));

      return true;
    }
    
    #endregion
  }

  public class NfoSeriesExtractor : NfoSeriesExtractorBase
  {
    public new Task<NfoSeriesEpisodeReader> TryGetNfoSeriesEpisodeReaderAsync(IResourceAccessor mediaItemAccessor, int? season, int? episode, bool includeFanart)
    {
      return base.TryGetNfoSeriesEpisodeReaderAsync(mediaItemAccessor, season, episode, includeFanart);
    }

    public new Task<NfoSeriesReader> TryGetNfoSeriesReaderAsync(IResourceAccessor mediaItemAccessor, bool includeFanart)
    {
      return base.TryGetNfoSeriesReaderAsync(mediaItemAccessor, includeFanart);
    }
  }
}
