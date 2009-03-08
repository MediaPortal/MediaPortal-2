#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using MediaPortal.Configuration;
using MediaPortal.Core;
using MediaPortal.Core.Commands;
using MediaPortal.Utilities.Exceptions;
using MediaPortal.Core.Logging;
using MediaPortal.Presentation.DataObjects;
using MediaPortal.Presentation.Localization;
using MediaPortal.Presentation.Models;
using MediaPortal.Presentation.Workflow;
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

    public const string KEY_NAME = "Name";
    public const string KEY_HELPTEXT = "Help";
    public const string KEY_ENABLED = "Enabled";

    /// <summary>
    /// Holds the information about all of the (already initialized) workflow states corresponding to config
    /// navigation locations. The dictionary maps workflow state ids to the corresponding config location.
    /// </summary>
    /// <remarks>
    /// An instance of this class will be stored in our root workflow navigation context.
    /// 
    /// While we browse through the config location structure, the workflow states will be created
    /// lazily on-the-fly and transient. We'll assign the relevant information for each of those lazily
    /// created workflow states in this instance.
    /// It is necessary to use an own data structure in this case rather than using the workflow context
    /// data because we need to build up the mapping data structure before the navigation to those states
    /// takes place. At that time, there is no workflow context for those transient states yet.
    /// </remarks>
    protected class ContextStateDataDictionary : Dictionary<Guid, string> { }

    #region Protected fields

    protected const string CONTEXT_STATE_DATA_KEY = "ConfigurationModel: CONTEXT_STATE_DATA";

    protected WorkflowConfigurationController _workflowConfigurationController;

    protected IDictionary<Type, ConfigurationController> _registeredSettingTypes = new Dictionary<Type, ConfigurationController>();

    protected ItemsList _configSettingsList = null;
    protected string _currentLocation = null;
    protected ConfigurationController _currentConfigController = null;
    protected Property _headerTextProperty;
    protected ICollection<Property> _trackedVisibleEnabledProperties = new List<Property>();

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
      _headerTextProperty = new Property(typeof(string), null);
      Initialize();
    }

    protected void Initialize()
    {
      Register(new YesNoController());
      Register(new EntryController());
      Register(new SingleSelectionListController());
      Register(new MultiSelectionListController());
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

    public Property HeaderTextProperty
    {
      get { return _headerTextProperty; }
    }

    /// <summary>
    /// Returns the header text for the current configuration state (section name).
    /// </summary>
    public string HeaderText
    {
      get { return (string) _headerTextProperty.GetValue(); }
      set { _headerTextProperty.SetValue(value); }
    }

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
    /// Returns the data dictionary which represents the mapping between workflow navigation contexts
    /// and config states.
    /// </summary>
    /// <param name="context">Current navigation context.</param>
    /// <returns>Mapping context state data dictionary from the specified <paramref name="context"/> or
    /// from one of its predecessors.</returns>
    protected static ContextStateDataDictionary GetContextStateDataDictionary(NavigationContext context)
    {
      return (ContextStateDataDictionary) context.GetContextVariable(CONTEXT_STATE_DATA_KEY, true);
    }

    /// <summary>
    /// Returns the data dictionary which represents the mapping between workflow navigation contexts
    /// and config states, or creates it if it doesn't exist in the current navigation <paramref name="context"/>.
    /// </summary>
    /// <param name="context">Current navigation context.</param>
    /// <returns>Mapping context state data dictionary from the specified <paramref name="context"/> or
    /// from one of its predecessors.</returns>
    protected static void InitializeContextStateDataDictionary(NavigationContext context)
    {
      ContextStateDataDictionary dd = new ContextStateDataDictionary();
      Guid configMainStateId = new Guid(CONFIGURATION_MAIN_STATE_ID_STR);
      dd[configMainStateId] = "/";
      context.SetContextVariable(CONTEXT_STATE_DATA_KEY, dd);
    }

    /// <summary>
    /// Returns the config location corresponding to the workflow state given by the specified
    /// workflow navigation <paramref name="context"/>.
    /// </summary>
    /// <param name="context">The workflow navigation context whose workflow state will be used to lookup
    /// the mapping context state data dictionary.</param>
    /// <returns>Config location or <c>null</c>, if the mapping context state data dictionary was not
    /// initialized in the context any nor in any predecessor contexts, or if the workflow state in the
    /// given navigation <paramref name="context"/> is not available in the state data dictionary.</returns>
    protected static string GetConfigLocation(NavigationContext context)
    {
      ContextStateDataDictionary dd = GetContextStateDataDictionary(context);
      if (dd == null)
        return null;
      string result;
      return dd.TryGetValue(context.WorkflowState.StateId, out result) ? result : null;
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
        else if (childNode.ConfigObj is ConfigGroup)
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

    protected void TrackItemVisibleEnabledProperty(Property property)
    {
      property.Attach(OnVisibleEnabledChanged);
      _trackedVisibleEnabledProperties.Add(property);
    }

    protected void ReleaseAllVisibleEnabledNotifications()
    {
      foreach (Property property in _trackedVisibleEnabledProperties)
        property.Detach(OnVisibleEnabledChanged);
      _trackedVisibleEnabledProperties.Clear();
    }

    void OnVisibleEnabledChanged(Property visibleOrEnabledProperty, object oldValue)
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
      if (configLocation == null)
      {
        // Should not happen - we run into this case if our internal data structures weren't initialized for
        // the new state
        ServiceScope.Get<ILogger>().Error("ConfigurationModel: Workflow state '{0}' was not initialized in the context of this model",
            newContext.WorkflowState.StateId);
        return;
      }
      IConfigurationManager configurationManager = ServiceScope.Get<IConfigurationManager>();
      bool enteringConfiguration = GetConfigLocation(oldContext) == null;
      if (enteringConfiguration)
        configurationManager.Initialize();

      IConfigurationNode currentNode = configurationManager.GetNode(configLocation);
      if (configLocation == "/")
        HeaderText = "[Configuration.MainSettings]";
      else if (currentNode.ConfigObj == null)
        HeaderText = currentNode.Id;
      else
        HeaderText = currentNode.ConfigObj.Text.Evaluate();
      _currentLocation = configLocation;
      _configSettingsList = new ItemsList(); // Break references to old GUI screens by creating a new list (can be removed as soon as weak references are used in the change events of ItemsList)
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
      InitializeContextStateDataDictionary(newContext);
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

    public void UpdateMenuActions(NavigationContext context, ICollection<WorkflowStateAction> actions)
    {
      IConfigurationManager configurationManager = ServiceScope.Get<IConfigurationManager>();
      string configLocation = GetConfigLocation(context);
      if (configLocation == null)
      {
        // Should never happen - we run into this case if either this method is called with a non-initialized
        // config state or with a non-config state or if the method StartModelContext was not called before
        ServiceScope.Get<ILogger>().Error("ConfigurationModel: Workflow state '{0}' was not initialized in the context of this model", context.WorkflowState.StateId);
        return;
      }
      WorkflowState mainState;
      ServiceScope.Get<IWorkflowManager>().States.TryGetValue(new Guid(CONFIGURATION_MAIN_STATE_ID_STR),
          out mainState);
      IConfigurationNode currentNode = configurationManager.GetNode(configLocation);
      ContextStateDataDictionary stateDataDictionary = GetContextStateDataDictionary(context);
      if (stateDataDictionary == null)
        throw new InvalidStateException("ConfigurationModel: Model state data dictionary is not initialized");
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
              string.Format("Config: '{0}'", childNode.Location), CONFIGURATION_SECTION_SCREEN,
              false, false);
          // Add action for menu
          actions.Add(new PushTransientStateNavigationTransition(
              Guid.NewGuid(), context.WorkflowState.Name + "->" + childNode.Location,
                  context.WorkflowState.StateId, newState, LocalizationHelper.CreateResourceString(section.Metadata.Text)));
          // Initialize status in internal dictionary
          stateDataDictionary[newState.StateId] = childNode.Location;
        }
      }
    }

    #endregion
  }
}
