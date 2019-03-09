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
using System.Threading.Tasks;
using MediaPortal.Plugins.InputDeviceManager.RawInput;
using Keys = System.Windows.Forms.Keys;

namespace MediaPortal.Plugins.InputDeviceManager.Models
{
  public class InputDeviceModel : IWorkflowModel
  {
    public const string INPUTDEVICES_ID_STR = "CC11183C-01A9-4F96-AF90-FAA046981006";
    public const string RES_REMOVE_MAPPING_TEXT = "[InputDeviceManager.KeyMapping.Dialog.RemoveMapping]";
    public const string RES_KEY_TEXT = "[InputDeviceManager.Key]";
    public const string RES_SCREEN_TEXT = "[InputDeviceManager.Screen]";
    public const string RES_DEFAULT_KEYBOARD_TEXT = "[InputDeviceManager.DefaultConfig.Keyboard]";
    public const string RES_DEFAULT_REMOTE_TEXT = "[InputDeviceManager.DefaultConfig.Remote]";
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
    protected AbstractProperty _selectedInputProperty;
    protected ItemsList _items;
    protected ItemsList _keyItems;
    protected ItemsList _homeScreenItems;
    protected ItemsList _configScreenItems;
    protected ItemsList _defaultConfigItems;

    private static (string Type, string Name) _currentInputDevice;
    private static bool _inWorkflowAddKey = false;
    private static Dictionary<string, long> _pressedAddKeyCombo = new Dictionary<string, long>();
    private static int _maxPressedKeys = 0;
    private static readonly Timer _keyInputTimer = new Timer(100);
    private DateTime _endTime;
    private Guid? _addKeyDialogHandle = null;
    private DialogCloseWatcher _dialogCloseWatcher = null;
    private string _chosenAction = null;

    #region Properties

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

    public ItemsList HomeScreenItems
    {
      get { return _homeScreenItems; }
    }

    public ItemsList ConfigScreenItems
    {
      get { return _configScreenItems; }
    }

