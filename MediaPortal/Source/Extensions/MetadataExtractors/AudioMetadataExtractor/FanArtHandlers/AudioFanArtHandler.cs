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
using MediaPortal.Common.FanArt;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess;
using MediaPortal.Extensions.OnlineLibraries;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TagLib;

namespace MediaPortal.Extensions.MetadataExtractors.AudioMetadataExtractor
{
  public class AudioFanArtHandler : BaseFanArtHandler
  {
    #region Constants

    private static readonly Guid[] FANART_ASPECTS = { AudioAspect.ASPECT_ID, PersonAspect.ASPECT_ID, CompanyAspect.ASPECT_ID };

    /// <summary>
    /// GUID string for the audio FanArt handler.
    /// </summary>
    public const string FANARTHANDLER_ID_STR = "96725CF7-74AD-4A0C-8466-480B2320903D";

    /// <summary>
    /// Audio FanArt handler GUID.
    /// </summary>
    public static Guid FANARTHANDLER_ID = new Guid(FANARTHANDLER_ID_STR);

    #endregion

    public AudioFanArtHandler()
      : base(new FanArtHandlerMetadata(FANARTHANDLER_ID, "Audio FanArt handler"), FANART_ASPECTS)
    {
    }

    public override async Task CollectFanArtAsync(Guid mediaItemId, IDictionary<Guid, IList<MediaItemAspect>> aspects)
    {
      if (aspects.ContainsKey(AudioAspect.ASPECT_ID))
        await ExtractAlbumAndArtistFanArt(mediaItemId, aspects);

      if (AudioMetadataExtractor.SkipFanArtDownload || !AddToCache(mediaItemId))
        return;

      if (TryGetOnlineInfo(mediaItemId, aspects, out BaseInfo onlineInfo))
        await OnlineMatcherService.Instance.DownloadAudioFanArtAsync(mediaItemId, onlineInfo).ConfigureAwait(false);
    }

    protected bool TryGetOnlineInfo(Guid mediaItemId, IDictionary<Guid, IList<MediaItemAspect>> aspects, out BaseInfo onlineInfo)
    {
      onlineInfo = null;
      if (aspects.ContainsKey(PersonAspect.ASPECT_ID))
      {
        PersonInfo personInfo = new PersonInfo();
        personInfo.FromMetadata(aspects);
        if (personInfo.Occupation == PersonAspect.OCCUPATION_ARTIST || personInfo.Occupation == PersonAspect.OCCUPATION_COMPOSER)
          onlineInfo = personInfo;
      }
      else if (aspects.ContainsKey(CompanyAspect.ASPECT_ID))
      {
        CompanyInfo companyInfo = new CompanyInfo();
        companyInfo.FromMetadata(aspects);
        if (companyInfo.Type == CompanyAspect.COMPANY_MUSIC_LABEL)
          onlineInfo = companyInfo;
      }
      return onlineInfo != null;
    }

