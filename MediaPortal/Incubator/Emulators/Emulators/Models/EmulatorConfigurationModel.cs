using Emulators.Emulator;
using Emulators.Models.Navigation;
using MediaPortal.Common.Commands;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.Common;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.UiComponents.SkinBase.General;
using Emulators.Common.Emulators;
using MediaPortal.Common.SystemResolver;
using SharpRetro.LibRetro;
using MediaPortal.Common.Settings;
using Emulators.LibRetro;
using Emulators.LibRetro.Settings;
using SharpRetro.Info;
using Emulators.LibRetro.Cores;
using System.IO;
using MediaPortal.Common.Logging;
using MediaPortal.Common.General;

namespace Emulators.Models
{
  public class EmulatorConfigurationModel : AbstractConfigurationModel
  {
    protected const string KEY_CONFIGURATION = "Emulator_Configuration";
    protected const string KEY_EMULATOR_TYPE = "Emulator_Type";
    protected const string KEY_WILDCARD = "Emulator_Wildcard";
    protected const string KEY_PLATFORMS = "Platforms";
    protected const string DIALOG_SHOW_WILDCARDS = "dialog_choose_wildcard";

    public static readonly Guid MODEL_ID = new Guid("6C96C335-7A79-45DA-90B7-541B3C7235EF");
    public static readonly Guid STATE_OVERVIEW = new Guid("903DD5EB-56B2-42B5-B1D8-64106651296A");
    public static readonly Guid STATE_CHOOSE_TYPE = new Guid("CFD8DD6F-6C80-49E1-9F79-A87AE259828D");
    public static readonly Guid STATE_CHOOSE_PATH = new Guid("B3B74541-3779-46EB-8EED-DF00CBEEC91A");
    public static readonly Guid STATE_CHOOSE_CORE = new Guid("13145964-27DA-4F29-A3FD-E75755DFB603");
    public static readonly Guid STATE_EDIT_NAME = new Guid("A533006C-D895-41AE-86A3-DF9193707120");
    public static readonly Guid STATE_CHOOSE_CATEGORIES = new Guid("E30DCBAE-BB1D-4701-84B0-FA3624481648");
    public static readonly Guid STATE_EDIT_EXTENSIONS = new Guid("3017CAD9-3EFD-48F4-BC8D-06295389D21D");
    public static readonly Guid STATE_EDIT_ARGUMENTS = new Guid("B2D4E8EC-0AE3-4C77-9E45-5EDC71EF4032");
    public static readonly Guid STATE_LIBRETRO_OPTIONS = new Guid("97E490B5-DF74-4894-9704-81B214C47EF8");
    public static readonly Guid STATE_REMOVE_EMULATOR = new Guid("A07A0648-878E-4762-9540-3939E308DD94");

    protected Guid _enteredState = Guid.Empty;
    protected EmulatorProxy _emulatorProxy;
    protected ItemsList _emulatorTypes = new ItemsList();
    protected ItemsList _localCoreItems = new ItemsList();
    protected ItemsList _wildcardItems = new ItemsList();

    public static EmulatorConfigurationModel Instance()
    {
      return (EmulatorConfigurationModel)ServiceRegistration.Get<IWorkflowManager>().GetModel(MODEL_ID);
    }

    public override Guid ModelId
    {
      get { return MODEL_ID; }
    }

    public EmulatorProxy EmulatorProxy
    {
      get { return _emulatorProxy; }
    }

    public ItemsList EmulatorTypes
    {
      get { return _emulatorTypes; }
    }

    public ItemsList LocalCoreItems
    {
      get { return _localCoreItems; }
    }

    public ItemsList WildcardItems
    {
      get { return _wildcardItems; }
    }

    public void AddNewEmulatorConfiguration()
    {
      NavigatePush(STATE_CHOOSE_TYPE);
    }

    public void BeginNewEmulatorConfiguration()
    {
      ListItem selectedTypeItem = _emulatorTypes.FirstOrDefault(i => i.Selected);
      if (selectedTypeItem == null)
        return;
      EmulatorType emulatorType = (EmulatorType)selectedTypeItem.AdditionalProperties[KEY_EMULATOR_TYPE];
      _emulatorProxy = new EmulatorProxy(emulatorType);

      if (emulatorType == EmulatorType.Emulator)
        NavigatePush(STATE_CHOOSE_PATH);
      else if (emulatorType == EmulatorType.LibRetro)
        NavigatePush(STATE_CHOOSE_CORE);
      else
        NavigatePush(STATE_EDIT_NAME);
    }

    protected void EditEmulatorConfiguration(ListItem item)
    {
      _emulatorProxy = new EmulatorProxy((EmulatorConfiguration)item.AdditionalProperties[KEY_CONFIGURATION]);
      if (_emulatorProxy.EmulatorType == EmulatorType.Emulator || _emulatorProxy.EmulatorType == EmulatorType.LibRetro)
        NavigatePush(STATE_CHOOSE_PATH);
      else
        NavigatePush(STATE_EDIT_NAME);
    }

