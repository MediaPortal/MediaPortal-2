using Emulators.Common.Games;
using Emulators.Common.Matchers;
using MediaPortal.Common.MediaManagement;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Emulators.Common.Settings;
using MediaPortal.Common;
using MediaPortal.Common.FanArt;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess;
using MediaPortal.Common.Services.Settings;

namespace Emulators.Server.FanArtHandlers
{
  public class GameFanArtHandler : BaseFanArtHandler
  {
    protected static readonly Guid ID = new Guid("E37E37A2-58E9-46FF-AEF9-7AB70E601489");
    protected static readonly Guid[] FANART_ASPECTS = { GameAspect.ASPECT_ID };

    protected SettingsChangeWatcher<ImporterSettings> _settingWatcher;
    protected bool _skipFanArtDownload { get; private set; }
    protected bool _cacheOfflineFanArt { get; private set; }
    protected bool _cacheLocalFanArt { get; private set; }

    public GameFanArtHandler() : base(new FanArtHandlerMetadata(ID, "Game FanArt handler"), FANART_ASPECTS)
    {
      _settingWatcher = new SettingsChangeWatcher<ImporterSettings>();
      _settingWatcher.SettingsChanged += SettingsChanged;
    }
    
    #region Settings

    protected void LoadSettings()
    {
      _skipFanArtDownload = _settingWatcher.Settings.SkipFanArtDownload;
      _cacheOfflineFanArt = _settingWatcher.Settings.CacheOfflineFanArt;
      _cacheLocalFanArt = _settingWatcher.Settings.CacheLocalFanArt;
    }

    private void SettingsChanged(object sender, EventArgs e)
    {
      LoadSettings();
    }

    #endregion

    public override async Task CollectFanArtAsync(Guid mediaItemId, IDictionary<Guid, IList<MediaItemAspect>> aspects)
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
          _cacheLocalFanArt, _cacheOfflineFanArt);
      }

      if (!shouldCacheLocal && _skipFanArtDownload)
        return; //Nothing to do

      string title = "";
      if (MediaItemAspect.TryGetAttribute(aspects, GameAspect.ATTR_GAME_NAME, out string gameName))
        title = gameName;
      else if (MediaItemAspect.TryGetAttribute(aspects, MediaAspect.ATTR_TITLE, out string mediaName))
        title = mediaName;

      if (shouldCacheLocal)
        await ExtractGameFolderFanArt(mediaItemLocator, mediaItemId, title).ConfigureAwait(false);

      if (!_skipFanArtDownload)
        await GameMatcher.Instance.DownloadFanArtAsync(mediaItemId, aspects);
    }

    /// <summary>
    /// Gets all game folder images and caches them in the <see cref="IFanArtCache"/> service.
    /// </summary>
    /// <param name="mediaItemLocator"><see cref="IResourceLocator>"/> that points to the file.</param>
    /// <param name="gameMediaItemId">Id of the media item.</param>
    /// <param name="title">Title of the media item.</param>
    /// <returns><see cref="Task"/> that completes when the images have been cached.</returns>
    protected async Task ExtractGameFolderFanArt(IResourceLocator mediaItemLocator, Guid gameMediaItemId, string title)
    {
      //Get the file's directory
      var gameDirectory = ResourcePathHelper.Combine(mediaItemLocator.NativeResourcePath, "../");
      try
      {
        var mediaItemFileName = ResourcePathHelper.GetFileNameWithoutExtension(mediaItemLocator.NativeResourcePath.ToString()).ToLowerInvariant();
        FanArtPathCollection paths = null;
        using (IResourceAccessor accessor = new ResourceLocator(mediaItemLocator.NativeSystemId, gameDirectory).CreateAccessor())
          if (accessor is IFileSystemResourceAccessor fsra)
          {
            paths = GetGameFolderFanArt(fsra, mediaItemFileName);
          }

        if (paths != null)
          await SaveFolderImagesToCache(mediaItemLocator.NativeSystemId, paths, gameMediaItemId, title).ConfigureAwait(false);
      }
      catch (Exception ex)
      {
        Logger.Warn("GameFanArtHandler: Exception while reading folder images for '{0}'", ex, gameDirectory);
      }
    }

    /// <summary>
    /// Gets a <see cref="FanArtPathCollection"/> containing all matching fanart paths in the specified <see cref="ResourcePath"/>.
    /// </summary>
    /// <param name="gameDirectory"><see cref="IFileSystemResourceAccessor"/> that points to the game directory.</param>
    /// <param name="filename">The file name of the media item to extract images for.</param>
    /// <returns><see cref="FanArtPathCollection"/> containing all matching paths.</returns>
    protected FanArtPathCollection GetGameFolderFanArt(IFileSystemResourceAccessor gameDirectory, string filename)
    {
      FanArtPathCollection paths = new FanArtPathCollection();
      if (gameDirectory == null)
        return paths;

      //Get all fanart in the current directory
      List<ResourcePath> potentialFanArtFiles = LocalFanartHelper.GetPotentialFanArtFiles(gameDirectory);
      filename = filename.ToLowerInvariant();
      ExtractAllFanArtImages(potentialFanArtFiles, paths, filename);

      return paths;
    }

    public override void DeleteFanArt(Guid mediaItemId)
    {
      ServiceRegistration.Get<IFanArtCache>().DeleteFanArtFiles(mediaItemId);
    }
  }
}
