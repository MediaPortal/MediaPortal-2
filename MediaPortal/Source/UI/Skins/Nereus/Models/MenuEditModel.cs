using MediaPortal.Common;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UiComponents.SkinBase.General;
using System;
using System.Collections.Generic;
using System.Linq;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UiComponents.Nereus.Models.HomeContent;

namespace MediaPortal.UiComponents.Nereus.Models
{
  public class MenuEditModel
  {
    public const string IS_UP_FOCUSED_KEY = "IsUpButtonFocused";
    public const string IS_DOWN_FOCUSED_KEY = "IsDownButtonFocused";
    public const string IS_REMOVE_FOCUSED_KEY = "IsRemoveButtonFocused";
    public const string IS_CONFIG_FOCUSED_KEY = "IsConfigButtonFocused";

    public const string IS_UP_ENABLED_KEY = "IsUpButtonEnabled";
    public const string IS_DOWN_ENABLED_KEY = "IsDownButtonEnabled";
    public const string IS_CONFIG_ENABLED_KEY = "IsConfigButtonEnabled";

    public const string BACKING_LIST_KEY = "BackingListKey";

    protected Guid _actionStateId; 
    protected Guid _selectedActionId; 
    protected IList<Guid> _menuActionIds;
    protected IList<ListItem> _sortedActionItems; 
    protected IList<ListItem> _allMediaListItems = new List<ListItem>(); 
    protected IDictionary<Guid, ListItem> _actionItemsById;
    protected IDictionary<Guid, object> _allHomeContent;
    protected IDictionary<Guid, IList<string>> _mediaLists;
    protected ItemsList _items;
    protected ItemsList _otherItems;
    protected ItemsList _mediaListItems;
    protected ItemsList _otherMediaListItems;

    public MenuEditModel(Guid actionStateId, IEnumerable<Guid> homeMenuActionIds, IDictionary<Guid, IList<string>>  mediaLists, IDictionary<Guid, object> allHomeContent)
    {
      _actionStateId = actionStateId;
      _menuActionIds = new List<Guid>(homeMenuActionIds);
      _allHomeContent = allHomeContent;
      _items = new ItemsList();
      _otherItems = new ItemsList();
      _mediaListItems = new ItemsList();
      _otherMediaListItems = new ItemsList();
      _mediaLists = mediaLists;
    }

    public ItemsList Items
    {
      get
      {
        CheckItemsCreated();
        return _items;
      }
    }

    public ItemsList OtherItems
    {
      get
      {
        CheckItemsCreated();
        return _otherItems;
      }
    }

    public ItemsList MediaListItems
    {
      get
      {
        return _mediaListItems;
      }
    }

    public ItemsList OtherMediaListItems
    {
      get
      {
        return _otherMediaListItems;
      }
    }

    #region Action menu handling

    public void MoveUp(ListItem item)
    {
      int index = _items.IndexOf(item);
      if (index < 1)
        return;
      _items.RemoveAt(index);
      _items.Insert(index - 1, item);
      ResetActionItemProperties(_items);
      if (index > 1)
        item.AdditionalProperties[IS_UP_FOCUSED_KEY] = true;
      else
        item.AdditionalProperties[IS_DOWN_FOCUSED_KEY] = true;
      _items.FireChange();
    }

    public void MoveDown(ListItem item)
    {
      int index = _items.IndexOf(item);
      if (index < 0 || index > _items.Count - 2)
        return;
      _items.RemoveAt(index);
      _items.Insert(index + 1, item);
      ResetActionItemProperties(_items);
      if (index < _items.Count - 2)
        item.AdditionalProperties[IS_DOWN_FOCUSED_KEY] = true;
      else
        item.AdditionalProperties[IS_UP_FOCUSED_KEY] = true;
      _items.FireChange();
    }

    public void Add(ListItem item)
    {
      if (_items.Contains(item))
        return;
      _items.Add(item);
      ResetActionItemProperties(_items);
      _items.FireChange();
      UpdateOtherItems();
    }

    public void Remove(ListItem item)
    {
      int index = _items.IndexOf(item);
      if (index < 0)
        return;

      _items.Remove(item);
      if (index >= _items.Count && _items.Count > 0)
        index = _items.Count - 1;
      ResetActionItemProperties(_items);
      if (index >= 0)
        _items[index].AdditionalProperties[IS_REMOVE_FOCUSED_KEY] = true;
      _items.FireChange();
      UpdateOtherItems();
    }

    public void Config(ListItem item)
    {
      int index = _items.IndexOf(item);
      if (index < 0)
        return;

      _selectedActionId = ((WorkflowAction)_items[index].AdditionalProperties[Consts.KEY_ITEM_ACTION]).ActionId;
      ResetActionItemProperties(_items);
      _items[index].AdditionalProperties[IS_CONFIG_FOCUSED_KEY] = true;
      _items.FireChange();

      CheckMediaListsItemsCreated();
      var sm = ServiceRegistration.Get<IScreenManager>();
      sm.ShowDialog("DialogEditMediaListMenu");
    }

