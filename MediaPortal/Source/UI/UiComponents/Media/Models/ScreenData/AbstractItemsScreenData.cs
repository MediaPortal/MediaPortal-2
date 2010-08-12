#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using MediaPortal.Core;
using MediaPortal.Core.Commands;
using MediaPortal.Core.Localization;
using MediaPortal.Core.Logging;
using MediaPortal.Core.MediaManagement;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Views;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.UiComponents.Media.Models.Navigation;
using MediaPortal.Utilities;

namespace MediaPortal.UiComponents.Media.Models.ScreenData
{
  public abstract class AbstractItemsScreenData : AbstractScreenData
  {
    public delegate PlayableItem PlayableItemCreatorDelegate(MediaItem mi);

    protected string _navbarSubViewNavigationDisplayLabel;
    protected bool _presentsBaseView;
    protected PlayableItemCreatorDelegate _playableItemCreator;

    protected object _syncObj = new object();
    // Variables to be synchronized for multithreading access
    protected View _view = null;
    protected bool _buildingList = false;
    protected bool _listDirty = false;

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
        PlayableItemCreatorDelegate playableItemCreator, bool presentsBaseView) : base(screen, menuItemLabel)
    {
      _navbarSubViewNavigationDisplayLabel = navbarSubViewNavigationDisplayLabel;
      _playableItemCreator = playableItemCreator;
      _presentsBaseView = presentsBaseView;
    }

    public override void CreateScreenData(NavigationData navigationData)
    {
      base.CreateScreenData(navigationData);
      if (!_presentsBaseView)
        return;
      ReloadMediaItems(navigationData.BaseViewSpecification.BuildView(), true);
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
      ReloadMediaItems(false);
    }

    public void ReloadMediaItems(View view, bool createNewList)
    {
      lock (_syncObj)
        _view = view;
      ReloadMediaItems(createNewList);
    }

    /// <summary>
    /// Updates the GUI data for a media items view screen which reflects the data of the <see cref="CurrentView"/>.
    /// </summary>
    /// <param name="createNewList">If set to <c>true</c>, this method will re-create the
    /// <see cref="AbstractScreenData.Items"/> list, else it will reuse it.</param>
    protected void ReloadMediaItems(bool createNewList)
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
        _buildingList = true;
        _listDirty = false;
        view = _view;
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
                List<ListItem> viewsList = new List<ListItem>();
                foreach (View sv in subViews)
                {
                  NavigationItem item = new NavigationItem(sv, null, sv.AbsNumItems);
                  View subView = sv;
                  item.Command = new MethodDelegateCommand(() => NavigateToView(subView.Specification));
                  viewsList.Add(item);
                }
                viewsList.Sort((v1, v2) => string.Compare(v1[Consts.NAME_KEY], v2[Consts.NAME_KEY]));
                CollectionUtils.AddAll(items, viewsList);

                lock (_syncObj)
                  if (_listDirty)
                    goto RebuildView;

                PlayableItemCreatorDelegate picd = PlayableItemCreator;
                List<ListItem> itemsList = new List<ListItem>();
                foreach (MediaItem childItem in mediaItems)
                {
                  PlayableItem item = picd(childItem);
                  if (item == null)
                    continue;
                  itemsList.Add(item);
                }
                itemsList.Sort((i1, i2) => string.Compare(i1[Consts.NAME_KEY], i2[Consts.NAME_KEY]));
                CollectionUtils.AddAll(items, itemsList);

                Display_Normal(items.Count);
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
        if (_listDirty)
        {
          lock (_syncObj)
            _buildingList = false;
          ReloadMediaItems(createNewList);
        }
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
    protected void NavigateToView(ViewSpecification subViewSpecification)
    {
      _navigationData.StackSubordinateNavigationContext(subViewSpecification, Derive(),
          LocalizationHelper.Translate(_navbarSubViewNavigationDisplayLabel,
              LocalizationHelper.Translate(subViewSpecification.ViewDisplayName)));
    }
  }
}