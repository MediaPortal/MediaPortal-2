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

using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UiComponents.Media.Models;
using System.Collections.Generic;

namespace MediaPortal.UiComponents.Nereus.Models.HomeContent
{
  public abstract class AbstractHomeContent
  {
    protected IList<ItemsListWrapper> _backingList = new List<ItemsListWrapper>();
    protected ItemsList _items = new ItemsList();
    protected bool _isInit = false;

    protected abstract void PopulateBackingList();

    protected void Init()
    {
      if (_isInit)
        return;
      _isInit = true;

      // Overidden in derived classes
      PopulateBackingList();

      AttachToBackingLists();
      UpdateAvailableItems();
    }

    protected void AttachToBackingLists()
    {
      foreach (ItemsListWrapper wrapper in _backingList)
        wrapper.HasItemsProperty.Attach(OnHasItemsChanged);
    }

    protected void DetachToBackingLists()
    {
      foreach (ItemsListWrapper wrapper in _backingList)
        wrapper.HasItemsProperty.Detach(OnHasItemsChanged);
    }

    void OnHasItemsChanged(AbstractProperty property, object oldValue)
    {
      UpdateAvailableItems();
    }

    protected void UpdateAvailableItems()
    {
      lock (_items.SyncRoot)
      {
        _items.Clear();
        foreach (ItemsListWrapper wrapper in _backingList)
          if (wrapper.HasItems)
            _items.Add(wrapper);
      }
      _items.FireChange();
    }

    public ItemsList Items
    {
      get
      {
        Init();
        return _items;
      }
    }

    protected static MediaListModel GetMediaListModel()
    {
      return (MediaListModel)ServiceRegistration.Get<IWorkflowManager>().GetModel(MediaListModel.MEDIA_LIST_MODEL_ID);
    }
  }
}
