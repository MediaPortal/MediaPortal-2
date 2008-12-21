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
using System.Collections.Generic;
using MediaPortal.Core;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Media.ClientMediaManager;
using MediaPortal.Media.ClientMediaManager.Views;
using MediaPortal.Presentation.DataObjects;
using MediaPortal.Presentation.Models;
using MediaPortal.Presentation.Workflow;

namespace Models.Media
{
  /// <summary>
  /// Model which holds the GUI state for the current navigation in the media views.
  /// </summary>
  public class MediaModel : IWorkflowModel
  {
    public const string MEDIA_MODEL_ID_STR = "4CDD601F-E280-43b9-AD0A-6D7B2403C856";
    #region Protected fields

    protected ItemsList _sortMenu;
    protected readonly ItemsList _items; // Only one items list allowed as the UI databinds to it.
    protected Property _currentViewProperty;
    protected Property _navigatableParentItemProperty;
    protected Property _hasParentDirectoryProperty;

    #endregion

    public MediaModel()
    {
      _items = new ItemsList();
      _currentViewProperty = new Property(typeof(View), ServiceScope.Get<MediaManager>().RootView);
      _navigatableParentItemProperty = new Property(typeof(NavigationItem), null);
      _hasParentDirectoryProperty = new Property(typeof(bool), false);
      ReloadItems();
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
      get { return (NavigationItem) _navigatableParentItemProperty.GetValue(); }
      set { _navigatableParentItemProperty.SetValue(value); }
    }

    public Property NavigatableParentItemProperty
    {
      get { return _navigatableParentItemProperty; }
    }

    /// <summary>
    /// Gets the information whether the current view has a navigatable parent view. In this case, the
    /// property <see cref="NavigatableParentItem"/> will contain the navigation item to the parent
    /// view. Else, <see cref="NavigatableParentItem"/> will be <c>null</c>.
    /// </summary>
    public bool HasParentDirectory
    {
      get { return (bool) _hasParentDirectoryProperty.GetValue(); }
      set { _hasParentDirectoryProperty.SetValue(value); }
    }

    public Property HasParentDirectoryProperty
    {
      get { return _hasParentDirectoryProperty; }
    }

    /// <summary>
    /// Provides the data of the view currently shown.
    /// </summary>
    public View CurrentView
    {
      get { return (View) _currentViewProperty.GetValue(); }
      set { _currentViewProperty.SetValue(value); }
    }

    public Property CurrentViewProperty
    {
      get { return _currentViewProperty; }
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
        NavigateToView(navigationItem.View);
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
    /// <see cref="CurrentView"/> to the specified <paramref name="view"/>.
    /// </summary>
    /// <param name="view">View to navigate to.</param>
    protected void NavigateToView(View view)
    {
      CurrentView = view;
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
      View currentView = CurrentView;
      NavigatableParentItem = currentView.ParentView == null ? null : new NavigationItem(currentView.ParentView, "..");
      HasParentDirectory = currentView.ParentView != null;
      // Note: we don't add the NavigateParentItem to _items - it is the job of the screenfile to
      // provide an item to navigate to the view denoted by NavigateParentItem

      // Add items for sub views
      foreach (View subView in currentView.SubViews)
        _items.Add(new NavigationItem(subView, null));
      foreach (MediaItem item in currentView.MediaItems)
        _items.Add(new PlayableItem(item));
      _items.FireChange();
    }

    #endregion

    #region IWorkflowModel implementation

    public Guid ModelId
    {
      get { return new Guid(MEDIA_MODEL_ID_STR); }
    }

    public void StartModelContext(NavigationContext context)
    {
      // We could initialize some data here when entering the media navigation state
    }

    public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
    }

    public void UpdateMenuActions(NavigationContext context, ICollection<WorkflowStateAction> actions)
    {
    }

    public void UpdateContextMenuActions(NavigationContext context, ICollection<WorkflowStateAction> actions)
    {
    }

    #endregion
  }
}
