#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using MediaPortal.Common;
using MediaPortal.Common.Commands;
using MediaPortal.Common.General;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Settings;
using MediaPortal.UiComponents.SkinBase.General;
using MediaPortal.UI.Control.InputManager;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.Utilities;
using System.Threading.Tasks;
using MediaPortal.Plugins.InputDeviceManager.RawInput;

namespace MediaPortal.Plugins.InputDeviceManager.Models
{
  public class InputDeviceModel : IWorkflowModel
  {
    public const string INPUTDEVICES_ID_STR = "CC11183C-01A9-4F96-AF90-FAA046981006";
    public const string RES_REMOVE_MAPPING_TEXT = "[InputDeviceManager.KeyMapping.Dialog.RemoveMapping]";
    public const string RES_KEY_TEXT = "[InputDeviceManager.Key]";
    public const string RES_SCREEN_TEXT = "[InputDeviceManager.Screen]";
    public const string KEY_KEYMAP_DATA = "KeyMapData";
    public const string KEY_KEYMAP = "KeyMap";
    public const string KEY_KEYMAP_NAME = "MapName";
    public const string KEY_MENU_MODEL = "MenuModel: Item-Action";
    public const string KEY_PREFIX = "Key.";
    public const string HOME_PREFIX = "Home.";
    public const string CONFIG_PREFIX = "Config.";

    public static readonly Guid HOME_STATE_ID = new Guid("7F702D9C-F2DD-42da-9ED8-0BA92F07787F");
    public static readonly Guid CONFIGURATION_STATE_ID = new Guid("E7422BB8-2779-49ab-BC99-E3F56138061B");

    protected AbstractProperty _inputDevicesProperty;
    protected AbstractProperty _addKeyLabelProperty;
    protected AbstractProperty _addKeyCountdownLabelProperty;
    protected AbstractProperty _showInputDeviceSelectionProperty;
    protected AbstractProperty _showKeyMappingProperty;
    protected AbstractProperty _showAddKeyProperty;
    protected AbstractProperty _showAddActionProperty;
    protected AbstractProperty _selectedItemProperty;
    protected ItemsList _items;
    protected ItemsList _keyItems;
    protected ItemsList _screenItems;

    private static string _currentInputDevice;
    private static bool _inWorkflowAddKey = false;
    private static ConcurrentDictionary<string, int> _pressedKeys = new ConcurrentDictionary<string, int>();
    private static Dictionary<string, int> _pressedAddKeyCombo = new Dictionary<string, int>();
    private static int _maxPressedKeys = 0;
    private static readonly Timer _timer = new Timer(100);
    private DateTime _endTime;
    private Guid? _addKeyDialogHandle = null;
    private DialogCloseWatcher _dialogCloseWatcher = null;
    private string _chosenAction = null;

    public string AddKeyLabel
    {
      get { return (string)_addKeyLabelProperty.GetValue(); }
      set { _addKeyLabelProperty.SetValue(value); }
    }

    public AbstractProperty AddKeyLabelProperty
    {
      get { return _addKeyLabelProperty; }
    }

    public string AddKeyCountdownLabel
    {
      get { return (string)_addKeyCountdownLabelProperty.GetValue(); }
      set { _addKeyCountdownLabelProperty.SetValue(value); }
    }

    public AbstractProperty AddKeyCountdownLabelProperty
    {
      get { return _addKeyCountdownLabelProperty; }
    }

    public string InputDevices
    {
      get { return (string)_inputDevicesProperty.GetValue(); }
      set { _inputDevicesProperty.SetValue(value); }
    }

    public ItemsList Items
    {
      get { return _items; }
    }

    public ItemsList KeyItems
    {
      get { return _keyItems; }
    }

    public ItemsList ScreenItems
    {
      get { return _screenItems; }
    }

    public bool ShowInputDeviceSelection
    {
      get { return (bool)_showInputDeviceSelectionProperty.GetValue(); }
      set { _showInputDeviceSelectionProperty.SetValue(value); }
    }

    public AbstractProperty ShowInputDeviceSelectionProperty
    {
      get { return _showInputDeviceSelectionProperty; }
    }

    public bool ShowKeyMapping
    {
      get { return (bool)_showKeyMappingProperty.GetValue(); }
      set { _showKeyMappingProperty.SetValue(value); }
    }

    public AbstractProperty ShowKeyMappingProperty
    {
      get { return _showKeyMappingProperty; }
    }