    public void FinishEmulatorConfiguration()
    {
      EmulatorConfiguration configuration;
      if (_emulatorProxy.Configuration != null)
      {
        configuration = _emulatorProxy.Configuration;
      }
      else
      {
        configuration = new EmulatorConfiguration()
        {
          IsNative = _emulatorProxy.EmulatorType == EmulatorType.Native,
          IsLibRetro = _emulatorProxy.EmulatorType == EmulatorType.LibRetro,
          Id = Guid.NewGuid(),
          LocalSystemId = ServiceRegistration.Get<ISystemResolver>().LocalSystemId
        };
      }

      if (!configuration.IsNative)
        configuration.Path = LocalFsResourceProviderBase.ToDosPath(_emulatorProxy.PathBrowser.ChoosenResourcePath);
      configuration.Arguments = _emulatorProxy.Arguments;
      configuration.WorkingDirectory = _emulatorProxy.WorkingDirectory;
      configuration.UseQuotes = _emulatorProxy.UseQuotes;
      configuration.Name = _emulatorProxy.Name;
      configuration.FileExtensions = _emulatorProxy.FileExtensions;
      configuration.ExitsOnEscapeKey = _emulatorProxy.ExitsOnEscapeKey;
      configuration.Platforms.Clear();
      foreach (string category in _emulatorProxy.SelectedGameCategories)
        configuration.Platforms.Add(category);
      ServiceRegistration.Get<IEmulatorManager>().AddOrUpdate(configuration);
      if (configuration.IsLibRetro && _emulatorProxy.LibRetroProxy != null)
        UpdateLibRetroCoreSetting(configuration.Path, _emulatorProxy.LibRetroProxy.Variables);
      UpdateConfigurations();
      NavigateBackToOverview();
    }

    public void RemoveEmulatorConfigurations()
    {
      NavigatePush(STATE_REMOVE_EMULATOR);
    }

    protected void UpdateEmulatorTypeItems()
    {
      _emulatorTypes.Clear();
      ListItem emulatorItem = new ListItem(Consts.KEY_NAME, "[Emulators.Config.EmulatorType.Emulator]");
      emulatorItem.AdditionalProperties[KEY_EMULATOR_TYPE] = EmulatorType.Emulator;
      emulatorItem.Selected = true;
      _emulatorTypes.Add(emulatorItem);
      ListItem libRetroItem = new ListItem(Consts.KEY_NAME, "[Emulators.Config.EmulatorType.LibRetro]");
      libRetroItem.AdditionalProperties[KEY_EMULATOR_TYPE] = EmulatorType.LibRetro;
      _emulatorTypes.Add(libRetroItem);
      ListItem nativeItem = new ListItem(Consts.KEY_NAME, "[Emulators.Config.EmulatorType.Native]");
      nativeItem.AdditionalProperties[KEY_EMULATOR_TYPE] = EmulatorType.Native;
      _emulatorTypes.Add(nativeItem);
      _emulatorTypes.FireChange();
    }

    protected void UpdateLocalCoreItems()
    {
      var sm = ServiceRegistration.Get<ISettingsManager>();
      var settings = sm.Load<LibRetroSettings>();

      _localCoreItems.Clear();
      try
      {
        if (Directory.Exists(settings.CoresDirectory))
        {
          foreach (string path in Directory.EnumerateFiles(settings.CoresDirectory, "*.dll"))
          {
            string coreName = Path.GetFileName(path);
            CoreInfo coreInfo = CoreHandler.LoadCoreInfo(coreName, settings.InfoDirectory);
            if (coreInfo != null)
              coreName = coreInfo.DisplayName;
            ListItem item = new ListItem(Consts.KEY_NAME, coreName);
            item.SetLabel(Consts.KEY_PATH, path);
            item.SelectedProperty.Attach(OnCoreItemSelectionChanged);
            _localCoreItems.Add(item);
          }
        }
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("EmulatorConfigurationModel: Exception loading cores from '{0}'", ex, settings.CoresDirectory);
      }
      _localCoreItems.FireChange();
    }

    protected void OnCoreItemSelectionChanged(AbstractProperty property, object oldValue)
    {
      ListItem selected = _localCoreItems.FirstOrDefault(i => i.Selected);
      if (selected == null)
        return;
      ResourcePath path = LocalFsResourceProviderBase.ToResourcePath(selected.Label(Consts.KEY_PATH, null).Evaluate());
      _emulatorProxy.PathBrowser.ChoosenResourcePath = path;
    }

