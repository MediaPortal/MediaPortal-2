#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using MediaPortal.Core.Configuration;
using MediaPortal.Core;
using MediaPortal.Core.Commands;
using MediaPortal.Core.General;
using MediaPortal.Core.Logging;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.Core.Localization;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Workflow;
using UiComponents.Configuration.ConfigurationControllers;

namespace UiComponents.Configuration
{
  /// <summary>
  /// Model providing the workflow for the configuration part.
  /// </summary>
  /// <remarks>
  /// <para>
  /// "Browsing" through the configuration includes
  /// <list type="bullet">
  /// <item>Navigation through configuration sections</item>
  /// <item>Displaying and changing configuration items of a specified configuration location</item>
  /// </list>
  /// In the navigation through sections, the root section is mapped to the (static) configuration
  /// main state with the id of <see cref="CONFIGURATION_MAIN_STATE_ID_STR"/>, and each of the hierarchical
  /// configuration sub-sections is mapped to a transient workflow state built on-the-fly by method
  /// <see cref="UpdateMenuActions"/>.<br/>
  /// The displaying and changing of a configuration item is done by holding an internal (sub-)state:
  /// We store a current configuration item as well as its display data.
  /// </para>
  /// <para>
  /// The skin will show two parts in respect to the config navigation:
  /// <list type="bullet">
  /// <item>A menu part providing the navigation through sections</item>
  /// <item>A content menu to choose a configuration item to edit</item>
  /// </list>
  /// The menu will completely be provided by the workflow manager; the configuration of the workflow manager
  /// is done by method <see cref="UpdateMenuActions"/>.<br/>
  /// The content menu entries are provided by property <see cref="ConfigSettings"/>, containing items to be shown
  /// in a list in the content part. Each item has an attached command which will trigger any state change
  /// in this model as well as the showing of the approppriate screen dialog.
  /// </para>
  /// </remarks>
  public class ConfigurationModel : IWorkflowModel, IDisposable
  {
    public const string CONFIGURATION_MODEL_ID_STR = "545674F1-D92A-4383-B6C1-D758CECDBDF5";
    public const string CONFIGURATION_MAIN_STATE_ID_STR = "E7422BB8-2779-49ab-BC99-E3F56138061B";
    public const string CONFIGURATION_SECTION_SCREEN = "configuration-section";

    public const string ACTIONS_WORKFLOW_CATEGORY = "a-ConfigurationSections";

    public const string KEY_NAME = "Name";
    public const string KEY_HELPTEXT = "Help";
    public const string KEY_ENABLED = "Enabled";

    #region Protected fields

    protected const string CONFIG_LOCATION_KEY = "ConfigurationModel: CONFIG_LOCATION";

    protected WorkflowConfigurationController _workflowConfigurationController;

    protected IDictionary<Type, ConfigurationController> _registeredSettingTypes = new Dictionary<Type, ConfigurationController>();

    protected ItemsList _configSettingsList = null;
    protected string _currentLocation = null;
    protected ConfigurationController _currentConfigController = null;
    protected AbstractProperty _headerTextProperty;
    protected ICollection<AbstractProperty> _trackedVisibleEnabledProperties = new List<AbstractProperty>();

    #endregion

    #region Ctor

    /// <summary>
    /// Registers the specified configuration <paramref name="controller"/> for the configuration
    /// type, the controller specifies in its <see cref="ConfigurationController.ConfigSettingType"/> property.
    /// </summary>
    void Register(ConfigurationController controller)
    {
      _registeredSettingTypes[controller.ConfigSettingType] = controller;
    }

    public ConfigurationModel()
    {
      _headerTextProperty = new WProperty(typeof(string), null);
      Initialize();
    }

    protected void Initialize()
    {
      Register(new YesNoController());
      Register(new EntryController());
      Register(new SingleSelectionListController());
      Register(new MultiSelectionListController());
      Register(new NumberSelectController());
      // More generic controller types go here

      _workflowConfigurationController = new WorkflowConfigurationController();
    }

    public void Dispose()
    {
      ReleaseAllVisibleEnabledNotifications();
      _registeredSettingTypes.Clear();
      _workflowConfigurationController = null;
      _currentConfigController = null;
    }

    #endregion

    #region Common properties for screens

    /// <summary>
    /// Returns a list of config settings in the section associated with the current workflow state.
    /// </summary>
    public ItemsList ConfigSettings
    {
      get { return _configSettingsList; }
    }

