#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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
using System.Linq;
using System.Windows.Forms;
using HomeEditor.Groups;
using HomeEditor.Settings;
using MediaPortal.Common;
using MediaPortal.Common.Commands;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.Services.Settings;
using MediaPortal.UiComponents.SkinBase.General;
using MediaPortal.UiComponents.SkinBase.Models;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.UI.SkinEngine.MpfElements.Input;
using MediaPortal.Utilities;
using MediaPortal.Utilities.Events;

namespace MediaPortal.UiComponents.WMCSkin.Models
{
  public class HomeMenuModel : MenuModel
  {
    #region Protected Members

    protected const string KEY_ITEM_SUB_ITEMS = "HomeMenuModel: SubItems";

    private readonly DelayedEvent _delayedMenueUpdateEvent;
    private NavigationList<ListItem> _navigationList;    
    protected List<HomeMenuGroup> _groups;
    protected List<ListItem> _groupItems;
    protected HashSet<Guid> _groupedActions;
    protected bool _refreshNeeded;
    protected SettingsChangeWatcher<HomeEditorSettings> _settings;

    #endregion

    #region Ctor

    public HomeMenuModel()
    {
      _navigationList = new NavigationList<ListItem>();
      _groupItems = new List<ListItem>();
      _groupedActions = new HashSet<Guid>();
      NestedMenuItems = new ItemsList();
      SubItems = new ItemsList();

      _settings = new SettingsChangeWatcher<HomeEditorSettings>();
      _settings.SettingsChanged += OnSettingsChanged;
      SubscribeToMessages();

      _delayedMenueUpdateEvent = new DelayedEvent(200); // Update menu items only if no more requests are following after 200 ms
      _delayedMenueUpdateEvent.OnEventHandler += ReCreateMenuItems;

      _navigationList.OnCurrentChanged += SetSelection;
    }

    #endregion

    #region Message Handling

    private void SubscribeToMessages()
    {
      if (_messageQueue == null)
        return;
      _messageQueue.MessageReceived += OnMessageReceived;
    }

    private void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      //if (message.ChannelName == MenuModelMessaging.CHANNEL)
      //{
      //  if (((MenuModelMessaging.MessageType)message.MessageType) == MenuModelMessaging.MessageType.UpdateMenu)
      {
        UpdateMenu();
      }
      //}
    }

    private void UpdateMenu()
    {
      _delayedMenueUpdateEvent.EnqueueEvent(this, EventArgs.Empty);
    }

    #endregion

    #region Public Properties

    public ItemsList NestedMenuItems { get; private set; }
    public ItemsList SubItems { get; private set; }

    #endregion

    #region Public Methods

    public void MoveNext()
    {
      _navigationList.MoveNext();
    }

    public void MovePrevious()
    {
      _navigationList.MovePrevious();
    }

    public void SetSelectedItem(object sender, SelectionChangedEventArgs e)
    {
      var item = e.FirstAddedItem as NestedItem;
      SetSubItems(item);
    }

    public void OnKeyPress(object sender, KeyPressEventArgs e)
    {

    }

    public void OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
      if (e.NumDetents > 0)
        _navigationList.MovePrevious(e.NumDetents);
      else
        _navigationList.MoveNext(-e.NumDetents);
    }

    #endregion

    private void UpdateList(bool recreateList)
    {
      // Get new menu entries from base list
      if (recreateList)
      {
        RecreateGroupItems();
        var previousSelected = _navigationList.Current;
        _navigationList.Clear();
        CollectionUtils.AddAll(_navigationList, _groupItems);
        if (!_navigationList.MoveTo(i => i == previousSelected))
          _navigationList.CurrentIndex = 0;
      }
      var currentIndex = _navigationList.CurrentIndex;
      NestedMenuItems.Clear();
      int fillItems = 3;
      var count = _navigationList.Count;
      for (int i = currentIndex - fillItems; i < currentIndex + count; i++)
      {
        var item = _navigationList.GetAt(i) ?? new NestedItem(Consts.KEY_NAME, ""); /* Placeholder for empty space before current list item */
        NestedMenuItems.Add(item);
      }
      foreach (var nestedItem in NestedMenuItems)
      {
        nestedItem.Selected = nestedItem == _navigationList.Current;
      }
      NestedMenuItems.FireChange();
      SetSubItems(_navigationList.Current);
    }

    protected void LoadGroupsFromSettings()
    {
      var settingsGroups = _settings.Settings.Groups;
      _groups = new List<HomeMenuGroup>();
      if (settingsGroups != null && settingsGroups.Count > 0)
        _groups.AddRange(settingsGroups);
      else
        _groups.AddRange(DefaultGroups.Create());
    }

    protected void RecreateGroupItems()
    {
      if (_groups != null && !_refreshNeeded)
        return;
      _refreshNeeded = false;

      LoadGroupsFromSettings();
      var actions = ServiceRegistration.Get<IWorkflowManager>().MenuStateActions;

      _groupItems.Clear();
      _groupedActions.Clear();
      foreach (HomeMenuGroup group in _groups)
      {
        CollectionUtils.AddAll(_groupedActions, group.Actions.Select(a => a.ActionId));
        ListItem item = new ListItem(Consts.KEY_NAME, group.DisplayName);
        item.AdditionalProperties[KEY_ITEM_SUB_ITEMS] = CreateSubItems(group, actions);
        _groupItems.Add(item);
      }

      //Entry for all actions without a group
      ListItem extrasItem = new ListItem(Consts.KEY_NAME, LocalizationHelper.CreateResourceString(_settings.Settings.OthersGroupName));
      _groupItems.Add(extrasItem);
    }

    protected List<ListItem> CreateSubItems(HomeMenuGroup group, IDictionary<Guid, WorkflowAction> actions)
    {
      List<ListItem> items = new List<ListItem>();
      foreach (var groupAction in group.Actions)
      {
        WorkflowAction workflowAction;
        if (actions.TryGetValue(groupAction.ActionId, out workflowAction))
        {
          var listItem = new ListItem(Consts.KEY_NAME, groupAction.DisplayName);
          listItem.AdditionalProperties[Consts.KEY_ITEM_ACTION] = workflowAction;
          listItem.Command = new MethodDelegateCommand(() => workflowAction.Execute());
          items.Add(listItem);
        }
      }
      return items;
    }

    private void SetSubItems(ListItem item)
    {
      if (item != null)
      {
        SubItems.Clear();
        object oSubItems;
        if (item.AdditionalProperties.TryGetValue(KEY_ITEM_SUB_ITEMS, out oSubItems))
        {
          IEnumerable<ListItem> subItems = oSubItems as IEnumerable<ListItem>;
          if (subItems != null)
            CollectionUtils.AddAll(SubItems, subItems);
        }
        else
        {
          //Get items without a group
          foreach (var menuItem in MenuItems)
          {
            object oAction;
            if (menuItem.AdditionalProperties.TryGetValue(Consts.KEY_ITEM_ACTION, out oAction))
            {
              WorkflowAction action = oAction as WorkflowAction;
              if (action != null && !_groupedActions.Contains(action.ActionId))
                SubItems.Add(menuItem);
            }
          }
        }
        SubItems.FireChange();
      }
    }

    private void OnSettingsChanged(object sender, EventArgs e)
    {
      _refreshNeeded = true;
    }

    private void ReCreateMenuItems(object sender, EventArgs e)
    {
      UpdateList(true);
    }

    private void SetSelection(int oldindex, int newindex)
    {
      UpdateList(false);
    }
  }
}
