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
using System.Collections.Generic;
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.Commands;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UiComponents.Media.Views;
using MediaPortal.UiComponents.Media.FilterCriteria;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.UiComponents.Media.Models.Navigation;
using MediaPortal.Utilities;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.MediaManagement;

namespace MediaPortal.UiComponents.Media.Models.ScreenData
{
  public abstract class AbstractFiltersScreenData<T> : AbstractScreenData where T : FilterItem, new()
  {
    protected MLFilterCriterion _filterCriterion;
    protected string _navbarSubViewNavigationDisplayLabel;
    protected IFilter _clusterFilter = null;
    protected bool _sortable = false;

    // Variables to be synchronized for multithreading access
    protected bool _buildingList = false;
    protected bool _listDirty = false;

    // Change tracking of FilterItems
    protected AsynchronousMessageQueue _messageQueue;

    /// <summary>
    /// Creates a new instance of <see cref="AbstractFiltersScreenData&lt;T&gt;"/>.
    /// </summary>
    /// <param name="screen">The screen associated with this screen data.</param>
    /// <param name="menuItemLabel">Label which will be shown in the menu to switch to this screen data.</param>
    /// <param name="navbarSubViewNavigationDisplayLabel">Display label to be shown in the navbar when we navigate to a sub view.</param>
    /// <param name="filterCriterion">Specifies the filter criterion which provides the filter entries for this screen.</param>
    protected AbstractFiltersScreenData(string screen, string menuItemLabel, string navbarSubViewNavigationDisplayLabel,
        MLFilterCriterion filterCriterion)
      : base(screen, menuItemLabel)
    {
      _navbarSubViewNavigationDisplayLabel = navbarSubViewNavigationDisplayLabel;
      _filterCriterion = filterCriterion;
    }

    public IFilter ClusterFilter
    {
      get { return _clusterFilter; }
      set { _clusterFilter = value; }
    }

    /// <summary>
    /// Creates a new screen data instance for a sub view.
    /// </summary>
    /// <remarks>
    /// Implementation of this method is necessary to handle value groups.
    /// If value groups are presented to the user and he chooses one of them, the new view will be shown by an instance
    /// similar to this one. This method is responsible for creating the screen data instance for that sub view.
    /// </remarks>
    /// <returns>Screen data instance which looks the same as this view.</returns>
    public abstract AbstractFiltersScreenData<T> Derive();

    private void SubscribeToMessages()
    {
      _messageQueue = new AsynchronousMessageQueue(this, new string[]
        {
            ContentDirectoryMessaging.CHANNEL
        });
      _messageQueue.MessageReceived += OnMessageReceived;
      _messageQueue.Start();
    }

    void UnsubscribeFromMessages()
    {
      if (_messageQueue == null)
        return;
      _messageQueue.Shutdown();
      _messageQueue = null;
    }

