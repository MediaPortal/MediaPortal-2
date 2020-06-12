using Emulators.Common.Emulators;
using Emulators.Common.GoodMerge;
using Emulators.Emulator;
using Emulators.GoodMerge;
using Emulators.LibRetro;
using Emulators.Models;
using Emulators.Settings;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.Settings;
using MediaPortal.Common.SystemCommunication;
using MediaPortal.UI.Control.InputManager;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.ServerCommunication;
using MediaPortal.UiComponents.Media.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.Game
{
  public class GameLauncher : IGameLauncher, IDisposable
  {
    protected static Key _mappedKey = Key.Stop;

    protected readonly object _syncRoot = new object();
    protected MediaItem _mediaItem;
    protected ILocalFsResourceAccessor _resourceAccessor;
    protected EmulatorProcess _emulatorProcess;
    protected SettingsChangeWatcher<EmulatorsSettings> _settingsChangeWatcher = new SettingsChangeWatcher<EmulatorsSettings>();

    public void LaunchGame(MediaItem mediaItem)
    {
      _mediaItem = mediaItem;
      EmulatorConfiguration configuration;
      if (!TryGetConfiguration(mediaItem, out configuration))
      {
        QueryCreateConfigurationModel.Instance().QueryCreateConfiguration();
        return;
      }

      lock (_syncRoot)
      {
        Cleanup();
        if (!mediaItem.GetResourceLocator().TryCreateLocalFsAccessor(out _resourceAccessor))
          return;
        ServiceRegistration.Get<ILogger>().Debug("GameLauncher: Created LocalFsAccessor: {0}, {1}", _resourceAccessor.CanonicalLocalResourcePath, _resourceAccessor.LocalFileSystemPath);

        if (mediaItem.Aspects.ContainsKey(GoodMergeAspect.ASPECT_ID))
          LaunchGoodmergeGame(configuration);
        else if (configuration.IsLibRetro)
          LaunchLibRetroGame(_resourceAccessor.LocalFileSystemPath, configuration, false);
        else
          LaunchGame(_resourceAccessor.LocalFileSystemPath, configuration);
      }
    }

    protected void LaunchGame(string path, EmulatorConfiguration configuration)
    {
      _emulatorProcess = new EmulatorProcess(path, configuration, _mappedKey);
      _emulatorProcess.Exited += ProcessExited;
      if (!_emulatorProcess.TryStart())
      {
        Cleanup();
        ShowErrorDialog("[Emulators.LaunchError.Label]");
        return;
      }
      OnGameStarted();
    }

    protected void LaunchLibRetroGame(string path, EmulatorConfiguration configuration, bool isExtractedPath)
    {
      LibRetroMediaItem mediaItem = new LibRetroMediaItem(configuration.Path, _mediaItem.Aspects);
      if (isExtractedPath)
        mediaItem.ExtractedPath = path;
      else
        Cleanup();
      PlayItemsModel.CheckQueryPlayAction(mediaItem);
    }

    protected void LaunchGoodmergeGame(EmulatorConfiguration configuration)
    {
      IEnumerable<string> goodmergeItems;
      if (!MediaItemAspect.TryGetAttribute(_mediaItem.Aspects, GoodMergeAspect.ATTR_GOODMERGE_ITEMS, out goodmergeItems))
        return;
      string selectedItem;
      MediaItemAspect.TryGetAttribute(_mediaItem.Aspects, GoodMergeAspect.ATTR_LAST_PLAYED_ITEM, out selectedItem);
      GoodMergeSelectModel.Instance().Extract(_resourceAccessor, goodmergeItems, selectedItem, e => OnExtractionCompleted(e, configuration));
    }

    protected void OnExtractionCompleted(ExtractionCompletedEventArgs e, EmulatorConfiguration configuration)
    {
      lock (_syncRoot)
      {
        Cleanup();
        if (!e.Success)
        {
          ShowErrorDialog("[Emulators.ExtractionError.Label]");
          return;
        }

        UpdateMediaItem(_mediaItem, GoodMergeAspect.ATTR_LAST_PLAYED_ITEM, e.ExtractedItem);
        if (configuration.IsLibRetro)
          LaunchLibRetroGame(e.ExtractedPath, configuration, true);
        else
          LaunchGame(e.ExtractedPath, configuration);
      }
    }

    protected bool TryGetConfiguration(MediaItem mediaItem, out EmulatorConfiguration configuration)
    {
      configuration = null;
      List<string> mimeTypes;
      string mimeType;
      if (mediaItem == null ||
        !MediaItemAspect.TryGetAttribute(mediaItem.Aspects, ProviderResourceAspect.ATTR_MIME_TYPE, out mimeTypes) ||
        string.IsNullOrEmpty(mimeType = mimeTypes.First()))
        return false;

      List<string> paths;
      string path;
      if (!MediaItemAspect.TryGetAttribute(mediaItem.Aspects, ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH, out paths) ||
        string.IsNullOrEmpty(path = paths.First()))
        return false;

      ResourcePath rp = ResourcePath.Deserialize(path);
      string ext = ProviderPathHelper.GetExtension(rp.FileName);
      return ServiceRegistration.Get<IEmulatorManager>().TryGetConfiguration(mimeType, ext, out configuration);
    }

    protected void ProcessExited(object sender, EventArgs e)
    {
      lock (_syncRoot)
        if (sender == _emulatorProcess)
          OnGameExited();
    }

    protected void OnGameStarted()
    {
      if (_settingsChangeWatcher.Settings.MinimiseOnGameStart)
        ServiceRegistration.Get<IScreenControl>().Minimize();
    }

    protected void OnGameExited()
    {
      Cleanup();
      if (_settingsChangeWatcher.Settings.MinimiseOnGameStart)
        ServiceRegistration.Get<IScreenControl>().Restore();
    }

    protected void UpdateMediaItem<T>(MediaItem mediaItem, MediaItemAspectMetadata.SingleAttributeSpecification attribute, T value)
    {
      if (mediaItem == null)
        return;

      T oldValue;
      if (MediaItemAspect.TryGetAttribute(mediaItem.Aspects, attribute, out oldValue))
      {
        if ((oldValue == null && value == null) || (oldValue != null && oldValue.Equals(value)))
          return;
      }

      IContentDirectory cd = ServiceRegistration.Get<IServerConnectionManager>().ContentDirectory;
      if (cd == null)
        return;
      var rl = mediaItem.GetResourceLocator();
      List<Guid> parentDirectoryIds;
      if (!MediaItemAspect.TryGetAttribute(mediaItem.Aspects, ProviderResourceAspect.ATTR_PARENT_DIRECTORY_ID, out parentDirectoryIds))
        return;
      MediaItemAspect.SetAttribute(mediaItem.Aspects, attribute, value);
      cd.AddOrUpdateMediaItemAsync(parentDirectoryIds.First(), rl.NativeSystemId, rl.NativeResourcePath, MediaItemAspect.GetAspects(mediaItem.Aspects));
    }

    protected void ShowErrorDialog(string text)
    {
      ServiceRegistration.Get<IDialogManager>().ShowDialog("[Emulators.Dialog.Error.Header]", text, DialogType.OkDialog, false, DialogButtonType.Ok);
    }

    protected void Cleanup()
    {
      if (_emulatorProcess != null)
      {
        _emulatorProcess.Exited -= ProcessExited;
        _emulatorProcess.Dispose();
        _emulatorProcess = null;
      }
      if (_resourceAccessor != null)
      {
        _resourceAccessor.Dispose();
        _resourceAccessor = null;
      }
    }

    public void Dispose()
    {
      Cleanup();
    }
  }
}