    public IList<Guid> GetCurrentActionIds()
    {
      return _items.Select(i => ((WorkflowAction)i.AdditionalProperties[Consts.KEY_ITEM_ACTION]).ActionId).ToList();
    }

    protected void CheckItemsCreated()
    {
      if (_sortedActionItems != null && _sortedActionItems.Count > 0)
        return;
      CreateActionItemsForState(_actionStateId);
      UpdateMenuItems(_menuActionIds);
      UpdateOtherItems();
    }

    protected void UpdateMenuItems(IList<Guid> actionIds)
    {
      _items.Clear();
      foreach (Guid actionId in actionIds)
        if (_actionItemsById.TryGetValue(actionId, out ListItem item))
          _items.Add(item);
      _items.FireChange();
    }

    protected void UpdateOtherItems()
    {
      _otherItems.Clear();
      foreach (ListItem item in _sortedActionItems)
        if (!_items.Contains(item))
          _otherItems.Add(item);
      _otherItems.FireChange();
    }

    protected void CreateActionItemsForState(Guid workflowStateId)
    {
      IList<WorkflowAction> actions = GetRegisteredActionsForState(workflowStateId);

      List<ListItem> actionItems = new List<ListItem>();
      Dictionary<Guid, ListItem> actionItemsById = new Dictionary<Guid, ListItem>();
      foreach (WorkflowAction action in actions)
      {
        ListItem item = CreateActionListItem(action);
        actionItems.Add(item);
        actionItemsById[action.ActionId] = item;
      }
      _sortedActionItems = actionItems;
      _actionItemsById = actionItemsById;
    }

    protected ListItem CreateActionListItem(WorkflowAction action)
    {
      ListItem item = new ListItem(Consts.KEY_NAME, action.DisplayTitle);
      item.AdditionalProperties[Consts.KEY_ITEM_ACTION] = action;

      bool hasConfig = false;
      if (_allHomeContent.ContainsKey(action.ActionId) && _allHomeContent[action.ActionId] is AbstractHomeContent ahc)
        hasConfig = ahc.Lists.Count > 0;

      item.AdditionalProperties[IS_UP_FOCUSED_KEY] = false;
      item.AdditionalProperties[IS_DOWN_FOCUSED_KEY] = false;
      item.AdditionalProperties[IS_REMOVE_FOCUSED_KEY] = false;
      item.AdditionalProperties[IS_CONFIG_FOCUSED_KEY] = false;

      item.AdditionalProperties[IS_UP_ENABLED_KEY] = true;
      item.AdditionalProperties[IS_DOWN_ENABLED_KEY] = true;
      item.AdditionalProperties[IS_CONFIG_ENABLED_KEY] = hasConfig;
      return item;
    }

    protected void ResetActionItemProperties(ItemsList items)
    {
      for (int i = 0; i < items.Count; i++)
      {
        var item = items[i];
        item.AdditionalProperties[IS_UP_FOCUSED_KEY] = false;
        item.AdditionalProperties[IS_DOWN_FOCUSED_KEY] = false;
        item.AdditionalProperties[IS_REMOVE_FOCUSED_KEY] = false;
        item.AdditionalProperties[IS_CONFIG_FOCUSED_KEY] = false;

        item.AdditionalProperties[IS_UP_ENABLED_KEY] = i > 0;
        item.AdditionalProperties[IS_DOWN_ENABLED_KEY] = i < items.Count - 1;
      }
    }

    protected List<WorkflowAction> GetRegisteredActionsForState(Guid workflowStateId)
    {
      List<WorkflowAction> actions;
      var wf = ServiceRegistration.Get<IWorkflowManager>();
      wf.Lock.EnterReadLock();
      try
      {
        actions = new List<WorkflowAction>(wf.MenuStateActions.Values
          .Where(a => a.SourceStateIds != null && a.SourceStateIds.Contains(workflowStateId)));
      }
      finally
      {
        wf.Lock.ExitReadLock();
      }
      actions.Sort(CompareActions);
      return actions;
    }

    protected static int CompareActions(WorkflowAction a, WorkflowAction b)
    {
      int res = string.Compare(a.DisplayCategory, b.DisplayCategory);
      if (res != 0)
        return res;
      return string.Compare(a.SortOrder, b.SortOrder);
    }

    #endregion

    #region Media list handling

    public void MoveMediaListUp(ListItem item)
    {
      int index = _mediaListItems.IndexOf(item);
      if (index < 1)
        return;
      _mediaListItems.RemoveAt(index);
      _mediaListItems.Insert(index - 1, item);
      ResetMediaListItemProperties(_mediaListItems);
      if (index > 1)
        item.AdditionalProperties[IS_UP_FOCUSED_KEY] = true;
      else
        item.AdditionalProperties[IS_DOWN_FOCUSED_KEY] = true;
      _mediaListItems.FireChange();
    }

