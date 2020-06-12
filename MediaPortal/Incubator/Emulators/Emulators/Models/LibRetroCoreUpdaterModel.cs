using Emulators.LibRetro.Cores;
using MediaPortal.Common;
using MediaPortal.UI.Presentation.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UiComponents.SkinBase.General;
using MediaPortal.Common.Threading;
using MediaPortal.Common.Commands;
using System.IO;
using MediaPortal.Common.Settings;
using Emulators.LibRetro.Settings;
using MediaPortal.Common.General;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.Common.Localization;
using Emulators.Models.Navigation;
using Emulators.Emulator;
using Emulators.Common.Emulators;

namespace Emulators.Models
{
  public class LibRetroCoreUpdaterModel : IWorkflowModel
  {
    public static readonly Guid MODEL_ID = new Guid("656E3AC1-0363-4DA9-A23F-F1422A9ADD74");
    public static readonly Guid STATE_ID = new Guid("BC9FAFDE-315E-4DBD-A99D-060D3225DCAA");
    public const string LABEL_CORE_NAME = "CoreName";
    public const string KEY_CORE_INFO = "LibRetro: CoreInfo";
    public const string KEY_CORE = "LibRetro: Core";
    public const string DIALOG_CORE_UPDATE_PROGRESS = "dialog_core_update_progress";
    public const string DIALOG_CONTEXT_MENU = "dialog_core_context_menu";

    protected AbstractProperty _dialogHeaderProperty = new WProperty(typeof(string), null);
    protected AbstractProperty _progressLabelProperty = new WProperty(typeof(string), null);

    protected readonly object _updateSync = new object();
    protected readonly object _downloadSync = new object();
    protected string _coresDirectory;
    protected string _infoDirectory;
    protected bool _onlyShowSupportedCores;
    protected CoreHandler _coreHandler;
    protected ItemsList _coreItems;
    protected ItemsList _contextMenuItems;
    protected HashSet<string> _downloadingUrls;
    protected bool _isUpdating;
    protected DateTime _lastUpdateTime = DateTime.MinValue;

    public LibRetroCoreUpdaterModel()
    {
      _coreItems = new ItemsList();
      _contextMenuItems = new ItemsList();
      _downloadingUrls = new HashSet<string>();
    }

    public ItemsList Items
    {
      get { return _coreItems; }
    }

    public ItemsList ContextMenuItems
    {
      get { return _contextMenuItems; }
    }

    public AbstractProperty DialogHeaderProperty
    {
      get { return _dialogHeaderProperty; }
    }

    public string DialogHeader
    {
      get { return (string)_dialogHeaderProperty.GetValue(); }
      set { _dialogHeaderProperty.SetValue(value); }
    }

    public AbstractProperty ProgressLabelProperty
    {
      get { return _progressLabelProperty; }
    }

    public string ProgressLabel
    {
      get { return (string)_progressLabelProperty.GetValue(); }
      set { _progressLabelProperty.SetValue(value); }
    }

    protected void ShowContextMenu(LibRetroCoreItem item, LocalCore core)
    {
      RebuildContextMenuItems(item, core);
      DialogHeader = item.Label(Consts.KEY_NAME, core.CoreName).Evaluate();
      ServiceRegistration.Get<IScreenManager>().ShowDialog(DIALOG_CONTEXT_MENU);
    }

    protected void DownloadCoreAsync(LibRetroCoreItem item, LocalCore core)
    {
      var sm = ServiceRegistration.Get<IScreenManager>();
      Guid? dialogId = null;
      ServiceRegistration.Get<IThreadPool>().Add(() =>
      {
        ProgressLabel = LocalizationHelper.Translate("[Emulators.CoreUpdater.Downloading]", core.CoreName);
        dialogId = sm.ShowDialog(DIALOG_CORE_UPDATE_PROGRESS);
        DownloadCore(item, core);
      },
      e =>
      {
        if (dialogId.HasValue)
          sm.CloseDialog(dialogId.Value);
      });
    }

    protected void DownloadCore(LibRetroCoreItem item, LocalCore core)
    {
      lock (_downloadSync)
      {
        if (_downloadingUrls.Contains(core.Url))
          return;
        _downloadingUrls.Add(core.Url);
      }

      try
      {
        if (_coreHandler.DownloadCore(core))
          item.Downloaded = true;
      }
      finally
      {
        lock (_downloadSync)
          _downloadingUrls.Remove(core.Url);
      }
    }

