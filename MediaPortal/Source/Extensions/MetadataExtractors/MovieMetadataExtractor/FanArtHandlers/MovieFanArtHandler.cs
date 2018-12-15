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

namespace MediaPortal.Extensions.MetadataExtractors.MovieMetadataExtractor
{
  public class MovieFanArtHandler : BaseFanArtHandler
  {
    #region Constants

    private static readonly Guid[] FANART_ASPECTS = { MovieAspect.ASPECT_ID, PersonAspect.ASPECT_ID, CharacterAspect.ASPECT_ID, CompanyAspect.ASPECT_ID };

    /// <summary>
    /// GUID string for the movie FanArt handler.
    /// </summary>
    public const string FANARTHANDLER_ID_STR = "CAE4C776-725A-4804-8FDA-3DB43E28A22A";

    /// <summary>
    /// Movie FanArt handler GUID.
    /// </summary>
    public static Guid FANARTHANDLER_ID = new Guid(FANARTHANDLER_ID_STR);

    #endregion

    #region Constructor

    public MovieFanArtHandler()
      : base(new FanArtHandlerMetadata(FANARTHANDLER_ID, "Movie FanArt handler"), FANART_ASPECTS)
    {
    }

    #endregion

    #region Base overrides

    public override async Task CollectFanArtAsync(Guid mediaItemId, IDictionary<Guid, IList<MediaItemAspect>> aspects)
    {
      if (aspects.ContainsKey(MovieAspect.ASPECT_ID))
      {
        //Movies also handles movie collection fanart extraction
        await ExtractMovieFanArt(mediaItemId, aspects).ConfigureAwait(false);
        return;
      }

      if (MovieMetadataExtractor.SkipFanArtDownload || !AddToCache(mediaItemId))
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
        if (companyInfo.Type == CompanyAspect.COMPANY_PRODUCTION)
          info = companyInfo;
      }

      if (info != null)
        await OnlineMatcherService.Instance.DownloadMovieFanArtAsync(mediaItemId, info).ConfigureAwait(false);
    }

    #endregion

    #region Protected methods

