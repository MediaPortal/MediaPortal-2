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

using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeEditor.Models
{
  public abstract class AbstractItemProxy<T>
  {
    protected const string KEY_IS_UP_BUTTON_FOCUSED = "IsUpButtonFocused";
    protected const string KEY_IS_DOWN_BUTTON_FOCUSED = "IsDownButtonFocused";
    protected const string KEY_ITEM_TO_REMOVE = "ItemToRemove";
    protected AbstractProperty _itemsToRemoveValidProperty = new WProperty(typeof(bool), false);

    protected List<T> _items = new List<T>();
    protected List<T> _itemsToRemove;
    protected ItemsList _itemsList = new ItemsList();
    protected ItemsList _itemsToRemoveList = new ItemsList();
    protected int _lastUpIndex;
    protected int _lastDownIndex;

    public ItemsList ItemsList
    {
      get { return _itemsList; }
    }

    public ItemsList ItemsToRemoveList
    {
      get { return _itemsToRemoveList; }
    }

    public AbstractProperty ItemsToRemoveValidProperty
    {
      get { return _itemsToRemoveValidProperty; }
    }

    public bool ItemsToRemoveValid
    {
      get { return (bool)_itemsToRemoveValidProperty.GetValue(); }
      set { _itemsToRemoveValidProperty.SetValue(value); }
    }

    public abstract void UpdateItems();
    protected abstract void UpdateItemsToRemove();

    public void MoveItem(T item, int count)
    {
      int index = _items.IndexOf(item);
      if (index < 0)
        return;
      int newIndex = index + count;
      if (newIndex < 0 || newIndex >= _items.Count)
        return;
      CollectionUtils.Swap(_items, index, newIndex);
      bool isUp = count < 0;
      _lastUpIndex = isUp ? newIndex : -1;
      _lastDownIndex = isUp ? -1 : newIndex;
      UpdateItems();
    }

    public void BeginRemoveItems()
    {
      ItemsToRemoveValid = false;
      _itemsToRemove = new List<T>();
      UpdateItemsToRemove();
    }

    public virtual void RemoveSelectedItems()
    {
      foreach (ListItem item in _itemsToRemoveList.Where(i => i.Selected))
        _items.Remove((T)item.AdditionalProperties[KEY_ITEM_TO_REMOVE]);
      UpdateItems();
      ServiceRegistration.Get<IWorkflowManager>().NavigatePop(1);
    }

    protected void ItemToRemoveSelectionChanged(AbstractProperty property, object oldValue)
    {
      ItemsToRemoveValid = _itemsToRemoveList.Any(i => i.Selected);
    }
  }
}