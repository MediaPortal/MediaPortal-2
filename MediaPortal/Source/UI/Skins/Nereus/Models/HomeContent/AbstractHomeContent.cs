#region Copyright (C) 2007-2021 Team MediaPortal

/*
    Copyright (C) 2007-2021 Team MediaPortal
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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MediaPortal.UiComponents.Nereus.Models.HomeContent
{
  /// <summary>
  /// Base class for home content that shows a list of <see cref="ItemsListWrapper"/>s that should be
  /// automatically shown and hidden on changes to the <see cref="ItemsListWrapper.HasItems"/> property.
  /// </summary>
  public abstract class AbstractHomeContent
  {
    private readonly object _syncObj = new object();

    /// <summary>
    /// Shortcut lists that are always displayed as the first item in the home content, should be populated in derived classes.
    /// </summary>
    protected IList<ItemsListWrapper> _shortcutLists = new List<ItemsListWrapper>();

    /// <summary>
    /// All MediaItemsListWrappers that this home content can display, should be populated by derived classes.
    /// The default order and lists to display will be the same as this list, but may be overridden by a call to <see cref="UpdateListsToDisplay(IEnumerable{string})"/>.
    /// </summary>
    protected IList<MediaListItemsListWrapper> _availableMediaLists = new List<MediaListItemsListWrapper>();

    /// <summary>
    /// Specifies the available media lists to display and their order.
    /// </summary>
    protected IList<string> _mediaListsToDisplay = null;

    // Contains the configured and ordered list wrappers
    protected IList<ItemsListWrapper> _backingList = new List<ItemsListWrapper>();

    // All ItemsListWrappers that currently have items
    protected ItemsList _availableItems = new ItemsList();

    protected bool _isInit = false;

    public ItemsList Items
    {
      get
      {
        Init();
        return _availableItems;
      }
    }

    public void UpdateListsToDisplay(IEnumerable<string> listKeys)
    {
      lock (_syncObj)
      {
        bool updated = TryUpdateMediaListsToDisplay(listKeys);
        // Only update the lists to display if this home content has already been initialized,
        // else they will be updated when init is called later
        if (!updated || !_isInit)
          return;
        RecreateBackingList();
      }
      // Outside lock as it fires events
      UpdateAvailableItems();
    }

    public void ForceUpdateList()
    {
      IContentListModel model = GetMediaListModel();
      if (model == null)
        return;

      List<string> listsToUpdate;
      lock (_syncObj)
      {
        if (!_isInit)
          return;
        listsToUpdate = new List<string>(_mediaListsToDisplay);
      }

      foreach (string listToUpdate in listsToUpdate)
        if (!string.IsNullOrEmpty(listToUpdate))
          model.ForceUpdate(listToUpdate);
    }

    public IList<MediaListItemsListWrapper> Lists
    {
      get
      {
        return _availableMediaLists;
      }
    }

    // Lazily called by the Items property getter,
    // usually by the screen showing this content.
    protected void Init()
    {
      lock (_syncObj)
      {
        if (_isInit)
          return;
        _isInit = true;

        RecreateBackingList();
      }

      // In some situations the backing list will stay hidden if initially being empty and then 
      // almost immediately after being filled with items.
      // TODO: This delay seems to fix it but should be removed when a better solution is found.
      Task.Delay(500).ContinueWith(t => UpdateAvailableItems());
    }

    /// <summary>
    /// Updates the media lists to display and the order to display them. If this returns <c>true</c>
    /// then <see cref="RecreateBackingList"/> should be called to actually update the backing list.
    /// </summary>
    /// <param name="mediaListKeys">Enumeration of media list keys that should be displayed.</param>
    /// <returns><c>true</c> if the lists to display was changed; else <c>false</c>.</returns>
    protected bool TryUpdateMediaListsToDisplay(IEnumerable<string> mediaListKeys)
    {
      if (mediaListKeys == null)
        return false;
      if (_mediaListsToDisplay != null && _mediaListsToDisplay.SequenceEqual(mediaListKeys))
        return false;
      _mediaListsToDisplay = new List<string>(mediaListKeys);
      return true;        
    }

    /// <summary>
    /// Recreates the backing list, including and ordering the lists as per the media list keys in <see cref="_mediaListsToDisplay"/>.
    /// </summary>
    protected void RecreateBackingList()
    {
      if (_mediaListsToDisplay == null)
        _mediaListsToDisplay = _availableMediaLists.Select(l => l.MediaListKey).ToList();

      IList<MediaListItemsListWrapper> currentMediaLists = _backingList.OfType<MediaListItemsListWrapper>().ToList();
      if (_mediaListsToDisplay.SequenceEqual(currentMediaLists.Select(l => l.MediaListKey)))
        return;

      //Remove all lists and add them in the right order
      DetachItemsListWrappers();
      foreach (ItemsListWrapper itemsList in _backingList)
        itemsList.DetachFromItemsList();

      _backingList.Clear();

      // Add any shortcuts first
      foreach (ItemsListWrapper shortcutList in _shortcutLists)
      {
        shortcutList.AttachToItemsList();
        _backingList.Add(shortcutList);
      }

      // Then add media lists in the configured order
      var model = GetContentListModel();
      if (model != null)
      {
        foreach (string mediaListKey in _mediaListsToDisplay.Where(k => model.Lists.ContainsKey(k)))
        {
          MediaListItemsListWrapper mediaList = _availableMediaLists.FirstOrDefault(l => l.MediaListKey == mediaListKey);
          if (mediaList != null)
          {
            if (!mediaList.Initialized)
              mediaList.Initialize(model.Lists[mediaListKey].AllItems);
            else
              mediaList.AttachToItemsList();
            _backingList.Add(mediaList);
          }
        }
      }
      AttachItemsListWrappers();
    }

    protected virtual IContentListModel GetContentListModel()
    {
      return null;
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

    private void OnHasItemsChanged(AbstractProperty property, object oldValue)
    {
      UpdateAvailableItems();
    }

    /// <summary>
    /// Adds all <see cref="ItemsListWrapper"/>s that
    /// have items to the list of available items.
    /// </summary>
    protected void UpdateAvailableItems()
    {
      // We need a double lock here, _availableItems.SyncRoot ensures that the SkinEngine isn't currently accessing
      // _availableItems and _syncObj ensures that _backingList doesn't change. This is potentially a risk for
      // deadlocks, however _syncObj is private so callers into this class can only potentially be holding
      // _availableItems.SyncRoot, so this should be safe as long as _syncObj is only ever held after _availableItems.SyncRoot. 
      lock (_availableItems.SyncRoot)
        lock (_syncObj)
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