    private void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == ContentDirectoryMessaging.CHANNEL)
      {
        ContentDirectoryMessaging.MessageType messageType = (ContentDirectoryMessaging.MessageType)message.MessageType;
        switch (messageType)
        {
          case ContentDirectoryMessaging.MessageType.MediaItemChanged:
            MediaItem mediaItem = (MediaItem)message.MessageData[ContentDirectoryMessaging.MEDIA_ITEM];
            ContentDirectoryMessaging.MediaItemChangeType changeType = (ContentDirectoryMessaging.MediaItemChangeType)message.MessageData[ContentDirectoryMessaging.MEDIA_ITEM_CHANGE_TYPE];
            UpdateLoadedMediaItems(mediaItem, changeType);
            break;
        }
      }
    }

    protected void UpdateLoadedMediaItems(MediaItem mediaItem, ContentDirectoryMessaging.MediaItemChangeType changeType)
    {
      if (changeType == ContentDirectoryMessaging.MediaItemChangeType.None)
        return;

      lock (_syncObj)
      {
        if (changeType == ContentDirectoryMessaging.MediaItemChangeType.Updated)
        {
          PlayableContainerMediaItem existingItem = _items.OfType<PlayableContainerMediaItem>().FirstOrDefault(pcm => pcm.MediaItem.Equals(mediaItem));
          if (existingItem != null)
          {
            existingItem.Update(mediaItem);
          }
        }
      }
    }

    public override void Reload()
    {
      lock (_syncObj)
        ReloadFilterValuesList(false);
    }

    public override void UpdateItems()
    {
      SortFilterValuesList();
    }

    public override void CreateScreenData(NavigationData navigationData)
    {
      base.CreateScreenData(navigationData);
      ReloadFilterValuesList(true);
      SubscribeToMessages();
    }

    public override void ReleaseScreenData()
    {
      base.ReleaseScreenData();
      UnsubscribeFromMessages();
    }

    /// <summary>
    /// Updates the GUI data for a filter values selection screen which reflects the available filter values for
    /// the current base view specification of our <see cref="AbstractScreenData._navigationData"/>.
    /// </summary>
    protected void ReloadFilterValuesList(bool createNewList)
    {
      MediaLibraryQueryViewSpecification currentVS = _navigationData.BaseViewSpecification as MediaLibraryQueryViewSpecification;
      if (currentVS == null)
      { // Should never happen
        ServiceRegistration.Get<ILogger>().Error("FilterScreenData: Wrong type of media library view '{0}'", _navigationData.BaseViewSpecification);
        return;
      }
      // Control other threads reentering this method
      lock (_syncObj)
      {
        if (_buildingList)
        { // Another thread is already building the items list - mark it as dirty and let the other thread
          // rebuild it.
          _listDirty = true;
          return;
        }
        // Mark the list as being built
        _buildingList = true;
        _listDirty = false;
      }
      try
      {
        ItemsList items;
        if (createNewList)
          items = new ItemsList();
        else
        {
          items = _items;
          items.Clear();
        }

        try
        {
          Display_ListBeingBuilt();
          bool grouping = true;
          //If currentVS is the base view it's possible that it has a filter that is incompatible with _filterCriterion.
          //This is the case if a plugin has added a base filter to exclude certain items, e.g. TV excludes recordings
          //and the new filter filters by a different media type, e.g. series'. Ignore the base filter in this case.
          IFilter currentFilter = currentVS.CanCombineFilters(_itemMias) ? currentVS.Filter : null;
          ICollection<FilterValue> fv = _clusterFilter == null ?
              _filterCriterion.GroupValues(currentVS.NecessaryMIATypeIds, _clusterFilter, currentFilter) : null;
          
          if (fv == null || fv.Count <= Consts.MAX_NUM_ITEMS_VISIBLE)
          {
            fv = _filterCriterion.GetAvailableValues(currentVS.NecessaryMIATypeIds, _clusterFilter, currentFilter);
            grouping = false;
          }
          if (fv.Count > Consts.MAX_NUM_ITEMS_VISIBLE)
            Display_TooManyItems(fv.Count);
          else
          {
            bool dirty;
            lock (_syncObj)
              dirty = _listDirty;
            if (dirty)
            {
              UpdateOrRebuildView(items, createNewList);
              return;
            }

            _sortable = true;
            int totalNumItems = 0;
            List<FilterItem> itemsList = new List<FilterItem>();
            // Build collection of available (filter/display) screens which will remain in the next view - that is all currently
            // available screens without the screen which equals this current screen. But we cannot simply remove "this"
            // from the collection, because "this" could be a derived screen (in case our base screen showed groups).
            // So we need an equality criterion when the screen to be removed is equal to this screen in terms of its
            // filter criterion. But with the given data, we actually cannot derive that equality.
            // So we simply use the MenuItemLabel, which should be the same in this and the base screen of the same filter.
            foreach (FilterValue filterValue in fv)
            {
              _sortable &= filterValue.Item != null;
              string filterTitle = filterValue.Title;
              IFilter selectAttributeFilter = filterValue.SelectAttributeFilter;
              MediaLibraryQueryViewSpecification subVS = currentVS.CreateSubViewSpecification(filterTitle, filterValue.Filter, _itemMias);
              T filterValueItem = new T
              {
                // Support non-playable MediaItems (i.e. Series, Seasons)
                MediaItem = filterValue.Item,
                SimpleTitle = filterTitle,
                NumItems = filterValue.NumItems,
                Id = filterValue.Id,
                Command = grouping ? 
                  new MethodDelegateCommand(() => NavigateToGroup(subVS, selectAttributeFilter)) :
                  new MethodDelegateCommand(() => NavigateToSubView(subVS))
              };
              itemsList.Add(filterValueItem);
              if (filterValue.NumItems.HasValue)
                totalNumItems += filterValue.NumItems.Value;
            }
            if (_sortable)
            {
              Sorting.Sorting sorting = CurrentSorting;
              if (sorting != null)
                itemsList.Sort((i1, i2) => sorting.Compare(i1.MediaItem, i2.MediaItem));
            }
            CollectionUtils.AddAll(items, itemsList);
            Display_Normal(items.Count, totalNumItems == 0 ? new int?() : totalNumItems);
          }
        }
        catch (Exception e)
        {
          ServiceRegistration.Get<ILogger>().Warn("AbstractFiltersScreenData: Error creating filter values list", e);
          Display_ItemsInvalid();
        }
        UpdateOrRebuildView(items, createNewList);
      }
      finally
      {
        lock (_syncObj)
          _buildingList = false;
      }
    }

    protected void SortFilterValuesList()
    {
      Sorting.Sorting sorting = CurrentSorting;
      if (sorting == null)
        return;

      lock (_syncObj)
      {
        if (_buildingList)
        { // Another thread is already building the items list - mark it as dirty and let the other thread
          // rebuild it.
          _listDirty = true;
          return;
        }
        if (!_sortable)
          return;
        // Mark the list as being built
        _buildingList = true;
        _listDirty = false;
      }
      try
      {

        ItemsList items = _items;
        List<FilterItem> itemsList = items.Select(li => li as FilterItem).ToList();
        itemsList.Sort((i1, i2) => sorting.Compare(i1.MediaItem, i2.MediaItem));

        bool dirty;
        lock (_syncObj)
          dirty = _listDirty;
        if (dirty)
        {
          UpdateOrRebuildView(items, false);
          return;
        }
        items.Clear();
        CollectionUtils.AddAll(items, itemsList);
        UpdateOrRebuildView(items, false);
      }
      finally
      {
        lock (_syncObj)
          _buildingList = false;
      }
    }

    protected void UpdateOrRebuildView(ItemsList items, bool createNewList)
    {
      if (_listDirty)
      {
        lock (_syncObj)
          _buildingList = false;
        ReloadFilterValuesList(createNewList);
      }
      else
      {
        _items = items;
        _items.FireChange();
      }
    }

    /// <summary>
    /// Constructs the display label for navigation bar. Derived classes can override the way how to construct it.
    /// </summary>
    /// <param name="subViewSpecification">ViewSpecification.</param>
    /// <returns>Display label.</returns>
    protected virtual string GetNavbarDisplayLabel(ViewSpecification subViewSpecification)
    {
      return LocalizationHelper.Translate(_navbarSubViewNavigationDisplayLabel, LocalizationHelper.Translate(subViewSpecification.ViewDisplayName));
    }

    protected void NavigateToGroup(ViewSpecification subViewSpecification, IFilter clusterFilter)
    {
      AbstractFiltersScreenData<T> childScreenData = Derive();
      childScreenData.ClusterFilter = clusterFilter; // We already showed the clusters in the current screen - avoid clusters again else we would present the same grouped screen contents again
      _navigationData.StackSubordinateNavigationContext(subViewSpecification, childScreenData, GetNavbarDisplayLabel(subViewSpecification));
    }

    protected void NavigateToSubView(ViewSpecification subViewSpecification)
    {
      _navigationData.StackAutonomousNavigationContext(subViewSpecification, MenuItemLabel, GetNavbarDisplayLabel(subViewSpecification));
    }
  }
}
