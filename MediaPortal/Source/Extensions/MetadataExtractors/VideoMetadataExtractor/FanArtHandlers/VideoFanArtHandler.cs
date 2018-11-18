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
using MediaPortal.Extensions.MetadataExtractors.MatroskaLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace MediaPortal.Extensions.MetadataExtractors.VideoMetadataExtractor
{
  /// <summary>
  /// <see cref="IMediaFanArtHandler"/> implementation that extracts fanart for
  /// video files from the local file system and mkv tags.
  /// </summary>
  public class VideoFanArtHandler : BaseFanArtHandler
  {
    #region Constants

    private static readonly Guid[] FANART_ASPECTS = { VideoAspect.ASPECT_ID };

    /// <summary>
    /// GUID string for the movie FanArt handler.
    /// </summary>
    public const string FANARTHANDLER_ID_STR = "183DBA7C-666A-4BBD-BCE8-AD0924B4FEF1";

    /// <summary>
    /// Movie FanArt handler GUID.
    /// </summary>
    public static Guid FANARTHANDLER_ID = new Guid(FANARTHANDLER_ID_STR);

    private static readonly ICollection<string> MKV_EXTENSIONS = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase) { ".mkv", ".mk3d", ".webm" };

    private static readonly ICollection<Tuple<string, string>> MKV_PATTERNS = new List<Tuple<string, string>>
    {
      new Tuple<string, string>("banner.", FanArtTypes.Banner),
      new Tuple<string, string>("clearart.", FanArtTypes.ClearArt),
      new Tuple<string, string>("cover.", FanArtTypes.Cover),
      new Tuple<string, string>("poster.", FanArtTypes.Poster),
      new Tuple<string, string>("folder.", FanArtTypes.Poster),
      new Tuple<string, string>("backdrop.", FanArtTypes.FanArt),
      new Tuple<string, string>("fanart.", FanArtTypes.FanArt),
      new Tuple<string, string>("clearlogo.", FanArtTypes.Logo),
    };

    #endregion

    #region Constructor

    public VideoFanArtHandler()
      : base(new FanArtHandlerMetadata(FANARTHANDLER_ID, "Video FanArt handler"), FANART_ASPECTS)
    { }

    #endregion

    #region Base overrides

    public override async Task CollectFanArtAsync(Guid mediaItemId, IDictionary<Guid, IList<MediaItemAspect>> aspects)
    {
      //Virtual resources won't have local fanart
      if (BaseInfo.IsVirtualResource(aspects))
        return;

      //Don't process the same item again
      if (!AddToCache(mediaItemId))
        return;

      IResourceLocator mediaItemLocator = GetResourceLocator(aspects);
      if (mediaItemLocator == null)
        return;
      
      //Only needed for the name used in the fanart cache
      MovieInfo movieInfo = new MovieInfo();
      movieInfo.FromMetadata(aspects);
      string title = movieInfo.ToString();

      //Fanart files in the local directory
      if (ShouldCacheLocalFanArt(mediaItemLocator.NativeResourcePath, VideoMetadataExtractor.CacheLocalFanArt, VideoMetadataExtractor.CacheOfflineFanArt))
        await ExtractFolderFanArt(mediaItemLocator, mediaItemId, title).ConfigureAwait(false);

      //Fanart in MKV tags
      if (MKV_EXTENSIONS.Contains(ResourcePathHelper.GetExtension(mediaItemLocator.NativeResourcePath.FileName)))
        await ExtractMkvFanArt(mediaItemLocator, mediaItemId, title).ConfigureAwait(false);
    }

    public override void DeleteFanArt(Guid mediaItemId)
    {
      //base implementation removes the id from the cache
      base.DeleteFanArt(mediaItemId);
      ServiceRegistration.Get<IFanArtCache>().DeleteFanArtFiles(mediaItemId);
    }

    #endregion

    #region Protected methods

    /// <summary>
    /// Reads all mkv tag images and caches them in the <see cref="IFanArtCache"/> service.
    /// </summary>
    /// <param name="mediaItemLocator"><see cref="IResourceLocator>"/> that points to the file.</param>
    /// <param name="mediaItemId">Id of the media item.</param>
    /// <param name="title">Title of the media item.</param>
    /// <returns><see cref="Task"/> that completes when the images have been cached.</returns>
    protected async Task ExtractMkvFanArt(IResourceLocator mediaItemLocator, Guid mediaItemId, string title)
    {
      try
      {
        //File based access
        using (IResourceAccessor mediaItemAccessor = mediaItemLocator.CreateAccessor())
        using (LocalFsResourceAccessorHelper rah = new LocalFsResourceAccessorHelper(mediaItemAccessor))
        using (rah.LocalFsResourceAccessor.EnsureLocalFileSystemAccess())
          await ExtractMkvFanArt(rah.LocalFsResourceAccessor, mediaItemId, title).ConfigureAwait(false);
      }
      catch (Exception ex)
      {
        Logger.Warn("VideoFanArtHandler: Exception while reading MKV tag images for '{0}'", ex, mediaItemLocator.NativeResourcePath);
      }
    }

    /// <summary>
    /// Reads all mkv tag images and caches them in the <see cref="IFanArtCache"/> service.
    /// </summary>
    /// <param name="lfsra"><see cref="ILocalFsResourceAccessor>"/> for the file.</param>
    /// <param name="mediaItemId">Id of the media item.</param>
    /// <param name="title">Title of the media item.</param>
    /// <returns><see cref="Task"/> that completes when the images have been cached.</returns>
    protected async Task ExtractMkvFanArt(ILocalFsResourceAccessor lfsra, Guid mediaItemId, string title)
    {
      if (lfsra == null)
        return;

      MatroskaBinaryReader mkvReader = new MatroskaBinaryReader(lfsra);
      IFanArtCache fanArtCache = ServiceRegistration.Get<IFanArtCache>();
      foreach (var pattern in MKV_PATTERNS)
      {
        byte[] binaryData = await mkvReader.GetAttachmentByNameAsync(pattern.Item1).ConfigureAwait(false);
        if (binaryData == null)
          continue;
        string filename = pattern + Path.GetFileNameWithoutExtension(lfsra.LocalFileSystemPath);
        await fanArtCache.TrySaveFanArt(mediaItemId, title, pattern.Item2,
          p => TrySaveFileImage(binaryData, p, filename)).ConfigureAwait(false);
      }
    }

    /// <summary>
    /// Gets all folder images and caches them in the <see cref="IFanArtCache"/> service.
    /// </summary>
    /// <param name="mediaItemLocator"><see cref="IResourceLocator>"/> that points to the file.</param>
    /// <param name="mediaItemId">Id of the media item.</param>
    /// <param name="title">Title of the media item.</param>
    /// <returns><see cref="Task"/> that completes when the images have been cached.</returns>
    protected async Task ExtractFolderFanArt(IResourceLocator mediaItemLocator, Guid mediaItemId, string title)
    {
      //Get the file's directory
      var videoDirectory = ResourcePathHelper.Combine(mediaItemLocator.NativeResourcePath, "../");
      try
      {
        var mediaItemFileName = ResourcePathHelper.GetFileNameWithoutExtension(mediaItemLocator.NativeResourcePath.ToString()).ToLowerInvariant();

        //Get all fanart paths in the current directory 
        FanArtPathCollection paths;
        using (IResourceAccessor accessor = new ResourceLocator(mediaItemLocator.NativeSystemId, videoDirectory).CreateAccessor())
          paths = GetFolderFanArt(accessor as IFileSystemResourceAccessor, mediaItemFileName);

        //Save the fanrt to the IFanArtCache service
        await SaveFolderImagesToCache(mediaItemLocator.NativeSystemId, paths, mediaItemId, title).ConfigureAwait(false);
      }
      catch (Exception ex)
      {
        Logger.Warn("VideoFanArtHandler: Exception while reading folder images for '{0}'", ex, videoDirectory);
      }
    }

    /// <summary>
    /// Gets a <see cref="FanArtPathCollection"/> containing all matching fanart paths in the specified <see cref="ResourcePath"/>.
    /// </summary>
    /// <param name="videoDirectory"><see cref="IFileSystemResourceAccessor"/> that points to the episode directory.</param>
    /// <param name="filename">The file name of the media item to extract images for.</param>
    /// <returns><see cref="FanArtPathCollection"/> containing all matching paths.</returns>
    protected FanArtPathCollection GetFolderFanArt(IFileSystemResourceAccessor videoDirectory, string filename)
    {
      FanArtPathCollection paths = new FanArtPathCollection();
      if (videoDirectory == null)
        return paths;

      //Get all fanart in the current directory
      List<ResourcePath> potentialFanArtFiles = LocalFanartHelper.GetPotentialFanArtFiles(videoDirectory);
      ExtractAllFanArtImages(potentialFanArtFiles, paths, filename);

      //Add extra backdrops in ExtraFanArt directory
      if (videoDirectory.ResourceExists("ExtraFanArt/"))
        using (IFileSystemResourceAccessor extraFanArtDirectory = videoDirectory.GetResource("ExtraFanArt/"))
          paths.AddRange(FanArtTypes.FanArt, LocalFanartHelper.GetPotentialFanArtFiles(extraFanArtDirectory));

      return paths;
    }

    #endregion
  }
}
