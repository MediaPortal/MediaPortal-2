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
using System.Collections.Generic;
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.Commands;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.Messaging;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UiComponents.Media.Views;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.UiComponents.Media.Models.Navigation;
using MediaPortal.Utilities;

namespace MediaPortal.UiComponents.Media.Models.ScreenData
{
  public abstract class AbstractItemsScreenData : AbstractScreenData
  {
    public delegate PlayableMediaItem PlayableItemCreatorDelegate(MediaItem mi);

    protected string _navbarSubViewNavigationDisplayLabel;
    protected bool _presentsBaseView;
    protected PlayableItemCreatorDelegate _playableItemCreator;

    // Variables to be synchronized for multithreading access
    protected View _view = null;
    protected bool _buildingList = false;
    protected bool _listDirty = false;
    protected IViewChangeNotificator _viewChangeNotificator = null;

    // Change tracking of MediaItems
    protected AsynchronousMessageQueue _messageQueue;
    protected int? _currentTotalNumItems;

    /// <summary>
    /// Creates a new instance of <see cref="AbstractItemsScreenData"/>.
    /// </summary>
    /// <param name="screen">The screen associated with this screen data.</param>
    /// <param name="menuItemLabel">Laben which will be shown in the menu to switch to this screen data.</param>
    /// <param name="navbarSubViewNavigationDisplayLabel">Display label to be shown in the navbar when we
    /// navigate to a sub view.</param>
    /// <param name="playableItemCreator">Delegate which will be called for a media item when the user chooses it.</param>
    /// <param name="presentsBaseView">If set to <c>true</c>, this items screen presents the base view (given by
    /// the <see cref="NavigationData.BaseViewSpecification"/>) and automatically creates the items. If set to
    /// <c>false</c>, the items have to be created by a sub class in method <see cref="CreateScreenData"/>.</param>
    protected AbstractItemsScreenData(string screen, string menuItemLabel, string navbarSubViewNavigationDisplayLabel,
        PlayableItemCreatorDelegate playableItemCreator, bool presentsBaseView)
      : base(screen, menuItemLabel)
    {
      _navbarSubViewNavigationDisplayLabel = navbarSubViewNavigationDisplayLabel;
      _playableItemCreator = playableItemCreator;
      _presentsBaseView = presentsBaseView;
    }

    private void SubscribeToMessages()
    {
      _messageQueue = new AsynchronousMessageQueue(this, new string[]
        {
            ContentDirectoryMessaging.CHANNEL,
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
          case ContentDirectoryMessaging.MessageType.ShareImportCompleted:
            Reload();
            break;
        }
      }
    }

    public override void CreateScreenData(NavigationData navigationData)
    {
      base.CreateScreenData(navigationData);
      if (!_presentsBaseView)
        return;
      ReloadMediaItems(navigationData.BaseViewSpecification.BuildView(), true);
      SubscribeToMessages();
    }

    public override void ReleaseScreenData()
    {
      base.ReleaseScreenData();
      UninstallViewChangeNotificator();
      UnsubscribeFromMessages();
    }

    public View CurrentView
    {
      get
      {
        lock (_syncObj)
          return _view;
      }
    }

    /// <summary>
    /// Delegate function to be used to wrap a playable media item should into a ListItem.
    /// This property will be inherited from predecessor navigation contexts.
    /// </summary>
    public PlayableItemCreatorDelegate PlayableItemCreator
    {
      get { return _playableItemCreator; }
    }

    /// <summary>
    /// Creates a new screen data instance for a sub view.
    /// </summary>
    /// <remarks>
    /// Implementation of this method is necessary to handle sub views of our <see cref="CurrentView"/>.
    /// If a sub view is returned by our view, choosing that sub view navigates to a view which is similar to this one but
    /// with an exchanged <see cref="CurrentView"/>. This method is responsible for creating the screen data instance
    /// for that sub view.
    /// </remarks>
    /// <returns>Screen data instance which looks the same as this view.</returns>
    public abstract AbstractItemsScreenData Derive();

