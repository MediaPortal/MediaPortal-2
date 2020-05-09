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
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TagLib;

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
    private static readonly ICollection<string> MP4_EXTENSIONS = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase) { ".mp4", ".m4v" };

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

    private const double DEFAULT_OPENCV_THUMBNAIL_OFFSET = 1.0 / 3.0;

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
      string title = "";
      if (aspects.ContainsKey(MovieAspect.ASPECT_ID))
        MediaItemAspect.TryGetAttribute(aspects, MovieAspect.ATTR_MOVIE_NAME, out title);
      else if (aspects.ContainsKey(VideoAspect.ASPECT_ID))
        MediaItemAspect.TryGetAttribute(aspects, MediaAspect.ATTR_TITLE, out title);
      else if (aspects.ContainsKey(MediaAspect.ASPECT_ID))
        MediaItemAspect.TryGetAttribute(aspects, MediaAspect.ATTR_TITLE, out title);

      bool shouldCacheFanart = false;
      if (aspects.ContainsKey(MovieAspect.ASPECT_ID))
        shouldCacheFanart = ShouldCacheLocalFanArt(mediaItemLocator.NativeResourcePath, VideoMetadataExtractor.CacheLocalMovieFanArt, VideoMetadataExtractor.CacheOfflineMovieFanArt);
      else if (aspects.ContainsKey(EpisodeAspect.ASPECT_ID))
        shouldCacheFanart = ShouldCacheLocalFanArt(mediaItemLocator.NativeResourcePath, VideoMetadataExtractor.CacheLocalSeriesFanArt, VideoMetadataExtractor.CacheOfflineSeriesFanArt);
      else if (aspects.ContainsKey(VideoAspect.ASPECT_ID))
        shouldCacheFanart = ShouldCacheLocalFanArt(mediaItemLocator.NativeResourcePath, VideoMetadataExtractor.CacheLocalFanArt, VideoMetadataExtractor.CacheOfflineFanArt);

      if (shouldCacheFanart)
      {
        //Fanart files in the local directory
        //Fanart for movies and episodes is handled in other MDE's
        if (!aspects.ContainsKey(EpisodeAspect.ASPECT_ID) && !aspects.ContainsKey(MovieAspect.ASPECT_ID))
          await ExtractFolderFanArt(mediaItemLocator, mediaItemId, title, aspects).ConfigureAwait(false);

        //Fanart in tags and media
        await ExtractFanArt(mediaItemLocator, mediaItemId, title, aspects).ConfigureAwait(false);
      }
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
    /// Reads all tag images and caches them in the <see cref="IFanArtCache"/> service.
    /// </summary>
    /// <param name="mediaItemLocator"><see cref="IResourceLocator>"/> that points to the file.</param>
    /// <param name="mediaItemId">Id of the media item.</param>
    /// <param name="title">Title of the media item.</param>
    /// <returns><see cref="Task"/> that completes when the images have been cached.</returns>
    protected async Task ExtractFanArt(IResourceLocator mediaItemLocator, Guid mediaItemId, string title, IDictionary<Guid, IList<MediaItemAspect>> aspects)
    {
      try
      {
        //File based access
        using (IResourceAccessor mediaItemAccessor = mediaItemLocator.CreateAccessor())
        using (LocalFsResourceAccessorHelper rah = new LocalFsResourceAccessorHelper(mediaItemAccessor))
        using (rah.LocalFsResourceAccessor.EnsureLocalFileSystemAccess())
        {
          if (MKV_EXTENSIONS.Contains(ResourcePathHelper.GetExtension(mediaItemLocator.NativeResourcePath.FileName)))
            await ExtractMkvFanArt(rah.LocalFsResourceAccessor, mediaItemId, title).ConfigureAwait(false);
          if (MP4_EXTENSIONS.Contains(ResourcePathHelper.GetExtension(mediaItemLocator.NativeResourcePath.FileName)))
            await ExtractTagFanArt(rah.LocalFsResourceAccessor, mediaItemId, title).ConfigureAwait(false);

          //Don't create thumbs if they already exist or if it is a movie (they use posters)
          var thumbs = ServiceRegistration.Get<IFanArtCache>().GetFanArtFiles(mediaItemId, FanArtTypes.Thumbnail);
          if (!thumbs.Any() && !aspects.ContainsKey(ThumbnailLargeAspect.ASPECT_ID) && !aspects.ContainsKey(MovieAspect.ASPECT_ID))
            await ExtractThumbnailFanArt(rah.LocalFsResourceAccessor, mediaItemId, title, aspects);
        }
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
        await fanArtCache.TrySaveFanArt(mediaItemId, title, pattern.Item2, p => TrySaveFileImage(binaryData, p, filename)).ConfigureAwait(false);
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
          bool posterFound = false;
          foreach (var pic in pics)
          {
            if (pic.Type == PictureType.FrontCover)
            {
              posterFound = true;
              filename = "poster." + filename;
              await fanArtCache.TrySaveFanArt(mediaItemId, title, FanArtTypes.Poster, p => TrySaveFileImage(pic.Data.Data, p, filename)).ConfigureAwait(false);
            }
            if (pic.Type == PictureType.MovieScreenCapture)
            {
              posterFound = true;
              filename = "thumb." + filename;
              await fanArtCache.TrySaveFanArt(mediaItemId, title, FanArtTypes.Thumbnail, p => TrySaveFileImage(pic.Data.Data, p, filename)).ConfigureAwait(false);
            }
          }
          if (!posterFound) //No image found by type, use first image
          {
            filename = "thumb." + filename;
            await fanArtCache.TrySaveFanArt(mediaItemId, title, FanArtTypes.Thumbnail, p => TrySaveFileImage(pics[0].Data.Data, p, filename)).ConfigureAwait(false);
          }
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
        tag = TagLib.File.Create(lfsra.LocalFileSystemPath);
        return true;
      }
      catch (CorruptFileException)
      {
        // Only log at the info level here - And simply return false. This makes the importer know that we
        // couldn't perform our task here.
        Logger.Info("VideoFanArtHandler: Video file '{0}' seems to be broken", lfsra.CanonicalLocalResourcePath);
        return false;
      }
    }

    /// <summary>
    /// Extracts a frame image and caches them in the <see cref="IFanArtCache"/> service.
    /// </summary>
    /// <param name="lfsra"><see cref="ILocalFsResourceAccessor>"/> for the file.</param>
    /// <param name="mediaItemId">Id of the media item.</param>
    /// <param name="title">Title of the media item.</param>
    /// <returns><see cref="Task"/> that completes when the images have been cached.</returns>
    protected async Task ExtractThumbnailFanArt(ILocalFsResourceAccessor lfsra, Guid mediaItemId, string title, IDictionary<Guid, IList<MediaItemAspect>> aspects)
    {
      IFanArtCache fanArtCache = ServiceRegistration.Get<IFanArtCache>();
      string filename = $"OpenCv.{Path.GetFileNameWithoutExtension(lfsra.LocalFileSystemPath)}";

      // Check for a reasonable time offset
      int defaultVideoOffset = 720;
      long videoDuration;
      double width = 0;
      double height = 0;
      double downscale = 7.5; // Reduces the HD video frame size to a quarter size to around 256
      IList<MultipleMediaItemAspect> videoAspects;
      if (MediaItemAspect.TryGetAspects(aspects, VideoStreamAspect.Metadata, out videoAspects))
      {
        if ((videoDuration = videoAspects[0].GetAttributeValue<long>(VideoStreamAspect.ATTR_DURATION)) > 0)
        {
          if (defaultVideoOffset > videoDuration * DEFAULT_OPENCV_THUMBNAIL_OFFSET)
            defaultVideoOffset = Convert.ToInt32(videoDuration * DEFAULT_OPENCV_THUMBNAIL_OFFSET);
        }

        width = videoAspects[0].GetAttributeValue<int>(VideoStreamAspect.ATTR_WIDTH);
        height = videoAspects[0].GetAttributeValue<int>(VideoStreamAspect.ATTR_HEIGHT);
        downscale = width / 256.0; //256 is max size of large thumbnail aspect
      }

      var sw = Stopwatch.StartNew();
      using (VideoCapture capture = new VideoCapture())
      {
        capture.Open(lfsra.LocalFileSystemPath);
        int capturePos = defaultVideoOffset * 1000;
        if (capture.FrameCount > 0 && capture.Fps > 0)
        {
          var duration = capture.FrameCount / capture.Fps;
          if (defaultVideoOffset > duration)
            capturePos = Convert.ToInt32(duration * DEFAULT_OPENCV_THUMBNAIL_OFFSET * 1000);
        }

        if (capture.FrameWidth > 0)
          downscale = capture.FrameWidth / 256.0; //256 is max size of large thumbnail aspect

        capture.PosMsec = capturePos;
        using (var mat = capture.RetrieveMat())
        {
          if (mat.Height > 0 && mat.Width > 0)
          {
            width = mat.Width;
            height = mat.Height;
            Logger.Debug("VideoFanArtHandler: Scaling thumbnail of size {1}x{2} for resource '{0}'", lfsra.LocalFileSystemPath, width, height);
            using (var scaledMat = mat.Resize(new OpenCvSharp.Size(width / downscale, height / downscale)))
            {
              var binary = scaledMat.ToBytes();
              await fanArtCache.TrySaveFanArt(mediaItemId, title, FanArtTypes.Thumbnail, p => TrySaveFileImage(binary, p, filename)).ConfigureAwait(false);
              Logger.Debug("VideoFanArtHandler: Successfully created thumbnail for resource '{0}' ({1} ms)", lfsra.LocalFileSystemPath, sw.ElapsedMilliseconds);
            }
          }
          else
          {
            Logger.Warn("VideoFanArtHandler: Failed to create thumbnail for resource '{0}'", lfsra.LocalFileSystemPath);
          }
        }
      }
    }

    /// <summary>
    /// Gets all folder images and caches them in the <see cref="IFanArtCache"/> service.
    /// </summary>
    /// <param name="mediaItemLocator"><see cref="IResourceLocator>"/> that points to the file.</param>
    /// <param name="mediaItemId">Id of the media item.</param>
    /// <param name="title">Title of the media item.</param>
    /// <returns><see cref="Task"/> that completes when the images have been cached.</returns>
    protected async Task ExtractFolderFanArt(IResourceLocator mediaItemLocator, Guid mediaItemId, string title, IDictionary<Guid, IList<MediaItemAspect>> aspects)
    {
      //Get the file's directory
      var videoDirectory = ResourcePathHelper.Combine(mediaItemLocator.NativeResourcePath, "../");
      try
      {
        var mediaItemFileName = ResourcePathHelper.GetFileNameWithoutExtension(mediaItemLocator.NativeResourcePath.ToString()).ToLowerInvariant();

        //Get all fanart paths in the current directory 
        FanArtPathCollection paths;
        using (IResourceAccessor accessor = new ResourceLocator(mediaItemLocator.NativeSystemId, videoDirectory).CreateAccessor())
          paths = GetFolderFanArt(accessor as IFileSystemResourceAccessor, mediaItemFileName, aspects);

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
    protected FanArtPathCollection GetFolderFanArt(IFileSystemResourceAccessor videoDirectory, string filename, IDictionary<Guid, IList<MediaItemAspect>> aspects)
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
