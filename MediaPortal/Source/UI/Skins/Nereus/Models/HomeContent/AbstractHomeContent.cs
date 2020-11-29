#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UiComponents.Media.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MediaPortal.UiComponents.Nereus.Models.HomeContent
{
  /// <summary>
  /// Base class for home content that shows a list of <see cref="ItemsListWrapper"/>s that should be
  /// automatically shown and hidden on changes to the <see cref="ItemsListWrapper.HasItems"/> property.
  /// </summary>
  public abstract class AbstractHomeContent
  {
    // All known ItemsListWrappers
    protected IList<ItemsListWrapper> _backingList = new List<ItemsListWrapper>();

    // All ItemsListWrappers that currently have items
    protected ItemsList _availableItems = new ItemsList();

    protected bool _isInit = false;
    protected bool _listNeedsUpdate = false;

    public ItemsList Items
    {
      get
      {
        Init();
        return _availableItems;
      }
    }

    public bool IsInited => _isInit;

    public bool ListNeedsUpdate
    {
      get => _listNeedsUpdate;
      set
      {
        if (_isInit)
          _listNeedsUpdate = value;
      }
    }

    protected void PopulateList()
    {
      PopulateBackingList();
      ListNeedsUpdate = false;
    }

    public void ForceUpdateList()
    {
      if (ListNeedsUpdate)
        ForceUpdateBackingList();
      ListNeedsUpdate = false;
    }


    /// <summary>
    /// Implementations of this method should populate <see cref="_backingList"/>
    /// with the <see cref="ItemsListWrapper"/>s to show.
    /// </summary>
    protected abstract void PopulateBackingList();

    /// <summary>
    /// Implementations of this method should force a refresh of the <see cref="_backingList"/>
    /// with the <see cref="ItemsListWrapper"/>s to show.
    /// </summary>
    protected abstract void ForceUpdateBackingList();

    // Lazily called by the Items property getter,
    // usually by the screen showing this content.
    protected async void Init()
    {
      if (_isInit)
        return;
      _isInit = true;

      PopulateList();
      AttachItemsListWrappers();

      // In some situations the backing list will stay hidden if initially being empty and then 
      // almost immediately after being filled with items.
      // TODO: This delay seems to fix it but should be removed when a better solution is found.
      await Task.Delay(500);

      UpdateAvailableItems();
    }

    protected void AttachItemsListWrappers()
    {
      // Attach to each HasItems property so we can add/remove
      // the wrapper from the list of available items if it changes.
      foreach (ItemsListWrapper wrapper in _backingList)
        wrapper.HasItemsProperty.Attach(OnHasItemsChanged);
    }

    protected void DetachItemsListWrappers()
    {
      foreach (ItemsListWrapper wrapper in _backingList)
        wrapper.HasItemsProperty.Detach(OnHasItemsChanged);
    }

    void OnHasItemsChanged(AbstractProperty property, object oldValue)
    {
      UpdateAvailableItems();
    }

    /// <summary>
    /// Adds a;; <see cref="ItemsListWrapper"/>s that
    /// have items to the list of available items.
    /// </summary>
    protected void UpdateAvailableItems()
    {
      lock (_availableItems.SyncRoot)
      {
        _availableItems.Clear();
        foreach (ItemsListWrapper wrapper in _backingList)
          if (wrapper.HasItems)
            _availableItems.Add(wrapper);
      }
      _availableItems.FireChange();
    }

    protected static MediaListModel GetMediaListModel()
    {
      return (MediaListModel)ServiceRegistration.Get<IWorkflowManager>().GetModel(MediaListModel.MEDIA_LIST_MODEL_ID);
    }

    protected static IContentListModel GetAppListModel()
    {
      return (IContentListModel)ServiceRegistration.Get<IWorkflowManager>().GetModel(HomeMenuModel.APPS_LIST_MODEL_ID);
    }

    protected static IContentListModel GetOnlineVideosListModel()
    {
      return (IContentListModel)ServiceRegistration.Get<IWorkflowManager>().GetModel(HomeMenuModel.OV_LIST_MODEL_ID);
    }

    protected static IContentListModel GetWebradioListModel()
    {
      return (IContentListModel)ServiceRegistration.Get<IWorkflowManager>().GetModel(HomeMenuModel.WEBRADIO_LIST_MODEL_ID);
    }
  }
}