    public void MoveMediaListDown(ListItem item)
    {
      int index = _mediaListItems.IndexOf(item);
      if (index < 0 || index > _mediaListItems.Count - 2)
        return;
      _mediaListItems.RemoveAt(index);
      _mediaListItems.Insert(index + 1, item);
      ResetMediaListItemProperties(_mediaListItems);
      if (index < _mediaListItems.Count - 2)
        item.AdditionalProperties[IS_DOWN_FOCUSED_KEY] = true;
      else
        item.AdditionalProperties[IS_UP_FOCUSED_KEY] = true;
      _mediaListItems.FireChange();
    }

    public void AddMediaList(ListItem item)
    {
      if (_mediaListItems.Contains(item))
        return;
      _mediaListItems.Add(item);
      ResetMediaListItemProperties(_mediaListItems);
      _mediaListItems.FireChange();
      UpdateOtherMediaListItems();
    }

    public void RemoveMediaList(ListItem item)
    {
      int index = _mediaListItems.IndexOf(item);
      if (index < 0)
        return;

      _mediaListItems.Remove(item);
      if (index >= _mediaListItems.Count && _mediaListItems.Count > 0)
        index = _mediaListItems.Count - 1;
      ResetMediaListItemProperties(_mediaListItems);
      if (index >= 0)
        _mediaListItems[index].AdditionalProperties[IS_REMOVE_FOCUSED_KEY] = true;
      _mediaListItems.FireChange();
      UpdateOtherMediaListItems();
    }

    protected void CheckMediaListsItemsCreated()
    {
      UpdateMediaListMenuItems();
      UpdateOtherMediaListItems();
    }

    protected void UpdateMediaListMenuItems()
    {
      _allMediaListItems.Clear();
      if (_allHomeContent.ContainsKey(_selectedActionId) && _allHomeContent[_selectedActionId] is AbstractHomeContent ahc)
      {
        foreach (var list in ahc.Lists)
          _allMediaListItems.Add(CreateMediaListItem(list));
      }

      _mediaListItems.Clear();
      if (_mediaLists.ContainsKey(_selectedActionId))
      {
        var list = _mediaLists[_selectedActionId];
        foreach (var key in list)
        {
          var item = _allMediaListItems.FirstOrDefault(l => (string)l.AdditionalProperties[BACKING_LIST_KEY] == key);
          if (item != null)
            _mediaListItems.Add(item);
        }
      }
      else
      {
        foreach (var item in _allMediaListItems)
          _mediaListItems.Add(item);
      }
      _mediaListItems.FireChange();
    }

    protected void UpdateOtherMediaListItems()
    {
      _otherMediaListItems.Clear();
      foreach (ListItem item in _allMediaListItems)
        if (!_mediaListItems.Contains(item))
          _otherMediaListItems.Add(item);
      _otherMediaListItems.FireChange();
    }

    protected ListItem CreateMediaListItem(MediaListItemsListWrapper list)
    {
      ListItem item = new ListItem(Consts.KEY_NAME, list.Name);
      item.AdditionalProperties[BACKING_LIST_KEY] = list.MediaListKey;

      item.AdditionalProperties[IS_UP_FOCUSED_KEY] = false;
      item.AdditionalProperties[IS_DOWN_FOCUSED_KEY] = false;
      item.AdditionalProperties[IS_REMOVE_FOCUSED_KEY] = false;
      item.AdditionalProperties[IS_CONFIG_FOCUSED_KEY] = false;

      item.AdditionalProperties[IS_UP_ENABLED_KEY] = true;
      item.AdditionalProperties[IS_DOWN_ENABLED_KEY] = true;
      item.AdditionalProperties[IS_CONFIG_ENABLED_KEY] = false;
      return item;
    }

    protected void ResetMediaListItemProperties(ItemsList items)
    {
      for (int i = 0; i < items.Count; i++)
      {
        var item = items[i];
        item.AdditionalProperties[IS_UP_FOCUSED_KEY] = false;
        item.AdditionalProperties[IS_DOWN_FOCUSED_KEY] = false;
        item.AdditionalProperties[IS_REMOVE_FOCUSED_KEY] = false;

        item.AdditionalProperties[IS_UP_ENABLED_KEY] = i > 0;
        item.AdditionalProperties[IS_DOWN_ENABLED_KEY] = i < items.Count - 1;
      }
    }

    public void SaveMediaListEdit()
    {
      var list = _mediaListItems.Select(i => (string)i.AdditionalProperties[BACKING_LIST_KEY]).ToList();
      _mediaLists[_selectedActionId] = list;
    }

    public IDictionary<Guid, IList<string>> GetMediaLists()
    {
      return _mediaLists;
    }

    #endregion
  }
}
