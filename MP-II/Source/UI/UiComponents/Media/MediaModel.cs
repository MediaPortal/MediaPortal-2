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

namespace UiComponents.Media
{
  /// <summary>
  /// Model which holds the GUI state for the current navigation in the media views.
  /// </summary>
  public class MediaModel : IWorkflowModel
  {
    public const string MEDIA_MODEL_ID_STR = "4CDD601F-E280-43b9-AD0A-6D7B2403C856";

    public const string MEDIA_MAIN_SCREEN = "media";

    protected const string VIEW_KEY = "MediaModel: VIEW";

    #region Protected fields

    protected readonly ItemsList _items; // Only one items list allowed as the UI databinds to it.
    protected Property _currentViewProperty;
    protected Property _hasParentDirectoryProperty;

    #endregion

    public MediaModel()
    {
      _items = new ItemsList();
      _currentViewProperty = new Property(typeof(View), RootView);
      _hasParentDirectoryProperty = new Property(typeof(bool), false);
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
    /// Gets the information whether the current view has a navigatable parent view.
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

    public View RootView
    {
      get { return ServiceScope.Get<MediaManager>().RootView; }
    }

    /// <summary>
    /// Provides a callable method for the skin to select an item.
    /// Depending on the item type, we will navigate to the choosen view or play the choosen item.
    /// </summary>
    /// <param name="item">The choosen item. This item should be one of the items in the
    /// <see cref="Items"/> list.</param>
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
    /// <see cref="CurrentView"/> to the specified <paramref name="view"/> and push a state onto
    /// the workflow manager's navigation stack.
    /// </summary>
    /// <param name="view">View to navigate to.</param>
    protected void NavigateToView(View view)
    {
      WorkflowState newState = WorkflowState.CreateTransientState(
          "View: " + view.DisplayName, MEDIA_MAIN_SCREEN, true, true);
      IWorkflowManager workflowManager = ServiceScope.Get<IWorkflowManager>();
      IDictionary<string, object> variables = new Dictionary<string, object>();
      variables.Add(VIEW_KEY, view);
      workflowManager.NavigatePushTransient(newState, variables);
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
      HasParentDirectory = currentView.ParentView != null;
      if (currentView.IsValid)
      {
        // Add items for sub views
        foreach (View subView in currentView.SubViews)
          _items.Add(new NavigationItem(subView, null));
        foreach (MediaItem item in currentView.MediaItems)
          _items.Add(new PlayableItem(item));
      }
      _items.FireChange();
    }

    protected View GetViewFromContext(NavigationContext context)
    {
      View view = context.GetContextVariable(VIEW_KEY, true) as View;
      return view ?? RootView;
    }

    #endregion

    #region IWorkflowModel implementation

    public Guid ModelId
    {
      get { return new Guid(MEDIA_MODEL_ID_STR); }
    }

    public void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      CurrentView = GetViewFromContext(newContext);
      ReloadItems();
    }

    public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      // We could dispose some data here when exiting media navigation context
    }

    public void ChangeModelContext(NavigationContext oldContext, NavigationContext newContext, bool push)
    {
      CurrentView = GetViewFromContext(newContext);
      ReloadItems();
    }

    public void Deactivate(NavigationContext oldContext, NavigationContext newContext)
    {
    }

    public void ReActivate(NavigationContext oldContext, NavigationContext newContext)
    {
      CurrentView = GetViewFromContext(newContext);
      ReloadItems();
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
