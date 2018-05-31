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
    public const string KEY_KEYMAP = "KeyMapData";
    public const string KEY_MENU_MODEL = "MenuModel: Item-Action";
    public const string KEY_PREFIX = "Key.";
    public const string MENU_PREFIX = "Menu.";

    private static readonly Guid HOME_STATE_ID = new Guid("7F702D9C-F2DD-42da-9ED8-0BA92F07787F");
    private static readonly Guid CONFIGURATION_STATE_ID = new Guid("E7422BB8-2779-49ab-BC99-E3F56138061B");

    protected AbstractProperty _inputDevicesProperty;
    protected AbstractProperty _addKeyLabelProperty;
    protected AbstractProperty _addKeyCountdownLabelProperty;
    protected AbstractProperty _showInputDeviceSelectionProperty;
    protected AbstractProperty _showKeyMappingProperty;
    protected AbstractProperty _showAddKeyProperty;
    protected AbstractProperty _showAddActionProperty;
    protected AbstractProperty _selectedItemProperty;
    protected ItemsList _items;
    protected ItemsList _actionItems;

    private static string _currentInputDevice;
    private static bool _inWorkflowAddKey = false;
    private static ConcurrentDictionary<string, int> _pressedKeys = new ConcurrentDictionary<string, int>();
    private static Dictionary<string, int> _pressedAddKeyCombo = new Dictionary<string, int>();
    private static int _maxPressedKeys = 0;
    private static readonly Timer _timer = new Timer(500);
    private DateTime _endTime;
    private Guid? _addKeyDialogHandle = null;
    private DialogCloseWatcher _dialogCloseWatcher = null;

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

    public ItemsList ActionItems
    {
      get { return _actionItems; }
    }

    public ItemsList KeyItems
    {
      get
      {
        ItemsList keyList = new ItemsList();
        foreach (var item in _actionItems.
          Where(i => ((string)i.AdditionalProperties[KEY_KEYMAP]).StartsWith(KEY_PREFIX, StringComparison.InvariantCultureIgnoreCase)).
          OrderBy(i => i.Labels[Consts.KEY_NAME].Evaluate()))
          keyList.Add(item);
        return keyList;
      }
    }

    public ItemsList MenuItems
    {
      get
      {
        ItemsList menuList = new ItemsList();
        foreach (var item in _actionItems.
          Where(i => ((string)i.AdditionalProperties[KEY_KEYMAP]).StartsWith(MENU_PREFIX, StringComparison.InvariantCultureIgnoreCase)).
          OrderBy(i => i.Labels[Consts.KEY_NAME].Evaluate()))
          menuList.Add(item);
        return menuList;
      }
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

    public bool ShowAddKey
    {
      get { return (bool)_showAddKeyProperty.GetValue(); }
      set { _showAddKeyProperty.SetValue(value); }
    }

    public AbstractProperty ShowAddKeyProperty
    {
      get { return _showAddKeyProperty; }
    }

    public bool ShowAddAction
    {
      get { return (bool)_showAddActionProperty.GetValue(); }
      set { _showAddActionProperty.SetValue(value); }
    }

    public AbstractProperty ShowAddActionProperty
    {
      get { return _showAddActionProperty; }
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
      _items = new ItemsList();

      if (_actionItems == null)
      {
        _actionItems = new ItemsList();
        _timer.Elapsed += timer_Tick;

        foreach (var key in Key.NAME2SPECIALKEY)
        {
          var listItem = new ListItem(Consts.KEY_NAME, key.Key) { Command = new MethodDelegateCommand(() => ChooseKeyAction(KEY_PREFIX + key.Key)) };
          listItem.AdditionalProperties[KEY_KEYMAP] = KEY_PREFIX + key.Key;
          _actionItems.Add(listItem);
        }

        // TODO: Add more actions?
        IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
        foreach (NavigationContext context in workflowManager.NavigationContextStack)
        {
          var itemsList = UpdateMenu(context);
          if (itemsList != null) CollectionUtils.AddAll(_actionItems, itemsList.Except(_actionItems));
        }
      }

      await Task.WhenAny(InputDeviceManager.InitComplete, Task.Delay(20000));
      if (InputDeviceManager.InitComplete.IsCompleted)
        InputDeviceManager.RawInput.KeyPressed += OnKeyPressed;
      else
        ServiceRegistration.Get<ILogger>().Error("InputDeviceModel: Timeout waiting for Input Manager");
    }

    protected List<ListItem> UpdateMenu(NavigationContext context)
    {
      List<ListItem> result = new List<ListItem>();
      
      foreach (var item in context.MenuActions.Values.ToList())
      {
        //ServiceRegistration.Get<ILogger>().Info("MenuActions - Name: {0}, DisplayTitle: {1}, ActionId: {2}", item.Name, item.DisplayTitle, item.ActionId);
        if (item.SourceStateIds?.Contains(HOME_STATE_ID) == true)
        {
          //Only add home menus for now. Other menus seem to have a dependency on the previews screen
          ListItem listItem = new ListItem(Consts.KEY_NAME, item.DisplayTitle) { Command = new MethodDelegateCommand(() => ChooseKeyAction(MENU_PREFIX + item.ActionId)) };
          listItem.AdditionalProperties[KEY_KEYMAP] = MENU_PREFIX + item.ActionId;
          result.Add(listItem);
        }
      }

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
          ShowAddActionScreen();
        }
        else
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
      _items.Clear();
      InputDevice device;
      if (InputDeviceManager.InputDevices.TryGetValue(_currentInputDevice, out device))
      {
        foreach (var keyMapping in device.KeyMap)
        {
          string text = (string)_actionItems.FirstOrDefault(i => string.Compare((string)i.AdditionalProperties[KEY_KEYMAP], keyMapping.Key, true) == 0)?.Labels[Consts.KEY_NAME]?.Evaluate() ?? keyMapping.Key;
          if (keyMapping.Key.StartsWith(KEY_PREFIX))
          {
            text = $"{LocalizationHelper.Translate(RES_KEY_TEXT)} \"{text}\"";
          }
          else if(keyMapping.Key.StartsWith(MENU_PREFIX))
          {
            text = $"{LocalizationHelper.Translate(RES_SCREEN_TEXT)} \"{text}\"";
          }
          var item = new ListItem(Consts.KEY_NAME, String.Format("{0}: {1}", text, String.Join(" + ", string.Join(" + ", keyMapping.Code.Select(KeyMapper.GetKeyName)))))
          {
            Command = new MethodDelegateCommand(MappingCommand)
          };
          item.AdditionalProperties.Add(KEY_KEYMAP, keyMapping);
          _items.Add(item);
        }
      }

      _items.FireChange();

      ShowKeyMappingScreen();
    }

    private void MappingCommand()
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
              MappedKeyCode mappedKeyCode = device.KeyMap.FirstOrDefault(k => ReferenceEquals(k, selectedItem.AdditionalProperties[KEY_KEYMAP]));
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
      ShowAddKey = false;
      ShowAddAction = false;
      ShowKeyMapping = false;
      if (removeOnKeyPressed)
        InputDeviceManager.RawInput.KeyPressed -= OnKeyPressed;
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

      ShowAddAction = false;
      ShowAddKey = true;
      _addKeyDialogHandle = ServiceRegistration.Get<IScreenManager>().ShowDialog("ConfigScreenAddKey", (s,g) =>
      {
        _addKeyDialogHandle = null;
        _timer.Stop();
      });
    }

    private void ShowAddActionScreen()
    {
      ShowAddKey = false;
      ShowAddAction = true;
    }

    #endregion Screen switching functions

    #region buttonActions

    public void AddKeyMapping()
    {
      ShowAddKeyScreen();
    }

    public void CancelMapping()
    {
      ResetCompleteModel(false);
    }

    #endregion buttonActions

    #region ListViewActions

    public void ChooseKeyAction(string chosenAction)
    {
      List<int> keys = _pressedAddKeyCombo.Select(key => key.Value).ToList();
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
        device.KeyMap.Add(new MappedKeyCode(chosenAction, keys));
      }
      else
      {
        var inputDevice = new InputDevice
        {
          DeviceID = _currentInputDevice,
          Name = _currentInputDevice,
          KeyMap = new List<MappedKeyCode> { new MappedKeyCode(chosenAction, keys) }
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

      ResetAddKey();

      // this brings us back to the add key menu
      UpdateKeymapping();
    }

    #endregion ListViewActions

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