    public ListItem SelectedItem
    {
      get { return (ListItem)_selectedItemProperty.GetValue(); }
      set { _selectedItemProperty.SetValue(value); }
    }

    public AbstractProperty SelectedItemProperty
    {
      get { return _selectedItemProperty; }
    }

    private async Task InitModel()
    {
      _inputDevicesProperty = new WProperty(typeof(string), "TEST");
      _addKeyLabelProperty = new WProperty(typeof(string), "No Keys");
      _addKeyCountdownLabelProperty = new WProperty(typeof(string), "5");
      _showInputDeviceSelectionProperty = new WProperty(typeof(bool), true);
      _showKeyMappingProperty = new WProperty(typeof(bool), false);
      _showAddKeyProperty = new WProperty(typeof(bool), false);
      _showAddActionProperty = new WProperty(typeof(bool), false);
      _selectedItemProperty = new WProperty(typeof(ListItem), null);

      if (_items == null)
      {
        _items = new ItemsList();
        _keyItems = new ItemsList();
        _screenItems = new ItemsList();
        _timer.Elapsed += timer_Tick;

        foreach (var key in Key.NAME2SPECIALKEY)
        {
          var listItem = new ListItem(Consts.KEY_NAME, $"{LocalizationHelper.Translate(RES_KEY_TEXT)} \"{key.Key}\"") { Command = new MethodDelegateCommand(() => ChooseKeyAction(KEY_PREFIX + key.Key)) };
          listItem.SetLabel(KEY_KEYMAP, "");
          listItem.SetLabel(KEY_KEYMAP_NAME, key.Key);
          listItem.AdditionalProperties[KEY_KEYMAP_DATA] = KEY_PREFIX + key.Key;
          _items.Add(listItem);
        }
        
        // TODO: Add more actions?
        IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
        foreach (NavigationContext context in workflowManager.NavigationContextStack)
        {
          foreach (var item in context.MenuActions.Values.ToList())
          {
            //ServiceRegistration.Get<ILogger>().Info("MenuActions - Name: {0}, DisplayTitle: {1}, ActionId: {2}", item.Name, item.DisplayTitle, item.ActionId);
            //Add home menu items
            if (item.SourceStateIds?.Contains(HOME_STATE_ID) == true)
            {
              AddScreen(item, HOME_PREFIX);
            }
            //Add config menu items
            else if (item.SourceStateIds?.Contains(CONFIGURATION_STATE_ID) == true)
            {
              //Only add home menus for now. Other menus seem to have a dependency on the previews screen
              AddScreen(item, CONFIG_PREFIX);
            }
          }
        }
      }
      InputDeviceManager.Instance.RegisterExternalKeyHandling(OnKeyPressed);
    }

    protected void AddScreen(WorkflowAction item, string prefix)
    {
      ListItem listItem = new ListItem(Consts.KEY_NAME, $"{LocalizationHelper.Translate(RES_SCREEN_TEXT)} \"{item.DisplayTitle}\"") { Command = new MethodDelegateCommand(() => ChooseKeyAction(prefix + item.Name)) };
      listItem.SetLabel(KEY_KEYMAP, "");
      listItem.SetLabel(KEY_KEYMAP_NAME, item.DisplayTitle);
      listItem.AdditionalProperties[KEY_KEYMAP_DATA] = prefix + item.Name;
      if (!_items.Any(i => string.Compare((string)i.AdditionalProperties[KEY_KEYMAP_DATA], (string)listItem.AdditionalProperties[KEY_KEYMAP_DATA], true) == 0))
        _items.Add(listItem);
    }