    /// <summary>
    /// Returns the current setting instance. The skin should update its data from and to this instance.
    /// </summary>
    public ConfigurationController CurrentConfigController
    {
      get { return _currentConfigController; }
    }

    #endregion

    #region Public methods for screens

    /// <summary>
    /// Initializes the model state variables for the GUI to be used in the screen for the configuration
    /// item of the specified <paramref name="configLocation"/> and shows the screen.
    /// </summary>
    /// <param name="configLocation">The configuration location to be shown.</param>
    public void ShowConfigItem(string configLocation)
    {
      _currentLocation = configLocation;
      IConfigurationManager configurationManager = ServiceScope.Get<IConfigurationManager>();
      IConfigurationNode currentNode = configurationManager.GetNode(configLocation);
      ConfigSetting configSetting = currentNode == null ? null : currentNode.ConfigObj as ConfigSetting;
      _currentConfigController = FindConfigurationController(configSetting);
      if (_currentConfigController == null)
      { // Error case: We don't have a configuration controller for the setting to be shown
        ServiceScope.Get<ILogger>().Warn(
            "ConfigurationModel: Cannot show configuration for setting '{0}', no configuration controller available",
            configSetting);
        return;
      }
      _currentConfigController.Initialize(configSetting);
      _currentConfigController.ExecuteConfiguration();
    }

    #endregion

    #region Protected methods

    /// <summary>
    /// Returns the config location corresponding to the workflow state given by the specified
    /// workflow navigation <paramref name="context"/>.
    /// </summary>
    /// <param name="context">The workflow navigation context to lookup the context state.</param>
    /// <returns>Previously initialized config location of the given navigation <paramref name="context"/> or <c>"/"</c>,
    /// if the context wasn't initialized before.</returns>
    protected static string GetConfigLocation(NavigationContext context)
    {
      string result = context.GetContextVariable(CONFIG_LOCATION_KEY, false) as string;
      if (result == null)
        context.SetContextVariable(CONFIG_LOCATION_KEY, result = "/");
      return result;
    }

    /// <summary>
    /// Returns the information whether the given navigation <paramref name="context"/> is a context which was already
    /// visited by this model. This method checks for the existence of the context variable <see cref="CONFIG_LOCATION_KEY"/>.
    /// </summary>
    /// <param name="context">Workflow navigation context to check.</param>
    /// <returns><c>true</c>, if the given <paramref name="context"/> contains the context variable
    /// <see cref="CONFIG_LOCATION_KEY"/>, else <c>false</c>.</returns>
    protected static bool IsInitialized(NavigationContext context)
    {
      return context.ContextVariables.ContainsKey(CONFIG_LOCATION_KEY);
    }

    /// <summary>
    /// Returns the configuration controller class which is responsible for the specified
    /// <paramref name="setting"/>.
    /// </summary>
    /// <param name="setting">The setting to check.</param>
    /// <returns>Configuration controller class which is responsible for the specified
    /// <paramref name="setting"/>, or <c>null</c>, if the setting is not supported.</returns>
    protected ConfigurationController FindConfigurationController(ConfigSetting setting)
    {
      if (setting == null)
        return null;
      ConfigurationController result = null;
      // Check if a custom configuration controller is requested
      ConfigSettingMetadata metadata = (ConfigSettingMetadata) setting.Metadata;
      if (metadata.AdditionalTypes != null && metadata.AdditionalTypes.ContainsKey("CustomConfigController"))
      {
        Type controllerType = metadata.AdditionalTypes["CustomConfigController"];
        if (controllerType == null)
        {
          ServiceScope.Get<ILogger>().Warn(
            "ConfigurationModel: Custom configuration controller could not be loaded (config setting at location '{0}')",
            metadata.Location);
          return null;
        }
        // Check if we already have the required controller available
        foreach (KeyValuePair<Type, ConfigurationController> registration in _registeredSettingTypes)
          if (registration.Value.GetType() == controllerType)
          {
            result = registration.Value;
            break;
          }
        if (result == null)
        {
          // FIXME Albert: Make configuration controllers to models; load them via the workflow manager.
          // This will make configuration controllers be managed correctly
          result = Activator.CreateInstance(controllerType) as ConfigurationController;
          if (result != null)
            // Lazily add the new controller type to our registered controllers
            Register(result);
        }
        if (result != null)
          return result;
      }
      // Check if the workflow configuration controller can handle the setting
      if (_workflowConfigurationController.IsSettingSupported(setting))
        return _workflowConfigurationController;
      // Else try a default configuration controller
      return FindConfigurationController(setting.GetType());
    }

