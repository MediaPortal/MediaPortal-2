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
using MediaPortal.Common.FanArt;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess;
using MediaPortal.Extensions.MetadataExtractors.MatroskaLib;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MediaPortal.Extensions.MetadataExtractors.VideoMetadataExtractor
{
  class VideoFanArtHandler : IMediaFanArtHandler
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

    private static readonly ICollection<string> MKV_EXTENSIONS = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase) { ".mkv", ".webm" };

    private static readonly ICollection<String> IMG_EXTENSIONS = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase) { ".jpg", ".png", ".tbn" };

    #endregion

    protected FanArtHandlerMetadata _metadata;
    private readonly SynchronizedCollection<Guid> _checkCache = new SynchronizedCollection<Guid>();

    public VideoFanArtHandler()
    {
      _metadata = new FanArtHandlerMetadata(FANARTHANDLER_ID, "Video FanArt handler");
    }

    public Guid[] FanArtAspects
    {
      get
      {
        return FANART_ASPECTS;
      }
    }

    public FanArtHandlerMetadata Metadata
    {
      get { return _metadata; }
    }

    public Task CollectFanArtAsync(Guid mediaItemId, IDictionary<Guid, IList<MediaItemAspect>> aspects)
    {
      if (_checkCache.Contains(mediaItemId))
        return Task.CompletedTask;
      _checkCache.Add(mediaItemId);
      return ExtractFanArt(mediaItemId, aspects);
    }

    private Task ExtractFanArt(Guid mediaItemId, IDictionary<Guid, IList<MediaItemAspect>> aspects)
    {
      if (!aspects.ContainsKey(VideoAspect.ASPECT_ID) || BaseInfo.IsVirtualResource(aspects))
        return Task.CompletedTask;

      MovieInfo movieInfo = new MovieInfo();
      movieInfo.FromMetadata(aspects);
      bool forceFanart = !movieInfo.IsRefreshed;
      return ExtractLocalImages(aspects, mediaItemId, movieInfo.ToString());
    }

    private IResourceLocator GetResourceLocator(IDictionary<Guid, IList<MediaItemAspect>> aspects)
    {
      IList<MultipleMediaItemAspect> providerAspects;
      if (!MediaItemAspect.TryGetAspects(aspects, ProviderResourceAspect.Metadata, out providerAspects))
        return null;
      foreach (MultipleMediaItemAspect providerAspect in providerAspects)
      {
        string systemId = (string)providerAspect[ProviderResourceAspect.ATTR_SYSTEM_ID];
        string resourceAccessorPath = (string)providerAspect[ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH];
        if (!string.IsNullOrEmpty(systemId) && !string.IsNullOrEmpty(resourceAccessorPath))
          return new ResourceLocator(systemId, ResourcePath.Deserialize(resourceAccessorPath));
      }
      return null;
    }

    private async Task ExtractLocalImages(IDictionary<Guid, IList<MediaItemAspect>> aspects, Guid? movieMediaItemId, string movieName)
    {
      if (BaseInfo.IsVirtualResource(aspects))
        return;

      IResourceLocator mediaItemLocater = GetResourceLocator(aspects);
      if (mediaItemLocater == null)
        return;

      await ExtractFolderImages(mediaItemLocater, movieMediaItemId, movieName).ConfigureAwait(false);
      using (IResourceAccessor mediaItemAccessor = mediaItemLocater.CreateAccessor())
      using (LocalFsResourceAccessorHelper rah = new LocalFsResourceAccessorHelper(mediaItemAccessor))
      using (rah.LocalFsResourceAccessor.EnsureLocalFileSystemAccess())
        await ExtractMkvImages(rah.LocalFsResourceAccessor, movieMediaItemId, movieName).ConfigureAwait(false);
    }

    private async Task ExtractMkvImages(ILocalFsResourceAccessor lfsra, Guid? movieMediaItemId, string movieTitle)
    {
      if (!movieMediaItemId.HasValue)
        return;

      Guid mediaItemId = movieMediaItemId.Value;
      string fileSystemPath = string.Empty;
      IDictionary<string, string> patterns = new Dictionary<string, string>()
      {
        { "banner.", FanArtTypes.Banner },
        { "clearart.", FanArtTypes.ClearArt },
        { "cover.", FanArtTypes.Cover },
        { "poster.", FanArtTypes.Poster },
        { "folder.", FanArtTypes.Poster },
        { "backdrop.", FanArtTypes.FanArt },
        { "fanart.", FanArtTypes.FanArt },
      };

      // File based access
      try
      {
        if (lfsra != null)
        {
          fileSystemPath = lfsra.LocalFileSystemPath;
          var ext = ResourcePathHelper.GetExtension(fileSystemPath);
          if (!MKV_EXTENSIONS.Contains(ext))
            return;

          MatroskaInfoReader mkvReader = new MatroskaInfoReader(lfsra);
          IFanArtCache fanArtCache = ServiceRegistration.Get<IFanArtCache>();
          foreach (var pattern in patterns)
          {
            byte[] binaryData;
            if (mkvReader.GetAttachmentByName(pattern.Key, out binaryData))
            {
              string filename = pattern + Path.GetFileNameWithoutExtension(lfsra.LocalFileSystemPath);
              await fanArtCache.TrySaveFanArt(mediaItemId, movieTitle, pattern.Value,
                p => TrySaveTagImage(binaryData, p, filename)).ConfigureAwait(false);
            }
          }
        }
      }
      catch (Exception ex)
      {
        Logger.Warn("MovieFanArtHandler: Exception while reading mkv attachments from '{0}'", ex, fileSystemPath);
      }
    }

    private Task<bool> TrySaveTagImage(byte[] imageData, string saveDirectory, string filename)
    {
      string savePath = Path.Combine(saveDirectory, "File." + filename + ".jpg");
      try
      {
        if (!File.Exists(savePath))
        {
          using (MemoryStream ms = new MemoryStream(imageData))
          using (Image img = Image.FromStream(ms, true, true))
            img.Save(savePath, System.Drawing.Imaging.ImageFormat.Jpeg);
          return Task.FromResult(true);
        }
      }
      catch (Exception ex)
      {
        // Decoding of invalid image data can fail, but main MediaItem is correct.
        Logger.Warn("VideoFanArtHandler: Error saving tag image to path '{0}'", ex, savePath);
      }
      return Task.FromResult(false);
    }

    private async Task ExtractFolderImages(IResourceLocator mediaItemLocater, Guid? movieMediaItemId, string movieTitle)
    {
      string fileSystemPath = string.Empty;

      // File based access
      try
      {
        if (mediaItemLocater != null)
        {
          fileSystemPath = mediaItemLocater.NativeResourcePath.FileName;
          var mediaItemPath = mediaItemLocater.NativeResourcePath;
          var mediaItemFileNameWithoutExtension = ResourcePathHelper.GetFileNameWithoutExtension(mediaItemPath.ToString()).ToLowerInvariant();
          var mediaItemDirectoryPath = ResourcePathHelper.Combine(mediaItemPath, "../");

          //Movie fanart
          var thumbPaths = new List<ResourcePath>();
          var fanArtPaths = new List<ResourcePath>();
          var posterPaths = new List<ResourcePath>();
          var bannerPaths = new List<ResourcePath>();
          var logoPaths = new List<ResourcePath>();
          var clearArtPaths = new List<ResourcePath>();
          var discArtPaths = new List<ResourcePath>();
          if (movieMediaItemId.HasValue)
          {
            using (var directoryRa = new ResourceLocator(mediaItemLocater.NativeSystemId, mediaItemDirectoryPath).CreateAccessor())
            {
              var directoryFsra = directoryRa as IFileSystemResourceAccessor;
              if (directoryFsra != null)
              {
                var potentialFanArtFiles = GetPotentialFanArtFiles(directoryFsra);

                thumbPaths.AddRange(
                    from potentialFanArtFile in potentialFanArtFiles
                    let potentialFanArtFileNameWithoutExtension = ResourcePathHelper.GetFileNameWithoutExtension(potentialFanArtFile.ToString()).ToLowerInvariant()
                    where potentialFanArtFileNameWithoutExtension.StartsWith(mediaItemFileNameWithoutExtension + "-thumb") || potentialFanArtFileNameWithoutExtension == "thumb"
                    select potentialFanArtFile);

                posterPaths.AddRange(
                    from potentialFanArtFile in potentialFanArtFiles
                    let potentialFanArtFileNameWithoutExtension = ResourcePathHelper.GetFileNameWithoutExtension(potentialFanArtFile.ToString()).ToLowerInvariant()
                    where potentialFanArtFileNameWithoutExtension == "poster" || potentialFanArtFileNameWithoutExtension == "folder" || potentialFanArtFileNameWithoutExtension == "cover" ||
                    potentialFanArtFileNameWithoutExtension.StartsWith(mediaItemFileNameWithoutExtension + "-poster")
                    select potentialFanArtFile);

                logoPaths.AddRange(
                    from potentialFanArtFile in potentialFanArtFiles
                    let potentialFanArtFileNameWithoutExtension = ResourcePathHelper.GetFileNameWithoutExtension(potentialFanArtFile.ToString()).ToLowerInvariant()
                    where potentialFanArtFileNameWithoutExtension == "logo" || potentialFanArtFileNameWithoutExtension.StartsWith(mediaItemFileNameWithoutExtension + "-logo")
                    select potentialFanArtFile);

                clearArtPaths.AddRange(
                    from potentialFanArtFile in potentialFanArtFiles
                    let potentialFanArtFileNameWithoutExtension = ResourcePathHelper.GetFileNameWithoutExtension(potentialFanArtFile.ToString()).ToLowerInvariant()
                    where potentialFanArtFileNameWithoutExtension == "clearart" || potentialFanArtFileNameWithoutExtension.StartsWith(mediaItemFileNameWithoutExtension + "-clearart")
                    select potentialFanArtFile);

                discArtPaths.AddRange(
                    from potentialFanArtFile in potentialFanArtFiles
                    let potentialFanArtFileNameWithoutExtension = ResourcePathHelper.GetFileNameWithoutExtension(potentialFanArtFile.ToString()).ToLowerInvariant()
                    where potentialFanArtFileNameWithoutExtension == "discart" || potentialFanArtFileNameWithoutExtension == "disc" || 
                    potentialFanArtFileNameWithoutExtension.StartsWith(mediaItemFileNameWithoutExtension + "-discart")
                    select potentialFanArtFile);

                bannerPaths.AddRange(
                    from potentialFanArtFile in potentialFanArtFiles
                    let potentialFanArtFileNameWithoutExtension = ResourcePathHelper.GetFileNameWithoutExtension(potentialFanArtFile.ToString()).ToLowerInvariant()
                    where potentialFanArtFileNameWithoutExtension == "banner" || potentialFanArtFileNameWithoutExtension.StartsWith(mediaItemFileNameWithoutExtension + "-banner")
                    select potentialFanArtFile);

                fanArtPaths.AddRange(
                    from potentialFanArtFile in potentialFanArtFiles
                    let potentialFanArtFileNameWithoutExtension = ResourcePathHelper.GetFileNameWithoutExtension(potentialFanArtFile.ToString()).ToLowerInvariant()
                    where potentialFanArtFileNameWithoutExtension == "backdrop" || potentialFanArtFileNameWithoutExtension == "fanart" ||
                    potentialFanArtFileNameWithoutExtension.StartsWith(mediaItemFileNameWithoutExtension + "-fanart")
                    select potentialFanArtFile);

                if (directoryFsra.ResourceExists("ExtraFanArt/"))
                  using (var extraFanArtDirectoryFsra = directoryFsra.GetResource("ExtraFanArt/"))
                    fanArtPaths.AddRange(GetPotentialFanArtFiles(extraFanArtDirectoryFsra));
              }
            }
            foreach (ResourcePath posterPath in posterPaths)
              await SaveFolderFile(mediaItemLocater, posterPath, FanArtTypes.Poster, movieMediaItemId.Value, movieTitle).ConfigureAwait(false);
            foreach (ResourcePath logoPath in logoPaths)
              await SaveFolderFile(mediaItemLocater, logoPath, FanArtTypes.Logo, movieMediaItemId.Value, movieTitle).ConfigureAwait(false);
            foreach (ResourcePath clearArtPath in clearArtPaths)
              await SaveFolderFile(mediaItemLocater, clearArtPath, FanArtTypes.ClearArt, movieMediaItemId.Value, movieTitle).ConfigureAwait(false);
            foreach (ResourcePath discArtPath in discArtPaths)
              await SaveFolderFile(mediaItemLocater, discArtPath, FanArtTypes.DiscArt, movieMediaItemId.Value, movieTitle).ConfigureAwait(false);
            foreach (ResourcePath bannerPath in bannerPaths)
              await SaveFolderFile(mediaItemLocater, bannerPath, FanArtTypes.Banner, movieMediaItemId.Value, movieTitle).ConfigureAwait(false);
            foreach (ResourcePath fanartPath in fanArtPaths)
              await SaveFolderFile(mediaItemLocater, fanartPath, FanArtTypes.FanArt, movieMediaItemId.Value, movieTitle).ConfigureAwait(false);
            foreach (ResourcePath thumbPath in thumbPaths)
              await SaveFolderFile(mediaItemLocater, thumbPath, FanArtTypes.Thumbnail, movieMediaItemId.Value, movieTitle).ConfigureAwait(false);
          }
        }
      }
      catch (Exception ex)
      {
        Logger.Warn("VideoFanArtHandler: Exception while reading folder images for '{0}'", ex, fileSystemPath);
      }
    }

    private List<ResourcePath> GetPotentialFanArtFiles(IFileSystemResourceAccessor directoryAccessor)
    {
      var result = new List<ResourcePath>();
      if (directoryAccessor.IsFile)
        return result;
      foreach (var file in directoryAccessor.GetFiles())
        using (file)
        {
          var path = file.CanonicalLocalResourcePath;
          if (IMG_EXTENSIONS.Contains(ResourcePathHelper.GetExtension(path.ToString())))
            result.Add(path);
        }
      return result;
    }

    private async Task SaveFolderFile(IResourceLocator mediaItemLocater, ResourcePath file, string fanArtType, Guid mediaItemId, string title)
    {
      if ((!VideoMetadataExtractor.CacheOfflineFanArt && mediaItemLocater.NativeResourcePath.IsNetworkResource) ||
          (!VideoMetadataExtractor.CacheLocalFanArt && (!mediaItemLocater.NativeResourcePath.IsNetworkResource && mediaItemLocater.NativeResourcePath.IsValidLocalPath)))
        return;

      IFanArtCache fanArtCache = ServiceRegistration.Get<IFanArtCache>();
      await fanArtCache.TrySaveFanArt(mediaItemId, title, fanArtType,
        p => TrySaveFolderImage(mediaItemLocater, file, p)).ConfigureAwait(false);
    }

    private async Task<bool> TrySaveFolderImage(IResourceLocator mediaItemLocater, ResourcePath file, string saveDirectory)
    {
      string savePath = Path.Combine(saveDirectory, "Folder." + ResourcePathHelper.GetFileName(file.ToString()));
      try
      {
        if (File.Exists(savePath))
          return false;

        using (var fileRa = new ResourceLocator(mediaItemLocater.NativeSystemId, file).CreateAccessor())
        {
          var fileFsra = fileRa as IFileSystemResourceAccessor;
          if (fileFsra != null)
          {
            using (Stream ms = fileFsra.OpenRead())
            using (FileStream fs = File.OpenWrite(savePath))
              await ms.CopyToAsync(fs).ConfigureAwait(false);
            return true;
          }
        }
      }
      catch (Exception ex)
      {
        // Decoding of invalid image data can fail, but main MediaItem is correct.
        Logger.Warn("VideoFanArtHandler: Error saving folder image to path '{0}'", ex, savePath);
      }
      return false;
    }

    private string GetCacheFileName(string cachePath, string fileName)
    {
      string cacheFile = Path.Combine(cachePath, fileName);
      string folder = Path.GetDirectoryName(cacheFile);
      if (!Directory.Exists(folder))
        Directory.CreateDirectory(folder);

      return cacheFile;
    }

    public void DeleteFanArt(Guid mediaItemId)
    {
      _checkCache.Remove(mediaItemId);
      ServiceRegistration.Get<IFanArtCache>().DeleteFanArtFiles(mediaItemId);
    }

    public void ClearCache()
    {
      _checkCache.Clear();
    }

    private static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