    protected List<ListItem> UpdateMenu(NavigationContext context)
    {
      List<ListItem> result = new List<ListItem>();
      foreach (var item in context.MenuActions.Values.ToList())
      {
        //ServiceRegistration.Get<ILogger>().Info("MenuActions - Name: {0}, DisplayTitle: {1}, ActionId: {2}", item.Name, item.DisplayTitle, item.ActionId);
        //  if (item.SourceStateIds?.Contains(HOME_STATE_ID) == true || item.SourceStateIds?.Contains(CONFIGURATION_STATE_ID) == true)
        //  {
        //    //Only add home menus for now. Other menus seem to have a dependency on the previews screen
        //    ListItem listItem = new ListItem(Consts.KEY_NAME, item.DisplayTitle) { Command = new MethodDelegateCommand(() => ChooseKeyAction(MENU_PREFIX + item.ActionId)) };
        //    listItem.AdditionalProperties[KEY_KEYMAP] = MENU_PREFIX + item.ActionId;
        //    result.Add(listItem);
        //  }
        //}

        //var menuItems = (ItemsList)context.GetContextVariable(Consts.KEY_MENU_ITEMS, false);
        //if (menuItems != null)
        //  foreach (var item in menuItems.ToList())
        //  {
        //    //ServiceRegistration.Get<ILogger>().Info(String.Join(" + ", string.Join(" + ", item.Labels.Select(kv => kv.Key.ToString() + "=" + kv.Value?.ToString() ?? "").ToArray())));
        //    if (item.AdditionalProperties.Keys.Contains(KEY_MENU_MODEL) && item.AdditionalProperties[KEY_MENU_MODEL] is WorkflowAction action)
        //    {
        //      ListItem listItem = new ListItem(Consts.KEY_NAME, action.DisplayTitle) { Command = new MethodDelegateCommand(() => ChooseKeyAction(MENU_PREFIX + action.ActionId)) };
        //      listItem.AdditionalProperties[KEY_KEYMAP] = MENU_PREFIX + action.ActionId;
        //      result.Add(listItem);
        //    }
        //  }

        //var temp4 = (ICollection<WorkflowAction>)context.GetContextVariable(Consts.KEY_ITEM_ACTION, false);
        //if (temp4 != null)
        //  foreach (var item in temp4.ToList())
        //  {
        //    ServiceRegistration.Get<ILogger>().Info("Temp4 - Name: {0}, ActionId: {1}, DisplayTitle: {2}", item.Name, item.ActionId.ToString(), item.DisplayTitle);
        //  }

        //var temp2 = (ICollection<WorkflowAction>)context.GetContextVariable(Consts.KEY_REGISTERED_ACTIONS, false);
        //if (temp2 != null)
        //  foreach (var item in temp2)
        //  {
        //    ServiceRegistration.Get<ILogger>().Info("Name: {0}, ActionId: {1}, DisplayTitle: {2}", item.Name, item.ActionId.ToString(), item.DisplayTitle);
        //  }
      }
      return result;
    }

    private void OnKeyPressed(object sender, RawInputEventArg e)
    {
      switch (e.KeyPressEvent.Message)
      {
        case Win32.WM_KEYDOWN:
        case Win32.WM_SYSKEYDOWN:
          _pressedKeys.GetOrAdd(e.KeyPressEvent.VKeyName, e.KeyPressEvent.VKey);
          break;
        case Win32.WM_KEYUP:
          _pressedKeys.TryRemove(e.KeyPressEvent.VKeyName, out int tmp);
          break;
      }
      e.Handled = true;

      if (ShowKeyMapping || _inWorkflowAddKey)
      {
        if (_inWorkflowAddKey)
        {
          if (_pressedKeys.Count > _maxPressedKeys)
          {
            _pressedAddKeyCombo = _pressedKeys.ToDictionary(pair => pair.Key, pair => pair.Value);
            _maxPressedKeys = _pressedKeys.Count;
            //ServiceRegistration.Get<ILogger>().Info("pressedKeys: {0}, maxPressedKEys: {1}, _pressedAddKeyCombo: {2}", _pressedKeys.Count, _maxPressedKeys, _pressedAddKeyCombo.Count);
            
            _endTime = DateTime.Now.AddSeconds(5);
            if (!_timer.Enabled)
              _timer.Start();
          }
          AddKeyLabel = String.Join(" + ", string.Join(" + ", _pressedAddKeyCombo.Select(kv => kv.Key.ToString())));
        }
      }
      else
      {
        _currentInputDevice = e.KeyPressEvent.Source;
        UpdateKeymapping();
      }

      /*ServiceRegistration.Get<ILogger>().Info("Confscren: {0}", e.KeyPressEvent.DeviceHandle.ToString());
      ServiceRegistration.Get<ILogger>().Info("Confscren: {0}", e.KeyPressEvent.DeviceType);
      ServiceRegistration.Get<ILogger>().Info("Confscren: {0}", e.KeyPressEvent.DeviceName);
      ServiceRegistration.Get<ILogger>().Info("Confscren: {0}", e.KeyPressEvent.Name);
      ServiceRegistration.Get<ILogger>().Info("Confscren: {0}", e.KeyPressEvent.VKey.ToString(CultureInfo.InvariantCulture));
      ServiceRegistration.Get<ILogger>().Info("Confscren: {0}", _rawinput.NumberOfKeyboards.ToString(CultureInfo.InvariantCulture));
      ServiceRegistration.Get<ILogger>().Info("Confscren: {0}", e.KeyPressEvent.VKeyName);
      ServiceRegistration.Get<ILogger>().Info("Confscren: {0}", e.KeyPressEvent.Source);
      ServiceRegistration.Get<ILogger>().Info("Confscren: {0}", e.KeyPressEvent.KeyPressState);
      ServiceRegistration.Get<ILogger>().Info("0x{0:X4} ({0})", e.KeyPressEvent.Message);*/
    }

