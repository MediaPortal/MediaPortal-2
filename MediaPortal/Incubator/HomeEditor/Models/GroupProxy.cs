using HomeEditor.Groups;
using MediaPortal.Common.General;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UiComponents.SkinBase.General;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeEditor.Models
{
  public class GroupProxy : AbstractItemProxy<HomeMenuAction>
  {
    protected AbstractProperty _displayNameProperty = new WProperty(typeof(string), null);
    protected HomeMenuGroup _group;

    public GroupProxy(HomeMenuGroup group)
    {
      _group = group;
      if (_group != null)
      {
        _items.AddRange(_group.Actions);
        DisplayName = _group.DisplayName;
      }
      UpdateItems();
    }

    public HomeMenuGroup Group
    {
      get { return _group; }
    }

    public AbstractProperty DisplayNameProperty
    {
      get { return _displayNameProperty; }
    }

    public string DisplayName
    {
      get { return (string)_displayNameProperty.GetValue(); }
      set { _displayNameProperty.SetValue(value); }
    }

    public List<HomeMenuAction> GroupActions
    {
      get { return _items; }
    }

    public bool SaveGroup()
    {
      bool created = false;
      if (_group == null)
      {
        created = true;
        _group = new HomeMenuGroup() { Id = Guid.NewGuid() };
      }
      _group.DisplayName = DisplayName;
      _group.Actions = new List<HomeMenuAction>(_items);
      return created;
    }

    public void MoveActionUp(ActionListItem item)
    {
      MoveItem(item.GroupAction, -1);
    }

    public void MoveActionDown(ActionListItem item)
    {
      MoveItem(item.GroupAction, 1);
    }

    public override void UpdateItems()
    {
      _itemsList.Clear();
      for (int i = 0; i < _items.Count; i++)
      {
        ActionListItem listItem = new ActionListItem(_items[i]);
        listItem.AdditionalProperties[KEY_IS_UP_BUTTON_FOCUSED] = i == _lastUpIndex;
        listItem.AdditionalProperties[KEY_IS_DOWN_BUTTON_FOCUSED] = i == _lastDownIndex;
        _itemsList.Add(listItem);
      }
      _itemsList.FireChange();
    }

    protected override void UpdateItemsToRemove()
    {
      _itemsToRemoveList.Clear();
      foreach (HomeMenuAction item in _items)
      {
        ListItem listItem = new ListItem(Consts.KEY_NAME, item.DisplayName);
        listItem.AdditionalProperties[KEY_ITEM_TO_REMOVE] = item;
        listItem.SelectedProperty.Attach(ItemToRemoveSelectionChanged);
        _itemsToRemoveList.Add(listItem);
      }
      _itemsToRemoveList.FireChange();
    }
  }
}
