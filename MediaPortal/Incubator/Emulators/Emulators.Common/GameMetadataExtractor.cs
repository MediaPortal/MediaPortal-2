using Emulators.Common.Emulators;
using Emulators.Common.Games;
using Emulators.Common.Matchers;
using Emulators.Common.Settings;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.Settings;
using MediaPortal.Common.Settings;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Emulators.Common
{
  public class GameMetadataExtractor : IMetadataExtractor
  {
    protected static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }

    public const string METADATAEXTRACTOR_ID_STR = "7ED0605F-E3B3-4B4A-AD58-AE56BC17A3E5";
    public static Guid METADATAEXTRACTOR_ID = new Guid(METADATAEXTRACTOR_ID_STR);

    protected static readonly Guid ISORESOURCEPROVIDER_ID = new Guid("112728B1-F71D-4284-9E5C-3462E8D3C74D");
    protected static readonly Guid ZIPRESOURCEPROVIDER_ID = new Guid("6B042DB8-69AD-4B57-B869-1BCEA4E43C77");

    protected static ICollection<MediaCategory> MEDIA_CATEGORIES = new List<MediaCategory>();
    protected static MediaCategory _gameCategory;
    protected static ConcurrentDictionary<string, MediaCategory> _platformCategories = new ConcurrentDictionary<string, MediaCategory>();
    protected MetadataExtractorMetadata _metadata;
    protected readonly SettingsChangeWatcher<CommonSettings> _settingsWatcher = new SettingsChangeWatcher<CommonSettings>();

    static GameMetadataExtractor()
    {
      IMediaAccessor mediaAccessor = ServiceRegistration.Get<IMediaAccessor>();
      if (!mediaAccessor.MediaCategories.TryGetValue(GameCategory.CATEGORY_NAME, out _gameCategory))
        _gameCategory = mediaAccessor.RegisterMediaCategory(GameCategory.CATEGORY_NAME, null);
      MEDIA_CATEGORIES.Add(_gameCategory);
      // All non-default media item aspects must be registered
      IMediaItemAspectTypeRegistration miatr = ServiceRegistration.Get<IMediaItemAspectTypeRegistration>();
      miatr.RegisterLocallyKnownMediaItemAspectTypeAsync(GameAspect.Metadata);
    }

    public GameMetadataExtractor()
    {
      _metadata = new MetadataExtractorMetadata(METADATAEXTRACTOR_ID, "Games metadata extractor", MetadataExtractorPriority.External, true,
          MEDIA_CATEGORIES, new[]
              {
                MediaAspect.Metadata,
                GameAspect.Metadata
              });

      _settingsWatcher.SettingsChanged += SettingsChanged;
      UpdateMediaCategories(_settingsWatcher.Settings.ConfiguredEmulators);
    }

    public static MediaCategory GameMediaCategory
    {
      get { return _gameCategory; }
    }

    public MetadataExtractorMetadata Metadata
    {
      get { return _metadata; }
    }

    public async Task<bool> TryExtractMetadataAsync(IResourceAccessor mediaItemAccessor, IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData, bool forceQuickMode)
    {
      IResourceAccessor disposeAccessor = null;
      try
      {
        IFileSystemResourceAccessor fsra = mediaItemAccessor as IFileSystemResourceAccessor;

        if (fsra == null)
          return false;

        if (!fsra.IsFile)
        {
          if (!IsChainedResourceProvider(mediaItemAccessor))
            return false;
          // The importer mounts iso and zip files as virtual directories but we need the accessor
          // to the original file so we have to create one here and remember to dispose it later.
          ResourcePath path = new ResourcePath(new[] { mediaItemAccessor.CanonicalLocalResourcePath.BasePathSegment });
          if (!path.TryCreateLocalResourceAccessor(out disposeAccessor))
            return false;
        }

        using (LocalFsResourceAccessorHelper rah = new LocalFsResourceAccessorHelper(disposeAccessor ?? mediaItemAccessor))
          return await ExtractGameData(rah.LocalFsResourceAccessor, extractedAspectData, forceQuickMode).ConfigureAwait(false);
      }
      catch (Exception e)
      {
        // Only log at the info level here - And simply return false. This lets the caller know that we
        // couldn't perform our task here.
        Logger.Info("GamesMetadataExtractor: Exception reading resource '{0}' (Text: '{1}')", mediaItemAccessor.CanonicalLocalResourcePath, e.Message);
      }
      finally
      {
        if (disposeAccessor != null)
          disposeAccessor.Dispose();
      }
      return false;
    }

    static async Task<bool> ExtractGameData(ILocalFsResourceAccessor lfsra, IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData, bool forceQuickMode)
    {
      var categories = ServiceRegistration.Get<IMediaCategoryHelper>().GetMediaCategories(lfsra.CanonicalLocalResourcePath);
      string platform = categories.FirstOrDefault(s => _platformCategories.ContainsKey(s));
      if (string.IsNullOrEmpty(platform))
      {
        Logger.Warn("GamesMetadataExtractor: Unable to import {0}, no platform categories have been selected", lfsra.LocalFileSystemPath);
        return false;
      }

      var configurations = ServiceRegistration.Get<ISettingsManager>().Load<CommonSettings>().ConfiguredEmulators;
      if (!HasGameExtension(lfsra.CanonicalLocalResourcePath.BasePathSegment.Path, platform, configurations))
        return false;

      Logger.Debug("GamesMetadataExtractor: Importing game: '{0}', '{1}'", lfsra.LocalFileSystemPath, platform);
      string name = DosPathHelper.GetFileNameWithoutExtension(lfsra.CanonicalLocalResourcePath.BasePathSegment.Path);
      GameInfo gameInfo = new GameInfo()
      {
        GameName = name,
        Platform = platform
      };           

      GameMatcher matcher = GameMatcher.Instance;
      if (!forceQuickMode && !await matcher.FindAndUpdateGameAsync(gameInfo).ConfigureAwait(false))
      {
        Logger.Debug("GamesMetadataExtractor: No match found for game: '{0}', '{1}'", lfsra.LocalFileSystemPath, platform);
        gameInfo.GameName = name;
      }
      gameInfo.SetMetadata(extractedAspectData, lfsra);
      return true;
    }

    protected void SettingsChanged(object sender, EventArgs e)
    {
      UpdateMediaCategories(_settingsWatcher.Settings.ConfiguredEmulators);
    }

    protected void UpdateMediaCategories(List<EmulatorConfiguration> configurations)
    {
      if (configurations == null || configurations.Count == 0)
        return;

      IMediaAccessor mediaAccessor = ServiceRegistration.Get<IMediaAccessor>();
      foreach (EmulatorConfiguration configuration in configurations)
        foreach (string platform in configuration.Platforms)
          if (!mediaAccessor.MediaCategories.ContainsKey(platform))
          {
            Logger.Debug("GamesMetadataExtractor: Adding Game Category {0}", platform);
            MediaCategory category = mediaAccessor.RegisterMediaCategory(platform, new[] { _gameCategory });
            _platformCategories.TryAdd(platform, category);
          }
    }

    protected static bool HasGameExtension(string fileName, string platform, List<EmulatorConfiguration> configurations)
    {
      string ext = DosPathHelper.GetExtension(fileName).ToLowerInvariant();
      return configurations.Any(c => c.Platforms.Contains(platform) && (c.FileExtensions.Count == 0 || c.FileExtensions.Contains(ext, StringComparer.InvariantCultureIgnoreCase)));
    }

    protected static bool IsChainedResourceProvider(IResourceAccessor mediaItemAccessor)
    {
      Guid providerId = mediaItemAccessor.ParentProvider.Metadata.ResourceProviderId;
      return ServiceRegistration.Get<IMediaAccessor>().LocalChainedResourceProviders.Any(rp => rp.Metadata.ResourceProviderId == providerId);
    }

    public bool IsDirectorySingleResource(IResourceAccessor mediaItemAccessor)
    {
      if (IsChainedResourceProvider(mediaItemAccessor))
      {
        var categories = ServiceRegistration.Get<IMediaCategoryHelper>().GetMediaCategories(mediaItemAccessor.CanonicalLocalResourcePath);
        var configurations = ServiceRegistration.Get<ISettingsManager>().Load<CommonSettings>().ConfiguredEmulators;
        string platform = categories.FirstOrDefault(s => _platformCategories.ContainsKey(s));
        if (HasGameExtension(mediaItemAccessor.CanonicalLocalResourcePath.BasePathSegment.Path, platform, configurations))
          return true;
      }
      return false;
    }

    public bool IsStubResource(IResourceAccessor mediaItemAccessor)
    {
      return false;
    }

    public bool TryExtractStubItems(IResourceAccessor mediaItemAccessor, ICollection<IDictionary<Guid, IList<MediaItemAspect>>> extractedStubAspectData)
    {
      return false;
    }

    public Task<IList<MediaItemSearchResult>> SearchForMatchesAsync(IDictionary<Guid, IList<MediaItemAspect>> searchAspectData, ICollection<string> searchCategories)
    {
      return Task.FromResult<IList<MediaItemSearchResult>>(null);
    }

    public Task<bool> AddMatchedAspectDetailsAsync(IDictionary<Guid, IList<MediaItemAspect>> matchedAspectData)
    {
      return Task.FromResult(false);
    }
  }
}