    public override void Reload()
    {
      lock (_syncObj)
        if (_view != null)
          _view.Invalidate();
      UpdateMediaItems(false);
    }

    public override void UpdateItems()
    {
      UpdateMediaItems(false);
    }

    public void ReloadMediaItems(View view, bool createNewList)
    {
      lock (_syncObj)
        _view = view;
      UpdateMediaItems(createNewList);
    }

    private void ViewChanged()
    {
      Reload();
    }

    protected void InstallViewChangeNotificator(IViewChangeNotificator viewChangeNotificator)
    {
      if (_viewChangeNotificator != null)
        UninstallViewChangeNotificator();
      _viewChangeNotificator = viewChangeNotificator;
      if (_viewChangeNotificator == null)
        return;
      _viewChangeNotificator.Changed += ViewChanged;
      _viewChangeNotificator.Install();
    }

    protected void UninstallViewChangeNotificator()
    {
      if (_viewChangeNotificator == null)
        return;
      _viewChangeNotificator.Changed -= ViewChanged;
      _viewChangeNotificator.Dispose();
      _viewChangeNotificator = null;
    }

    protected void UpdateLoadedMediaItems(MediaItem mediaItem, ContentDirectoryMessaging.MediaItemChangeType changeType)
    {
      if (changeType == ContentDirectoryMessaging.MediaItemChangeType.None)
        return;

      bool changed = false;
      lock (_syncObj)
      {
        if (changeType == ContentDirectoryMessaging.MediaItemChangeType.Deleted)
        {
          PlayableMediaItem existingItem = _items.OfType<PlayableMediaItem>().FirstOrDefault(pmi => pmi.MediaItem.Equals(mediaItem));
          if (existingItem != null)
          {
            int oldIndex = _items.IndexOf(existingItem);
            _items.Remove(existingItem);

            // Restore focus on same position of old item
            SetSelectedIndex(oldIndex);

            _currentTotalNumItems--;
            changed = true;
          }
        }
        if (changeType == ContentDirectoryMessaging.MediaItemChangeType.Updated)
        {
          IEnumerable<PlayableMediaItem> playableItems = _items.OfType<PlayableMediaItem>();
          PlayableMediaItem existingItem = playableItems.FirstOrDefault(pmi => pmi.MediaItem.Equals(mediaItem));
          if (existingItem != null)
          {
            existingItem.Update(mediaItem);
            changed = SetSelectedItem(playableItems);
          }
        }
      }
      if (changed)
      {
        _items.FireChange();
        Display_Normal(_items.Count, _currentTotalNumItems);
      }
    }

    /// <summary>
    /// Sets the "Selected" indicator to the MediaItem which is in the position of the last index. If there are less items in collection than the index, the last item is selected.
    /// </summary>
    /// <param name="oldIndex">Old item index</param>
    protected void SetSelectedIndex(int oldIndex)
    {
      if (oldIndex < 0 || _items.Count == 0)
        return;

      if (oldIndex >= _items.Count)
        oldIndex = _items.Count - 1;

      for (int i = 0; i < _items.Count; i++)
        _items[i].Selected = i == oldIndex;
    }

    /// <summary>
    /// Can be overriden in derived classes to set the initially selected item.
    /// </summary>
    /// <param name="items">Enumeration of items to select from.</param>
    protected virtual bool SetSelectedItem(IEnumerable<PlayableMediaItem> items)
    {
      return false;
    }