    protected async Task ExtractMovieFanArt(Guid mediaItemId, IDictionary<Guid, IList<MediaItemAspect>> aspects)
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
          MovieMetadataExtractor.CacheLocalFanArt, MovieMetadataExtractor.CacheOfflineFanArt);
      }

      if (!shouldCacheLocal && MovieMetadataExtractor.SkipFanArtDownload)
        return; //Nothing to do

      MovieInfo movieInfo = new MovieInfo();
      movieInfo.FromMetadata(aspects);

      //Movie fanart
      if (AddToCache(mediaItemId))
      {
        //Actor fanart may be stored in the movie directory, so get the actors now
        IList<Tuple<Guid, string>> actors = null;
        if (MediaItemAspect.TryGetAspect(aspects, VideoAspect.Metadata, out SingleMediaItemAspect videoAspect))
        {
          var actorNames = videoAspect.GetCollectionAttribute<string>(VideoAspect.ATTR_ACTORS);
          if (actorNames != null)
            RelationshipExtractorUtils.TryGetMappedLinkedIds(PersonAspect.ROLE_ACTOR, aspects, actorNames.ToList(), out actors);
        }
        if (shouldCacheLocal)
          await ExtractMovieFolderFanArt(mediaItemLocator, mediaItemId, movieInfo.ToString(), actors).ConfigureAwait(false);
        if (!MovieMetadataExtractor.SkipFanArtDownload)
          await OnlineMatcherService.Instance.DownloadMovieFanArtAsync(mediaItemId, movieInfo).ConfigureAwait(false);
      }

      //Collection fanart
      if (RelationshipExtractorUtils.TryGetLinkedId(MovieCollectionAspect.ROLE_MOVIE_COLLECTION, aspects, out Guid collectionMediaItemId) &&
        AddToCache(collectionMediaItemId))
      {
        MovieCollectionInfo collectionInfo = movieInfo.CloneBasicInstance<MovieCollectionInfo>();
        if (shouldCacheLocal)
          await ExtractCollectionFolderFanArt(mediaItemLocator, collectionMediaItemId, collectionInfo.ToString()).ConfigureAwait(false);
        if (!MovieMetadataExtractor.SkipFanArtDownload)
          await OnlineMatcherService.Instance.DownloadMovieFanArtAsync(collectionMediaItemId, collectionInfo).ConfigureAwait(false);
      }
    }

    /// <summary>
    /// Gets all movie folder images and caches them in the <see cref="IFanArtCache"/> service.
    /// </summary>
    /// <param name="mediaItemLocator"><see cref="IResourceLocator>"/> that points to the file.</param>
    /// <param name="movieMediaItemId">Id of the media item.</param>
    /// <param name="title">Title of the media item.</param>
    /// <returns><see cref="Task"/> that completes when the images have been cached.</returns>
    protected async Task ExtractMovieFolderFanArt(IResourceLocator mediaItemLocator, Guid movieMediaItemId, string title, IList<Tuple<Guid, string>> actors)
    {
      //Get the file's directory
      var movieDirectory = ResourcePathHelper.Combine(mediaItemLocator.NativeResourcePath, "../");
      try
      {
        var mediaItemFileName = ResourcePathHelper.GetFileNameWithoutExtension(mediaItemLocator.NativeResourcePath.ToString()).ToLowerInvariant();
        FanArtPathCollection paths = null;
        IList<ResourcePath> potentialActorImages = null;
        using (IResourceAccessor accessor = new ResourceLocator(mediaItemLocator.NativeSystemId, movieDirectory).CreateAccessor())
          if (accessor is IFileSystemResourceAccessor fsra)
          {
            paths = GetMovieFolderFanArt(fsra, mediaItemFileName);
            //See if there's an actor fanart directory and try and get any actor fanart
            if (actors != null && actors.Count > 0 && fsra.ResourceExists(".actors"))
              using (IFileSystemResourceAccessor actorsDirectory = fsra.GetResource(".actors"))
                potentialActorImages = LocalFanartHelper.GetPotentialFanArtFiles(actorsDirectory);
          }

        if (paths != null)
          await SaveFolderImagesToCache(mediaItemLocator.NativeSystemId, paths, movieMediaItemId, title).ConfigureAwait(false);
        if (potentialActorImages != null)
          await SavePersonFolderImages(mediaItemLocator.NativeSystemId, potentialActorImages, actors).ConfigureAwait(false);
      }
      catch (Exception ex)
      {
        Logger.Warn("MovieFanArtHandler: Exception while reading folder images for '{0}'", ex, movieDirectory);
      }
    }

    /// <summary>
    /// Gets a <see cref="FanArtPathCollection"/> containing all matching fanart paths in the specified <see cref="ResourcePath"/>.
    /// </summary>
    /// <param name="movieDirectory"><see cref="IFileSystemResourceAccessor"/> that points to the movie directory.</param>
    /// <param name="filename">The file name of the media item to extract images for.</param>
    /// <returns><see cref="FanArtPathCollection"/> containing all matching paths.</returns>
    protected FanArtPathCollection GetMovieFolderFanArt(IFileSystemResourceAccessor movieDirectory, string filename)
    {
      FanArtPathCollection paths = new FanArtPathCollection();
      if (movieDirectory == null)
        return paths;

      //Get all fanart in the current directory
      List<ResourcePath> potentialFanArtFiles = LocalFanartHelper.GetPotentialFanArtFiles(movieDirectory);
      ExtractAllFanArtImages(potentialFanArtFiles, paths, filename);

      //Add extra backdrops in ExtraFanArt directory
      if (movieDirectory.ResourceExists("ExtraFanArt/"))
        using (IFileSystemResourceAccessor extraFanArtDirectory = movieDirectory.GetResource("ExtraFanArt/"))
          paths.AddRange(FanArtTypes.FanArt, LocalFanartHelper.GetPotentialFanArtFiles(extraFanArtDirectory));

      return paths;
    }

    /// <summary>
    /// Gets all collection folder images and caches them in the <see cref="IFanArtCache"/> service.
    /// </summary>
    /// <param name="mediaItemLocator"><see cref="IResourceLocator>"/> that points to the file.</param>
    /// <param name="collectionMediaItemId">Id of the series media item.</param>
    /// <param name="title">Title of the media item.</param>
    /// <returns><see cref="Task"/> that completes when the images have been cached.</returns>
    protected async Task ExtractCollectionFolderFanArt(IResourceLocator mediaItemLocator, Guid collectionMediaItemId, string title)
    {
      var collectionDirectory = ResourcePathHelper.Combine(mediaItemLocator.NativeResourcePath, "../../");
      var movieDirectory = ResourcePathHelper.Combine(mediaItemLocator.NativeResourcePath, "../");
      try
      {
        FanArtPathCollection paths = new FanArtPathCollection();
        using (IResourceAccessor accessor = new ResourceLocator(mediaItemLocator.NativeSystemId, collectionDirectory).CreateAccessor())
          paths.AddRange(GetCollectionFolderFanArt(accessor as IFileSystemResourceAccessor));
        using (IResourceAccessor accessor = new ResourceLocator(mediaItemLocator.NativeSystemId, movieDirectory).CreateAccessor())
          paths.AddRange(GetMovieFolderCollectionFanArt(accessor as IFileSystemResourceAccessor));
        await SaveFolderImagesToCache(mediaItemLocator.NativeSystemId, paths, collectionMediaItemId, title).ConfigureAwait(false);
      }
      catch (Exception ex)
      {
        Logger.Warn("MovieFanArtHandler: Exception while reading folder images for '{0}'", ex, collectionDirectory);
      }
    }

    /// <summary>
    /// Gets a <see cref="FanArtPathCollection"/> containing all matching collection fanart paths in the specified <see cref="ResourcePath"/>.
    /// </summary>
    /// <param name="movieDirectory"><see cref="IFileSystemResourceAccessor"/> that points to the collection directory.</param>
    /// <returns><see cref="FanArtPathCollection"/> containing all matching paths.</returns>
    protected FanArtPathCollection GetCollectionFolderFanArt(IFileSystemResourceAccessor collectionDirectory)
    {
      FanArtPathCollection paths = new FanArtPathCollection();
      if (collectionDirectory == null)
        return paths;

      List<ResourcePath> potentialFanArtFiles = LocalFanartHelper.GetPotentialFanArtFiles(collectionDirectory);
      ExtractAllFanArtImages(potentialFanArtFiles, paths);

      if (collectionDirectory.ResourceExists("ExtraFanArt/"))
        using (IFileSystemResourceAccessor extraFanArtDirectory = collectionDirectory.GetResource("ExtraFanArt/"))
          paths.AddRange(FanArtTypes.FanArt, LocalFanartHelper.GetPotentialFanArtFiles(extraFanArtDirectory));

      return paths;
    }

    /// <summary>
    /// Gets a <see cref="FanArtPathCollection"/> containing all matching collection fanart paths in the specified <see cref="ResourcePath"/>.
    /// </summary>
    /// <param name="movieDirectory"><see cref="IFileSystemResourceAccessor"/> that points to the movie directory.</param>
    /// <returns><see cref="FanArtPathCollection"/> containing all matching paths.</returns>
    protected FanArtPathCollection GetMovieFolderCollectionFanArt(IFileSystemResourceAccessor movieDirectory)
    {
      FanArtPathCollection paths = new FanArtPathCollection();
      if (movieDirectory == null)
        return paths;

      List<ResourcePath> potentialFanArtFiles = LocalFanartHelper.GetPotentialFanArtFiles(movieDirectory);
      ExtractAllFanArtImagesByPrefix(potentialFanArtFiles, paths, "movieset");

      return paths;
    }

    #endregion
  }
}