    protected async Task ExtractAlbumAndArtistFanArt(Guid mediaItemId, IDictionary<Guid, IList<MediaItemAspect>> aspects)
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
          AudioMetadataExtractor.CacheLocalFanArt, AudioMetadataExtractor.CacheOfflineFanArt);
      }

      if (!shouldCacheLocal && AudioMetadataExtractor.SkipFanArtDownload)
        return; //Nothing to do

      TrackInfo trackInfo = new TrackInfo();
      trackInfo.FromMetadata(aspects);
      AlbumInfo albumInfo = trackInfo.CloneBasicInstance<AlbumInfo>();
      string albumTitle = albumInfo.ToString();

      ResourcePath albumDirectory = null;
      if (shouldCacheLocal)
      {
        albumDirectory = ResourcePathHelper.Combine(mediaItemLocator.NativeResourcePath, "../");
        if (AudioMetadataExtractor.IsDiscFolder(albumTitle, albumDirectory.FileName))
          //Probably a CD folder so try next parent
          albumDirectory = ResourcePathHelper.Combine(albumDirectory, "../");
      }

      //Artist fanart may be stored in the album directory, so get the artists now
      IList<Tuple<Guid, string>> artists = GetArtists(aspects);

      //Album fanart
      if (RelationshipExtractorUtils.TryGetLinkedId(AudioAlbumAspect.ROLE_ALBUM, aspects, out Guid albumMediaItemId) &&
        AddToCache(albumMediaItemId))
      {
        if (shouldCacheLocal)
        {
          //If the track is not a stub, Store track tag images in the album
          if (!aspects.ContainsKey(ReimportAspect.ASPECT_ID) && MediaItemAspect.TryGetAttribute(aspects, MediaAspect.ATTR_ISSTUB, out bool isStub) && isStub == false)
            await ExtractTagFanArt(mediaItemLocator, albumMediaItemId, albumTitle);
          await ExtractAlbumFolderFanArt(mediaItemLocator.NativeSystemId, albumDirectory, albumMediaItemId, albumTitle, artists).ConfigureAwait(false);
        }
        if (!AudioMetadataExtractor.SkipFanArtDownload)
          await OnlineMatcherService.Instance.DownloadAudioFanArtAsync(albumMediaItemId, albumInfo).ConfigureAwait(false);
      }

      if (shouldCacheLocal && artists != null)
        await ExtractArtistFolderFanArt(mediaItemLocator.NativeSystemId, albumDirectory, artists).ConfigureAwait(false);
    }

    protected IList<Tuple<Guid, string>> GetArtists(IDictionary<Guid, IList<MediaItemAspect>> aspects)
    {
      IList<Tuple<Guid, string>> artists = null;
      if (MediaItemAspect.TryGetAspect(aspects, AudioAspect.Metadata, out SingleMediaItemAspect audioAspect))
      {
        var artistNames = audioAspect.GetCollectionAttribute<string>(AudioAspect.ATTR_ALBUMARTISTS);
        if (artistNames != null)
          RelationshipExtractorUtils.TryGetMappedLinkedIds(PersonAspect.ROLE_ALBUMARTIST, aspects, artistNames.ToList(), out artists);
      }
      return artists;
    }

    /// <summary>
    /// Reads all tag images and caches them in the <see cref="IFanArtCache"/> service.
    /// </summary>
    /// <param name="mediaItemLocator"><see cref="IResourceLocator>"/> that points to the file.</param>
    /// <param name="mediaItemId">Id of the media item.</param>
    /// <param name="title">Title of the media item.</param>
    /// <returns><see cref="Task"/> that completes when the images have been cached.</returns>
    protected async Task ExtractTagFanArt(IResourceLocator mediaItemLocator, Guid mediaItemId, string title)
    {
      try
      {
        //File based access
        using (IResourceAccessor mediaItemAccessor = mediaItemLocator.CreateAccessor())
        using (LocalFsResourceAccessorHelper rah = new LocalFsResourceAccessorHelper(mediaItemAccessor))
        using (rah.LocalFsResourceAccessor.EnsureLocalFileSystemAccess())
          await ExtractTagFanArt(rah.LocalFsResourceAccessor, mediaItemId, title).ConfigureAwait(false);
      }
      catch (Exception ex)
      {
        Logger.Warn("VideoFanArtHandler: Exception while reading MKV tag images for '{0}'", ex, mediaItemLocator.NativeResourcePath);
      }
    }

    /// <summary>
    /// Reads all tag images and caches them in the <see cref="IFanArtCache"/> service.
    /// </summary>
    /// <param name="lfsra"><see cref="ILocalFsResourceAccessor>"/> for the file.</param>
    /// <param name="mediaItemId">Id of the media item.</param>
    /// <param name="title">Title of the media item.</param>
    /// <returns><see cref="Task"/> that completes when the images have been cached.</returns>
    protected async Task ExtractTagFanArt(ILocalFsResourceAccessor lfsra, Guid mediaItemId, string title)
    {
      TagLib.File tag;
      if (!TryCreateTagReader(lfsra, out tag))
        return;

      using (tag)
      {
        IFanArtCache fanArtCache = ServiceRegistration.Get<IFanArtCache>();
        IPicture[] pics = tag.Tag.Pictures;
        if (pics.Length > 0)
        {
          string filename = Path.GetFileNameWithoutExtension(lfsra.LocalFileSystemPath);
          await fanArtCache.TrySaveFanArt(mediaItemId, title, FanArtTypes.Thumbnail,
            p => TrySaveFileImage(pics[0].Data.Data, p, filename)).ConfigureAwait(false);
        }
      }
    }

    protected bool TryCreateTagReader(ILocalFsResourceAccessor lfsra, out TagLib.File tag)
    {
      tag = null;
      if (lfsra == null)
        return false;

      try
      {
        ByteVector.UseBrokenLatin1Behavior = true;  // Otherwise we have problems retrieving non-latin1 chars
        tag = TagLib.File.Create(new AudioMetadataExtractor.ResourceProviderFileAbstraction(lfsra));
        return true;
      }
      catch (CorruptFileException)
      {
        // Only log at the info level here - And simply return false. This makes the importer know that we
        // couldn't perform our task here.
        Logger.Info("AudioFanArtHandler: Audio file '{0}' seems to be broken", lfsra.CanonicalLocalResourcePath);
        return false;
      }
    }

    /// <summary>
    /// Gets all album folder images and caches them in the <see cref="IFanArtCache"/> service.
    /// </summary>
    /// <param name="nativeSystemId">The native system id of the media item.</param>
    /// <param name="albumDirectory"><see cref="ResourcePath>"/> that points to the album directory.</param>
    /// <param name="albumMediaItemId">Id of the media item.</param>
    /// <param name="title">Title of the media item.</param>
    /// <param name="artists">List of artists.</param>
    /// <returns><see cref="Task"/> that completes when the images have been cached.</returns>
    protected async Task ExtractAlbumFolderFanArt(string nativeSystemId, ResourcePath albumDirectory, Guid albumMediaItemId, string title, IList<Tuple<Guid, string>> artists)
    {
      try
      {
        FanArtPathCollection paths = null;
        IList<ResourcePath> potentialArtistImages = null;
        using (IResourceAccessor accessor = new ResourceLocator(nativeSystemId, albumDirectory).CreateAccessor())
          if (accessor is IFileSystemResourceAccessor fsra)
          {
            paths = GetAlbumFolderFanArt(fsra);
            //See if there's an actor fanart directory and try and get any actor fanart
            if (artists != null && artists.Count > 0 && fsra.ResourceExists(".artists"))
              using (IFileSystemResourceAccessor actorsDirectory = fsra.GetResource(".artists"))
                potentialArtistImages = LocalFanartHelper.GetPotentialFanArtFiles(actorsDirectory);
          }

        if (paths != null)
          await SaveFolderImagesToCache(nativeSystemId, paths, albumMediaItemId, title).ConfigureAwait(false);
        if (potentialArtistImages != null)
          await SavePersonFolderImages(nativeSystemId, potentialArtistImages, artists).ConfigureAwait(false);
      }
      catch (Exception ex)
      {
        Logger.Warn("MovieFanArtHandler: Exception while reading folder images for '{0}'", ex, albumDirectory);
      }
    }

    /// <summary>
    /// Gets a <see cref="FanArtPathCollection"/> containing all matching fanart paths in the specified <see cref="ResourcePath"/>.
    /// </summary>
    /// <param name="albumDirectory"><see cref="IFileSystemResourceAccessor"/> that points to the album directory.</param>
    /// <returns><see cref="FanArtPathCollection"/> containing all matching paths.</returns>
    protected FanArtPathCollection GetAlbumFolderFanArt(IFileSystemResourceAccessor albumDirectory)
    {
      FanArtPathCollection paths = new FanArtPathCollection();
      if (albumDirectory == null)
        return paths;

      //Get all fanart in the current directory
      List<ResourcePath> potentialFanArtFiles = LocalFanartHelper.GetPotentialFanArtFiles(albumDirectory);
      ExtractAllFanArtImages(potentialFanArtFiles, paths);

      //Add extra backdrops in ExtraFanArt directory
      if (albumDirectory.ResourceExists("ExtraFanArt/"))
        using (IFileSystemResourceAccessor extraFanArtDirectory = albumDirectory.GetResource("ExtraFanArt/"))
          paths.AddRange(FanArtTypes.FanArt, LocalFanartHelper.GetPotentialFanArtFiles(extraFanArtDirectory));

      List<ResourcePath> covers;
      //Albums store posters as covers so switch the fanart type
      if (paths.Paths.TryGetValue(FanArtTypes.Poster, out covers))
      {
        paths.Paths.Remove(FanArtTypes.Poster);
        paths.AddRange(FanArtTypes.Cover, covers);
      }

      return paths;
    }

    /// <summary>
    /// Gets all artist folder images and caches them in the <see cref="IFanArtCache"/> service.
    /// </summary>
    /// <param name="nativeSystemId">The native system id of the media item.</param>
    /// <param name="albumDirectory"><see cref="ResourcePath>"/> that points to the album directory.</param>
    /// <param name="albumMediaItemId">Id of the media item.</param>
    /// <param name="title">Title of the media item.</param>
    /// <param name="artists">List of artists.</param>
    /// <returns><see cref="Task"/> that completes when the images have been cached.</returns>
    protected async Task ExtractArtistFolderFanArt(string nativeSystemId, ResourcePath albumDirectory, IList<Tuple<Guid, string>> artists)
    {
      if (artists == null || artists.Count == 0)
        return;

      //Get the file's directory
      var artistDirectory = ResourcePathHelper.Combine(albumDirectory, "../");

      try
      {
        var artist = artists.FirstOrDefault(a => string.Compare(a.Item2, artistDirectory.FileName, StringComparison.OrdinalIgnoreCase) == 0);
        if (artist == null)
          artist = artists[0];

        //See if we've already processed this artist
        if (!AddToCache(artist.Item1))
          return;

        //Get all fanart paths in the current directory 
        FanArtPathCollection paths;
        using (IResourceAccessor accessor = new ResourceLocator(nativeSystemId, artistDirectory).CreateAccessor())
          paths = GetArtistFolderFanArt(accessor as IFileSystemResourceAccessor);

        //Save the fanrt to the IFanArtCache service
        await SaveFolderImagesToCache(nativeSystemId, paths, artist.Item1, artist.Item2).ConfigureAwait(false);
      }
      catch (Exception ex)
      {
        Logger.Warn("MovieFanArtHandler: Exception while reading folder images for '{0}'", ex, artistDirectory);
      }
    }

    /// <summary>
    /// Gets a <see cref="FanArtPathCollection"/> containing all matching fanart paths in the specified <see cref="ResourcePath"/>.
    /// </summary>
    /// <param name="artistDirectory"><see cref="IFileSystemResourceAccessor"/> that points to the artist directory.</param>
    /// <returns><see cref="FanArtPathCollection"/> containing all matching paths.</returns>
    protected FanArtPathCollection GetArtistFolderFanArt(IFileSystemResourceAccessor artistDirectory)
    {
      FanArtPathCollection paths = new FanArtPathCollection();
      if (artistDirectory == null)
        return paths;

      //Get all fanart in the current directory
      List<ResourcePath> potentialFanArtFiles = LocalFanartHelper.GetPotentialFanArtFiles(artistDirectory);
      ExtractAllFanArtImages(potentialFanArtFiles, paths);

      //Add extra backdrops in ExtraFanArt directory
      if (artistDirectory.ResourceExists("ExtraFanArt/"))
        using (IFileSystemResourceAccessor extraFanArtDirectory = artistDirectory.GetResource("ExtraFanArt/"))
          paths.AddRange(FanArtTypes.FanArt, LocalFanartHelper.GetPotentialFanArtFiles(extraFanArtDirectory));

      return paths;
    }

    public override void DeleteFanArt(Guid mediaItemId)
    {
      //base method emoves the id from the cache
      base.DeleteFanArt(mediaItemId);
      ServiceRegistration.Get<IFanArtCache>().DeleteFanArtFiles(mediaItemId);
    }
  }
}