    protected ConfigurationController FindConfigurationController(Type settingType)
    {
      foreach (KeyValuePair<Type, ConfigurationController> registration in _registeredSettingTypes)
        if (registration.Key.IsAssignableFrom(settingType))
          return registration.Value;
      return null;
    }

    /// <summary>
    /// Returns the information if the specified <paramref name="setting"/> is supported by this
    /// configuration plugin.
    /// </summary>
    /// <param name="setting">The setting to check.</param>
    /// <returns><c>true</c>, if the setting is supported, i.e. it can be displayed in the GUI, else
    /// <c>false</c>.</returns>
    protected bool IsSettingSupported(ConfigSetting setting)
    {
      ConfigurationController controller = FindConfigurationController(setting);
      return controller != null && controller.IsSettingSupported(setting);
    }

    /// <summary>
    /// Returns the number of supported settings in or under the specified <paramref name="sectionOrGroupNode"/>.
    /// </summary>
    /// <param name="sectionOrGroupNode">Section or group node to check.</param>
    /// <returns>Number of supported settings in the specified node.</returns>
    protected int NumSettingsSupported(IConfigurationNode sectionOrGroupNode)
    {
      int result = 0;
      foreach (IConfigurationNode childNode in sectionOrGroupNode.ChildNodes)
      {
        if (childNode.ConfigObj is ConfigSetting)
        {
          if (IsSettingSupported((ConfigSetting) childNode.ConfigObj))
            result++;
        }
        else if (childNode.ConfigObj is ConfigGroup || childNode.ConfigObj is ConfigSection)
          result += NumSettingsSupported(childNode);
      }
      return result;
    }

    /// <summary>
    /// Adds all supported settings in the specified <paramref name="sectionOrGroupNode"/> to the specified
    /// <paramref name="settingsList"/>. For each setting, a <see cref="ListItem"/> will be created
    /// with the setting text as name and a command which triggers the method <see cref="ShowConfigItem"/>
    /// with the according setting location.
    /// </summary>
    /// <param name="sectionOrGroupNode">Section or group node, whose contained settings should be added.</param>
    /// <param name="settingsList">List where the extracted settings will be added to.</param>
    protected void AddConfigSettings(IConfigurationNode sectionOrGroupNode, ItemsList settingsList)
    {
      foreach (IConfigurationNode childNode in sectionOrGroupNode.ChildNodes)
      {
        if (childNode.ConfigObj is ConfigSetting)
        {
          ConfigSetting setting = (ConfigSetting) childNode.ConfigObj;
          if (!setting.Visible || !IsSettingSupported(setting))
            continue;
          string location = childNode.Location;
          ListItem item = new ListItem(KEY_NAME, setting.SettingMetadata.Text)
          {
              Command = new MethodDelegateCommand(() => ShowConfigItem(location))
          };
          item.SetLabel(KEY_HELPTEXT, setting.SettingMetadata.HelpText);
          item.Enabled = setting.Enabled;
          TrackItemVisibleEnabledProperty(setting.VisibleProperty);
          TrackItemVisibleEnabledProperty(setting.EnabledProperty);
          settingsList.Add(item);
        }
        if (childNode.ConfigObj is ConfigGroup)
          AddConfigSettings(childNode, settingsList);
      }
    }

    protected void TrackItemVisibleEnabledProperty(AbstractProperty property)
    {
      property.Attach(OnVisibleEnabledChanged);
      _trackedVisibleEnabledProperties.Add(property);
    }

    protected void ReleaseAllVisibleEnabledNotifications()
    {
      foreach (AbstractProperty property in _trackedVisibleEnabledProperties)
        property.Detach(OnVisibleEnabledChanged);
      _trackedVisibleEnabledProperties.Clear();
    }

    void OnVisibleEnabledChanged(AbstractProperty visibleOrEnabledProperty, object oldValue)
    {
      UpdateConfigSettings();
    }