    /// <summary>
    /// Updates the GUI data for a media items view screen which reflects the data of the <see cref="CurrentView"/>.
    /// </summary>
    /// <param name="createNewList">If set to <c>true</c>, this method will re-create the
    /// <see cref="AbstractScreenData.Items"/> list, else it will reuse it.</param>
    protected void UpdateMediaItems(bool createNewList)
    {
      View view;
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
        view = _view;
        InstallViewChangeNotificator(_view.CreateViewChangeNotificator());
        _buildingList = true;
        _listDirty = false;
      }
      try
      {
        Display_ListBeingBuilt();
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
          // TODO: Add the items in a separate job while the UI already shows the new screen
          if (view.IsValid)
          {
            // Add items for sub views
            IList<View> subViews = view.SubViews;
            IList<MediaItem> mediaItems = view.MediaItems;
            lock (_syncObj)
              if (_listDirty)
                goto RebuildView;
            if (subViews == null || mediaItems == null)
              Display_ItemsInvalid();
            else
            {
              if (subViews.Count + mediaItems.Count > Consts.MAX_NUM_ITEMS_VISIBLE)
                Display_TooManyItems(subViews.Count + mediaItems.Count);
              else
              {
                int totalNumItems = 0;

                bool subViewsPreSorted = false;
                List<NavigationItem> viewsList = new List<NavigationItem>();
                foreach (View sv in subViews)
                {
                  if (sv.Specification.SortedSubViews)
                    subViewsPreSorted = true;
                  ViewItem item = new ViewItem(sv, null, sv.AbsNumItems);
                  View subView = sv;
                  item.Command = new MethodDelegateCommand(() => NavigateToView(subView.Specification));
                  viewsList.Add(item);
                  if (sv.AbsNumItems.HasValue)
                    totalNumItems += sv.AbsNumItems.Value;
                }
                // Morpheus_xx, 2014-05-03: Only sort the subviews here, if they are not pre-sorted by the ViewSpecification
                if (!subViewsPreSorted)
                  viewsList.Sort((v1, v2) => string.Compare(v1.SortString, v2.SortString));
                CollectionUtils.AddAll(items, viewsList);

                lock (_syncObj)
                  if (_listDirty)
                    goto RebuildView;

                PlayableItemCreatorDelegate picd = PlayableItemCreator;
                List<PlayableMediaItem> itemsList = mediaItems.Select(childItem => picd(childItem)).Where(item => item != null).ToList();
                Sorting.Sorting sorting = CurrentSorting;
                if (sorting != null)
                  itemsList.Sort((i1, i2) => sorting.Compare(i1.MediaItem, i2.MediaItem));
                else
                  // Default sorting: Use SortString
                  itemsList.Sort((i1, i2) => string.Compare(i1.SortString, i2.SortString));

                // Derived classes can implement special initial selection handling here,
                // e.g. the first unwatched episode could be selected from a list of episodes
                SetSelectedItem(itemsList);
                CollectionUtils.AddAll(items, itemsList);

                // Support custom sorting logic by view specification. At this time it can work on both MediaItems and SubViews.
                if (view.Specification.CustomItemsListSorting != null)
                  view.Specification.CustomItemsListSorting(items, sorting);

                _currentTotalNumItems = totalNumItems == 0 ? new int?() : totalNumItems;
                Display_Normal(items.Count, _currentTotalNumItems);
              }
            }
          }
          else
            Display_ItemsInvalid();
        }
        catch (Exception e)
        {
          ServiceRegistration.Get<ILogger>().Warn("AbstractItemsScreenData: Error creating items list", e);
          Display_ItemsInvalid();
        }
        RebuildView:
        bool dirty;
        lock (_syncObj)
          if (_listDirty)
          {
            dirty = true;
            _buildingList = false;
          }
          else
            dirty = false;
        if (dirty)
          UpdateMediaItems(createNewList);
        else
        {
          _items = items;
          _items.FireChange();
        }
      }
      finally
      {
        lock (_syncObj)
          _buildingList = false;
      }
    }

    /// <summary>
    /// Does the actual work of navigating to the specifield sub view. This will create a new <see cref="NavigationData"/>
    /// instance for the new screen and push a new transient workflow state onto the workflow navigation stack.
    /// </summary>
    /// <param name="subViewSpecification">Specification of the sub view to navigate to.</param>
    protected internal NavigationData NavigateToView(ViewSpecification subViewSpecification)
    {
      return _navigationData.StackSubordinateNavigationContext(subViewSpecification, Derive(),
          LocalizationHelper.Translate(_navbarSubViewNavigationDisplayLabel,
              LocalizationHelper.Translate(subViewSpecification.ViewDisplayName)));
    }
  }
}