    private void timer_Tick(object sender, EventArgs e)
    {
      try
      {
        TimeSpan leftTime = _endTime.Subtract(DateTime.Now);
        if (leftTime.TotalSeconds < 0)
        {
          AddKeyCountdownLabel = "0";
          _timer.Stop();
          _inWorkflowAddKey = false;

          List<int> keys = _pressedAddKeyCombo.Select(key => key.Value).ToList();
          if (keys.Count > 0 && _chosenAction != null)
          {
            ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();
            InputManagerSettings settings = settingsManager.Load<InputManagerSettings>();
            List<InputDevice> inputDevices = new List<InputDevice>();
            if (settings != null)
            {
              try
              {
                inputDevices = settings.InputDevices.ToList();
              }
              catch { }
            }

            var device = inputDevices.FirstOrDefault(d => d.DeviceID == _currentInputDevice);
            if (device != null)
            {
              device.KeyMap.Add(new MappedKeyCode(_chosenAction, keys));
            }
            else
            {
              var inputDevice = new InputDevice
              {
                DeviceID = _currentInputDevice,
                Name = _currentInputDevice,
                KeyMap = new List<MappedKeyCode> { new MappedKeyCode(_chosenAction, keys) }
              };
              inputDevices.Add(inputDevice);
            }
            if (settings != null)
              settings.InputDevices = inputDevices;
            else
              settings = new InputManagerSettings { InputDevices = inputDevices };
            settingsManager.Save(settings);

            // update settings in the main plugin
            InputDeviceManager.Instance.UpdateLoadedSettings(settings);
          }

          ResetAddKey();
          _chosenAction = null;

          // this brings us back to the add key menu
          UpdateKeymapping();
        }
        else if(AddKeyCountdownLabel != leftTime.Seconds.ToString("0"))
        {
          AddKeyCountdownLabel = leftTime.Seconds.ToString("0");
        }
      }
      catch
      {
        // ignored
      }
    }

    /// <summary>
    /// This updates the screen where the user can select which Keys he wants to add to the current input device.
    /// </summary>
    private void UpdateKeymapping()
    {
      InputDevice device;
      if (InputDeviceManager.InputDevices.TryGetValue(_currentInputDevice, out device))
      {
        List<MappedKeyCode> mappedKeys = device.KeyMap.ToList();

        //Update labels
        foreach (var item in _items)
        {
          var itemMap = (string)item.AdditionalProperties[KEY_KEYMAP_DATA];
          var keyMapping = device.KeyMap.FirstOrDefault(k => k.Key.Equals(itemMap, StringComparison.InvariantCultureIgnoreCase));
          if (keyMapping?.Code?.Count > 0)
            item.SetLabel(KEY_KEYMAP, string.Join(" + ", keyMapping.Code.Select(KeyMapper.GetKeyName)));
          else
            item.SetLabel(KEY_KEYMAP, "");
          if (keyMapping != null)
            mappedKeys.RemoveAll(k => k.Key.Equals(itemMap, StringComparison.InvariantCultureIgnoreCase));
        }
        //Add items for unknown key mappings
        foreach (var keyMapping in mappedKeys)
        {
          var item = new ListItem(Consts.KEY_NAME, keyMapping.Key) { Command = new MethodDelegateCommand(() => ChooseKeyAction(keyMapping.Key)) };
          item.SetLabel(KEY_KEYMAP, "");
          item.AdditionalProperties[KEY_KEYMAP_DATA] = keyMapping.Key;
          _items.Add(item);
        }
      }
      _items.FireChange();

      _keyItems.Clear();
      foreach (var item in _items.
          Where(i => ((string)i.AdditionalProperties[KEY_KEYMAP_DATA]).StartsWith(KEY_PREFIX, StringComparison.InvariantCultureIgnoreCase)).
          OrderBy(i => i.Labels[Consts.KEY_NAME].Evaluate()))
        _keyItems.Add(item);
      _keyItems.FireChange();

      _screenItems.Clear();
      foreach (var item in _items.
          Where(i => !((string)i.AdditionalProperties[KEY_KEYMAP_DATA]).StartsWith(KEY_PREFIX, StringComparison.InvariantCultureIgnoreCase)).
          OrderBy(i => i.Labels[Consts.KEY_NAME].Evaluate()))
        _screenItems.Add(item);
      _screenItems.FireChange();

      ShowKeyMappingScreen();
    }

