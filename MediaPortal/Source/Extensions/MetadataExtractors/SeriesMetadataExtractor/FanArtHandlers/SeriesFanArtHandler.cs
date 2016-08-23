#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.ResourceAccess;
using System.IO;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.Services.ResourceAccess;
using MediaPortal.Extensions.MetadataExtractors.MatroskaLib;
using System.Linq;
using MediaPortal.Common.PathManager;
using System.Drawing;
using System.Threading.Tasks;
using MediaPortal.Extensions.OnlineLibraries;
using MediaPortal.Common.FanArt;

namespace MediaPortal.Extensions.MetadataExtractors.SeriesMetadataExtractor
{
  class SeriesFanArtHandler : IMediaFanArtHandler
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

    private static readonly ICollection<string> MKV_EXTENSIONS = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase) { ".mkv", ".webm" };

    private static readonly ICollection<String> IMG_EXTENSIONS = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase) { ".jpg", ".png", ".tbn" };

    public static string CACHE_PATH = ServiceRegistration.Get<IPathManager>().GetPath(@"<DATA>\FanArt\");

    #endregion

    protected FanArtHandlerMetadata _metadata;
    private SynchronizedCollection<Guid> _checkCache = new SynchronizedCollection<Guid>();

    public SeriesFanArtHandler()
    {
      _metadata = new FanArtHandlerMetadata(FANARTHANDLER_ID, "Series FanArt handler");

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

      Guid? seriesMediaItemId = null;
      Guid? seasonMediaItemId = null;
      IList<MultipleMediaItemAspect> relationAspects;
      if (MediaItemAspect.TryGetAspects(aspects, RelationshipAspect.Metadata, out relationAspects))
      {
        foreach (MultipleMediaItemAspect relation in relationAspects)
        {
          if ((Guid?)relation[RelationshipAspect.ATTR_LINKED_ROLE] == SeriesAspect.ROLE_SERIES)
          {
            seriesMediaItemId = (Guid)relation[RelationshipAspect.ATTR_LINKED_ID];
          }
          if ((Guid?)relation[RelationshipAspect.ATTR_LINKED_ROLE] == SeasonAspect.ROLE_SEASON)
          {
            seasonMediaItemId = (Guid)relation[RelationshipAspect.ATTR_LINKED_ID];
          }
        }
      }
      Task.Run(() => ExtractFanArt(mediaItemId, aspects, seriesMediaItemId, seasonMediaItemId));
      _checkCache.Add(mediaItemId);
    }

    private void ExtractFanArt(Guid mediaItemId, IDictionary<Guid, IList<MediaItemAspect>> aspects, Guid? seriesMediaItemId, Guid? seasonMediaItemId)
    {
      if (aspects.ContainsKey(EpisodeAspect.ASPECT_ID))
      {
        EpisodeInfo episodeInfo = new EpisodeInfo();
        episodeInfo.FromMetadata(aspects);
        FanArtCache.InitFanArtCache(mediaItemId.ToString(), episodeInfo.ToString());
        ExtractLocalImages(aspects, seriesMediaItemId, seasonMediaItemId);
        OnlineMatcherService.DownloadSeriesFanArt(mediaItemId, episodeInfo);

        //Take advantage of the audio language being known and download season and series too
        if (seasonMediaItemId.HasValue && !_checkCache.Contains(seasonMediaItemId.Value))
        {
          SeasonInfo seasonInfo = episodeInfo.CloneBasicInstance<SeasonInfo>();
          FanArtCache.InitFanArtCache(seasonMediaItemId.Value.ToString(), seasonInfo.ToString());
          OnlineMatcherService.DownloadSeriesFanArt(seasonMediaItemId.Value, seasonInfo);
          _checkCache.Add(seasonMediaItemId.Value);
        }
        if (seriesMediaItemId.HasValue && !_checkCache.Contains(seriesMediaItemId.Value))
        {
          SeriesInfo seriesInfo = episodeInfo.CloneBasicInstance<SeriesInfo>();
          FanArtCache.InitFanArtCache(seriesMediaItemId.Value.ToString(), seriesInfo.ToString());
          OnlineMatcherService.DownloadSeriesFanArt(seriesMediaItemId.Value, seriesInfo);
          _checkCache.Add(seriesMediaItemId.Value);
        }
      }
      else if (aspects.ContainsKey(PersonAspect.ASPECT_ID))
      {
        PersonInfo personInfo = new PersonInfo();
        personInfo.FromMetadata(aspects);
        FanArtCache.InitFanArtCache(mediaItemId.ToString(), personInfo.ToString());
        if (personInfo.Occupation == PersonAspect.OCCUPATION_ACTOR || personInfo.Occupation == PersonAspect.OCCUPATION_DIRECTOR ||
          personInfo.Occupation == PersonAspect.OCCUPATION_WRITER)
          OnlineMatcherService.DownloadSeriesFanArt(mediaItemId, personInfo);
      }
      else if (aspects.ContainsKey(CharacterAspect.ASPECT_ID))
      {
        CharacterInfo characterInfo = new CharacterInfo();
        characterInfo.FromMetadata(aspects);
        FanArtCache.InitFanArtCache(mediaItemId.ToString(), characterInfo.ToString());
        OnlineMatcherService.DownloadSeriesFanArt(mediaItemId, characterInfo);
      }
      else if (aspects.ContainsKey(CompanyAspect.ASPECT_ID))
      {
        CompanyInfo companyInfo = new CompanyInfo();
        companyInfo.FromMetadata(aspects);
        FanArtCache.InitFanArtCache(mediaItemId.ToString(), companyInfo.ToString());
        if (companyInfo.Type == CompanyAspect.COMPANY_PRODUCTION || companyInfo.Type == CompanyAspect.COMPANY_TV_NETWORK)
          OnlineMatcherService.DownloadSeriesFanArt(mediaItemId, companyInfo);
      }
    }

    private IResourceLocator GetResourceLocator(IDictionary<Guid, IList<MediaItemAspect>> aspects)
    {
      IList<MultipleMediaItemAspect> providerAspects;
      if (!MediaItemAspect.TryGetAspects(aspects, ProviderResourceAspect.Metadata, out providerAspects))
        return null;

      string systemId = (string)providerAspects[0][ProviderResourceAspect.ATTR_SYSTEM_ID];
      string resourceAccessorPath = (string)providerAspects[0][ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH];
      return new ResourceLocator(systemId, ResourcePath.Deserialize(resourceAccessorPath));
    }

    private void ExtractLocalImages(IDictionary<Guid, IList<MediaItemAspect>> aspects, Guid? seriesMediaItemId, Guid? seasonMediaItemId)
    {
      IResourceLocator mediaItemLocater = GetResourceLocator(aspects);
      if (mediaItemLocater.NativeResourcePath.IsNetworkResource) //No need to add it to cache if already locally available
      {
        ExtractFolderImages(mediaItemLocater, seriesMediaItemId, seasonMediaItemId);
        using (IResourceAccessor mediaItemAccessor = mediaItemLocater.CreateAccessor())
        {
          using (LocalFsResourceAccessorHelper rah = new LocalFsResourceAccessorHelper(mediaItemAccessor))
          {
            using (rah.LocalFsResourceAccessor.EnsureLocalFileSystemAccess())
            {
              ExtractMkvImages(rah.LocalFsResourceAccessor, seriesMediaItemId);
            }
          }
        }
      }
    }

    private void ExtractMkvImages(ILocalFsResourceAccessor lfsra, Guid? seriesMediaItemId)
    {
      if (!seriesMediaItemId.HasValue)
        return;

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
              if (FanArtCache.GetFanArtFiles(seriesMediaItemId.ToString(), patterns[pattern]).Count >= FanArtCache.MAX_FANART_IMAGES[patterns[pattern]])
                continue;

              string cacheFile = GetCacheFileName(seriesMediaItemId.Value, patterns[pattern],
                "File." + pattern + Path.GetFileNameWithoutExtension(lfsra.LocalFileSystemPath) + ".jpg");
              if (!File.Exists(cacheFile))
              {
                using (MemoryStream ms = new MemoryStream(binaryData))
                {
                  using (Image img = Image.FromStream(ms, true, true))
                    img.Save(cacheFile, System.Drawing.Imaging.ImageFormat.Jpeg);
                }
              }
            }
          }
        }
      }
      catch (Exception ex)
      {
        Logger.Warn("SeriesFanArtHandler: Exception while reading mkv attachments from '{0}'", ex, fileSystemPath);
      }
    }

    private void ExtractFolderImages(IResourceLocator mediaItemLocater, Guid? seriesMediaItemId, Guid? seasonMediaItemId)
    {
      string fileSystemPath = string.Empty;

      // File based access
      try
      {
        if (mediaItemLocater != null)
        {
          fileSystemPath = mediaItemLocater.NativeResourcePath.FileName;
          var mediaItemPath = mediaItemLocater.NativeResourcePath;
          var seasonMediaItemDirectoryPath = ResourcePathHelper.Combine(mediaItemPath, "../");
          var seriesMediaItemDirectoryPath = ResourcePathHelper.Combine(mediaItemPath, "../../");

          //Series fanart
          var fanArtPaths = new List<ResourcePath>();
          var posterPaths = new List<ResourcePath>();
          var bannerPaths = new List<ResourcePath>();
          var logoPaths = new List<ResourcePath>();
          var clearArtPaths = new List<ResourcePath>();
          if (seriesMediaItemId.HasValue)
          {
            using (var directoryRa = new ResourceLocator(mediaItemLocater.NativeSystemId, seriesMediaItemDirectoryPath).CreateAccessor())
            {
              var directoryFsra = directoryRa as IFileSystemResourceAccessor;
              if (directoryFsra != null)
              {
                var potentialFanArtFiles = GetPotentialFanArtFiles(directoryFsra);

                posterPaths.AddRange(
                    from potentialFanArtFile in potentialFanArtFiles
                    let potentialFanArtFileNameWithoutExtension = ResourcePathHelper.GetFileNameWithoutExtension(potentialFanArtFile.ToString())
                    where potentialFanArtFileNameWithoutExtension == "poster" || potentialFanArtFileNameWithoutExtension == "folder"
                    select potentialFanArtFile);

                logoPaths.AddRange(
                    from potentialFanArtFile in potentialFanArtFiles
                    let potentialFanArtFileNameWithoutExtension = ResourcePathHelper.GetFileNameWithoutExtension(potentialFanArtFile.ToString())
                    where potentialFanArtFileNameWithoutExtension == "logo"
                    select potentialFanArtFile);

                clearArtPaths.AddRange(
                    from potentialFanArtFile in potentialFanArtFiles
                    let potentialFanArtFileNameWithoutExtension = ResourcePathHelper.GetFileNameWithoutExtension(potentialFanArtFile.ToString())
                    where potentialFanArtFileNameWithoutExtension == "clearart"
                    select potentialFanArtFile);

                bannerPaths.AddRange(
                    from potentialFanArtFile in potentialFanArtFiles
                    let potentialFanArtFileNameWithoutExtension = ResourcePathHelper.GetFileNameWithoutExtension(potentialFanArtFile.ToString())
                    where potentialFanArtFileNameWithoutExtension == "banner"
                    select potentialFanArtFile);

                fanArtPaths.AddRange(
                    from potentialFanArtFile in potentialFanArtFiles
                    let potentialFanArtFileNameWithoutExtension = ResourcePathHelper.GetFileNameWithoutExtension(potentialFanArtFile.ToString())
                    where potentialFanArtFileNameWithoutExtension == "backdrop" || potentialFanArtFileNameWithoutExtension == "fanart"
                    select potentialFanArtFile);

                if (directoryFsra.ResourceExists("ExtraFanArt/"))
                  using (var extraFanArtDirectoryFsra = directoryFsra.GetResource("ExtraFanArt/"))
                    fanArtPaths.AddRange(GetPotentialFanArtFiles(extraFanArtDirectoryFsra));
              }
            }
            foreach (ResourcePath posterPath in posterPaths)
              SaveFolderFile(mediaItemLocater.NativeSystemId, posterPath, FanArtTypes.Poster, seriesMediaItemId.Value);
            foreach (ResourcePath logoPath in logoPaths)
              SaveFolderFile(mediaItemLocater.NativeSystemId, logoPath, FanArtTypes.Logo, seriesMediaItemId.Value);
            foreach (ResourcePath clearArtPath in clearArtPaths)
              SaveFolderFile(mediaItemLocater.NativeSystemId, clearArtPath, FanArtTypes.ClearArt, seriesMediaItemId.Value);
            foreach (ResourcePath bannerPath in bannerPaths)
              SaveFolderFile(mediaItemLocater.NativeSystemId, bannerPath, FanArtTypes.Banner, seriesMediaItemId.Value);
            foreach (ResourcePath fanartPath in fanArtPaths)
              SaveFolderFile(mediaItemLocater.NativeSystemId, fanartPath, FanArtTypes.FanArt, seriesMediaItemId.Value);
          }

          //Season fanart
          fanArtPaths.Clear();
          posterPaths.Clear();
          bannerPaths.Clear();
          logoPaths.Clear();
          clearArtPaths.Clear();
          if (seasonMediaItemId.HasValue)
          {
            using (var directoryRa = new ResourceLocator(mediaItemLocater.NativeSystemId, seasonMediaItemDirectoryPath).CreateAccessor())
            {
              var directoryFsra = directoryRa as IFileSystemResourceAccessor;
              if (directoryFsra != null)
              {
                var potentialFanArtFiles = GetPotentialFanArtFiles(directoryFsra);

                posterPaths.AddRange(
                    from potentialFanArtFile in potentialFanArtFiles
                    let potentialFanArtFileNameWithoutExtension = ResourcePathHelper.GetFileNameWithoutExtension(potentialFanArtFile.ToString())
                    where potentialFanArtFileNameWithoutExtension == "poster" || potentialFanArtFileNameWithoutExtension == "folder"
                    select potentialFanArtFile);

                bannerPaths.AddRange(
                    from potentialFanArtFile in potentialFanArtFiles
                    let potentialFanArtFileNameWithoutExtension = ResourcePathHelper.GetFileNameWithoutExtension(potentialFanArtFile.ToString())
                    where potentialFanArtFileNameWithoutExtension == "banner"
                    select potentialFanArtFile);

                fanArtPaths.AddRange(
                    from potentialFanArtFile in potentialFanArtFiles
                    let potentialFanArtFileNameWithoutExtension = ResourcePathHelper.GetFileNameWithoutExtension(potentialFanArtFile.ToString())
                    where potentialFanArtFileNameWithoutExtension == "backdrop" || potentialFanArtFileNameWithoutExtension == "fanart"
                    select potentialFanArtFile);

                if (directoryFsra.ResourceExists("ExtraFanArt/"))
                  using (var extraFanArtDirectoryFsra = directoryFsra.GetResource("ExtraFanArt/"))
                    fanArtPaths.AddRange(GetPotentialFanArtFiles(extraFanArtDirectoryFsra));
              }
            }
            foreach (ResourcePath posterPath in posterPaths)
              SaveFolderFile(mediaItemLocater.NativeSystemId, posterPath, FanArtTypes.Poster, seasonMediaItemId.Value);
            foreach (ResourcePath bannerPath in bannerPaths)
              SaveFolderFile(mediaItemLocater.NativeSystemId, bannerPath, FanArtTypes.Banner, seasonMediaItemId.Value);
            foreach (ResourcePath fanartPath in fanArtPaths)
              SaveFolderFile(mediaItemLocater.NativeSystemId, fanartPath, FanArtTypes.FanArt, seasonMediaItemId.Value);
          }
        }
      }
      catch (Exception ex)
      {
        Logger.Warn("SeriesFanArtHandler: Exception while reading folder images for '{0}'", ex, fileSystemPath);
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

    private void SaveFolderFile(string systemId, ResourcePath file, string fanartType, Guid parentId)
    {
      if (FanArtCache.GetFanArtFiles(parentId.ToString(), fanartType).Count >= FanArtCache.MAX_FANART_IMAGES[fanartType])
        return;

      string cacheFile = GetCacheFileName(parentId, fanartType, "Folder." + ResourcePathHelper.GetFileName(file.ToString()));
      if (!File.Exists(cacheFile))
      {
        using (var fileRa = new ResourceLocator(systemId, file).CreateAccessor())
        {
          var fileFsra = fileRa as IFileSystemResourceAccessor;
          if (fileFsra != null)
          {
            using (Stream ms = fileFsra.OpenRead())
            {
              using (Image img = Image.FromStream(ms, true, true))
                img.Save(cacheFile);
            }
          }
        }
      }
    }

    private string GetCacheFileName(Guid mediaItemId, string fanartType, string fileName)
    {
      string cacheFile = Path.Combine(CACHE_PATH,
              mediaItemId.ToString().ToUpperInvariant(), fanartType, fileName);

      string folder = Path.GetDirectoryName(cacheFile);
      if (!Directory.Exists(folder))
        Directory.CreateDirectory(folder);

      return cacheFile;
    }

    public void DeleteFanArt(Guid mediaItemId)
    {
      _checkCache.Remove(mediaItemId);
      Task.Run(() => FanArtCache.DeleteFanArtFiles(mediaItemId.ToString()));
    }

    private static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
