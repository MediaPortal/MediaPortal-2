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
      Guid? albumMediaItemId = null;
      IDictionary<Guid, string> artistMediaItems = new Dictionary<Guid, string>();
      SingleMediaItemAspect audioAspect;
      List<string> artists = new List<string>();
      if (MediaItemAspect.TryGetAspect(aspects, AudioAspect.Metadata, out audioAspect))
      {
        IEnumerable<string> artistObjects = audioAspect.GetCollectionAttribute<string>(AudioAspect.ATTR_ALBUMARTISTS);
        if (artistObjects != null)
          artists.AddRange(artistObjects);
      }

      IList<MultipleMediaItemAspect> relationAspects;
      if (MediaItemAspect.TryGetAspects(aspects, RelationshipAspect.Metadata, out relationAspects))
      {
        foreach (MultipleMediaItemAspect relation in relationAspects)
        {
          if ((Guid?)relation[RelationshipAspect.ATTR_LINKED_ROLE] == AudioAlbumAspect.ROLE_ALBUM)
          {
            albumMediaItemId = (Guid)relation[RelationshipAspect.ATTR_LINKED_ID];
          }
          if ((Guid?)relation[RelationshipAspect.ATTR_LINKED_ROLE] == PersonAspect.ROLE_ALBUMARTIST)
          {
            int? index = (int?)relation[RelationshipAspect.ATTR_RELATIONSHIP_INDEX];
            if (index.HasValue && artists.Count > index.Value && index.Value >= 0)
              artistMediaItems.Add((Guid)relation[RelationshipAspect.ATTR_LINKED_ID], artists[index.Value]);
          }
        }
      }

      if(albumMediaItemId.HasValue && artistMediaItems.Count > 0)
      {
        if (_checkCache.Contains(mediaItemId) && _checkCache.Contains(albumMediaItemId.Value) && _checkCache.Contains(artistMediaItems.Keys.First()))
          return;
      }
      else if (albumMediaItemId.HasValue)
      {
        if (_checkCache.Contains(mediaItemId) && _checkCache.Contains(albumMediaItemId.Value))
          return;
      }
      else
      {
        if (_checkCache.Contains(mediaItemId))
          return;
      }

      Task.Run(() => ExtractFanArt(mediaItemId, aspects, albumMediaItemId, artistMediaItems));
      _checkCache.Add(mediaItemId);
      if (albumMediaItemId.HasValue)
        _checkCache.Add(albumMediaItemId.Value);
      if (artistMediaItems.Count > 0)
        _checkCache.Add(artistMediaItems.Keys.First());
    }

    private void ExtractFanArt(Guid mediaItemId, IDictionary<Guid, IList<MediaItemAspect>> aspects, Guid? albumMediaItemId, IDictionary<Guid, string> artistMediaItems)
    {
      if (aspects.ContainsKey(AudioAspect.ASPECT_ID))
      {
        if (BaseInfo.IsVirtualResource(aspects))
          return;

        TrackInfo trackInfo = new TrackInfo();
        trackInfo.FromMetadata(aspects);
        bool forceFanart = !trackInfo.IsRefreshed;
        AlbumInfo albumInfo = trackInfo.CloneBasicInstance<AlbumInfo>();
        ExtractLocalImages(aspects, albumMediaItemId, artistMediaItems, albumInfo.ToString());
        if(!AudioMetadataExtractor.SkipFanArtDownload)
          OnlineMatcherService.Instance.DownloadAudioFanArt(mediaItemId, trackInfo, forceFanart);

        if (albumMediaItemId.HasValue && !_checkCache.Contains(albumMediaItemId.Value))
        {
          if (!AudioMetadataExtractor.SkipFanArtDownload)
            OnlineMatcherService.Instance.DownloadAudioFanArt(albumMediaItemId.Value, albumInfo, forceFanart);
          _checkCache.Add(albumMediaItemId.Value);
        }
      }
      else if (aspects.ContainsKey(PersonAspect.ASPECT_ID))
      {
        PersonInfo personInfo = new PersonInfo();
        personInfo.FromMetadata(aspects);
        if (personInfo.Occupation == PersonAspect.OCCUPATION_ARTIST || personInfo.Occupation == PersonAspect.OCCUPATION_COMPOSER)
        {
          if (!AudioMetadataExtractor.SkipFanArtDownload)
            OnlineMatcherService.Instance.DownloadAudioFanArt(mediaItemId, personInfo, !personInfo.IsRefreshed);
        }
      }
      else if (aspects.ContainsKey(CompanyAspect.ASPECT_ID))
      {
        CompanyInfo companyInfo = new CompanyInfo();
        companyInfo.FromMetadata(aspects);
        if (companyInfo.Type == CompanyAspect.COMPANY_MUSIC_LABEL)
        {
          if (!AudioMetadataExtractor.SkipFanArtDownload)
            OnlineMatcherService.Instance.DownloadAudioFanArt(mediaItemId, companyInfo, !companyInfo.IsRefreshed);
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

    private void ExtractLocalImages(IDictionary<Guid, IList<MediaItemAspect>> aspects, Guid? albumMediaItemId, IDictionary<Guid, string> artistMediaItems, string albumTitle)
    {
      if (BaseInfo.IsVirtualResource(aspects))
        return;

      IResourceLocator mediaItemLocater = GetResourceLocator(aspects);
      if (mediaItemLocater == null)
        return;

      ExtractFolderImages(mediaItemLocater, albumMediaItemId, artistMediaItems, albumTitle);
      using (IResourceAccessor mediaItemAccessor = mediaItemLocater.CreateAccessor())
      {
        using (LocalFsResourceAccessorHelper rah = new LocalFsResourceAccessorHelper(mediaItemAccessor))
        {
          using (rah.LocalFsResourceAccessor.EnsureLocalFileSystemAccess())
          {
            ExtractFileImages(rah.LocalFsResourceAccessor, albumMediaItemId, albumTitle);
          }
        }
      }
    }

    private void ExtractFileImages(ILocalFsResourceAccessor lfsra, Guid? albumMediaItemId, string albumTitle)
    {
      if (!albumMediaItemId.HasValue)
        return;

      string mediaItemId = albumMediaItemId.Value.ToString().ToUpperInvariant();
      TagLib.File tag;
      try
      {
        ByteVector.UseBrokenLatin1Behavior = true;  // Otherwise we have problems retrieving non-latin1 chars
        tag = TagLib.File.Create(new AudioMetadataExtractor.ResourceProviderFileAbstraction(lfsra));
      }
      catch (CorruptFileException)
      {
        // Only log at the info level here - And simply return false. This makes the importer know that we
        // couldn't perform our task here.
        Logger.Info("AudioFanArtHandler: Audio file '{0}' seems to be broken", lfsra.CanonicalLocalResourcePath);
        return;
      }

      using (tag)
      {
        IPicture[] pics = tag.Tag.Pictures;
        if (pics.Length > 0)
        {
          try
          {
            string fanArtType = FanArtTypes.Cover;
            using (FanArtCache.FanArtCountLock countLock = FanArtCache.GetFanArtCountLock(mediaItemId, fanArtType))
            {
              if (countLock.Count >= FanArtCache.MAX_FANART_IMAGES[fanArtType])
                return;

              FanArtCache.InitFanArtCache(mediaItemId, albumTitle);
              string cacheFile = GetCacheFileName(mediaItemId, fanArtType,
                "File." + Path.GetFileNameWithoutExtension(lfsra.LocalFileSystemPath) + ".jpg");
              if (!System.IO.File.Exists(cacheFile))
              {
                using (MemoryStream ms = new MemoryStream(pics[0].Data.Data))
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
          // Decoding of invalid image data can fail, but main MediaItem is correct.
          catch { }
        }
      }
    }

    private void ExtractFolderImages(IResourceLocator mediaItemLocater, Guid? albumMediaItemId, IDictionary<Guid, string> artistMediaItems, string albumTitle)
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
          var albumMediaItemDirectoryPath = ResourcePathHelper.Combine(mediaItemPath, "../");
          var artistMediaItemDirectoryPath = ResourcePathHelper.Combine(mediaItemPath, "../../");
          if (AudioMetadataExtractor.IsDiscFolder(albumTitle, albumMediaItemDirectoryPath.FileName))
          {
            //Probably a CD folder so try next parent
            albumMediaItemDirectoryPath = ResourcePathHelper.Combine(mediaItemPath, "../../");
            artistMediaItemDirectoryPath = ResourcePathHelper.Combine(mediaItemPath, "../../../");
          }

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
                if (artistMediaItems.Count > 0)
                {
                  //Get Artists thumbs
                  IFileSystemResourceAccessor alternateArtistMediaItemDirectory = directoryFsra.GetResource(".artists");
                  if (alternateArtistMediaItemDirectory != null)
                  {
                    foreach (var artist in artistMediaItems)
                    {
                      var potentialArtistFanArtFiles = GetPotentialFanArtFiles(alternateArtistMediaItemDirectory);

                      foreach (ResourcePath thumbPath in
                          from potentialFanArtFile in potentialArtistFanArtFiles
                          let potentialFanArtFileNameWithoutExtension = ResourcePathHelper.GetFileNameWithoutExtension(potentialFanArtFile.ToString())
                          where potentialFanArtFileNameWithoutExtension.StartsWith(artist.Value.Replace(" ", "_"), StringComparison.InvariantCultureIgnoreCase)
                          select potentialFanArtFile)
                        SaveFolderFile(mediaItemLocater, thumbPath, FanArtTypes.Thumbnail, artist.Key, artist.Value);
                    }
                  }
                }

                var potentialFanArtFiles = GetPotentialFanArtFiles(directoryFsra);

                coverPaths.AddRange(
                    from potentialFanArtFile in potentialFanArtFiles
                    let potentialFanArtFileNameWithoutExtension = ResourcePathHelper.GetFileNameWithoutExtension(potentialFanArtFile.ToString()).ToLowerInvariant()
                    where potentialFanArtFileNameWithoutExtension == "poster" || potentialFanArtFileNameWithoutExtension == "folder" ||
                    potentialFanArtFileNameWithoutExtension == "cover"
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
            foreach (ResourcePath posterPath in coverPaths)
              SaveFolderFile(mediaItemLocater, posterPath, FanArtTypes.Cover, albumMediaItemId.Value, albumTitle);
            foreach (ResourcePath fanartPath in fanArtPaths)
              SaveFolderFile(mediaItemLocater, fanartPath, FanArtTypes.FanArt, albumMediaItemId.Value, albumTitle);


            //Artist fanart
            fanArtPaths.Clear();
            coverPaths.Clear();
            bannerPaths.Clear();
            logoPaths.Clear();
            clearArtPaths.Clear();
            thumbPaths.Clear();
            if (artistMediaItems.Count > 0)
            {
              using (var directoryRa = new ResourceLocator(mediaItemLocater.NativeSystemId, artistMediaItemDirectoryPath).CreateAccessor())
              {
                var directoryFsra = directoryRa as IFileSystemResourceAccessor;
                if (directoryFsra != null)
                {
                  Guid artistId = artistMediaItems.Where(a => string.Compare(a.Value, directoryFsra.ResourceName, true) == 0).Select(a => a.Key).FirstOrDefault();
                  if (artistId == Guid.Empty && artistMediaItems.Count == 1)
                    artistId = artistMediaItems.First().Key;
                  if (artistId != Guid.Empty)
                  {
                    var potentialFanArtFiles = GetPotentialFanArtFiles(directoryFsra);

                    thumbPaths.AddRange(
                        from potentialFanArtFile in potentialFanArtFiles
                        let potentialFanArtFileNameWithoutExtension = ResourcePathHelper.GetFileNameWithoutExtension(potentialFanArtFile.ToString()).ToLowerInvariant()
                        where potentialFanArtFileNameWithoutExtension.StartsWith("thumb") || potentialFanArtFileNameWithoutExtension.StartsWith("folder") ||
                        potentialFanArtFileNameWithoutExtension.StartsWith("artist")
                        select potentialFanArtFile);

                    bannerPaths.AddRange(
                      from potentialFanArtFile in potentialFanArtFiles
                      let potentialFanArtFileNameWithoutExtension = ResourcePathHelper.GetFileNameWithoutExtension(potentialFanArtFile.ToString()).ToLowerInvariant()
                      where potentialFanArtFileNameWithoutExtension.StartsWith("banner")
                      select potentialFanArtFile);

                    logoPaths.AddRange(
                      from potentialFanArtFile in potentialFanArtFiles
                      let potentialFanArtFileNameWithoutExtension = ResourcePathHelper.GetFileNameWithoutExtension(potentialFanArtFile.ToString()).ToLowerInvariant()
                      where potentialFanArtFileNameWithoutExtension.StartsWith("logo")
                      select potentialFanArtFile);

                    clearArtPaths.AddRange(
                        from potentialFanArtFile in potentialFanArtFiles
                        let potentialFanArtFileNameWithoutExtension = ResourcePathHelper.GetFileNameWithoutExtension(potentialFanArtFile.ToString()).ToLowerInvariant()
                        where potentialFanArtFileNameWithoutExtension.StartsWith("clearart")
                        select potentialFanArtFile);

                    fanArtPaths.AddRange(
                        from potentialFanArtFile in potentialFanArtFiles
                        let potentialFanArtFileNameWithoutExtension = ResourcePathHelper.GetFileNameWithoutExtension(potentialFanArtFile.ToString()).ToLowerInvariant()
                        where potentialFanArtFileNameWithoutExtension.StartsWith("backdrop") || potentialFanArtFileNameWithoutExtension.StartsWith("fanart")
                        select potentialFanArtFile);

                    if (directoryFsra.ResourceExists("ExtraFanArt/"))
                      using (var extraFanArtDirectoryFsra = directoryFsra.GetResource("ExtraFanArt/"))
                        fanArtPaths.AddRange(GetPotentialFanArtFiles(extraFanArtDirectoryFsra));

                    foreach (ResourcePath thumbPath in thumbPaths)
                      SaveFolderFile(mediaItemLocater, thumbPath, FanArtTypes.Thumbnail, artistId, artistMediaItems[artistId]);
                    foreach (ResourcePath bannerPath in bannerPaths)
                      SaveFolderFile(mediaItemLocater, bannerPath, FanArtTypes.Banner, artistId, artistMediaItems[artistId]);
                    foreach (ResourcePath logoPath in logoPaths)
                      SaveFolderFile(mediaItemLocater, logoPath, FanArtTypes.Logo, artistId, artistMediaItems[artistId]);
                    foreach (ResourcePath clearArtPath in clearArtPaths)
                      SaveFolderFile(mediaItemLocater, clearArtPath, FanArtTypes.ClearArt, artistId, artistMediaItems[artistId]);
                    foreach (ResourcePath fanartPath in fanArtPaths)
                      SaveFolderFile(mediaItemLocater, fanartPath, FanArtTypes.FanArt, artistId, artistMediaItems[artistId]);
                  }
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

    private void SaveFolderFile(IResourceLocator mediaItemLocater, ResourcePath file, string fanartType, Guid parentId, string title)
    {
      string mediaItemId = parentId.ToString().ToUpperInvariant();
      using (FanArtCache.FanArtCountLock countLock = FanArtCache.GetFanArtCountLock(mediaItemId, fanartType))
      {
        if (countLock.Count >= FanArtCache.MAX_FANART_IMAGES[fanartType])
          return;

        if ((AudioMetadataExtractor.CacheOfflineFanArt && mediaItemLocater.NativeResourcePath.IsNetworkResource) ||
          (AudioMetadataExtractor.CacheLocalFanArt && !mediaItemLocater.NativeResourcePath.IsNetworkResource && mediaItemLocater.NativeResourcePath.IsValidLocalPath))
        {
          FanArtCache.InitFanArtCache(mediaItemId, title);
          string cacheFile = GetCacheFileName(mediaItemId, fanartType, "Folder." + ResourcePathHelper.GetFileName(file.ToString()));
          if (!System.IO.File.Exists(cacheFile))
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