    protected void UpdateConfigSettings()
    {
      _configSettingsList.Clear();
      IConfigurationManager configurationManager = ServiceScope.Get<IConfigurationManager>();
      IConfigurationNode currentNode = configurationManager.GetNode(_currentLocation);
      if (currentNode == null)
        // This is an error case, should not happen
        return;
      AddConfigSettings(currentNode, _configSettingsList);
      _configSettingsList.FireChange();
    }

    /// <summary>
    /// Sets up the internal and external states to conform to the specified <paramref name="newContext"/>.
    /// </summary>
    /// <param name="oldContext">Old workflow navigation context which is left.</param>
    /// <param name="newContext">New workflow navigation context which is entered.</param>
    protected void PrepareConfigLocation(NavigationContext oldContext, NavigationContext newContext)
    {
      _currentConfigController = null;
      ReleaseAllVisibleEnabledNotifications();
      string configLocation = GetConfigLocation(newContext);
      IConfigurationManager configurationManager = ServiceScope.Get<IConfigurationManager>();
      bool enteringConfiguration = !IsInitialized(oldContext);
      if (enteringConfiguration)
        configurationManager.Initialize();

      _currentLocation = configLocation;
      // We need to create a new items list instance because the old GUI screen is still showing the old items list and we don't
      // want to update it
      _configSettingsList = new ItemsList();
      UpdateConfigSettings();
    }

    #endregion

    #region IWorkflowModel implementation

    public Guid ModelId
    {
      get { return new Guid(CONFIGURATION_MODEL_ID_STR); }
    }

    public bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
    {
      return true;
    }

    public void Deactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      // We're temporary stepping out of our model context... We just hold our state
      // until we step in again.
    }

    public void ReActivate(NavigationContext oldContext, NavigationContext newContext)
    {
      PrepareConfigLocation(oldContext, newContext);
    }

    public void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      PrepareConfigLocation(oldContext, newContext);
    }

    public void ChangeModelContext(NavigationContext oldContext, NavigationContext newContext, bool push)
    {
      PrepareConfigLocation(oldContext, newContext);
    }

    public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      // The cached configuration won't be saved persistently here as it only will be saved on explicit
      // calls to the appropriate method from the skin.
      if (!ServiceScope.Get<IWorkflowManager>().IsModelContainedInNavigationStack(ModelId))
        ServiceScope.Get<IConfigurationManager>().Dispose();
    }

    public void UpdateMenuActions(NavigationContext context, IDictionary<Guid, WorkflowAction> actions)
    {
      IConfigurationManager configurationManager = ServiceScope.Get<IConfigurationManager>();
      string configLocation = GetConfigLocation(context);
      WorkflowState mainState;
      ServiceScope.Get<IWorkflowManager>().States.TryGetValue(new Guid(CONFIGURATION_MAIN_STATE_ID_STR),
          out mainState);
      IConfigurationNode currentNode = configurationManager.GetNode(configLocation);
      foreach (IConfigurationNode childNode in currentNode.ChildNodes)
      {
        if (childNode.ConfigObj is ConfigSection)
        {
          bool supportedSettings = NumSettingsSupported(childNode) > 0;
          // Hint (Albert): Instead of skipping, we could disable the transition in case there are no supported
          // settings contained in it
          if (!supportedSettings)
            continue;
          ConfigSection section = (ConfigSection) childNode.ConfigObj;
          // Create transient state for new config section
          WorkflowState newState = WorkflowState.CreateTransientState(
              string.Format("Config: '{0}'", childNode.Location), section.SectionMetadata.Text, false, CONFIGURATION_SECTION_SCREEN,
              false, WorkflowType.Workflow);
          // Add action for menu
          IResourceString res = LocalizationHelper.CreateResourceString(section.Metadata.Text);
          WorkflowAction wa = new PushTransientStateNavigationTransition(
              Guid.NewGuid(), context.WorkflowState.Name + "->" + childNode.Location, null,
              context.WorkflowState.StateId, newState, res)
            {
                DisplayCategory = ACTIONS_WORKFLOW_CATEGORY,
                SortOrder = res.Evaluate(),
                WorkflowNavigationContextVariables = new Dictionary<string, object>
                {
                    {CONFIG_LOCATION_KEY, childNode.Location}
                }
            };
          actions.Add(wa.ActionId, wa);
        }
      }
    }

    public ScreenUpdateMode UpdateScreen(NavigationContext context, ref string screen)
    {
      return ScreenUpdateMode.AutoWorkflowManager;
    }

    #endregion
  }
}
