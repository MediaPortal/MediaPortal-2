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
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.Settings;
using MediaPortal.Plugins.SystemStateMenu.Settings;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Workflow;

namespace MediaPortal.Plugins.SystemStateMenu.Models
{
  /// <summary>
  /// Workflow model for the system state configuration.
  /// </summary>
  public class SystemStateConfigurationModel : IWorkflowModel
  {
    public const string SYSTEM_STATE_CONFIGURATION_MODEL_ID_STR = "869C15FC-AF55-4003-BF0D-F5AF7B6D0B3B";

    #region Private fields

    private List<SystemStateItem> _shutdownItemList;
    private ItemsList _shutdownItems = null;

    protected int _topIndex = 0;
    protected int _focusedDownButton = -1;
    protected int _focusedUpButton = -1;

    #endregion

    public SystemStateConfigurationModel()
    {
      _shutdownItemList = null;
    }

    #region Public fields

    protected AbstractProperty _maxMinutesProperty = new WProperty(typeof(string), string.Empty);

    public AbstractProperty MaxMinutesProperty
    {
      get { return _maxMinutesProperty; }
    }
    public string MaxMinutes
    {
      get { return (string)_maxMinutesProperty.GetValue(); }
      set { _maxMinutesProperty.SetValue(value); }
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Loads SleepTimer-related configuration from the settings.
    /// </summary>
    private void GetSleepTimerConfigFromSettings()
    {
      SystemStateDialogSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<SystemStateDialogSettings>();
      MaxMinutes = settings.MaxSleepTimeout.ToString();
    }

    /// <summary>
    /// Loads shutdown actions from the settings.
    /// </summary>
    private void GetShutdownActionsFromSettings()
    {
      SystemStateDialogSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<SystemStateDialogSettings>();
      _shutdownItemList = settings.ShutdownItemList;
      
      // Add the SleepTimer, if the Element is missing in the current configuration
      bool foundSleepTimer = false;
      foreach(SystemStateItem item in _shutdownItemList)
      {
        if(item.Action == SystemStateAction.SleepTimer)
        {
          foundSleepTimer = true;
          break;
        }
      }
      if(foundSleepTimer == false)
      {
        // Add the SleepTimerItem after "Shutdown"
        SystemStateItem sleepTimerItem = new SystemStateItem(SystemStateAction.SleepTimer, true);
        int index = 0;
        foreach (SystemStateItem item in _shutdownItemList)
        {
          index++;
          if (item.Action == SystemStateAction.Shutdown)
            break;
        }
        _shutdownItemList.Insert(index, sleepTimerItem);
      }
    }

    private bool ItemCheckedChanged(int index, ListItem item)
    {
      bool isChecked = (bool) item.AdditionalProperties[Consts.KEY_IS_CHECKED];

      _shutdownItemList[index].Enabled = isChecked;

      return true;
    }

    private bool MoveItemUp(int index, ListItem item)
    {
      if (index <= 0 || index >= _shutdownItems.Count)
        return false;
      Utilities.CollectionUtils.Swap(_shutdownItemList, index, index - 1);

      _focusedDownButton = -1;
      _focusedUpButton = index - 1;

      UpdateShutdownItems();
      return true;
    }

    private bool MoveItemDown(int index, ListItem item)
    {
      if (index < 0 || index >= _shutdownItems.Count - 1)
        return false;
      Utilities.CollectionUtils.Swap(_shutdownItemList, index, index + 1);

      _focusedDownButton = index + 1;
      _focusedUpButton = -1;

      UpdateShutdownItems();
      return true;
    }

    private bool TryGetIndex(ListItem item, out int index)
    {
      index = -1;
      if (item == null)
        return false;
      object oIndex;
      if (item.AdditionalProperties.TryGetValue(Consts.KEY_INDEX, out oIndex))
      {
        int? i = oIndex as int?;
        if (i.HasValue)
        {
          index = i.Value;
          return true;
        }
      }
      return false;
    }

    private void UpdateShutdownItems()
    {
      _shutdownItems.Clear();
      if (_shutdownItemList != null)
      {
        for (int i = 0; i < _shutdownItemList.Count; i++)
        {
          SystemStateItem si = _shutdownItemList[i];

          ListItem item = new ListItem();
          item.SetLabel(Consts.KEY_NAME, Consts.GetResourceIdentifierForMenuItem(si.Action));

          item.AdditionalProperties[Consts.KEY_IS_CHECKED] = si.Enabled;
          item.AdditionalProperties[Consts.KEY_IS_DOWN_BUTTON_FOCUSED] = i == _focusedDownButton;
          item.AdditionalProperties[Consts.KEY_IS_UP_BUTTON_FOCUSED] = i == _focusedUpButton;
          item.AdditionalProperties[Consts.KEY_INDEX] = i;
          _shutdownItems.Add(item);
        }
        _focusedDownButton = -1;
        _focusedUpButton = -1;
      }
      _shutdownItems.FireChange();
    }

    #endregion

    #region Public properties (can be used by the GUI)

    public ItemsList ShutdownItems
    {
      get { return _shutdownItems; }
    }

    #endregion

    #region Public methods (can be used by the GUI)

    /// <summary>
    /// Saves the current state to the settings file.
    /// </summary>
    public void SaveSettings()
    {
      ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();
      SystemStateDialogSettings settings = settingsManager.Load<SystemStateDialogSettings>();
      // Apply new shutdown item list
      settings.ShutdownItemList = _shutdownItemList;

      // Test the SleepTimer-MaxMinutes
      int _maxMinutes = 0;
      if(Int32.TryParse(MaxMinutes, out _maxMinutes))
      {
        if (_maxMinutes > 0)
          settings.MaxSleepTimeout = _maxMinutes;
      }

      settingsManager.Save(settings);
    }

    /// <summary>
    /// Provides a callable method for the skin to change the checked state of a given shutdown <paramref name="item"/> in the itemlist.
    /// </summary>
    /// <param name="item">The choosen item.</param>
    public void CheckedChange(ListItem item)
    {
      int index;
      if (TryGetIndex(item, out index))
        ItemCheckedChanged(index, item);
    }

    /// <summary>
    /// Provides a callable method for the skin to move the given shutdown <paramref name="item"/> up in the itemlist.
    /// </summary>
    /// <param name="item">The choosen item.</param>
    public void MoveUp(ListItem item)
    {
      int index;
      if (TryGetIndex(item, out index))
        MoveItemUp(index, item);
    }

    /// <summary>
    /// Provides a callable method for the skin to move the given shutdown <paramref name="item"/> down in the itemlist.
    /// </summary>
    /// <param name="item">The choosen item.</param>
    public void MoveDown(ListItem item)
    {
      int index;

      if (TryGetIndex(item, out index))
        MoveItemDown(index, item);
    }

    #endregion

    #region IWorkflowModel implementation

    public Guid ModelId
    {
      get { return new Guid(SYSTEM_STATE_CONFIGURATION_MODEL_ID_STR); }
    }

    public bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
    {
      return true;
    }

    public void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      _shutdownItemList = new List<SystemStateItem>();
      _shutdownItems = new ItemsList();
      // Load settings
      GetShutdownActionsFromSettings();
      GetSleepTimerConfigFromSettings();
      UpdateShutdownItems();
    }

    public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      _shutdownItems.Clear();
      _shutdownItems = null;
    }

    public void ChangeModelContext(NavigationContext oldContext, NavigationContext newContext, bool push)
    {
      // TODO
    }

    public void Deactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      // Nothing to do here
    }

    public void Reactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      // Nothing to do here
    }

    public void UpdateMenuActions(NavigationContext context, IDictionary<Guid, WorkflowAction> actions)
    {
      // Nothing to do here
    }

    public ScreenUpdateMode UpdateScreen(NavigationContext context, ref string screen)
    {
      return ScreenUpdateMode.AutoWorkflowManager;
    }

    #endregion
  }
}
