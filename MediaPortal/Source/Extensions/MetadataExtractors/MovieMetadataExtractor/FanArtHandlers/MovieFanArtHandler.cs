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
using MediaPortal.Common.PathManager;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess;
using MediaPortal.Extensions.MetadataExtractors.MatroskaLib;
using MediaPortal.Extensions.OnlineLibraries;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MediaPortal.Extensions.MetadataExtractors.MovieMetadataExtractor
{
  class MovieFanArtHandler : IMediaFanArtHandler
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

    private static readonly ICollection<string> MKV_EXTENSIONS = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase) { ".mkv", ".webm" };

    private static readonly ICollection<String> IMG_EXTENSIONS = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase) { ".jpg", ".png", ".tbn" };

    public static string CACHE_PATH = ServiceRegistration.Get<IPathManager>().GetPath(@"<DATA>\FanArt\");

    #endregion

    protected FanArtHandlerMetadata _metadata;
    private SynchronizedCollection<Guid> _checkCache = new SynchronizedCollection<Guid>();

    public MovieFanArtHandler()
    {
      _metadata = new FanArtHandlerMetadata(FANARTHANDLER_ID, "Movie FanArt handler");
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

    public void CollectFanArt(Guid mediaItemId, IDictionary<Guid, IList<MediaItemAspect>> aspects)
    {
      if (_checkCache.Contains(mediaItemId))
        return;

      Guid? collectionMediaItemId = null;
      IDictionary<Guid, string> actorMediaItems = new Dictionary<Guid, string>();
      SingleMediaItemAspect videoAspect;
      List<string> actors = new List<string>();
      if (MediaItemAspect.TryGetAspect(aspects, VideoAspect.Metadata, out videoAspect))
      {
        IEnumerable<string> actorObjects = videoAspect.GetCollectionAttribute<string>(VideoAspect.ATTR_ACTORS);
        if(actorObjects != null)
          actors.AddRange(actorObjects);
      }

      IList<MultipleMediaItemAspect> relationAspects;
      if (MediaItemAspect.TryGetAspects(aspects, RelationshipAspect.Metadata, out relationAspects))
      {
        foreach (MultipleMediaItemAspect relation in relationAspects)
        {
          if ((Guid?)relation[RelationshipAspect.ATTR_LINKED_ROLE] == MovieCollectionAspect.ROLE_MOVIE_COLLECTION)
          {
            collectionMediaItemId = (Guid)relation[RelationshipAspect.ATTR_LINKED_ID];
          }
          if ((Guid?)relation[RelationshipAspect.ATTR_LINKED_ROLE] == PersonAspect.ROLE_ACTOR)
          {
            int? index = (int?)relation[RelationshipAspect.ATTR_RELATIONSHIP_INDEX];
            if (index.HasValue && actors.Count > index.Value && index.Value >= 0)
              actorMediaItems.Add((Guid)relation[RelationshipAspect.ATTR_LINKED_ID], actors[index.Value]);
          }
        }
      }
      Task.Run(() => ExtractFanArt(mediaItemId, aspects, collectionMediaItemId, actorMediaItems));
      _checkCache.Add(mediaItemId);
    }

    private void ExtractFanArt(Guid mediaItemId, IDictionary<Guid, IList<MediaItemAspect>> aspects, Guid? collectionMediaItemId, IDictionary<Guid, string> actorMediaItems)
    {
      if (aspects.ContainsKey(MovieAspect.ASPECT_ID))
      {
        if (BaseInfo.IsVirtualResource(aspects))
          return;

        MovieInfo movieInfo = new MovieInfo();
        movieInfo.FromMetadata(aspects);
        bool forceFanart = !movieInfo.IsRefreshed;
        MovieCollectionInfo collectionInfo = movieInfo.CloneBasicInstance<MovieCollectionInfo>();
        ExtractLocalImages(aspects, mediaItemId, collectionMediaItemId, movieInfo.ToString(), collectionInfo.ToString(), actorMediaItems);
        if(!MovieMetadataExtractor.SkipFanArtDownload)
          OnlineMatcherService.Instance.DownloadMovieFanArt(mediaItemId, movieInfo, forceFanart);

        //Take advantage of the movie language being known and download collection too
        if (collectionMediaItemId.HasValue && !_checkCache.Contains(collectionMediaItemId.Value))
        {
          if (!MovieMetadataExtractor.SkipFanArtDownload)
            OnlineMatcherService.Instance.DownloadMovieFanArt(collectionMediaItemId.Value, collectionInfo, forceFanart);
          _checkCache.Contains(collectionMediaItemId.Value);
        }
      }
      else if (aspects.ContainsKey(PersonAspect.ASPECT_ID))
      {
        PersonInfo personInfo = new PersonInfo();
        personInfo.FromMetadata(aspects);
        if (personInfo.Occupation == PersonAspect.OCCUPATION_ACTOR || personInfo.Occupation == PersonAspect.OCCUPATION_DIRECTOR ||
          personInfo.Occupation == PersonAspect.OCCUPATION_WRITER)
        {
          if (!MovieMetadataExtractor.SkipFanArtDownload)
            OnlineMatcherService.Instance.DownloadMovieFanArt(mediaItemId, personInfo, !personInfo.IsRefreshed);
        }
      }
      else if (aspects.ContainsKey(CharacterAspect.ASPECT_ID))
      {
        CharacterInfo characterInfo = new CharacterInfo();
        characterInfo.FromMetadata(aspects);
        if (!MovieMetadataExtractor.SkipFanArtDownload)
          OnlineMatcherService.Instance.DownloadMovieFanArt(mediaItemId, characterInfo, !characterInfo.IsRefreshed);
      }
      else if (aspects.ContainsKey(CompanyAspect.ASPECT_ID))
      {
        CompanyInfo companyInfo = new CompanyInfo();
        companyInfo.FromMetadata(aspects);
        if (companyInfo.Type == CompanyAspect.COMPANY_PRODUCTION)
        {
          if (!MovieMetadataExtractor.SkipFanArtDownload)
            OnlineMatcherService.Instance.DownloadMovieFanArt(mediaItemId, companyInfo, !companyInfo.IsRefreshed);
        }
      }
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

    private void ExtractLocalImages(IDictionary<Guid, IList<MediaItemAspect>> aspects, Guid? movieMediaItemId, Guid? collectionMediaItemId, string movieName, string collectionName, IDictionary<Guid, string> actorMediaItems)
    {
      if (BaseInfo.IsVirtualResource(aspects))
        return;

      IResourceLocator mediaItemLocater = GetResourceLocator(aspects);
      if (mediaItemLocater == null)
        return;

      ExtractFolderImages(mediaItemLocater, movieMediaItemId, collectionMediaItemId, movieName, collectionName, actorMediaItems);
      using (IResourceAccessor mediaItemAccessor = mediaItemLocater.CreateAccessor())
      {
        using (LocalFsResourceAccessorHelper rah = new LocalFsResourceAccessorHelper(mediaItemAccessor))
        {
          using (rah.LocalFsResourceAccessor.EnsureLocalFileSystemAccess())
          {
            ExtractMkvImages(rah.LocalFsResourceAccessor, movieMediaItemId, movieName);
          }
        }
      }
    }

    private void ExtractMkvImages(ILocalFsResourceAccessor lfsra, Guid? movieMediaItemId, string movieTitle)
    {
      if (!movieMediaItemId.HasValue)
        return;

      string mediaItemId = movieMediaItemId.Value.ToString().ToUpperInvariant();
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
          var ext = ResourcePathHelper.GetExtension(lfsra.LocalFileSystemPath);
          if (!MKV_EXTENSIONS.Contains(ext))
            return;

          MatroskaInfoReader mkvReader = new MatroskaInfoReader(lfsra);
          byte[] binaryData = null;
          foreach (string pattern in patterns.Keys)
          {
            if (mkvReader.GetAttachmentByName(pattern, out binaryData))
            {
              string fanArtType = patterns[pattern];
              using (FanArtCache.FanArtCountLock countLock = FanArtCache.GetFanArtCountLock(mediaItemId, fanArtType))
              {
                if (countLock.Count >= FanArtCache.MAX_FANART_IMAGES[fanArtType])
                  return;

                FanArtCache.InitFanArtCache(mediaItemId, movieTitle);
                string cacheFile = GetCacheFileName(mediaItemId, fanArtType,
                  "File." + pattern + Path.GetFileNameWithoutExtension(lfsra.LocalFileSystemPath) + ".jpg");
                if (!File.Exists(cacheFile))
                {
                  using (MemoryStream ms = new MemoryStream(binaryData))
                  {
                    using (Image img = Image.FromStream(ms, true, true))
                    {
                      img.Save(cacheFile, System.Drawing.Imaging.ImageFormat.Jpeg);
                      countLock.Count++;
                    }
                  }
                }
              }
            }
          }
        }
      }
      catch (Exception ex)
      {
        Logger.Warn("MovieFanArtHandler: Exception while reading mkv attachments from '{0}'", ex, fileSystemPath);
      }
    }

    private void ExtractFolderImages(IResourceLocator mediaItemLocater, Guid? movieMediaItemId, Guid? collectionMediaItemId, string movieTitle, string collectionTitle, IDictionary<Guid, string> actorMediaItems)
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
          var collectionMediaItemDirectoryPath = ResourcePathHelper.Combine(mediaItemPath, "../../");

          //Movie fanart
          var fanArtPaths = new List<ResourcePath>();
          var posterPaths = new List<ResourcePath>();
          var bannerPaths = new List<ResourcePath>();
          var logoPaths = new List<ResourcePath>();
          var clearArtPaths = new List<ResourcePath>();
          if (movieMediaItemId.HasValue)
          {
            using (var directoryRa = new ResourceLocator(mediaItemLocater.NativeSystemId, mediaItemDirectoryPath).CreateAccessor())
            {
              var directoryFsra = directoryRa as IFileSystemResourceAccessor;
              if (directoryFsra != null)
              {
                if (actorMediaItems.Count > 0)
                {
                  //Get Actor thumbs
                  IFileSystemResourceAccessor actorMediaItemDirectory = directoryFsra.GetResource(".actors");
                  if (actorMediaItemDirectory != null)
                  {
                    foreach (var actor in actorMediaItems)
                    {
                      var potentialArtistFanArtFiles = GetPotentialFanArtFiles(actorMediaItemDirectory);

                      foreach (ResourcePath thumbPath in
                          from potentialFanArtFile in potentialArtistFanArtFiles
                          let potentialFanArtFileNameWithoutExtension = ResourcePathHelper.GetFileNameWithoutExtension(potentialFanArtFile.ToString())
                          where potentialFanArtFileNameWithoutExtension.StartsWith(actor.Value.Replace(" ", "_"), StringComparison.InvariantCultureIgnoreCase)
                          select potentialFanArtFile)
                        SaveFolderFile(mediaItemLocater, thumbPath, FanArtTypes.Thumbnail, actor.Key, actor.Value);
                    }
                  }
                }

                var potentialFanArtFiles = GetPotentialFanArtFiles(directoryFsra);

                posterPaths.AddRange(
                    from potentialFanArtFile in potentialFanArtFiles
                    let potentialFanArtFileNameWithoutExtension = ResourcePathHelper.GetFileNameWithoutExtension(potentialFanArtFile.ToString()).ToLowerInvariant()
                    where potentialFanArtFileNameWithoutExtension == "poster" || potentialFanArtFileNameWithoutExtension == "folder" ||
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
              SaveFolderFile(mediaItemLocater, posterPath, FanArtTypes.Poster, movieMediaItemId.Value, movieTitle);
            foreach (ResourcePath logoPath in logoPaths)
              SaveFolderFile(mediaItemLocater, logoPath, FanArtTypes.Logo, movieMediaItemId.Value, movieTitle);
            foreach (ResourcePath clearArtPath in clearArtPaths)
              SaveFolderFile(mediaItemLocater, clearArtPath, FanArtTypes.ClearArt, movieMediaItemId.Value, movieTitle);
            foreach (ResourcePath bannerPath in bannerPaths)
              SaveFolderFile(mediaItemLocater, bannerPath, FanArtTypes.Banner, movieMediaItemId.Value, movieTitle);
            foreach (ResourcePath fanartPath in fanArtPaths)
              SaveFolderFile(mediaItemLocater, fanartPath, FanArtTypes.FanArt, movieMediaItemId.Value, movieTitle);
          }

          //Collection fanart
          fanArtPaths.Clear();
          posterPaths.Clear();
          bannerPaths.Clear();
          logoPaths.Clear();
          clearArtPaths.Clear();
          if (collectionMediaItemId.HasValue)
          {
            using (var directoryRa = new ResourceLocator(mediaItemLocater.NativeSystemId, collectionMediaItemDirectoryPath).CreateAccessor())
            {
              var directoryFsra = directoryRa as IFileSystemResourceAccessor;
              if (directoryFsra != null)
              {
                var potentialFanArtFiles = GetPotentialFanArtFiles(directoryFsra);

                posterPaths.AddRange(
                    from potentialFanArtFile in potentialFanArtFiles
                    let potentialFanArtFileNameWithoutExtension = ResourcePathHelper.GetFileNameWithoutExtension(potentialFanArtFile.ToString()).ToLowerInvariant()
                    where potentialFanArtFileNameWithoutExtension == "poster" || potentialFanArtFileNameWithoutExtension == "folder"
                    select potentialFanArtFile);

                bannerPaths.AddRange(
                    from potentialFanArtFile in potentialFanArtFiles
                    let potentialFanArtFileNameWithoutExtension = ResourcePathHelper.GetFileNameWithoutExtension(potentialFanArtFile.ToString()).ToLowerInvariant()
                    where potentialFanArtFileNameWithoutExtension == "banner"
                    select potentialFanArtFile);

                fanArtPaths.AddRange(
                    from potentialFanArtFile in potentialFanArtFiles
                    let potentialFanArtFileNameWithoutExtension = ResourcePathHelper.GetFileNameWithoutExtension(potentialFanArtFile.ToString()).ToLowerInvariant()
                    where potentialFanArtFileNameWithoutExtension == "backdrop" || potentialFanArtFileNameWithoutExtension == "fanart"
                    select potentialFanArtFile);

                if (directoryFsra.ResourceExists("ExtraFanArt/"))
                  using (var extraFanArtDirectoryFsra = directoryFsra.GetResource("ExtraFanArt/"))
                    fanArtPaths.AddRange(GetPotentialFanArtFiles(extraFanArtDirectoryFsra));
              }
            }
            foreach (ResourcePath posterPath in posterPaths)
              SaveFolderFile(mediaItemLocater, posterPath, FanArtTypes.Poster, collectionMediaItemId.Value, collectionTitle);
            foreach (ResourcePath bannerPath in bannerPaths)
              SaveFolderFile(mediaItemLocater, bannerPath, FanArtTypes.Banner, collectionMediaItemId.Value, collectionTitle);
            foreach (ResourcePath fanartPath in fanArtPaths)
              SaveFolderFile(mediaItemLocater, fanartPath, FanArtTypes.FanArt, collectionMediaItemId.Value, collectionTitle);
          }
        }
      }
      catch (Exception ex)
      {
        Logger.Warn("MovieFanArtHandler: Exception while reading folder images for '{0}'", ex, fileSystemPath);
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

    private void SaveFolderFile(IResourceLocator mediaItemLocater, ResourcePath file, string fanartType, Guid parentId, string title)
    {
      string mediaItemId = parentId.ToString().ToUpperInvariant();
      using (FanArtCache.FanArtCountLock countLock = FanArtCache.GetFanArtCountLock(mediaItemId, fanartType))
      {
        if (countLock.Count >= FanArtCache.MAX_FANART_IMAGES[fanartType])
          return;

        if ((MovieMetadataExtractor.CacheOfflineFanArt && mediaItemLocater.NativeResourcePath.IsNetworkResource) ||
          (MovieMetadataExtractor.CacheLocalFanArt && !mediaItemLocater.NativeResourcePath.IsNetworkResource && mediaItemLocater.NativeResourcePath.IsValidLocalPath))
        {
          FanArtCache.InitFanArtCache(mediaItemId, title);
          string cacheFile = GetCacheFileName(mediaItemId, fanartType, "Folder." + ResourcePathHelper.GetFileName(file.ToString()));
          if (!File.Exists(cacheFile))
          {
            using (var fileRa = new ResourceLocator(mediaItemLocater.NativeSystemId, file).CreateAccessor())
            {
              var fileFsra = fileRa as IFileSystemResourceAccessor;
              if (fileFsra != null)
              {
                using (Stream ms = fileFsra.OpenRead())
                {
                  using (Image img = Image.FromStream(ms, true, true))
                  {
                    img.Save(cacheFile);
                    countLock.Count++;
                  }
                }
              }
            }
          }
        }
        else
        {
          //Also count local FanArt
          countLock.Count++;
        }
      }
    }

    private string GetCacheFileName(string mediaItemId, string fanartType, string fileName)
    {
      string cacheFile = Path.Combine(CACHE_PATH, mediaItemId, fanartType, fileName);
      string folder = Path.GetDirectoryName(cacheFile);
      if (!Directory.Exists(folder))
        Directory.CreateDirectory(folder);

      return cacheFile;
    }

    public void DeleteFanArt(Guid mediaItemId)
    {
      Task.Run(() => FanArtCache.DeleteFanArtFiles(mediaItemId.ToString()));
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
