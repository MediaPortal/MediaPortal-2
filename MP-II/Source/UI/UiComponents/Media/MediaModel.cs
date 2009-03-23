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
using MediaPortal.Core.Commands;
using MediaPortal.Core.Logging;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Media.ClientMediaManager;
using MediaPortal.Media.ClientMediaManager.Views;
using MediaPortal.Presentation.DataObjects;
using MediaPortal.Presentation.Models;
using MediaPortal.Presentation.Players;
using MediaPortal.Presentation.Screens;
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

    // Keys for the ListItem's Labels in the ItemsLists
    public const string NAME_KEY = "Name";
    public const string MEDIA_ITEM_KEY = "MediaItem";

    public const string PLAY_ITEM_RESOURCE = "[Media.PlayItem]";
    public const string ENQUEUE_ITEM_RESOURCE = "[Media.EnqueueItem]";
    public const string PLAY_ITEM_PRIMARY_RESOURCE = "[Media.PlayItemPrimary]";
    public const string ENQUEUE_ITEM_PRIMARY_RESOURCE = "[Media.EnqueueItemPrimary]";
    public const string PLAY_ITEM_SECONDARY_RESOURCE = "[Media.PlayItemSecondary]";
    public const string ENQUEUE_ITEM_SECONDARY_RESOURCE = "[Media.EnqueueItemSecondary]";

    public const string PLAY_MENU_DIALOG_SCREEN = "DialogPlayMenu";

    #region Protected fields

    // Media screen
    protected ItemsList _mediaItems = null;
    protected View _currentView;
    protected bool _hasParentDirectory;

    // Play menu
    protected ItemsList _playMenuItems = null;

    #endregion

    public MediaModel()
    {
      _currentView = RootView;
      _hasParentDirectory = false;
    }

    /// <summary>
    /// Provides a list with the sub views and media items of the current view.
    /// Note: This <see cref="MediaItems"/> list doesn't contain an item to navigate to the parent view.
    /// It is job of the skin to provide a means to navigate to the parent view.
    /// </summary>
    public ItemsList MediaItems
    {
      get { return _mediaItems; }
    }

    /// <summary>
    /// Gets the information whether the current view has a navigatable parent view.
    /// </summary>
    public bool HasParentDirectory
    {
      get { return _hasParentDirectory; }
      set { _hasParentDirectory = value; }
    }

    /// <summary>
    /// Provides the data of the view currently shown.
    /// </summary>
    public View CurrentView
    {
      get { return _currentView; }
      set { _currentView = value; }
    }

    public View RootView
    {
      get { return ServiceScope.Get<MediaManager>().RootView; }
    }

    /// <summary>
    /// Provides a list of items to be shown in the play menu.
    /// </summary>
    public ItemsList PlayMenuItems
    {
      get { return _playMenuItems; }
    }

    /// <summary>
    /// Provides a callable method for the skin to select an item.
    /// Depending on the item type, we will navigate to the choosen view or play the choosen item.
    /// </summary>
    /// <param name="item">The choosen item. This item should be one of the items in the
    /// <see cref="MediaItems"/> list.</param>
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
        CheckPlayMenu(playableItem.MediaItem);
        return;
      }
    }

    /// <summary>
    /// Provides a callable method for the play menu dialog to select one of the items representing
    /// a user choice of a player slot.
    /// </summary>
    /// <param name="playerSlotItem">The item which was generated by method <see cref="CheckPlayMenu"/>.</param>
    public void SelectPlayerSlot(ListItem playerSlotItem)
    {
      ICommand command = playerSlotItem.Command;
      if (command != null)
        command.Execute();
    }

    #region Protected methods

    /// <summary>
    /// Does the actual work of navigating to the specifield view. This will exchange our
    /// <see cref="CurrentView"/> to the specified <paramref name="view"/> and push a state onto
    /// the workflow manager's navigation stack.
    /// </summary>
    /// <param name="view">View to navigate to.</param>
    protected static void NavigateToView(View view)
    {
      WorkflowState newState = WorkflowState.CreateTransientState(
          "View: " + view.DisplayName, MEDIA_MAIN_SCREEN, true, true);
      IWorkflowManager workflowManager = ServiceScope.Get<IWorkflowManager>();
      IDictionary<string, object> variables = new Dictionary<string, object>
        {
            {VIEW_KEY, view}
        };
      workflowManager.NavigatePushTransient(newState, variables);
    }

    /// <summary>
    /// Checks if we need to show a menu for playing the specified <paramref name="item"/>.
    /// </summary>
    /// <param name="item">The item to play.</param>
    protected void CheckPlayMenu(MediaItem item)
    {
      IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>();
      int numOpen = playerManager.NumOpenSlots;
      if (numOpen == 0)
      {
        PlayItem(item);
        return;
      }
      IScreenManager screenManager = ServiceScope.Get<IScreenManager>();
      if (numOpen == 1)
      {
        _playMenuItems = new ItemsList();
        ListItem playItem = new ListItem(NAME_KEY, PLAY_ITEM_RESOURCE);
        playItem.Command = new MethodDelegateCommand(() => PlayItem(item));
        _playMenuItems.Add(playItem);
        ListItem enqueueItem = new ListItem(NAME_KEY, ENQUEUE_ITEM_RESOURCE);
        enqueueItem.Command = new MethodDelegateCommand(() => PlayOrEnqueueItem(item, false, PlayerManagerConsts.PRIMARY_SLOT));
        _playMenuItems.Add(enqueueItem);
        ListItem playItemSecondary = new ListItem(NAME_KEY, PLAY_ITEM_SECONDARY_RESOURCE);
        playItemSecondary.Command = new MethodDelegateCommand(() => PlayOrEnqueueItem(item, true, PlayerManagerConsts.SECONDARY_SLOT));
        _playMenuItems.Add(playItemSecondary);
        screenManager.ShowDialog(PLAY_MENU_DIALOG_SCREEN);
        return;
      }
      // numOpen == 2
      _playMenuItems = new ItemsList();
      ListItem playItemPrimary = new ListItem(NAME_KEY, PLAY_ITEM_PRIMARY_RESOURCE);
      playItemPrimary.Command = new MethodDelegateCommand(() => PlayOrEnqueueItem(item, true, PlayerManagerConsts.PRIMARY_SLOT));
      _playMenuItems.Add(playItemPrimary);
      ListItem enqueueItemPrimary = new ListItem(NAME_KEY, ENQUEUE_ITEM_PRIMARY_RESOURCE);
      enqueueItemPrimary.Command = new MethodDelegateCommand(() => PlayOrEnqueueItem(item, false, PlayerManagerConsts.PRIMARY_SLOT));
      _playMenuItems.Add(enqueueItemPrimary);
      ListItem playItemSecondary_ = new ListItem(NAME_KEY, PLAY_ITEM_SECONDARY_RESOURCE);
      playItemSecondary_.Command = new MethodDelegateCommand(() => PlayOrEnqueueItem(item, true, PlayerManagerConsts.SECONDARY_SLOT));
      _playMenuItems.Add(playItemSecondary_);
      ListItem enqueueItemSecondary = new ListItem(NAME_KEY, ENQUEUE_ITEM_SECONDARY_RESOURCE);
      enqueueItemSecondary.Command = new MethodDelegateCommand(() => PlayOrEnqueueItem(item, false, PlayerManagerConsts.SECONDARY_SLOT));
      _playMenuItems.Add(enqueueItemSecondary);
      screenManager.ShowDialog(PLAY_MENU_DIALOG_SCREEN);
    }

    /// <summary>
    /// Discards any currently playing items and plays the specified media <paramref name="item"/>.
    /// </summary>
    /// <param name="item">Media item to be played.</param>
    protected static void PlayItem(MediaItem item)
    {
      IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>();
      playerManager.CloseSlot(PlayerManagerConsts.SECONDARY_SLOT);
      PlayOrEnqueueItem(item, true, PlayerManagerConsts.PRIMARY_SLOT);
    }

    /// <summary>
    /// Depending on parameter <paramref name="play"/>, plays or enqueues the specified media item
    /// <paramref name="item"/> in the specified <paramref name="playerSlot"/>.
    /// </summary>
    /// <param name="item">Media item to be played.</param>
    /// <param name="play">If <c>true</c>, plays the specified <paramref name="item"/>, else enqueues it.</param>
    /// <param name="playerSlot">Slot index to enqueue the item.</param>
    protected static void PlayOrEnqueueItem(MediaItem item, bool play, int playerSlot)
    {
      IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>();
      IPlayerSlotController psc;
      int slotIndex;
      if (playerManager.NumOpenSlots > playerSlot)
        psc = playerManager.GetSlot(playerSlot);
      else if (!playerManager.OpenSlot(out slotIndex, out psc))
      {
        ILogger logger = ServiceScope.Get<ILogger>();
        logger.Error("MediaModel: Unable to open a player slot");
        return;
      }
      if (play)
        psc.Reset();
      psc.PlayList.Add(item);
      if (play)
        psc.Play();
    }

    protected void ReloadItems()
    {
      // We need to create a new items list because the reloading of items takes place while the old
      // screen still shows the old items
      _mediaItems = new ItemsList();
      // TODO: Add the items in a separate job while the UI already shows the new screen
      View currentView = CurrentView;
      HasParentDirectory = currentView.ParentView != null;
      if (currentView.IsValid)
      {
        // Add items for sub views
        foreach (View subView in currentView.SubViews)
          _mediaItems.Add(new NavigationItem(subView, null));
        foreach (MediaItem item in currentView.MediaItems)
          _mediaItems.Add(new PlayableItem(item));
      }
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

    public bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
    {
      return true;
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
      View newView = GetViewFromContext(newContext);
      if (newView == CurrentView)
        return;
      CurrentView = newView;
      ReloadItems();
    }

    public void UpdateMenuActions(NavigationContext context, ICollection<WorkflowStateAction> actions)
    {
    }

    #endregion
  }
}