    protected void UpdateAsync()
    {
      LibRetroSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<LibRetroSettings>();
      _coresDirectory = settings.CoresDirectory;
      _infoDirectory = settings.InfoDirectory;
      _onlyShowSupportedCores = settings.OnlyShowSupportedCores;
      _coreHandler = new CoreHandler(_coresDirectory, _infoDirectory);

      if ((DateTime.Now - _lastUpdateTime).TotalMinutes < settings.CoreUpdateIntervalMinutes)
        return;
      _lastUpdateTime = DateTime.Now;

      var sm = ServiceRegistration.Get<IScreenManager>();
      Guid? dialogId = null;
      ServiceRegistration.Get<IThreadPool>().Add(() =>
      {
        ProgressLabel = "[Emulators.CoreUpdater.UpdatingCores]";
        dialogId = sm.ShowDialog(DIALOG_CORE_UPDATE_PROGRESS);
        Update();
      },
      e =>
      {
        if (dialogId.HasValue)
          sm.CloseDialog(dialogId.Value);
      });
    }

    protected void Update()
    {
      lock (_updateSync)
      {
        if (_isUpdating)
          return;
        _isUpdating = true;
      }

      try
      {
        _coreHandler.Update();
        RebuildItemsList();
      }
      finally
      {
        lock (_updateSync)
          _isUpdating = false;
      }
    }

    protected void RebuildItemsList()
    {
      string coresDirectory = _coresDirectory;
      List<EmulatorConfiguration> configurations = ServiceRegistration.Get<IEmulatorManager>().Load();

      _coreItems.Clear();
      foreach (LocalCore core in _coreHandler.Cores)
      {
        if (_onlyShowSupportedCores && !core.Supported)
          continue;
        string localPath = Path.Combine(coresDirectory, core.CoreName);
        bool downloaded = File.Exists(localPath);
        bool configured = configurations.Any(c => c.Path == localPath);
        _coreItems.Add(CreateListItem(core, downloaded, configured));
      }
      _coreItems.FireChange();
    }

    protected ListItem CreateListItem(LocalCore core, bool downloaded, bool configured)
    {
      LibRetroCoreItem item = new LibRetroCoreItem();
      item.SetLabel(LABEL_CORE_NAME, core.CoreName);
      item.AdditionalProperties[KEY_CORE] = core;
      item.Downloaded = downloaded;
      item.Configured = configured;
      item.Command = new MethodDelegateCommand(() => ShowContextMenu(item, core));

      if (core.Info != null)
      {
        item.SetLabel(Consts.KEY_NAME, core.Info.DisplayName);
        item.AdditionalProperties[KEY_CORE_INFO] = core.Info;
      }
      else
      {
        item.SetLabel(Consts.KEY_NAME, Path.GetFileNameWithoutExtension(core.CoreName));
      }
      return item;
    }

    protected void RebuildContextMenuItems(LibRetroCoreItem item, LocalCore core)
    {
      _contextMenuItems.Clear();
      string name = item.Downloaded ? "[Emulators.CoreUpdater.Update]" : "[Emulators.CoreUpdater.Download]";
      ListItem contextItem = new ListItem(Consts.KEY_NAME, name);
      contextItem.Command = new MethodDelegateCommand(() => DownloadCoreAsync(item, core));
      _contextMenuItems.Add(contextItem);
      _contextMenuItems.FireChange();
    }

    #region IWorkflow
    public Guid ModelId
    {
      get { return MODEL_ID; }
    }

    public bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
    {
      return true;
    }

    public void ChangeModelContext(NavigationContext oldContext, NavigationContext newContext, bool push)
    {

    }

    public void Deactivate(NavigationContext oldContext, NavigationContext newContext)
    {

    }

    public void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      UpdateAsync();
    }

    public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {

    }

    public void Reactivate(NavigationContext oldContext, NavigationContext newContext)
    {

    }

    public void UpdateMenuActions(NavigationContext context, IDictionary<Guid, WorkflowAction> actions)
    {

    }

    public ScreenUpdateMode UpdateScreen(NavigationContext context, ref string screen)
    {
      return ScreenUpdateMode.AutoWorkflowManager;
    }
    #endregion
  }
}