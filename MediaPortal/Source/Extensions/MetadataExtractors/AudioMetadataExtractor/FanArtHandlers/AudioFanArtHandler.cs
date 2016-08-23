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
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.FanArt;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.PathManager;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess;
using MediaPortal.Common.Settings;
using MediaPortal.Extensions.MetadataExtractors.AudioMetadataExtractor.Settings;
using MediaPortal.Extensions.OnlineLibraries;
using TagLib;
using static MediaPortal.Extensions.MetadataExtractors.AudioMetadataExtractor.AudioMetadataExtractor;

namespace MediaPortal.Extensions.MetadataExtractors.AudioMetadataExtractor
{
  class AudioFanArtHandler : IMediaFanArtHandler
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

    private static ICollection<string> AUDIO_EXTENSIONS = new List<string>();

    private static readonly ICollection<String> IMG_EXTENSIONS = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase) { ".jpg", ".png", ".tbn" };

    public static string CACHE_PATH = ServiceRegistration.Get<IPathManager>().GetPath(@"<DATA>\FanArt\");

    #endregion

    protected FanArtHandlerMetadata _metadata;
    private SynchronizedCollection<Guid> _checkCache = new SynchronizedCollection<Guid>();

    public AudioFanArtHandler()
    {
      _metadata = new FanArtHandlerMetadata(FANARTHANDLER_ID, "Audio FanArt handler");

      AudioMetadataExtractorSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<AudioMetadataExtractorSettings>();
      AUDIO_EXTENSIONS = new List<string>(settings.AudioExtensions.Select(e => e.ToLowerInvariant()));
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

      Guid? albumMediaItemId = null;
      IList<Guid> artistMediaItemIds = new List<Guid>();
      IList<MultipleMediaItemAspect> relationAspects;
      if (MediaItemAspect.TryGetAspects(aspects, RelationshipAspect.Metadata, out relationAspects))
      {
        foreach (MultipleMediaItemAspect relation in relationAspects)
        {
          if ((Guid?)relation[RelationshipAspect.ATTR_LINKED_ROLE] == AudioAlbumAspect.ROLE_ALBUM)
          {
            albumMediaItemId = (Guid)relation[RelationshipAspect.ATTR_LINKED_ID];
          }
          if ((Guid?)relation[RelationshipAspect.ATTR_LINKED_ROLE] == PersonAspect.ROLE_ARTIST)
          {
            artistMediaItemIds.Add((Guid)relation[RelationshipAspect.ATTR_LINKED_ID]);
          }
        }
      }
      Task.Run(() => ExtractFanArt(mediaItemId, aspects, albumMediaItemId, artistMediaItemIds));
      _checkCache.Add(mediaItemId);
    }

    private void ExtractFanArt(Guid mediaItemId, IDictionary<Guid, IList<MediaItemAspect>> aspects, Guid? albumMediaItemId, IList<Guid> artistMediaItemIds)
    {
      if (aspects.ContainsKey(AudioAspect.ASPECT_ID))
      {
        TrackInfo trackInfo = new TrackInfo();
        trackInfo.FromMetadata(aspects);
        FanArtCache.InitFanArtCache(mediaItemId.ToString(), trackInfo.ToString());
        ExtractLocalImages(aspects, albumMediaItemId, artistMediaItemIds);
        OnlineMatcherService.DownloadAudioFanArt(mediaItemId, trackInfo);

        if (albumMediaItemId.HasValue && !_checkCache.Contains(albumMediaItemId.Value))
        {
          AlbumInfo albumInfo = trackInfo.CloneBasicInstance<AlbumInfo>();
          FanArtCache.InitFanArtCache(albumMediaItemId.Value.ToString(), albumInfo.ToString());
          OnlineMatcherService.DownloadAudioFanArt(albumMediaItemId.Value, albumInfo);
          _checkCache.Add(albumMediaItemId.Value);
        }
      }
      else if (aspects.ContainsKey(PersonAspect.ASPECT_ID))
      {
        PersonInfo personInfo = new PersonInfo();
        personInfo.FromMetadata(aspects);
        FanArtCache.InitFanArtCache(mediaItemId.ToString(), personInfo.ToString());
        if (personInfo.Occupation == PersonAspect.OCCUPATION_ARTIST || personInfo.Occupation == PersonAspect.OCCUPATION_COMPOSER)
          OnlineMatcherService.DownloadAudioFanArt(mediaItemId, personInfo);
      }
      else if (aspects.ContainsKey(CompanyAspect.ASPECT_ID))
      {
        CompanyInfo companyInfo = new CompanyInfo();
        companyInfo.FromMetadata(aspects);
        FanArtCache.InitFanArtCache(mediaItemId.ToString(), companyInfo.ToString());
        if (companyInfo.Type == CompanyAspect.COMPANY_MUSIC_LABEL)
          OnlineMatcherService.DownloadAudioFanArt(mediaItemId, companyInfo);
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

    private void ExtractLocalImages(IDictionary<Guid, IList<MediaItemAspect>> aspects, Guid? albumMediaItemId, IList<Guid> artistMediaItemIds)
    {
      IResourceLocator mediaItemLocater = GetResourceLocator(aspects);
      if (mediaItemLocater.NativeResourcePath.IsNetworkResource) //No need to add it to cache if already locally available
      {
        ExtractFolderImages(mediaItemLocater, albumMediaItemId, artistMediaItemIds);
        using (IResourceAccessor mediaItemAccessor = mediaItemLocater.CreateAccessor())
        {
          using (LocalFsResourceAccessorHelper rah = new LocalFsResourceAccessorHelper(mediaItemAccessor))
          {
            using (rah.LocalFsResourceAccessor.EnsureLocalFileSystemAccess())
            {
              ExtractFileImages(rah.LocalFsResourceAccessor, albumMediaItemId);
            }
          }
        }
      }
    }

    private void ExtractFileImages(ILocalFsResourceAccessor lfsra, Guid? albumMediaItemId)
    {
      if (!albumMediaItemId.HasValue)
        return;

      TagLib.File tag;
      try
      {
        ByteVector.UseBrokenLatin1Behavior = true;  // Otherwise we have problems retrieving non-latin1 chars
        tag = TagLib.File.Create(new ResourceProviderFileAbstraction(lfsra));
      }
      catch (CorruptFileException)
      {
        // Only log at the info level here - And simply return false. This makes the importer know that we
        // couldn't perform our task here.
        Logger.Info("AudioFanArtHandler: Audio file '{0}' seems to be broken", lfsra.CanonicalLocalResourcePath);
        return;
      }

      IPicture[] pics = tag.Tag.Pictures;
      if (pics.Length > 0)
      {
        try
        {
          if (FanArtCache.GetFanArtFiles(albumMediaItemId.ToString(), FanArtTypes.Cover).Count >= FanArtCache.MAX_FANART_IMAGES[FanArtTypes.Cover])
            return;

          string cacheFile = GetCacheFileName(albumMediaItemId.Value, FanArtTypes.Cover,
            "File." + Path.GetFileNameWithoutExtension(lfsra.LocalFileSystemPath) + ".jpg");
          if (!System.IO.File.Exists(cacheFile))
          {
            using (MemoryStream ms = new MemoryStream(pics[0].Data.Data))
            {
              using (Image img = Image.FromStream(ms, true, true))
                img.Save(cacheFile, System.Drawing.Imaging.ImageFormat.Jpeg);
            }
          }
        }
        // Decoding of invalid image data can fail, but main MediaItem is correct.
        catch { }
      }
    }

    private void ExtractFolderImages(IResourceLocator mediaItemLocater, Guid? albumMediaItemId, IList<Guid> artistMediaItemIds)
    {
      string fileSystemPath = string.Empty;

      // File based access
      try
      {
        if (mediaItemLocater != null)
        {
          fileSystemPath = mediaItemLocater.NativeResourcePath.FileName;
          var mediaItemPath = mediaItemLocater.NativeResourcePath;
          var mediaItemFileNameWithoutExtension = ResourcePathHelper.GetFileNameWithoutExtension(mediaItemPath.ToString());
          var albumMediaItemDirectoryPath = ResourcePathHelper.Combine(mediaItemPath, "../");
          var artistMediaItemDirectoryPath = ResourcePathHelper.Combine(mediaItemPath, "../../");

          //Album fanart
          var fanArtPaths = new List<ResourcePath>();
          var coverPaths = new List<ResourcePath>();
          var bannerPaths = new List<ResourcePath>();
          var logoPaths = new List<ResourcePath>();
          var clearArtPaths = new List<ResourcePath>();
          var thumbPaths = new List<ResourcePath>();
          if (albumMediaItemId.HasValue)
          {
            using (var directoryRa = new ResourceLocator(mediaItemLocater.NativeSystemId, albumMediaItemDirectoryPath).CreateAccessor())
            {
              var directoryFsra = directoryRa as IFileSystemResourceAccessor;
              if (directoryFsra != null)
              {
                var potentialFanArtFiles = GetPotentialFanArtFiles(directoryFsra);

                coverPaths.AddRange(
                    from potentialFanArtFile in potentialFanArtFiles
                    let potentialFanArtFileNameWithoutExtension = ResourcePathHelper.GetFileNameWithoutExtension(potentialFanArtFile.ToString())
                    where potentialFanArtFileNameWithoutExtension == "poster" || potentialFanArtFileNameWithoutExtension == "folder" ||
                    potentialFanArtFileNameWithoutExtension == "cover"
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
            foreach (ResourcePath posterPath in coverPaths)
              SaveFolderFile(mediaItemLocater.NativeSystemId, posterPath, FanArtTypes.Cover, albumMediaItemId.Value);
            foreach (ResourcePath fanartPath in fanArtPaths)
              SaveFolderFile(mediaItemLocater.NativeSystemId, fanartPath, FanArtTypes.FanArt, albumMediaItemId.Value);


            //Artist fanart
            fanArtPaths.Clear();
            coverPaths.Clear();
            bannerPaths.Clear();
            logoPaths.Clear();
            clearArtPaths.Clear();
            thumbPaths.Clear();
            if (artistMediaItemIds.Count > 0)
            {
              using (var directoryRa = new ResourceLocator(mediaItemLocater.NativeSystemId, artistMediaItemDirectoryPath).CreateAccessor())
              {
                var directoryFsra = directoryRa as IFileSystemResourceAccessor;
                if (directoryFsra != null)
                {
                  var potentialFanArtFiles = GetPotentialFanArtFiles(directoryFsra);

                  thumbPaths.AddRange(
                      from potentialFanArtFile in potentialFanArtFiles
                      let potentialFanArtFileNameWithoutExtension = ResourcePathHelper.GetFileNameWithoutExtension(potentialFanArtFile.ToString())
                      where potentialFanArtFileNameWithoutExtension.StartsWith("thumb") || potentialFanArtFileNameWithoutExtension.StartsWith("folder") ||
                      potentialFanArtFileNameWithoutExtension.StartsWith("artist")
                      select potentialFanArtFile);

                  bannerPaths.AddRange(
                    from potentialFanArtFile in potentialFanArtFiles
                    let potentialFanArtFileNameWithoutExtension = ResourcePathHelper.GetFileNameWithoutExtension(potentialFanArtFile.ToString())
                    where potentialFanArtFileNameWithoutExtension.StartsWith("banner")
                    select potentialFanArtFile);

                  logoPaths.AddRange(
                    from potentialFanArtFile in potentialFanArtFiles
                    let potentialFanArtFileNameWithoutExtension = ResourcePathHelper.GetFileNameWithoutExtension(potentialFanArtFile.ToString())
                    where potentialFanArtFileNameWithoutExtension.StartsWith("logo")
                    select potentialFanArtFile);

                  clearArtPaths.AddRange(
                      from potentialFanArtFile in potentialFanArtFiles
                      let potentialFanArtFileNameWithoutExtension = ResourcePathHelper.GetFileNameWithoutExtension(potentialFanArtFile.ToString())
                      where potentialFanArtFileNameWithoutExtension.StartsWith("clearart")
                      select potentialFanArtFile);

                  fanArtPaths.AddRange(
                      from potentialFanArtFile in potentialFanArtFiles
                      let potentialFanArtFileNameWithoutExtension = ResourcePathHelper.GetFileNameWithoutExtension(potentialFanArtFile.ToString())
                      where potentialFanArtFileNameWithoutExtension.StartsWith("backdrop") || potentialFanArtFileNameWithoutExtension.StartsWith("fanart")
                      select potentialFanArtFile);

                  if (directoryFsra.ResourceExists("ExtraFanArt/"))
                    using (var extraFanArtDirectoryFsra = directoryFsra.GetResource("ExtraFanArt/"))
                      fanArtPaths.AddRange(GetPotentialFanArtFiles(extraFanArtDirectoryFsra));
                }
              }
              if (artistMediaItemIds.Count == 1)
              {
                foreach (ResourcePath thumbPath in thumbPaths)
                  SaveFolderFile(mediaItemLocater.NativeSystemId, thumbPath, FanArtTypes.Thumbnail, artistMediaItemIds[0]);
                foreach (ResourcePath bannerPath in bannerPaths)
                  SaveFolderFile(mediaItemLocater.NativeSystemId, bannerPath, FanArtTypes.Banner, artistMediaItemIds[0]);
                foreach (ResourcePath logoPath in logoPaths)
                  SaveFolderFile(mediaItemLocater.NativeSystemId, logoPath, FanArtTypes.Logo, artistMediaItemIds[0]);
                foreach (ResourcePath clearArtPath in clearArtPaths)
                  SaveFolderFile(mediaItemLocater.NativeSystemId, clearArtPath, FanArtTypes.ClearArt, artistMediaItemIds[0]);
                foreach (ResourcePath fanartPath in fanArtPaths)
                  SaveFolderFile(mediaItemLocater.NativeSystemId, fanartPath, FanArtTypes.FanArt, artistMediaItemIds[0]);
              }
              else if (artistMediaItemIds.Count > 1)
              {
                for (int i = 0; i < artistMediaItemIds.Count; i++)
                {
                  if (thumbPaths.Count > i)
                    SaveFolderFile(mediaItemLocater.NativeSystemId, thumbPaths[i], FanArtTypes.Thumbnail, artistMediaItemIds[i]);
                  if (bannerPaths.Count > i)
                    SaveFolderFile(mediaItemLocater.NativeSystemId, bannerPaths[i], FanArtTypes.Banner, artistMediaItemIds[i]);
                  if (logoPaths.Count > i)
                    SaveFolderFile(mediaItemLocater.NativeSystemId, logoPaths[i], FanArtTypes.Logo, artistMediaItemIds[i]);
                  if (clearArtPaths.Count > i)
                    SaveFolderFile(mediaItemLocater.NativeSystemId, clearArtPaths[i], FanArtTypes.ClearArt, artistMediaItemIds[i]);
                  if (fanArtPaths.Count > i)
                    SaveFolderFile(mediaItemLocater.NativeSystemId, fanArtPaths[i], FanArtTypes.FanArt, artistMediaItemIds[i]);
                }
              }
            }
          }
        }
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Warn("AudioFanArtHandler: Exception while reading folder images for '{0}'", ex, fileSystemPath);
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
      if (!System.IO.File.Exists(cacheFile))
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
      Task.Run(() => FanArtCache.DeleteFanArtFiles(mediaItemId.ToString()));
    }

    private static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
