using MediaPortal.Common;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UiComponents.SkinBase.General;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaPortal.UiComponents.Nereus.Models
{
  public class MenuEditModel
  {
    public const string IS_UP_FOCUSED_KEY = "IsUpButtonFocused";
    public const string IS_DOWN_FOCUSED_KEY = "IsDownButtonFocused";
    public const string IS_REMOVE_FOCUSED_KEY = "IsRemoveButtonFocused";

    public const string IS_UP_ENABLED_KEY = "IsUpButtonEnabled";
    public const string IS_DOWN_ENABLED_KEY = "IsDownButtonEnabled";

    protected Guid _actionStateId;
    protected IList<Guid> _menuActionIds;
    protected IList<ListItem> _sortedActionItems;
    protected IDictionary<Guid, ListItem> _actionItemsById;
    protected ItemsList _items;
    protected ItemsList _otherItems;

    public MenuEditModel(Guid actionStateId, IEnumerable<Guid> homeMenuActionIds)
    {
      _actionStateId = actionStateId;
      _menuActionIds = new List<Guid>(homeMenuActionIds);
      _items = new ItemsList();
      _otherItems = new ItemsList();
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
      _items[index].AdditionalProperties[IS_REMOVE_FOCUSED_KEY] = true;
      _items.FireChange();
      UpdateOtherItems();
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
      item.AdditionalProperties[IS_UP_FOCUSED_KEY] = false;
      item.AdditionalProperties[IS_DOWN_FOCUSED_KEY] = false;
      item.AdditionalProperties[IS_REMOVE_FOCUSED_KEY] = false;
      item.AdditionalProperties[IS_UP_ENABLED_KEY] = true;
      item.AdditionalProperties[IS_DOWN_ENABLED_KEY] = true;
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
        item.AdditionalProperties[IS_UP_ENABLED_KEY] = i > 0;
        item.AdditionalProperties[IS_DOWN_ENABLED_KEY] = i < _items.Count - 1;
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
  }
}
