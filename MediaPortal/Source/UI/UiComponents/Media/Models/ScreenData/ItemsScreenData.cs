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

using System.Collections.Generic;
using MediaPortal.Core;
using MediaPortal.Core.Commands;
using MediaPortal.Core.MediaManagement;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UI.Views;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.UiComponents.Media.Models.Navigation;
using MediaPortal.Utilities;

namespace MediaPortal.UiComponents.Media.Models.ScreenData
{
  public abstract class ItemsScreenData : AbstractScreenData
  {
    public delegate PlayableItem PlayableItemCreatorDelegate(MediaItem mi);

    protected View _view = null;
    protected PlayableItemCreatorDelegate _playableItemCreator;

    protected ItemsScreenData(string screen, string menuItemLabel, PlayableItemCreatorDelegate playableItemCreator) :
        base(screen, menuItemLabel)
    {
      _playableItemCreator = playableItemCreator;
    }

    public override void CreateScreenData(NavigationData navigationData)
    {
      base.CreateScreenData(navigationData);
      _view = navigationData.BaseViewSpecification.BuildView();
      ReloadMediaItems(_view, true);
    }

    public View CurrentView
    {
      get { return _view; }
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
    public abstract AbstractScreenData Derive();

    /// <summary>
    /// Updates the GUI data for a media items view screen which reflects the data of the given <paramref name="view"/>.
    /// </summary>
    /// <remarks>
    /// Updates the properties <see cref="AbstractScreenData.Items"/>, <see cref="AbstractScreenData.IsItemsEmpty"/> and
    /// <see cref="AbstractScreenData.IsItemsValid"/>.
    /// </remarks>
    /// <param name="view">View contents to be filled into the <see cref="AbstractScreenData.Items"/> GUI property.</param>
    /// <param name="createNewList">If set to <c>true</c>, this method will re-create the
    /// <see cref="AbstractScreenData.Items"/> list, else it will reuse it.</param>
    protected void ReloadMediaItems(View view, bool createNewList)
    {
      // We need to create a new items list because the reloading of items takes place while the old
      // screen still shows the old items
      ItemsList items;
      if (createNewList)
        items = new ItemsList();
      else
      {
        items = Items;
        items.Clear();
      }
      // TODO: Add the items in a separate job while the UI already shows the new screen
      if (view.IsValid)
      {
        // Add items for sub views
        IList<View> subViews = view.SubViews;
        IList<MediaItem> mediaItems = view.MediaItems;
        if (subViews == null || mediaItems == null)
        {
          IsItemsEmpty = false;
          IsItemsValid = false;
          TooManyItems = false;
          NumItemsStr = string.Empty;
        }
        else
        {
          if (subViews.Count + mediaItems.Count > MAX_NUM_ITEMS)
          {
            // TODO: Cluster results
            IsItemsValid = true;
            IsItemsEmpty = false;
            TooManyItems = true;
            NumItemsStr = Utils.BuildNumItemsStr(subViews.Count + mediaItems.Count);
          }
          else
          {
            List<ListItem> viewsList = new List<ListItem>();
            foreach (View subView in subViews)
            {
              NavigationItem item = new NavigationItem(subView, null);
              View sv = subView;
              item.Command = new MethodDelegateCommand(() => NavigateToView(sv.Specification));
              viewsList.Add(item);
            }
            viewsList.Sort((v1, v2) => string.Compare(v1[Consts.NAME_KEY], v2[Consts.NAME_KEY]));
            CollectionUtils.AddAll(items, viewsList);

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

            IsItemsValid = true;
            IsItemsEmpty = items.Count == 0;
            TooManyItems = false;
            NumItemsStr = Utils.BuildNumItemsStr(items.Count);
          }
        }
      }
      else
      {
        IsItemsEmpty = false;
        IsItemsValid = false;
        TooManyItems = false;
        NumItemsStr = string.Empty;
      }
      _items = items;
      _items.FireChange();
    }

    /// <summary>
    /// Does the actual work of navigating to the specifield sub view. This will create a new <see cref="NavigationData"/>
    /// instance for the new screen and push a new transient workflow state onto the workflow navigation stack.
    /// </summary>
    /// <param name="subViewSpecification">Specification of the sub view to navigate to.</param>
    protected void NavigateToView(ViewSpecification subViewSpecification)
    {
      WorkflowState newState = WorkflowState.CreateTransientState(
          "View: " + subViewSpecification.ViewDisplayName, subViewSpecification.ViewDisplayName,
          false, null, true, WorkflowType.Workflow);
      ICollection<AbstractScreenData> remainingScreens = new List<AbstractScreenData>(_navigationData.AvailableScreens);
      remainingScreens.Remove(this);
      NavigationData newNavigationData = new NavigationData(subViewSpecification.ViewDisplayName,
          _navigationData.BaseWorkflowStateId, newState.StateId, subViewSpecification, Derive(), remainingScreens);
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      IDictionary<string, object> variables = new Dictionary<string, object>
        {
            {MediaModel.NAVIGATION_DATA_KEY, newNavigationData},
        };
      workflowManager.NavigatePushTransient(newState, new NavigationContextConfig { AdditionalContextVariables = variables });
    }

  }
}