    /// <summary>
    /// This function makes us ready to accept new key mappings
    /// </summary>
    private void ResetAddKey()
    {
      _timer.Stop();
      _maxPressedKeys = 0;
      _pressedKeys.Clear();
      _pressedAddKeyCombo.Clear();
      AddKeyLabel = "";
      AddKeyCountdownLabel = "5";
      _inWorkflowAddKey = false;
    }

    private void ResetCompleteModel(bool removeOnKeyPressed = true)
    {
      ResetAddKey();

      // Reset screens
      ShowInputDeviceSelection = false;
      ShowKeyMapping = false;
      if (removeOnKeyPressed)
        InputDeviceManager.Instance.UnRegisterExternalKeyHandling(OnKeyPressed);
    }

    #region Screen switching functions

    private void ShowKeyMappingScreen()
    {
      ResetAddKey();

      ShowInputDeviceSelection = false;
      ShowKeyMapping = true;
      if (_addKeyDialogHandle.HasValue)
        ServiceRegistration.Get<IScreenManager>().CloseDialog(_addKeyDialogHandle.Value);
    }

    private void ShowAddKeyScreen()
    {
      ResetAddKey();
      _inWorkflowAddKey = true;

      _addKeyDialogHandle = ServiceRegistration.Get<IScreenManager>().ShowDialog("ConfigScreenAddKey", (s, g) =>
      {
        _addKeyDialogHandle = null;
        _timer.Stop();
      });
    }

    #endregion Screen switching functions

    #region Button Actions

    public void AddKeyMapping()
    {
      ShowAddKeyScreen();
    }

    public void CancelMapping()
    {
      ResetCompleteModel(false);
    }

    public void DeleteKeyMapping()
    {
      if (SelectedItem != null)
      {
        var selectedItem = SelectedItem;
        var dialogManager = ServiceRegistration.Get<IDialogManager>();
        Guid handle = dialogManager.ShowDialog(selectedItem.Label(Consts.KEY_NAME, selectedItem.ToString()).ToString(),
          LocalizationHelper.Translate(RES_REMOVE_MAPPING_TEXT),
          DialogType.YesNoDialog, false, DialogButtonType.No);
        _dialogCloseWatcher = new DialogCloseWatcher(this, handle,
        dialogResult =>
        {
          if (dialogResult == DialogResult.Yes && selectedItem != null)
          {
            InputDevice device;
            if (InputDeviceManager.InputDevices.TryGetValue(_currentInputDevice, out device))
            {
              MappedKeyCode mappedKeyCode = device.KeyMap.FirstOrDefault(k => k.Key == (string)selectedItem.AdditionalProperties[KEY_KEYMAP_DATA]);
              if (mappedKeyCode != null)
              {
                device.KeyMap.Remove(mappedKeyCode);

                ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();
                var settings = settingsManager.Load<InputManagerSettings>();
                var inputDevice = settings?.InputDevices.FirstOrDefault(d => d.DeviceID == _currentInputDevice);
                if (inputDevice != null)
                {
                  inputDevice.KeyMap = device.KeyMap;
                  settingsManager.Save(settings);
                  // update settings in the main plugin
                  InputDeviceManager.Instance.UpdateLoadedSettings(settings);
                  // this brings us back to the add key menu
                  UpdateKeymapping();
                }
              }
            }
          }
          _dialogCloseWatcher?.Dispose();
        });
      }
    }

    #endregion buttonActions

    #region List View Actions

    public void ChooseKeyAction(string chosenAction)
    {
      _chosenAction = chosenAction;
      ShowAddKeyScreen();
    }

    #endregion List View Actions

    #region IWorkflowModel implementation

    public Guid ModelId
    {
      get { return new Guid(INPUTDEVICES_ID_STR); }
    }

    public bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
    {
      return true;
    }

    public void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      InitModel().Wait();
    }

    public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      ResetCompleteModel();
    }

    public void ChangeModelContext(NavigationContext oldContext, NavigationContext newContext, bool push)
    {
      // Nothing to do here
    }

    public void Deactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      // Nothing to do here
    }

    public void Reactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      ShowInputDeviceSelection = true;
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