    public void ShowWildcardDialog()
    {
      if (_wildcardItems.Count == 0)
      {
        ListItem game = new ListItem(Consts.KEY_NAME, "[Emulators.Config.SelectWildcard.Game]");
        game.AdditionalProperties[KEY_WILDCARD] = EmulatorConfiguration.WILDCARD_GAME_PATH;
        _wildcardItems.Add(game);
        ListItem gameNoExt = new ListItem(Consts.KEY_NAME, "[Emulators.Config.SelectWildcard.GameNoExt]");
        gameNoExt.AdditionalProperties[KEY_WILDCARD] = EmulatorConfiguration.WILDCARD_GAME_PATH_NO_EXT;
        _wildcardItems.Add(gameNoExt);
        ListItem gameDir = new ListItem(Consts.KEY_NAME, "[Emulators.Config.SelectWildcard.GameDir]");
        gameDir.AdditionalProperties[KEY_WILDCARD] = EmulatorConfiguration.WILDCARD_GAME_DIRECTORY;
        _wildcardItems.Add(gameDir);
      }
      ServiceRegistration.Get<IScreenManager>().ShowDialog(DIALOG_SHOW_WILDCARDS);
    }

    public void WildcardItemSelected(ListItem item)
    {
      _emulatorProxy.InsertWildcard((string)item.AdditionalProperties[KEY_WILDCARD]);
    }

    protected override void OnItemsRemoved(IEnumerable<ListItem> items)
    {
      IEmulatorManager manager = ServiceRegistration.Get<IEmulatorManager>();
      foreach (ListItem item in items)
        manager.Remove((EmulatorConfiguration)item.AdditionalProperties[KEY_CONFIGURATION]);
    }

    protected override List<ListItem> GetItems(bool removing)
    {
      List<EmulatorConfiguration> configurations = ServiceRegistration.Get<IEmulatorManager>().Load();
      if (configurations == null)
        return null;

      List<ListItem> items = new List<ListItem>();
      foreach (EmulatorConfiguration configuration in configurations)
      {
        ListItem item = new ListItem(Consts.KEY_NAME, configuration.Name);
        item.SetLabel(Consts.KEY_PATH, configuration.Path);
        item.SetLabel(KEY_PLATFORMS, GetPlatformLabel(configuration.Platforms));
        item.AdditionalProperties[KEY_CONFIGURATION] = configuration;
        if (!removing)
          item.Command = new MethodDelegateCommand(() => EditEmulatorConfiguration(item));
        items.Add(item);
      }
      return items;
    }

    public override void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      base.EnterModelContext(oldContext, newContext);
      _enteredState = newContext.WorkflowState.StateId;
    }

    public override void Reactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      base.Reactivate(oldContext, newContext);
      if (newContext.WorkflowState.StateId == STATE_CHOOSE_CORE)
        UpdateLocalCoreItems();
    }

    protected override void UpdateState(NavigationContext newContext, bool push)
    {
      if (!push)
        return;

      Guid stateId = newContext.WorkflowState.StateId;
      if (stateId == STATE_CHOOSE_TYPE)
      {
        UpdateEmulatorTypeItems();
      }
      else if (stateId == STATE_CHOOSE_CORE)
      {
        UpdateLocalCoreItems();
      }
      else if (stateId == STATE_CHOOSE_PATH)
      {
        _emulatorProxy.PathBrowser.UpdatePathTree();
      }
      else if (stateId == STATE_EDIT_NAME)
      {
        _emulatorProxy.SetSuggestedSettings(false);
      }
      else if (stateId == STATE_CHOOSE_CATEGORIES)
      {
        _emulatorProxy.UpdateGameCategories();
      }
      else if (stateId == STATE_EDIT_EXTENSIONS)
      {
        _emulatorProxy.UpdateFileExtensionItems();
      }
      else if (stateId == STATE_LIBRETRO_OPTIONS)
      {
        _emulatorProxy.UpdateLibRetroSettings();
      }
      else if (stateId == STATE_REMOVE_EMULATOR)
      {
        UpdateConfigurationsToRemove();
      }
    }

    public override void NavigateBackToOverview()
    {
      _emulatorProxy = null;
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      workflowManager.NavigatePopToState(_enteredState, _enteredState != STATE_OVERVIEW);
    }

    protected void UpdateLibRetroCoreSetting(string corePath, List<VariableDescription> variables)
    {
      if (variables == null)
        return;
      var sm = ServiceRegistration.Get<ISettingsManager>();
      var coreSettings = sm.Load<LibRetroCoreSettings>();
      coreSettings.AddOrUpdateCoreSetting(corePath, variables);
      sm.Save(coreSettings);
    }

    protected static string GetPlatformLabel(HashSet<string> platforms)
    {
      string platformLabel = "";
      foreach (string platform in platforms)
      {
        if (platformLabel != "")
          platformLabel += ", ";
        platformLabel += platform;
      }
      return platformLabel;
    }
  }
}
