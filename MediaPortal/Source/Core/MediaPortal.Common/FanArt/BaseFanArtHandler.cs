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

using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MediaPortal.Common.FanArt
{
  /// <summary>
  /// Base class for <see cref="IMediaFanArtHandler"/> implementations that cache the ids of
  /// all processed media items and save local fanart images to the <see cref="IFanArtCache"/> service.
  /// </summary>
  public abstract class BaseFanArtHandler : IMediaFanArtHandler
  {
    #region Members

    private ConcurrentDictionary<Guid, byte> _cache;
    protected FanArtHandlerMetadata _metadata;
    protected Guid[] _fanArtAspects;

    #endregion

    #region Construvtor

    public BaseFanArtHandler(FanArtHandlerMetadata metadata, IEnumerable<Guid> fanArtAspects)
    {
      _metadata = metadata;
      _fanArtAspects = fanArtAspects.ToArray();

      _cache = new ConcurrentDictionary<Guid, byte>();
    }

    #endregion

    #region Cache

    /// <summary>
    /// Adds the specified id to the cache.
    /// </summary>
    /// <param name="id">Media item id to cache.</param>
    /// <returns><c>true</c> if the specified id was not already contained in the cache.</returns>
    protected bool AddToCache(Guid id)
    {
      return _cache.TryAdd(id, default(byte));
    }

    /// <summary>
    /// Removes the specified id from the cache.
    /// </summary>
    /// <param name="id">Media item id to remove from the cache.</param>
    /// <returns><c>true</c> if the id was removed from the cache.</returns>
    protected bool RemoveFromCache(Guid id)
    {
      byte value;
      return _cache.TryRemove(id, out value);
    }

    #endregion

    #region Protected methods

    /// <summary>
    /// Determines whether images in the specified path should be cached based on the values of
    /// <paramref name="cacheLocal"/> and <paramref name="cacheNetwork"/>.
    /// </summary>
    /// <param name="path">The path to the images.</param>
    /// <param name="cacheLocal">Whether fanart in local paths should be cached.</param>
    /// <param name="cacheNetwork">Whether fanart in network paths should be cached.</param>
    /// <returns><c>true</c> if images in the specified path should be cached.</returns>
    protected static bool ShouldCacheLocalFanArt(ResourcePath path, bool cacheLocal, bool cacheNetwork)
    {
      return (cacheLocal && !path.IsNetworkResource && path.IsValidLocalPath) || (cacheNetwork && path.IsNetworkResource);
    }

    /// <summary>
    /// Gets an <see cref="IResourceLocator"/> that points to the location of the first
    /// <see cref="ResourcePath"/> contained in the specified MediaItemAspect dictionary.
    /// </summary>
    /// <param name="aspects">MediaItemAspect dictionary containing the <see cref="ResourcePath"/>.</param>
    /// <returns><see cref="IResourceLocator"/> instance, or <c>null</c> if a valid oath was not found.</returns>
    protected static IResourceLocator GetResourceLocator(IDictionary<Guid, IList<MediaItemAspect>> aspects)
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

    /// <summary>
    /// Populates the <paramref name="paths"/> with all matching image paths in <paramref name="potentialFanArtFiles"/>. 
    /// </summary>
    /// <param name="potentialFanArtFiles"><see cref="ResourcePath"/> collection containing potential fanart paths.</param>
    /// <param name="paths"><see cref="FanArtPathCollection"/> to populate</param>
    protected void ExtractAllFanArtImages(ICollection<ResourcePath> potentialFanArtFiles, FanArtPathCollection paths)
    {
      if (potentialFanArtFiles == null && potentialFanArtFiles.Count == 0)
        return;
      paths.AddRange(FanArtTypes.Thumbnail, LocalFanartHelper.FilterPotentialFanArtFilesByName(potentialFanArtFiles, LocalFanartHelper.THUMB_FILENAMES));
      paths.AddRange(FanArtTypes.Poster, LocalFanartHelper.FilterPotentialFanArtFilesByName(potentialFanArtFiles, LocalFanartHelper.POSTER_FILENAMES));
      paths.AddRange(FanArtTypes.Logo, LocalFanartHelper.FilterPotentialFanArtFilesByName(potentialFanArtFiles, LocalFanartHelper.LOGO_FILENAMES));
      paths.AddRange(FanArtTypes.ClearArt, LocalFanartHelper.FilterPotentialFanArtFilesByName(potentialFanArtFiles, LocalFanartHelper.CLEARART_FILENAMES));
      paths.AddRange(FanArtTypes.DiscArt, LocalFanartHelper.FilterPotentialFanArtFilesByName(potentialFanArtFiles, LocalFanartHelper.DISCART_FILENAMES));
      paths.AddRange(FanArtTypes.Banner, LocalFanartHelper.FilterPotentialFanArtFilesByName(potentialFanArtFiles, LocalFanartHelper.BANNER_FILENAMES));
      paths.AddRange(FanArtTypes.FanArt, LocalFanartHelper.FilterPotentialFanArtFilesByName(potentialFanArtFiles, LocalFanartHelper.BACKDROP_FILENAMES));
    }

    /// <summary>
    /// Populates the <paramref name="paths"/> with all matching image paths in <paramref name="potentialFanArtFiles"/>
    /// including image paths that start with the specified <paramref name="filename"/>. 
    /// </summary>
    /// <param name="potentialFanArtFiles"><see cref="ResourcePath"/> collection containing potential fanart paths.</param>
    /// <param name="paths"><see cref="FanArtPathCollection"/> to populate</param>
    /// <param name="filename">The filename of the media item.</param>
    protected void ExtractAllFanArtImages(ICollection<ResourcePath> potentialFanArtFiles, FanArtPathCollection paths, string filename)
    {
      if (potentialFanArtFiles == null && potentialFanArtFiles.Count == 0)
        return;

      filename = filename.ToLowerInvariant();
      
      paths.AddRange(FanArtTypes.Thumbnail, LocalFanartHelper.FilterPotentialFanArtFilesByNameOrPrefix(potentialFanArtFiles,
        LocalFanartHelper.THUMB_FILENAMES, LocalFanartHelper.THUMB_FILENAMES.Select(f => filename + "-" + f)));
      
      paths.AddRange(FanArtTypes.Poster, LocalFanartHelper.FilterPotentialFanArtFilesByNameOrPrefix(potentialFanArtFiles,
        LocalFanartHelper.POSTER_FILENAMES, LocalFanartHelper.POSTER_FILENAMES.Select(f => filename + "-" + f)));
      
      paths.AddRange(FanArtTypes.Logo, LocalFanartHelper.FilterPotentialFanArtFilesByNameOrPrefix(potentialFanArtFiles,
        LocalFanartHelper.LOGO_FILENAMES, LocalFanartHelper.LOGO_FILENAMES.Select(f => filename + "-" + f)));
      
      paths.AddRange(FanArtTypes.ClearArt, LocalFanartHelper.FilterPotentialFanArtFilesByNameOrPrefix(potentialFanArtFiles,
        LocalFanartHelper.CLEARART_FILENAMES, LocalFanartHelper.CLEARART_FILENAMES.Select(f => filename + "-" + f)));
      
      paths.AddRange(FanArtTypes.DiscArt, LocalFanartHelper.FilterPotentialFanArtFilesByNameOrPrefix(potentialFanArtFiles,
        LocalFanartHelper.DISCART_FILENAMES, LocalFanartHelper.DISCART_FILENAMES.Select(f => filename + "-" + f)));
      
      paths.AddRange(FanArtTypes.Banner, LocalFanartHelper.FilterPotentialFanArtFilesByNameOrPrefix(potentialFanArtFiles,
        LocalFanartHelper.BANNER_FILENAMES, LocalFanartHelper.BANNER_FILENAMES.Select(f => filename + "-" + f)));
      
      paths.AddRange(FanArtTypes.FanArt, LocalFanartHelper.FilterPotentialFanArtFilesByNameOrPrefix(potentialFanArtFiles,
        LocalFanartHelper.BACKDROP_FILENAMES, LocalFanartHelper.BACKDROP_FILENAMES.Select(f => filename + "-" + f)));
    }

    /// <summary>
    /// Populates the <paramref name="paths"/> with all matching image paths in <paramref name="potentialFanArtFiles"/>
    /// including only image paths that start with the specified <paramref name="prefix"/>. 
    /// </summary>
    /// <param name="potentialFanArtFiles"><see cref="ResourcePath"/> collection containing potential fanart paths.</param>
    /// <param name="paths"><see cref="FanArtPathCollection"/> to populate</param>
    /// <param name="prefix">The filename prefix of the media item.</param>
    protected void ExtractAllFanArtImagesByPrefix(ICollection<ResourcePath> potentialFanArtFiles, FanArtPathCollection paths, string prefix)
    {
      if (potentialFanArtFiles == null && potentialFanArtFiles.Count == 0)
        return;

      prefix = prefix.ToLowerInvariant();

      paths.AddRange(FanArtTypes.Thumbnail, LocalFanartHelper.FilterPotentialFanArtFilesByNameOrPrefix(potentialFanArtFiles,
        null, LocalFanartHelper.THUMB_FILENAMES.Select(f => prefix + "-" + f)));

      paths.AddRange(FanArtTypes.Poster, LocalFanartHelper.FilterPotentialFanArtFilesByNameOrPrefix(potentialFanArtFiles,
        null, LocalFanartHelper.POSTER_FILENAMES.Select(f => prefix + "-" + f)));

      paths.AddRange(FanArtTypes.Logo, LocalFanartHelper.FilterPotentialFanArtFilesByNameOrPrefix(potentialFanArtFiles,
        null, LocalFanartHelper.LOGO_FILENAMES.Select(f => prefix + "-" + f)));

      paths.AddRange(FanArtTypes.ClearArt, LocalFanartHelper.FilterPotentialFanArtFilesByNameOrPrefix(potentialFanArtFiles,
        null, LocalFanartHelper.CLEARART_FILENAMES.Select(f => prefix + "-" + f)));

      paths.AddRange(FanArtTypes.DiscArt, LocalFanartHelper.FilterPotentialFanArtFilesByNameOrPrefix(potentialFanArtFiles,
        null, LocalFanartHelper.DISCART_FILENAMES.Select(f => prefix + "-" + f)));

      paths.AddRange(FanArtTypes.Banner, LocalFanartHelper.FilterPotentialFanArtFilesByNameOrPrefix(potentialFanArtFiles,
        null, LocalFanartHelper.BANNER_FILENAMES.Select(f => prefix + "-" + f)));

      paths.AddRange(FanArtTypes.FanArt, LocalFanartHelper.FilterPotentialFanArtFilesByNameOrPrefix(potentialFanArtFiles,
        null, LocalFanartHelper.BACKDROP_FILENAMES.Select(f => prefix + "-" + f)));
    }

    /// <summary>
    /// Gets all actor folder images and caches them in the <see cref="IFanArtCache"/> service.
    /// </summary>
    /// <param name="mediaItemLocator"><see cref="IResourceLocator>"/> that points to the file.</param>
    /// <param name="nativeResourcePath">Path to the actor fanart directory.</param>
    /// <param name="persons">Collection of actor ids and names.</param>
    /// <returns><see cref="Task"/> that completes when the images have been cached.</returns>
    protected async Task SavePersonFolderImages(string nativeSystemId, ICollection<ResourcePath> potentialFanArtFiles, IList<Tuple<Guid, string>> persons)
    {
      if (persons == null || persons.Count == 0 ||
        potentialFanArtFiles == null || potentialFanArtFiles.Count == 0)
        return;
      foreach (var person in persons)
      {
        FanArtPathCollection paths = GetPersonFolderImages(potentialFanArtFiles, person.Item2);
        await SaveFolderImagesToCache(nativeSystemId, paths, person.Item1, person.Item2).ConfigureAwait(false);
      }
    }

    /// <summary>
    /// Gets a <see cref="FanArtPathCollection"/> containing all matching person fanart paths in the specified <see cref="ResourcePath"/>.
    /// </summary>
    /// <param name="potentialFanArtFiles">Enumeration of potential fanart paths..</param>
    /// <param name="personName">Name of the person to find fanart for..</param>
    /// <returns><see cref="FanArtPathCollection"/> containing all matching paths.</returns>
    protected FanArtPathCollection GetPersonFolderImages(IEnumerable<ResourcePath> potentialFanArtFiles, string personName)
    {
      FanArtPathCollection paths = new FanArtPathCollection();
      paths.AddRange(FanArtTypes.Thumbnail, LocalFanartHelper.FilterPotentialFanArtFilesByPrefix(potentialFanArtFiles, personName.Replace(" ", "_").ToLowerInvariant()));
      return paths;
    }

    /// <summary>
    /// Asynchronously saves all images contained in the specified <see cref="FanArtPathCollection"/> to the <see cref="IFanArtCache"/> service.
    /// </summary>
    /// <param name="nativeSystemId">The native system id of the paths contained in the <see cref="FanArtPathCollection"/>.</param>
    /// <param name="paths">Collection of image paths to save.</param>
    /// <param name="mediaItemId">The id of the media item to which the images belong.</param>
    /// <param name="title">The title of the media item to which the images belong.</param>
    /// <returns>A <see cref="Task"/> that completes when the images have been saved to the <see cref="IFanArtCache"/> service.</returns>
    protected async Task SaveFolderImagesToCache(string nativeSystemId, FanArtPathCollection paths, Guid mediaItemId, string title)
    {
      foreach (var typePaths in paths)
        await SaveFolderImagesToCache(nativeSystemId, typePaths.Value, typePaths.Key, mediaItemId, title).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously saves all images contained in the specified paths to the <see cref="IFanArtCache"/> service.
    /// </summary>
    /// <param name="nativeSystemId">The native system id of the paths contained in the paths collection.</param>
    /// <param name="paths">Collection of image paths to save.</param>
    /// <param name="fanArtType">The fanart type of the images.</param>
    /// <param name="mediaItemId">The id of the media item to which the images belong.</param>
    /// <param name="title">The title of the media item to which the images belong.</param>
    /// <returns>A <see cref="Task"/> that completes when the images have been saved to the <see cref="IFanArtCache"/> service.</returns>
    protected async Task SaveFolderImagesToCache(string nativeSystemId, ICollection<ResourcePath> paths, string fanArtType, Guid mediaItemId, string title)
    {
      IFanArtCache fanArtCache = ServiceRegistration.Get<IFanArtCache>();
      await fanArtCache.TrySaveFanArt(mediaItemId, title, fanArtType, paths, (p, f) => TrySaveFolderImage(nativeSystemId, f, p)).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously saves the folder image with the specified path to the specified directory.
    /// </summary>
    /// <param name="nativeSystemId">The native system id of the path.</param>
    /// <param name="path">Path to the image to save.</param>
    /// <param name="saveDirectory">Directory to save the image.</param>
    /// <returns>A <see cref="Task"/> that completes when the image has been saved.</returns>
    protected async Task<bool> TrySaveFolderImage(string nativeSystemId, ResourcePath path, string saveDirectory)
    {
      string savePath = Path.Combine(saveDirectory, "Folder." + ResourcePathHelper.GetFileName(path.ToString()));
      try
      {
        if (File.Exists(savePath))
          return false;

        using (IResourceAccessor accessor = new ResourceLocator(nativeSystemId, path).CreateAccessor())
          if (accessor is IFileSystemResourceAccessor fsra)
          {
            using (Stream ms = fsra.OpenRead())
            using (FileStream fs = File.OpenWrite(savePath))
              await ms.CopyToAsync(fs).ConfigureAwait(false);
            return true;
          }
      }
      catch (Exception ex)
      {
        // Decoding of invalid image data can fail, but main MediaItem is correct.
        Logger.Warn("{0}: Error saving folder image to path '{1}'", ex, _metadata.Name, savePath);
      }
      return false;
    }

    protected Task<bool> TrySaveFileImage(byte[] imageData, string saveDirectory, string filename)
    {
      string savePath = Path.Combine(saveDirectory, "File." + filename + ".jpg");
      try
      {
        if (File.Exists(savePath))
          return Task.FromResult(false);
        using (MemoryStream ms = new MemoryStream(imageData))
        using (Image img = Image.FromStream(ms, true, true))
          img.Save(savePath, System.Drawing.Imaging.ImageFormat.Jpeg);
        return Task.FromResult(true);
      }
      catch (Exception ex)
      {
        // Decoding of invalid image data can fail, but main MediaItem is correct.
        Logger.Warn("{0}: Error saving file image to path '{1}'", ex, _metadata.Name, savePath);
      }
      return Task.FromResult(false);
    }

    #endregion

    #region IMediaFanArtHandler implementation

    public abstract Task CollectFanArtAsync(Guid mediaItemId, IDictionary<Guid, IList<MediaItemAspect>> aspects);

    public FanArtHandlerMetadata Metadata
    {
      get { return _metadata; }
    }

    public Guid[] FanArtAspects
    {
      get { return _fanArtAspects; }
    }

    public virtual void ClearCache()
    {
      _cache.Clear();
    }

    public virtual void DeleteFanArt(Guid mediaItemId)
    {
      RemoveFromCache(mediaItemId);
    }

    #endregion

    #region Logger

    protected static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }

    #endregion
  }
}
