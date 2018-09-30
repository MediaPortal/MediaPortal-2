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

using MediaPortal.Common.FanArt;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess;
using MediaPortal.Extensions.OnlineLibraries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MediaPortal.Extensions.MetadataExtractors.SeriesMetadataExtractor
{
  public class SeriesFanArtHandler : BaseFanArtHandler
  {
    #region Constants

    private static readonly Guid[] FANART_ASPECTS = { EpisodeAspect.ASPECT_ID, PersonAspect.ASPECT_ID, CharacterAspect.ASPECT_ID, CompanyAspect.ASPECT_ID };

    /// <summary>
    /// GUID string for the series FanArt handler.
    /// </summary>
    public const string FANARTHANDLER_ID_STR = "5FC11696-48B0-480F-9557-53AE1FE6D395";

    /// <summary>
    /// Episode FanArt handler GUID.
    /// </summary>
    public static Guid FANARTHANDLER_ID = new Guid(FANARTHANDLER_ID_STR);

    #endregion

    #region Constructor

    public SeriesFanArtHandler()
      : base(new FanArtHandlerMetadata(FANARTHANDLER_ID, "Series FanArt handler"), FANART_ASPECTS)
    {
    }

    #endregion

    #region Base overrides

    public override async Task CollectFanArtAsync(Guid mediaItemId, IDictionary<Guid, IList<MediaItemAspect>> aspects)
    {
      if (aspects.ContainsKey(EpisodeAspect.ASPECT_ID))
      {
        //Episodes also handle season and series fanart extraction
        await ExtractEpisodeFanArt(mediaItemId, aspects).ConfigureAwait(false);
        return;
      }

      if (SeriesMetadataExtractor.SkipFanArtDownload || !AddToCache(mediaItemId))
        return;

      BaseInfo info = null;
      if (aspects.ContainsKey(PersonAspect.ASPECT_ID))
      {
        PersonInfo personInfo = new PersonInfo();
        personInfo.FromMetadata(aspects);
        if (personInfo.Occupation == PersonAspect.OCCUPATION_ACTOR || personInfo.Occupation == PersonAspect.OCCUPATION_DIRECTOR ||
          personInfo.Occupation == PersonAspect.OCCUPATION_WRITER)
          info = personInfo;
      }
      else if (aspects.ContainsKey(CharacterAspect.ASPECT_ID))
      {
        CharacterInfo characterInfo = new CharacterInfo();
        characterInfo.FromMetadata(aspects);
        info = characterInfo;
      }
      else if (aspects.ContainsKey(CompanyAspect.ASPECT_ID))
      {
        CompanyInfo companyInfo = new CompanyInfo();
        companyInfo.FromMetadata(aspects);
        if (companyInfo.Type == CompanyAspect.COMPANY_PRODUCTION || companyInfo.Type == CompanyAspect.COMPANY_TV_NETWORK)
          info = companyInfo;
      }

      if(info != null)
        await OnlineMatcherService.Instance.DownloadSeriesFanArtAsync(mediaItemId, info).ConfigureAwait(false);
    }

    #endregion

    #region Protected methods

    protected async Task ExtractEpisodeFanArt(Guid mediaItemId, IDictionary<Guid, IList<MediaItemAspect>> aspects)
    {
      bool shouldCacheLocal = false;
      IResourceLocator mediaItemLocator = null;

      if (!BaseInfo.IsVirtualResource(aspects))
      {
        mediaItemLocator = GetResourceLocator(aspects);
        if (mediaItemLocator == null)
          return;

        //Whether local fanart should be stored in the fanart cache
        shouldCacheLocal = ShouldCacheLocalFanArt(mediaItemLocator.NativeResourcePath,
          SeriesMetadataExtractor.CacheLocalFanArt, SeriesMetadataExtractor.CacheOfflineFanArt);
      }

      if (!shouldCacheLocal && SeriesMetadataExtractor.SkipFanArtDownload)
        return; //Nothing to do

      EpisodeInfo episodeInfo = new EpisodeInfo();
      episodeInfo.FromMetadata(aspects);

      //Episode fanart
      if (AddToCache(mediaItemId))
      {
        if (shouldCacheLocal)
          await ExtractEpisodeFolderFanArt(mediaItemLocator, mediaItemId, episodeInfo.ToString()).ConfigureAwait(false);
        if (!SeriesMetadataExtractor.SkipFanArtDownload)
          await OnlineMatcherService.Instance.DownloadSeriesFanArtAsync(mediaItemId, episodeInfo).ConfigureAwait(false);
      }

      //Actor fanart may be stored in the season or series directory, so get the actors now
      IList<Tuple<Guid, string>> actors = null;
      if (MediaItemAspect.TryGetAspect(aspects, VideoAspect.Metadata, out SingleMediaItemAspect videoAspect))
      {
        var actorNames = videoAspect.GetCollectionAttribute<string>(VideoAspect.ATTR_ACTORS);
        if (actorNames != null)
          RelationshipExtractorUtils.TryGetMappedLinkedIds(PersonAspect.ROLE_ACTOR, aspects, actorNames.ToList(), out actors);
      }

      //Take advantage of the audio language being known and download season and series too

      //Season fanart
      if (RelationshipExtractorUtils.TryGetLinkedId(SeasonAspect.ROLE_SEASON, aspects, out Guid seasonMediaItemId) &&
        AddToCache(seasonMediaItemId))
      {
        SeasonInfo seasonInfo = episodeInfo.CloneBasicInstance<SeasonInfo>();
        if (shouldCacheLocal)
          await ExtractSeasonFolderFanArt(mediaItemLocator, seasonMediaItemId, seasonInfo.ToString(), seasonInfo.SeasonNumber, actors).ConfigureAwait(false);
        if (!SeriesMetadataExtractor.SkipFanArtDownload)
          await OnlineMatcherService.Instance.DownloadSeriesFanArtAsync(seasonMediaItemId, seasonInfo).ConfigureAwait(false);
      }

      //Series fanart
      if (RelationshipExtractorUtils.TryGetLinkedId(SeriesAspect.ROLE_SERIES, aspects, out Guid seriesMediaItemId) &&
        AddToCache(seriesMediaItemId))
      {
        SeriesInfo seriesInfo = episodeInfo.CloneBasicInstance<SeriesInfo>();
        if (shouldCacheLocal)
          await ExtractSeriesFolderFanArt(mediaItemLocator, seriesMediaItemId, seriesInfo.ToString(), actors).ConfigureAwait(false);
        if (!SeriesMetadataExtractor.SkipFanArtDownload)
          await OnlineMatcherService.Instance.DownloadSeriesFanArtAsync(seriesMediaItemId, seriesInfo).ConfigureAwait(false);
      }
    }

    /// <summary>
    /// Gets all episode folder images and caches them in the <see cref="IFanArtCache"/> service.
    /// </summary>
    /// <param name="mediaItemLocator"><see cref="IResourceLocator>"/> that points to the file.</param>
    /// <param name="episodeMediaItemId">Id of the episode media item.</param>
    /// <param name="title">Title of the media item.</param>
    /// <returns><see cref="Task"/> that completes when the images have been cached.</returns>
    protected async Task ExtractEpisodeFolderFanArt(IResourceLocator mediaItemLocator, Guid episodeMediaItemId, string title)
    {
      var episodeDirectory = ResourcePathHelper.Combine(mediaItemLocator.NativeResourcePath, "../");
      try
      {
        var mediaItemFileName = ResourcePathHelper.GetFileNameWithoutExtension(mediaItemLocator.NativeResourcePath.ToString()).ToLowerInvariant();
        FanArtPathCollection paths;
        using (IResourceAccessor accessor = new ResourceLocator(mediaItemLocator.NativeSystemId, episodeDirectory).CreateAccessor())
          paths = GetEpisodeFolderFanArt(accessor as IFileSystemResourceAccessor, mediaItemFileName);
        await SaveFolderImagesToCache(mediaItemLocator.NativeSystemId, paths, episodeMediaItemId, title).ConfigureAwait(false);
      }
      catch (Exception ex)
      {
        Logger.Warn("SeriesFanArtHandler: Exception while reading folder images for '{0}'", ex, episodeDirectory);
      }
    }

    /// <summary>
    /// Gets a <see cref="FanArtPathCollection"/> containing all matching episode fanart paths in the specified <see cref="ResourcePath"/>.
    /// </summary>
    /// <param name="episodeDirectory"><see cref="IFileSystemResourceAccessor"/> that points to the episode directory.</param>
    /// <param name="filename">The file name of the media item to extract images for.</param>
    /// <returns><see cref="FanArtPathCollection"/> containing all matching paths.</returns>
    protected FanArtPathCollection GetEpisodeFolderFanArt(IFileSystemResourceAccessor episodeDirectory, string filename)
    {
      FanArtPathCollection paths = new FanArtPathCollection();
      if (episodeDirectory == null)
        return paths;

      List<ResourcePath> potentialFanArtFiles = LocalFanartHelper.GetPotentialFanArtFiles(episodeDirectory);
      paths.AddRange(FanArtTypes.Thumbnail,
        LocalFanartHelper.FilterPotentialFanArtFilesByNameOrPrefix(potentialFanArtFiles, "thumb", filename + "-thumb"));

      return paths;
    }

    /// <summary>
    /// Gets all series folder images and caches them in the <see cref="IFanArtCache"/> service.
    /// </summary>
    /// <param name="mediaItemLocator"><see cref="IResourceLocator>"/> that points to the file.</param>
    /// <param name="seriesMediaItemId">Id of the series media item.</param>
    /// <param name="title">Title of the media item.</param>
    /// <param name="actors">Collection of actor ids and names.</param>
    /// <returns><see cref="Task"/> that completes when the images have been cached.</returns>
    protected async Task ExtractSeriesFolderFanArt(IResourceLocator mediaItemLocator, Guid seriesMediaItemId, string title, IList<Tuple<Guid, string>> actors)
    {
      var seriesDirectory = ResourcePathHelper.Combine(mediaItemLocator.NativeResourcePath, "../../");

      try
      {
        FanArtPathCollection paths = null;
        IList<ResourcePath> potentialActorImages = null;
        using (IResourceAccessor accessor = new ResourceLocator(mediaItemLocator.NativeSystemId, seriesDirectory).CreateAccessor())
          if (accessor is IFileSystemResourceAccessor fsra)
          {
            paths = GetSeriesFolderFanArt(fsra);
            //See if there's an actor fanart directory and try and get any actor fanart
            if (actors != null && actors.Count > 0 && fsra.ResourceExists(".actors"))
              using (IFileSystemResourceAccessor actorsDirectory = fsra.GetResource(".actors"))
                potentialActorImages = LocalFanartHelper.GetPotentialFanArtFiles(actorsDirectory);
          }

        if (paths != null)
          await SaveFolderImagesToCache(mediaItemLocator.NativeSystemId, paths, seriesMediaItemId, title).ConfigureAwait(false);
        if (potentialActorImages != null)
          await SavePersonFolderImages(mediaItemLocator.NativeSystemId, potentialActorImages, actors).ConfigureAwait(false);
      }
      catch (Exception ex)
      {
        Logger.Warn("SeriesFanArtHandler: Exception while reading folder images for '{0}'", ex, seriesDirectory);
      }
    }

    /// <summary>
    /// Gets a <see cref="FanArtPathCollection"/> containing all matching series fanart paths in the specified <see cref="ResourcePath"/>.
    /// </summary>
    /// <param name="seriesDirectory"><see cref="IFileSystemResourceAccessor"/> that points to the series directory.</param>
    /// <returns><see cref="FanArtPathCollection"/> containing all matching paths.</returns>
    protected FanArtPathCollection GetSeriesFolderFanArt(IFileSystemResourceAccessor seriesDirectory)
    {
      FanArtPathCollection paths = new FanArtPathCollection();
      if (seriesDirectory == null)
        return paths;

      if (seriesDirectory != null)
      {
        List<ResourcePath> potentialFanArtFiles = LocalFanartHelper.GetPotentialFanArtFiles(seriesDirectory);
        ExtractAllFanArtImages(potentialFanArtFiles, paths);

        if (seriesDirectory.ResourceExists("ExtraFanArt/"))
          using (IFileSystemResourceAccessor extraFanArtDirectory = seriesDirectory.GetResource("ExtraFanArt/"))
            paths.AddRange(FanArtTypes.FanArt, LocalFanartHelper.GetPotentialFanArtFiles(extraFanArtDirectory));
      }

      return paths;
    }

    /// <summary>
    /// Gets all season folder images and caches them in the <see cref="IFanArtCache"/> service.
    /// </summary>
    /// <param name="mediaItemLocator"><see cref="IResourceLocator>"/> that points to the file.</param>
    /// <param name="seasonMediaItemId">Id of the season media item.</param>
    /// <param name="title">Title of the media item.</param>
    /// <param name="seasonNumber">Season number.</param>
    /// <param name="actors">Collection of actor ids and names.</param>
    /// <returns><see cref="Task"/> that completes when the images have been cached.</returns>
    protected async Task ExtractSeasonFolderFanArt(IResourceLocator mediaItemLocator, Guid seasonMediaItemId, string title, int? seasonNumber, IList<Tuple<Guid, string>> actors)
    {
      var seasonDirectory = ResourcePathHelper.Combine(mediaItemLocator.NativeResourcePath, "../");
      try
      {
        FanArtPathCollection paths = null;
        IList<ResourcePath> potentialActorImages = null;
        using (IResourceAccessor accessor = new ResourceLocator(mediaItemLocator.NativeSystemId, seasonDirectory).CreateAccessor())
          if (accessor is IFileSystemResourceAccessor fsra)
          {
            paths = GetSeasonFolderFanArt(fsra, seasonNumber);
            //See if there's an actor fanart directory and try and get any actor fanart
            if (actors != null && actors.Count > 0 && fsra.ResourceExists(".actors"))
              using (IFileSystemResourceAccessor actorsDirectory = fsra.GetResource(".actors"))
                potentialActorImages = LocalFanartHelper.GetPotentialFanArtFiles(actorsDirectory);
          }

        if (paths != null)
          await SaveFolderImagesToCache(mediaItemLocator.NativeSystemId, paths, seasonMediaItemId, title).ConfigureAwait(false);
        if (potentialActorImages != null)
          await SavePersonFolderImages(mediaItemLocator.NativeSystemId, potentialActorImages, actors).ConfigureAwait(false);
      }
      catch (Exception ex)
      {
        Logger.Warn("SeriesFanArtHandler: Exception while reading folder images for '{0}'", ex, seasonDirectory);
      }
    }

    /// <summary>
    /// Gets a <see cref="FanArtPathCollection"/> containing all matching season fanart paths in the specified <see cref="ResourcePath"/>.
    /// </summary>
    /// <param name="seasonDirectory"><see cref="IFileSystemResourceAccessor"/> that points to the season directory.</param>
    /// <param name="seasonNumber">Season number.</param>
    /// <returns><see cref="FanArtPathCollection"/> containing all matching paths.</returns>
    protected FanArtPathCollection GetSeasonFolderFanArt(IFileSystemResourceAccessor seasonDirectory, int? seasonNumber)
    {
      FanArtPathCollection paths = new FanArtPathCollection();
      if (seasonDirectory == null)
        return paths;

      List<ResourcePath> potentialFanArtFiles = LocalFanartHelper.GetPotentialFanArtFiles(seasonDirectory);
      ExtractAllFanArtImages(potentialFanArtFiles, paths);

      if (!seasonNumber.HasValue || !seasonDirectory.ResourceExists("../"))
        return paths;

      //Try and populate any missing fanart from the series directory
      using (IFileSystemResourceAccessor seriesDirectory = seasonDirectory.GetResource("../"))
        potentialFanArtFiles = LocalFanartHelper.GetPotentialFanArtFiles(seriesDirectory);
      GetAdditionalSeasonFolderFanArt(paths, potentialFanArtFiles, seasonNumber.Value);

      return paths;
    }

    /// <summary>
    /// Tries to populate any empty fanart types in the specified <see cref="FanArtPathCollection"/> with image paths
    /// contained in <paramref name="potentialFanArtFiles"/> that start with 'season-all', 'season{<paramref name="seasonNumber"/>}' 
    /// or, if <paramref name="seasonNumber"/> is 0, 'season-specials'.
    /// </summary>
    /// <param name="paths">The <see cref="FanArtPathCollection"/> to add matching paths to.</param>
    /// <param name="potentialFanArtFiles">Collection of potential fanart paths.</param>
    /// <param name="seasonNumber">The season number.</param>
    protected void GetAdditionalSeasonFolderFanArt(FanArtPathCollection paths, ICollection<ResourcePath> potentialFanArtFiles, int seasonNumber)
    {
      if (potentialFanArtFiles == null || potentialFanArtFiles.Count == 0)
        return;

      string[] prefixes = new[]
      {
        string.Format("season{0:00}", seasonNumber),
        seasonNumber == 0 ? "season-specials" : "season-all"
      };

      if (paths.Count(FanArtTypes.Thumbnail) == 0)
        paths.AddRange(FanArtTypes.Thumbnail, LocalFanartHelper.FilterPotentialFanArtFilesByName(potentialFanArtFiles,
          LocalFanartHelper.THUMB_FILENAMES.SelectMany(f => prefixes.Select(p => p + "-" + f))));

      if (paths.Count(FanArtTypes.Poster) == 0)
        paths.AddRange(FanArtTypes.Poster, LocalFanartHelper.FilterPotentialFanArtFilesByName(potentialFanArtFiles,
          LocalFanartHelper.POSTER_FILENAMES.SelectMany(f => prefixes.Select(p => p + "-" + f))));

      if (paths.Count(FanArtTypes.Logo) == 0)
        paths.AddRange(FanArtTypes.Logo, LocalFanartHelper.FilterPotentialFanArtFilesByName(potentialFanArtFiles,
          LocalFanartHelper.LOGO_FILENAMES.SelectMany(f => prefixes.Select(p => p + "-" + f))));

      if (paths.Count(FanArtTypes.ClearArt) == 0)
        paths.AddRange(FanArtTypes.ClearArt, LocalFanartHelper.FilterPotentialFanArtFilesByName(potentialFanArtFiles,
          LocalFanartHelper.CLEARART_FILENAMES.SelectMany(f => prefixes.Select(p => p + "-" + f))));

      if (paths.Count(FanArtTypes.DiscArt) == 0)
        paths.AddRange(FanArtTypes.DiscArt, LocalFanartHelper.FilterPotentialFanArtFilesByName(potentialFanArtFiles,
          LocalFanartHelper.DISCART_FILENAMES.SelectMany(f => prefixes.Select(p => p + "-" + f))));

      if (paths.Count(FanArtTypes.Banner) == 0)
        paths.AddRange(FanArtTypes.Banner, LocalFanartHelper.FilterPotentialFanArtFilesByName(potentialFanArtFiles,
          LocalFanartHelper.BANNER_FILENAMES.SelectMany(f => prefixes.Select(p => p + "-" + f))));

      if (paths.Count(FanArtTypes.FanArt) == 0)
        paths.AddRange(FanArtTypes.FanArt, LocalFanartHelper.FilterPotentialFanArtFilesByName(potentialFanArtFiles,
          LocalFanartHelper.BACKDROP_FILENAMES.SelectMany(f => prefixes.Select(p => p + "-" + f))));
    }

    #endregion
  }
}
