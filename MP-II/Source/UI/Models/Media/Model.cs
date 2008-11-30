#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using MediaPortal.Core;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Media.ClientMediaManager;
using MediaPortal.Media.ClientMediaManager.Views;
using MediaPortal.Presentation.DataObjects;
using MediaPortal.Presentation.MenuManager;

namespace Models.Media
{
  /// <summary>
  /// Model which holds the GUI state for the current navigation in the media views.
  /// </summary>
  public class Model
  {
    #region Protected fields

    protected ItemsList _sortMenu;
    protected readonly ItemsList _items; // Only one items list allowed as the UI databinds to it.
    protected ViewMetadata _currentView;
    protected NavigationItem _navigateParentItem;

    #endregion

    public Model()
    {
      _items = new ItemsList();
      _currentView = ServiceScope.Get<MediaManager>().RootView;
      ReloadItems();
    }

    // FIXME: Main menu management will be moved out of models
    public ItemsList MainMenu
    {
      get
      {
        IMenuCollection menuCollect = ServiceScope.Get<IMenuCollection>();
        return MenuHelper.WrapMenu(menuCollect.GetMenu("mymedia-main"));
      }
    }

    /// <summary>
    /// Provides a list with the sub views and media items of the current view.
    /// Note: This <see cref="Items"/> list doesn't contain an item to navigate to the parent view.
    /// It is job of the skin to provide a means to navigate to the parent view.
    /// </summary>
    public ItemsList Items
    {
      get { return _items; }
    }

    /// <summary>
    /// Provides a <see cref="NavigationItem"/> to the GUI which denotes the parent view of the current
    /// view.
    /// </summary>
    public NavigationItem NavigatableParentItem
    {
      get { return _navigateParentItem; }
    }

    /// <summary>
    /// Provides the data to the view currently shown.
    /// </summary>
    public ViewMetadata CurrentView
    {
      get { return _currentView; }
    }

    /// <summary>
    /// Provides a callable method for the skin to select an item.
    /// Depending on the item type, we will navigate to the choosen view or play the choosen item.
    /// </summary>
    /// <param name="item">The choosen item. This item should be either <see cref="NavigatableParentItem"/> or
    /// one of the items in the <see cref="Items"/> list.</param>
    public void Select(ListItem item)
    {
      if (item == null)
        return;
      NavigationItem navigationItem = item as NavigationItem;
      if (navigationItem != null)
      {
        NavigateToView(navigationItem.ViewId);
        return;
      }
      PlayableItem playableItem = item as PlayableItem;
      if (playableItem != null)
      {
        PlayItem(playableItem.MediaItem);
        return;
      }
    }

    #region Protected methods

    /// <summary>
    /// Does the actual work of navigating to the specifield view. This will exchange our
    /// <see cref="CurrentView"/> to the view specified by the given <paramref name="viewId"/>.
    /// </summary>
    /// <param name="viewId">Id of the view to navigate to.</param>
    protected void NavigateToView(Guid viewId)
    {
      MediaManager mediaManager = ServiceScope.Get<MediaManager>();
      _currentView = mediaManager.GetViewMetadata(viewId);
      ReloadItems();
    }

    /// <summary>
    /// Does the actual work of playing a media item.
    /// </summary>
    /// <param name="item">Media item to be played.</param>
    protected static void PlayItem(MediaItem item)
    {
      // TODO: Play item
    }

    protected void ReloadItems()
    {
      _items.Clear();
      MediaManager mediaManager = ServiceScope.Get<MediaManager>();
      _navigateParentItem = _currentView.ParentViewId.HasValue ? new NavigationItem(_currentView.ParentViewId.Value, "..") : null;
      // Note: we don't add the NavigateParentItem to _items - it is the job of the screenfile to
      // provide an item to navigate to the view denoted by NavigateParentItem

      // Add items for sub views
      foreach (Guid subViewId in _currentView.SubViewIds)
        _items.Add(new NavigationItem(subViewId, null));
      View view = mediaManager.GetView(_currentView.ViewId);
      foreach (MediaItem item in view.MediaItems)
        _items.Add(new PlayableItem(item));
      _items.FireChange();
    }

    #endregion

    // TODO: Context menu handling
  }
}