    public ItemsList DefaultConfigItems
    {
      get { return _defaultConfigItems; }
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

    public string SelectedInputName
    {
      get { return (string)_selectedInputProperty.GetValue(); }
      set { _selectedInputProperty.SetValue(value); }
    }

    public AbstractProperty SelectedInputNameProperty
    {
      get { return _selectedInputProperty; }
    }

    #endregion

    #region Initialization

    private Task InitModel()
    {
      _inputDevicesProperty = new WProperty(typeof(string), "TEST");
      _addKeyLabelProperty = new WProperty(typeof(string), "No Keys");
      _addKeyCountdownLabelProperty = new WProperty(typeof(string), "5");
      _showInputDeviceSelectionProperty = new WProperty(typeof(bool), true);
      _showKeyMappingProperty = new WProperty(typeof(bool), false);
      _showAddKeyProperty = new WProperty(typeof(bool), false);
      _showAddActionProperty = new WProperty(typeof(bool), false);
      _selectedItemProperty = new WProperty(typeof(ListItem), null);
      _selectedInputProperty = new WProperty(typeof(string), "");

      if (_items == null)
      {
        _items = new ItemsList();
        _keyItems = new ItemsList();
        _configScreenItems = new ItemsList();
        _homeScreenItems = new ItemsList();
        _defaultConfigItems = new ItemsList();
        _keyInputTimer.Elapsed += KeyInputTimer_Tick;

        foreach (var key in Key.NAME2SPECIALKEY)
        {
          if (key.Value == Key.None)
            continue;

          var listItem = new ListItem(Consts.KEY_NAME, $"{LocalizationHelper.Translate(RES_KEY_TEXT)} \"{key.Key}\"")
          {
            Command = new MethodDelegateCommand(() => ChooseKeyAction(KEY_PREFIX + key.Key))
          };
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

        AddDefaultConfig(RES_DEFAULT_KEYBOARD_TEXT, GetDefaultKeyboardMap());
        AddDefaultConfig(RES_DEFAULT_REMOTE_TEXT, GetDefaultRemoteMap());
      }
      InputDeviceManager.Instance.RegisterExternalKeyHandling(OnKeyPressed);
      return Task.CompletedTask;
    }

    private void ResetCompleteModel(bool removeOnKeyPressed = true)
    {
      ServiceRegistration.Get<ILogger>().Debug("InputDeviceManager: Reset model");

      ResetAddKey();

      // Reset screens
      SelectedInputName = "";
      _currentInputDevice = ("", "");
      ShowInputDeviceSelection = false;
      ShowKeyMapping = false;
      if (removeOnKeyPressed)
        InputDeviceManager.Instance.UnRegisterExternalKeyHandling(OnKeyPressed);
    }

    protected void AddDefaultConfig(string text, List<MappedKeyCode> config)
    {
      ListItem listItem = new ListItem(Consts.KEY_NAME, $"{LocalizationHelper.Translate(text)}");
      listItem.AdditionalProperties[KEY_KEYMAP_DATA] = config;
      _defaultConfigItems.Add(listItem);
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

    #endregion

    #region Key input handling

    private void OnKeyPressed(object sender, string name, string device, IDictionary<string, long> pressedKeys)
    {
      try
      {
        if (_inWorkflowAddKey)
        {
          //Add key screen
          if (_currentInputDevice.Type == device)
          {
            if (pressedKeys.Count > _maxPressedKeys)
            {
              _pressedAddKeyCombo = pressedKeys.ToDictionary(pair => pair.Key, pair => pair.Value);
              ServiceRegistration.Get<ILogger>().Debug("InputDeviceManager: Currently mapped keys: " + string.Join(", ", _pressedAddKeyCombo.Select(k => k.Key)));
              ServiceRegistration.Get<ILogger>().Debug("InputDeviceManager: Currently mapped codes: " + string.Join(", ", _pressedAddKeyCombo.Select(k => k.Value)));
              _maxPressedKeys = pressedKeys.Count;
              _endTime = DateTime.Now.AddSeconds(5);
              if (!_keyInputTimer.Enabled)
                _keyInputTimer.Start();
            }
            AddKeyLabel = String.Join(" + ", string.Join(" + ", _pressedAddKeyCombo.Select(kv => kv.Key.ToString())));
          }
        }
        else if (!ShowKeyMapping)
        {
          //Device selection screen
          _currentInputDevice = (device, name ?? "?");
          SelectedInputName = name ?? "?";
          UpdateKeymapping();
        }
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("InputDeviceManager: Key press failed", ex);
      }
    }

    #endregion

    #region Dialog handling

    public void OpenDefaultConfigurationDialog()
    {
      ServiceRegistration.Get<IScreenManager>().ShowDialog("ConfigScreenAddDefaultKeys");
    }

    public void SelectDefaultConfig(ListItem item)
    {
      if (item == null)
        return;

      List<MappedKeyCode> config = (List<MappedKeyCode>)item.AdditionalProperties[KEY_KEYMAP_DATA];
      UpdateKeymapping(config);
      ServiceRegistration.Get<IScreenManager>().CloseTopmostDialog();
    }

    #endregion

    #region Default mappings

    private List<MappedKeyCode> GetDefaultRemoteMap()
    {
      return new List<MappedKeyCode>
      {
        { new MappedKeyCode { Key = KEY_PREFIX + Key.Power.Name, Codes = new List<KeyCode> { new KeyCode { Key = RemoteButton.Power2.ToString(), Code = (int)RemoteButton.Power2 } } } },
        { new MappedKeyCode { Key = KEY_PREFIX + Key.Escape.Name, Codes = new List<KeyCode> { new KeyCode { Key = RemoteButton.Back.ToString(), Code = (int)RemoteButton.Back } } } },
        { new MappedKeyCode { Key = KEY_PREFIX + Key.Ok.Name, Codes = new List<KeyCode> { new KeyCode { Key = RemoteButton.Ok.ToString(), Code = (int)RemoteButton.Ok } } } },
        { new MappedKeyCode { Key = KEY_PREFIX + Key.Start.Name, Codes = new List<KeyCode> { new KeyCode { Key = RemoteButton.Start.ToString(), Code = (int)RemoteButton.Start } } } },
        { new MappedKeyCode { Key = KEY_PREFIX + Key.RecordedTV.Name, Codes = new List<KeyCode> { new KeyCode { Key = RemoteButton.RecordedTV.ToString(), Code = (int)RemoteButton.RecordedTV } } } },
        { new MappedKeyCode { Key = KEY_PREFIX + Key.Guide.Name, Codes = new List<KeyCode> { new KeyCode { Key = RemoteButton.Guide.ToString(), Code = (int)RemoteButton.Guide } } } },
        { new MappedKeyCode { Key = KEY_PREFIX + Key.LiveTV.Name, Codes = new List<KeyCode> { new KeyCode { Key = RemoteButton.LiveTV.ToString(), Code = (int)RemoteButton.LiveTV } } } },
        { new MappedKeyCode { Key = KEY_PREFIX + Key.DVDMenu.Name, Codes = new List<KeyCode> { new KeyCode { Key = RemoteButton.DVDMenu.ToString(), Code = (int)RemoteButton.DVDMenu } } } },
        { new MappedKeyCode { Key = KEY_PREFIX + Key.VolumeUp.Name, Codes = new List<KeyCode> { new KeyCode { Key = RemoteButton.VolumeUp.ToString(), Code = (int)RemoteButton.VolumeUp } } } },
        { new MappedKeyCode { Key = KEY_PREFIX + Key.VolumeDown.Name, Codes = new List<KeyCode> { new KeyCode { Key = RemoteButton.VolumeDown.ToString(), Code = (int)RemoteButton.VolumeDown } } } },
        { new MappedKeyCode { Key = KEY_PREFIX + Key.Mute.Name, Codes = new List<KeyCode> { new KeyCode { Key = RemoteButton.Mute.ToString(), Code = (int)RemoteButton.Mute } } } },
        { new MappedKeyCode { Key = KEY_PREFIX + Key.PageUp.Name, Codes = new List<KeyCode> { new KeyCode { Key = RemoteButton.ChannelUp.ToString(), Code = (int)RemoteButton.ChannelUp } } } },
        { new MappedKeyCode { Key = KEY_PREFIX + Key.PageDown.Name, Codes = new List<KeyCode> { new KeyCode { Key = RemoteButton.ChannelDown.ToString(), Code = (int)RemoteButton.ChannelDown } } } },
        { new MappedKeyCode { Key = KEY_PREFIX + Key.Up.Name, Codes = new List<KeyCode> { new KeyCode { Key = RemoteButton.Up.ToString(), Code = (int)RemoteButton.Up } } } },
        { new MappedKeyCode { Key = KEY_PREFIX + Key.Down.Name, Codes = new List<KeyCode> { new KeyCode { Key = RemoteButton.Down.ToString(), Code = (int)RemoteButton.Down } } } },
        { new MappedKeyCode { Key = KEY_PREFIX + Key.Left.Name, Codes = new List<KeyCode> { new KeyCode { Key = RemoteButton.Left.ToString(), Code = (int)RemoteButton.Left } } } },
        { new MappedKeyCode { Key = KEY_PREFIX + Key.Right.Name, Codes = new List<KeyCode> { new KeyCode { Key = RemoteButton.Right.ToString(), Code = (int)RemoteButton.Right } } } },
        { new MappedKeyCode { Key = KEY_PREFIX + Key.Info.Name, Codes = new List<KeyCode> { new KeyCode { Key = RemoteButton.Info.ToString(), Code = (int)RemoteButton.Info } } } },
        { new MappedKeyCode { Key = KEY_PREFIX + Key.Stop.Name, Codes = new List<KeyCode> { new KeyCode { Key = RemoteButton.Stop.ToString(), Code = (int)RemoteButton.Stop } } } },
        { new MappedKeyCode { Key = KEY_PREFIX + Key.Pause.Name, Codes = new List<KeyCode> { new KeyCode { Key = RemoteButton.Pause.ToString(), Code = (int)RemoteButton.Pause } } } },
        { new MappedKeyCode { Key = KEY_PREFIX + Key.Record.Name, Codes = new List<KeyCode> { new KeyCode { Key = RemoteButton.Record.ToString(), Code = (int)RemoteButton.Record } } } },
        { new MappedKeyCode { Key = KEY_PREFIX + Key.Play.Name, Codes = new List<KeyCode> { new KeyCode { Key = RemoteButton.Play.ToString(), Code = (int)RemoteButton.Play } } } },
        { new MappedKeyCode { Key = KEY_PREFIX + Key.Rew.Name, Codes = new List<KeyCode> { new KeyCode { Key = RemoteButton.Rewind.ToString(), Code = (int)RemoteButton.Rewind } } } },
        { new MappedKeyCode { Key = KEY_PREFIX + Key.Fwd.Name, Codes = new List<KeyCode> { new KeyCode { Key = RemoteButton.Forward.ToString(), Code = (int)RemoteButton.Forward } } } },
        { new MappedKeyCode { Key = KEY_PREFIX + Key.Previous.Name, Codes = new List<KeyCode> { new KeyCode { Key = RemoteButton.Replay.ToString(), Code = (int)RemoteButton.Replay } } } },
        { new MappedKeyCode { Key = KEY_PREFIX + Key.Next.Name, Codes = new List<KeyCode> { new KeyCode { Key = RemoteButton.Skip.ToString(), Code = (int)RemoteButton.Skip } } } },
        { new MappedKeyCode { Key = KEY_PREFIX + Key.Clear.Name, Codes = new List<KeyCode> { new KeyCode { Key = RemoteButton.Clear.ToString(), Code = (int)RemoteButton.Clear } } } },
        { new MappedKeyCode { Key = KEY_PREFIX + Key.Enter.Name, Codes = new List<KeyCode> { new KeyCode { Key = RemoteButton.Enter.ToString(), Code = (int)RemoteButton.Enter } } } },
        { new MappedKeyCode { Key = KEY_PREFIX + Key.TeleText.Name, Codes = new List<KeyCode> { new KeyCode { Key = RemoteButton.Teletext.ToString(), Code = (int)RemoteButton.Teletext } } } },
        { new MappedKeyCode { Key = KEY_PREFIX + Key.Red.Name, Codes = new List<KeyCode> { new KeyCode { Key = RemoteButton.Red.ToString(), Code = (int)RemoteButton.Red } } } },
        { new MappedKeyCode { Key = KEY_PREFIX + Key.Green.Name, Codes = new List<KeyCode> { new KeyCode { Key = RemoteButton.Green.ToString(), Code = (int)RemoteButton.Green } } } },
        { new MappedKeyCode { Key = KEY_PREFIX + Key.Yellow.Name, Codes = new List<KeyCode> { new KeyCode { Key = RemoteButton.Yellow.ToString(), Code = (int)RemoteButton.Yellow } } } },
        { new MappedKeyCode { Key = KEY_PREFIX + Key.Blue.Name, Codes = new List<KeyCode> { new KeyCode { Key = RemoteButton.Blue.ToString(), Code = (int)RemoteButton.Blue } } } },
      };
    }

    private List<MappedKeyCode> GetDefaultKeyboardMap()
    {
      return new List<MappedKeyCode>
      {
        { new MappedKeyCode { Key = KEY_PREFIX + Key.Info.Name, Codes = new List<KeyCode> { new KeyCode { Key = KeyMapper.GetMicrosoftKeyName((int)Keys.Apps), Code = (int)Keys.Apps } } } },
        { new MappedKeyCode { Key = KEY_PREFIX + Key.Up.Name, Codes = new List<KeyCode> { new KeyCode { Key = KeyMapper.GetMicrosoftKeyName((int)Keys.Up), Code = (int)Keys.Up } } } },
        { new MappedKeyCode { Key = KEY_PREFIX + Key.Down.Name, Codes = new List<KeyCode> { new KeyCode { Key = KeyMapper.GetMicrosoftKeyName((int)Keys.Down), Code = (int)Keys.Down } } } },
        { new MappedKeyCode { Key = KEY_PREFIX + Key.Right.Name, Codes = new List<KeyCode> { new KeyCode { Key = KeyMapper.GetMicrosoftKeyName((int)Keys.Right), Code = (int)Keys.Right } } } },
        { new MappedKeyCode { Key = KEY_PREFIX + Key.Left.Name, Codes = new List<KeyCode> { new KeyCode { Key = KeyMapper.GetMicrosoftKeyName((int)Keys.Left), Code = (int)Keys.Left } } } },
        //{ new MappedKeyCode { Key = KEY_PREFIX + Key.Escape.Name, Codes = new List<KeyCode> { new KeyCode { Key = KeyMapper.GetMicrosoftKeyName((int)Keys.Escape), Code = (int)Keys.Escape } } } },
        { new MappedKeyCode { Key = KEY_PREFIX + Key.Ok.Name, Codes = new List<KeyCode> { new KeyCode { Key = KeyMapper.GetMicrosoftKeyName((int)Keys.Return), Code = (int)Keys.Return } } } },
        { new MappedKeyCode { Key = KEY_PREFIX + Key.PageUp.Name, Codes = new List<KeyCode> { new KeyCode { Key = KeyMapper.GetMicrosoftKeyName((int)Keys.PageUp), Code = (int)Keys.PageUp } } } },
        { new MappedKeyCode { Key = KEY_PREFIX + Key.PageDown.Name, Codes = new List<KeyCode> { new KeyCode { Key = KeyMapper.GetMicrosoftKeyName((int)Keys.PageDown), Code = (int)Keys.PageDown } } } },
        { new MappedKeyCode { Key = KEY_PREFIX + Key.Record.Name, Codes = new List<KeyCode> { new KeyCode { Key = KeyMapper.GetMicrosoftKeyName((int)Keys.R), Code = (int)Keys.R } } } },
        { new MappedKeyCode { Key = KEY_PREFIX + Key.Fullscreen.Name, Codes = new List<KeyCode> { new KeyCode { Key = KeyMapper.GetMicrosoftKeyName((int)Keys.F), Code = (int)Keys.F } } } },
        { new MappedKeyCode { Key = KEY_PREFIX + Key.Play.Name, Codes = new List<KeyCode> { new KeyCode { Key = KeyMapper.GetMicrosoftKeyName((int)Keys.Play), Code = (int)Keys.Play } } } },
        { new MappedKeyCode { Key = KEY_PREFIX + Key.Pause.Name, Codes = new List<KeyCode> { new KeyCode { Key = KeyMapper.GetMicrosoftKeyName((int)Keys.Pause), Code = (int)Keys.Pause } } } },
        { new MappedKeyCode { Key = KEY_PREFIX + Key.PlayPause.Name, Codes = new List<KeyCode> { new KeyCode { Key = KeyMapper.GetMicrosoftKeyName((int)Keys.MediaPlayPause), Code = (int)Keys.MediaPlayPause } } } },
        { new MappedKeyCode { Key = KEY_PREFIX + Key.Stop.Name, Codes = new List<KeyCode> { new KeyCode { Key = KeyMapper.GetMicrosoftKeyName((int)Keys.MediaStop), Code = (int)Keys.MediaStop } } } },
        { new MappedKeyCode { Key = KEY_PREFIX + Key.Previous.Name, Codes = new List<KeyCode> { new KeyCode { Key = KeyMapper.GetMicrosoftKeyName((int)Keys.MediaPreviousTrack), Code = (int)Keys.MediaPreviousTrack } } } },
        { new MappedKeyCode { Key = KEY_PREFIX + Key.Next.Name, Codes = new List<KeyCode> { new KeyCode { Key = KeyMapper.GetMicrosoftKeyName((int)Keys.MediaNextTrack), Code = (int)Keys.MediaNextTrack } } } },
        { new MappedKeyCode { Key = KEY_PREFIX + Key.Mute.Name, Codes = new List<KeyCode> { new KeyCode { Key = KeyMapper.GetMicrosoftKeyName((int)Keys.VolumeMute), Code = (int)Keys.VolumeMute } } } },
        { new MappedKeyCode { Key = KEY_PREFIX + Key.VolumeUp.Name, Codes = new List<KeyCode> { new KeyCode { Key = KeyMapper.GetMicrosoftKeyName((int)Keys.Add), Code = (int)Keys.Add } } } },
        { new MappedKeyCode { Key = KEY_PREFIX + Key.VolumeDown.Name, Codes = new List<KeyCode> { new KeyCode { Key = KeyMapper.GetMicrosoftKeyName((int)Keys.Subtract), Code = (int)Keys.Subtract } } } },
        { new MappedKeyCode { Key = KEY_PREFIX + Key.ZapBack.Name, Codes = new List<KeyCode> { new KeyCode { Key = KeyMapper.GetMicrosoftKeyName((int)Keys.Multiply), Code = (int)Keys.Multiply } } } },
      };
    }

    #endregion

    #region Mapping input and storage

    private void KeyInputTimer_Tick(object sender, EventArgs e)
    {
      try
      {
        TimeSpan leftTime = _endTime.Subtract(DateTime.Now);
        if (leftTime.TotalSeconds < 0)
        {
          AddKeyCountdownLabel = "0";
          _keyInputTimer.Stop();
          _inWorkflowAddKey = false;

          if (_pressedAddKeyCombo.Count > 0 && _chosenAction != null)
          {
            var keys = _pressedAddKeyCombo.Select(c => new KeyCode(c.Key, c.Value)).ToList();
            UpdateSettings(new List<MappedKeyCode> { new MappedKeyCode(_chosenAction, keys) }, false);
            ServiceRegistration.Get<ILogger>().Debug("InputDeviceManager: Saved mapped keys: " + string.Join(", ", _pressedAddKeyCombo.Select(k => k.Key)));
            ServiceRegistration.Get<ILogger>().Debug("InputDeviceManager: Saved mapped codes: " + string.Join(", ", _pressedAddKeyCombo.Select(k => k.Value)));
            ServiceRegistration.Get<ILogger>().Debug("InputDeviceManager: Saved action: " + _chosenAction);
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

    private void UpdateSettings(List<MappedKeyCode> actions, bool overwrite)
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

      var device = inputDevices.FirstOrDefault(d => d.Type == _currentInputDevice.Type);
      if (device != null)
      {
        if (overwrite)
          device.KeyMap.Clear();

        foreach (var action in actions)
        {
          device.KeyMap.RemoveAll(k => k.Key == action.Key);
          device.KeyMap.Add(action);
        }
      }
      else
      {
        var inputDevice = new InputDevice
        {
          Type = _currentInputDevice.Type,
          Name = _currentInputDevice.Name,
          KeyMap = actions
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

    /// <summary>
    /// This function makes us ready to accept new key mappings
    /// </summary>
    private void ResetAddKey()
    {
      _keyInputTimer.Stop();
      _maxPressedKeys = 0;
      _pressedAddKeyCombo.Clear();
      AddKeyLabel = "";
      AddKeyCountdownLabel = "5";
      _inWorkflowAddKey = false;
    }

    #endregion

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
        _keyInputTimer.Stop();
      });
    }

    /// <summary>
    /// This updates the screen where the user can select which Keys he wants to add to the current input device.
    /// </summary>
    private void UpdateKeymapping(List<MappedKeyCode> defaultKeys = null)
    {
      InputDevice device;
      List<MappedKeyCode> mappedKeys = null;
      if (defaultKeys != null)
      {
        UpdateSettings(defaultKeys, true);
      }
      if (InputDeviceManager.InputDevices.TryGetValue(_currentInputDevice.Type, out device))
      {
        mappedKeys = device.KeyMap.ToList();
      }
      if (mappedKeys != null)
      {
        //Update labels
        foreach (var item in _items)
        {
          var itemMap = (string)item.AdditionalProperties[KEY_KEYMAP_DATA];
          var keyMapping = device.KeyMap.FirstOrDefault(k => k.Key.Equals(itemMap, StringComparison.InvariantCultureIgnoreCase));
          if (keyMapping?.Codes?.Count > 0)
            item.SetLabel(KEY_KEYMAP, string.Join(" + ", keyMapping.Codes.Select(c => c.Key)));
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

      _homeScreenItems.Clear();
      foreach (var item in _items.
          Where(i => ((string)i.AdditionalProperties[KEY_KEYMAP_DATA]).StartsWith(HOME_PREFIX, StringComparison.InvariantCultureIgnoreCase)).
          OrderBy(i => i.Labels[Consts.KEY_NAME].Evaluate()))
        _homeScreenItems.Add(item);
      _homeScreenItems.FireChange();

      _configScreenItems.Clear();
      foreach (var item in _items.
          Where(i => ((string)i.AdditionalProperties[KEY_KEYMAP_DATA]).StartsWith(CONFIG_PREFIX, StringComparison.InvariantCultureIgnoreCase)).
          OrderBy(i => i.Labels[Consts.KEY_NAME].Evaluate()))
        _configScreenItems.Add(item);
      _configScreenItems.FireChange();

      ShowKeyMappingScreen();
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
            if (InputDeviceManager.InputDevices.TryGetValue(_currentInputDevice.Type, out device))
            {
              MappedKeyCode mappedKeyCode = device.KeyMap.FirstOrDefault(k => k.Key == (string)selectedItem.AdditionalProperties[KEY_KEYMAP_DATA]);
              if (mappedKeyCode != null)
              {
                device.KeyMap.Remove(mappedKeyCode);

                ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();
                var settings = settingsManager.Load<InputManagerSettings>();
                var inputDevice = settings?.InputDevices.FirstOrDefault(d => d.Type == _currentInputDevice.Type);
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
      ResetCompleteModel();
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
