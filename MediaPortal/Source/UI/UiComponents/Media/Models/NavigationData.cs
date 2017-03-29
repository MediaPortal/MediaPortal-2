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
using MediaPortal.Common.Localization;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.Settings;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UiComponents.Media.Settings;
using MediaPortal.UiComponents.Media.Views;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.UiComponents.Media.Models.Navigation;
using MediaPortal.UiComponents.Media.Models.ScreenData;
using MediaPortal.UI.SkinEngine.Controls.Panels;

namespace MediaPortal.UiComponents.Media.Models
{
  /// <summary>
  /// Corresponds to the current media navigation step. The navigation data basically specifies the current underlaying set of
  /// media items, which are presented in one of the <see cref="AvailableScreens"/>, represented to the user as
  /// <see cref="DynamicWorkflowActions"/> in the menu.
  /// </summary>
  /// <remarks>
  /// The <see cref="CurrentScreenData"/> holds the concrete data for that representation mode of the current navigation
  /// position, i.e. it provides the concrete UI data for the skin. The <see cref="CurrentScreenData"/> can change to present
  /// the current media items in a different way, for example grouped by different criteria.
  /// </remarks>
  public class NavigationData : IGroupingValueProvider
  {
    #region Protected properties

    protected bool _initializing = true;
    protected NavigationData _parent;
    protected string _navigationContextName;
    protected Guid _currentWorkflowStateId;
    protected Guid _baseWorkflowStateId;
    protected ViewSpecification _baseViewSpecification;
    protected AbstractScreenData _currentScreenData;
    protected ICollection<AbstractScreenData> _availableScreens;
    protected ICollection<WorkflowAction> _dynamicWorkflowActions;

    protected Sorting.Sorting _currentSorting = null;
    protected Sorting.Sorting _currentGrouping = null;
    protected ICollection<Sorting.Sorting> _availableSortings = null;
    protected ICollection<Sorting.Sorting> _availableGroupings = null;
    protected LayoutType _layoutType = LayoutType.ListLayout;
    protected LayoutSize _layoutSize = LayoutSize.Small;

    #endregion

    /// <summary>
    /// Creates a new navigation data structure for a new media navigation step.
    /// </summary>
    /// <param name="parent">Parent navigation data, this navigation data is derived from.</param>
    /// <param name="navigationContextName">Name, which is used for the corresponding workflow navigation context.</param>
    /// <param name="currentWorkflowStateId">Id of the workflow state which corresponds to the new media navigation step.</param>
    /// <param name="parentWorkflowStateId">Id of the workflow state to which the workflow navigation should be reverted when
    /// another filter is choosen.</param>
    /// <param name="baseViewSpecification">View specification for the media items of the new media navigation step.</param>
    /// <param name="defaultScreen">Screen which should present the new navigation step by default.</param>
    /// <param name="availableScreens">Available set of screen descriptions which can present the new media navigation step.</param>
    /// <param name="currentSorting">Denotes the current sorting for the items to be shown. If this is set to <c>null</c>,
    /// default sorting will be applied.</param>
    /// <param name="currentGrouping">Denotes the current grouping for the items to be shown.</param>
    public NavigationData(NavigationData parent, string navigationContextName, Guid parentWorkflowStateId, Guid currentWorkflowStateId,
        ViewSpecification baseViewSpecification, AbstractScreenData defaultScreen, ICollection<AbstractScreenData> availableScreens,
        Sorting.Sorting currentSorting, Sorting.Sorting currentGrouping) :
      this(parent, navigationContextName, parentWorkflowStateId, currentWorkflowStateId, baseViewSpecification, defaultScreen, availableScreens,
        currentSorting, currentGrouping, false) { }

    // If the suppressActions parameter is set to <c>true</c>, no actions will be built. Instead, they will be inherited from
    // the parent navigation step. That is used for subview navigation where the navigation step doesn't produce own
    // workflow actions.
    protected NavigationData(NavigationData parent, string navigationContextName, Guid parentWorkflowStateId, Guid currentWorkflowStateId,
        ViewSpecification baseViewSpecification, AbstractScreenData defaultScreen, ICollection<AbstractScreenData> availableScreens,
        Sorting.Sorting currentSorting, Sorting.Sorting currentGrouping, bool suppressActions)
    {
      _parent = parent;
      _navigationContextName = navigationContextName;
      _currentWorkflowStateId = currentWorkflowStateId;
      _baseWorkflowStateId = parentWorkflowStateId;
      _baseViewSpecification = baseViewSpecification;
      _currentScreenData = defaultScreen;
      _availableScreens = availableScreens ?? new List<AbstractScreenData>();
      _currentSorting = currentSorting;
      _currentGrouping = currentGrouping;
      if (suppressActions)
        _dynamicWorkflowActions = null;
      else
        BuildWorkflowActions();
    }

    public void Dispose()
    {
      _currentScreenData.ReleaseScreenData();
    }

    public ViewSpecification BaseViewSpecification
    {
      get { return _baseViewSpecification; }
    }

    /// <summary>
    /// Returns the id of the workflow state to which we must revert when a different view presentation screen is choosen.
    /// </summary>
    public Guid BaseWorkflowStateId
    {
      get { return _baseWorkflowStateId; }
    }

    /// <summary>
    /// Returns the id of the corresponding workflow state.
    /// </summary>
    public Guid CurrentWorkflowStateId
    {
      get { return _currentWorkflowStateId; }
    }

    public AbstractScreenData CurrentScreenData
    {
      get { return _currentScreenData; }
      internal set { _currentScreenData = value; }
    }

    public NavigationData Parent
    {
      get { return _parent; }
    }

    public ICollection<AbstractScreenData> AvailableScreens
    {
      get { return _availableScreens; }
    }

    public Sorting.Sorting CurrentSorting
    {
      get { return _currentSorting; }
      set
      {
        _currentSorting = value;
        AbstractScreenData screenData = _currentScreenData;
        if (screenData != null)
          screenData.UpdateItems();
        SaveLayoutSettings();
      }
    }

    public ICollection<Sorting.Sorting> AvailableSortings
    {
      get
      {
        ICollection<Sorting.Sorting> result = _availableSortings;
        if (result != null)
          return result;
        NavigationData parent = _parent;
        if (parent == null)
          return null;
        return parent.AvailableSortings;
      }
      set { _availableSortings = value; }
    }
    
    public Sorting.Sorting CurrentGrouping
    {
      get { return _currentGrouping; }
      set
      {
        _currentGrouping = value;
        AbstractScreenData screenData = _currentScreenData;
        if (screenData != null)
          screenData.UpdateItems();
        SaveLayoutSettings();
      }
    }


    public object GetGroupingValue(object item)
    {
      if (CurrentGrouping == null)
        return null;
      var pmi = item as PlayableMediaItem;
      if (pmi != null)
        item = pmi.MediaItem;
      var mi = item as MediaItem;
      if (mi != null)
        return CurrentGrouping.GetGroupByValue(mi);
      return null;
    }

    public bool IsGroupingActive
    {
      get { return CurrentGrouping != null; }
    }

    public ICollection<Sorting.Sorting> AvailableGroupings
    {
      get
      {
        ICollection<Sorting.Sorting> result = _availableGroupings;
        if (result != null)
          return result;
        NavigationData parent = _parent;
        if (parent == null)
          return null;
        return parent.AvailableGroupings;
      }
      set { _availableGroupings = value; }
    }

    public Sorting.Sorting GetCombinedSorting()
    {
      if (CurrentSorting == null && CurrentGrouping == null)
        return null;
      if (CurrentSorting == null)
        return CurrentGrouping;
      if (CurrentGrouping == null)
        return CurrentSorting;
      // in this case create a combined grouping sorting
      return new CombinedSorting(CurrentGrouping, CurrentSorting);
    }

    private class CombinedSorting : Sorting.Sorting
    {
      private readonly Sorting.Sorting _grouping;
      private readonly Sorting.Sorting _sorting;

      public CombinedSorting(Sorting.Sorting grouping, Sorting.Sorting sorting)
      {
        _grouping = grouping;
        _sorting = sorting;
      }

      public override string DisplayName
      {
        get { return _sorting == null ? String.Empty :_sorting.DisplayName; }
      }

      public override string GroupByDisplayName
      {
        get { return _grouping == null ? String.Empty : _grouping.DisplayName; }
      }

      public override int Compare(MediaItem x, MediaItem y)
      {
        var g = _grouping.Compare(x, y);
        if (g == 0)
          return _sorting.Compare(x, y);
        return g;
      }

      public override object GetGroupByValue(MediaItem item)
      {
        if (_grouping != null)
          return _grouping.GetGroupByValue(item);
        return null;
      }
    }

    /// <summary>
    /// Gets a collection of workflow actions to be shown in the menu which reflect the current
    /// <see cref="AvailableScreens"/>.
    /// </summary>
    public ICollection<WorkflowAction> DynamicWorkflowActions
    {
      get { return _dynamicWorkflowActions; }
    }

    /// <summary>
    /// Gets the information whether this navigation data is currently enabled.
    /// </summary>
    public bool IsEnabled
    {
      get { return _currentScreenData.IsEnabled; }
    }

    /// <summary>
    /// Gets the <see cref="LayoutType"/> associated with current navigation data.
    /// </summary>
    public LayoutType LayoutType
    {
      get { return _layoutType; }
      internal set
      {
        _layoutType = value;
        SaveLayoutSettings();
      }
    }

    /// <summary>
    /// Gets the <see cref="LayoutSize"/> associated with current navigation data.
    /// </summary>
    public LayoutSize LayoutSize
    {
      get { return _layoutSize; }
      internal set
      {
        _layoutSize = value;
        SaveLayoutSettings();
      }
    }

    /// <summary>
    /// Releases resources which are needed by the current screen.
    /// </summary>
    public void Disable()
    {
      _currentScreenData.ReleaseScreenData();
    }

    /// <summary>
    /// Restores resources which are needed by the current screen.
    /// </summary>
    public void Enable()
    {
      _currentScreenData.CreateScreenData(this);
      UpdateLayout();
    }

    /// <summary>
    /// Enters a new media navigation context by inheriting all currently available screens. This is used for
    /// presenting the contents of a media items or filter group, where the current menu should remain available.
    /// Only the currently visible screen can be exchanged to configure another presentation mode for the group to
    /// be stepped-in.
    /// </summary>
    /// <remarks>
    /// Actually, we mix two different concerns in this method:
    /// <list type="number">
    /// <item>The setting that the new navigation context will be subordinated, i.e. it will be removed/exchanged by a filter action</item>
    /// <item>The setting that all menu actions will be adopted from the parent navigation context</item>
    /// </list>
    /// But in fact, filter actions are only used together with the concept that there exist two different kind of navigation contexts;
    /// autonomous contexts and subordinated contexts.
    /// If there are no filter actions present (like in the browse media navigation modes), the only difference between the methods
    /// <see cref="StackSubordinateNavigationContext"/> and <see cref="StackAutonomousNavigationContext"/> is the inheritance of the menu.
    /// </remarks>
    /// <param name="subViewSpecification">Specification for the sub view to be shown in the new navigation context.</param>
    /// <param name="visibleScreen">Screen which should be visible in the new navigation context.</param>
    /// <param name="navbarDisplayLabel">Display label to be shown in the navigation bar for the new navigation context.</param>
    /// <returns>Newly created navigation data.</returns>
    public NavigationData StackSubordinateNavigationContext(ViewSpecification subViewSpecification, AbstractScreenData visibleScreen,
        string navbarDisplayLabel)
    {
      WorkflowState newState = WorkflowState.CreateTransientState(
          "View: " + subViewSpecification.ViewDisplayName, subViewSpecification.ViewDisplayName,
          false, null, true, WorkflowType.Workflow);

      ScreenConfig nextScreenConfig;
      LoadLayoutSettings(visibleScreen.ToString(), out nextScreenConfig);

      Sorting.Sorting nextSortingMode = AvailableSortings.FirstOrDefault(
        sorting => sorting.GetType().ToString() == nextScreenConfig.Sorting && sorting.IsAvailable(visibleScreen)) ?? _currentSorting;
      Sorting.Sorting nextGroupingMode = string.IsNullOrEmpty(nextScreenConfig.Grouping) ? null : AvailableGroupings.FirstOrDefault(
        grouping => grouping.GetType().ToString() == nextScreenConfig.Grouping && grouping.IsAvailable(visibleScreen)) ?? _currentGrouping;

      NavigationData newNavigationData = new NavigationData(this, subViewSpecification.ViewDisplayName,
          _baseWorkflowStateId, newState.StateId, subViewSpecification, visibleScreen, _availableScreens, nextSortingMode, nextGroupingMode, true)
      {
        LayoutType = nextScreenConfig.LayoutType,
        LayoutSize = nextScreenConfig.LayoutSize
      };
      PushNewNavigationWorkflowState(newState, navbarDisplayLabel, newNavigationData);
      return newNavigationData;
    }

    /// <summary>
    /// Enters a new media navigation context by modifying the list of available screens. This is used for
    /// presenting the result of a filter, where the menu must be changed.
    /// </summary>
    /// <param name="subViewSpecification">Specification for the sub view to be shown in the new navigation context.</param>
    /// <param name="currentMenuItemLabel">Current menu item label needed for distinction of available screens.</param>
    /// <param name="navbarDisplayLabel">Display label to be shown in the navigation bar for the new navigation context.</param>
    /// <returns>Newly created navigation data.</returns>
    public NavigationData StackAutonomousNavigationContext(ViewSpecification subViewSpecification, string currentMenuItemLabel, string navbarDisplayLabel)
    {
      AbstractScreenData currentScreen = AvailableScreens.FirstOrDefault(screen => screen.MenuItemLabel == currentMenuItemLabel);
      ICollection<AbstractScreenData> remainingScreens = new List<AbstractScreenData>(AvailableScreens.Where(screen => screen != currentScreen));

      WorkflowState newState = WorkflowState.CreateTransientState(
          "View: " + subViewSpecification.ViewDisplayName, subViewSpecification.ViewDisplayName,
          false, null, false, WorkflowType.Workflow);

      string nextScreenName;
      AbstractScreenData nextScreen = null;

      // Try to load the prefered next screen from settings.
      if (LoadScreenHierarchy(CurrentScreenData.GetType().ToString(), out nextScreenName))
        nextScreen = remainingScreens.FirstOrDefault(s => s.GetType().ToString() == nextScreenName && s.CanFilter(currentScreen));

      // Default way: always take the first of the available screens.
      if (nextScreen == null)
        nextScreen = remainingScreens.First(s => s != currentScreen && s.CanFilter(currentScreen));

      ScreenConfig nextScreenConfig;
      LoadLayoutSettings(nextScreen.GetType().ToString(), out nextScreenConfig);

      Sorting.Sorting nextSortingMode = AvailableSortings.FirstOrDefault(
        sorting => sorting.GetType().ToString() == nextScreenConfig.Sorting && sorting.IsAvailable(nextScreen)) ?? _currentSorting;
      Sorting.Sorting nextGroupingMode = String.IsNullOrEmpty(nextScreenConfig.Grouping) ? null : AvailableGroupings.FirstOrDefault(
        grouping => grouping.GetType().ToString() == nextScreenConfig.Grouping && grouping.IsAvailable(nextScreen)) ?? _currentGrouping;

      NavigationData newNavigationData = new NavigationData(this, subViewSpecification.ViewDisplayName,
          newState.StateId, newState.StateId, subViewSpecification, nextScreen, remainingScreens,
          nextSortingMode, nextGroupingMode) { LayoutType = nextScreenConfig.LayoutType, LayoutSize = nextScreenConfig.LayoutSize };
      PushNewNavigationWorkflowState(newState, navbarDisplayLabel, newNavigationData);
      return newNavigationData;
    }

    private void UpdateLayout()
    {
      // Clear the initializing flag, so further changes to layout properties will be saved.
      _initializing = false;

      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      ViewModeModel vm = workflowManager.GetModel(ViewModeModel.VM_MODEL_ID) as ViewModeModel;
      if (vm != null)
        vm.Update();
    }

    private void SaveLayoutSettings()
    {
      // Avoid saving changes when constructing new instances.
      if (_initializing)
        return;

      ViewSettings viewSettings = ServiceRegistration.Get<ISettingsManager>().Load<ViewSettings>();
      viewSettings.ScreenConfigs[CurrentScreenData.GetType().ToString()] = new ScreenConfig
      {
        Sorting = CurrentSorting.GetType().ToString(),
        Grouping = CurrentGrouping == null ? String.Empty : CurrentGrouping.GetType().ToString(),
        LayoutSize = LayoutSize,
        LayoutType = LayoutType
      };
      ServiceRegistration.Get<ISettingsManager>().Save(viewSettings);
    }

    public static bool LoadLayoutSettings(string nextScreen, out ScreenConfig screenConfig)
    {
      ViewSettings viewSettings = ServiceRegistration.Get<ISettingsManager>().Load<ViewSettings>();
      return viewSettings.ScreenConfigs.TryGetValue(nextScreen, out screenConfig);
    }

    public static bool LoadScreenHierarchy(string currentScreen, out string nextScreen)
    {
      ViewSettings viewSettings = ServiceRegistration.Get<ISettingsManager>().Load<ViewSettings>();
      return viewSettings.ScreenHierarchy.TryGetValue(currentScreen, out nextScreen);
    }

    public static void SaveScreenHierarchy(string currentScreen, string nextScreen, bool backupOld = false)
    {
      ViewSettings viewSettings = ServiceRegistration.Get<ISettingsManager>().Load<ViewSettings>();
      if (backupOld)
      {
        string oldScreen;
        if (viewSettings.ScreenHierarchy.TryGetValue(currentScreen, out oldScreen))
          viewSettings.ScreenHierarchy[currentScreen + "_OLD"] = oldScreen;
      }
      viewSettings.ScreenHierarchy[currentScreen] = nextScreen;
      ServiceRegistration.Get<ISettingsManager>().Save(viewSettings);
    }

    protected static void PushNewNavigationWorkflowState(WorkflowState newState, string navbarDisplayLabel, NavigationData newNavigationData)
    {
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      workflowManager.NavigatePushTransient(newState, new NavigationContextConfig
        {
          AdditionalContextVariables = new Dictionary<string, object>
            {
              {Consts.KEY_NAVIGATION_DATA, newNavigationData}
            },
          NavigationContextDisplayLabel = navbarDisplayLabel
        });
    }

    protected void BuildWorkflowActions()
    {
      _dynamicWorkflowActions = GetWorkflowActions(true);
    }

    /// <summary>
    /// Gets a list of available screens ("filters") of the current <see cref="NavigationData"/>. This method can return either filtering screens that are showing
    /// results of predefined query (<paramref name="onlySearchScreens"/>=<c>false</c>), or custom search screens that allow user input for searching (<paramref name="onlySearchScreens"/>=<c>true</c>).
    /// </summary>
    /// <param name="onlySearchScreens"><c>true</c> to return only search screens.</param>
    /// <returns>List of workflow actions</returns>
    public IList<WorkflowAction> GetWorkflowActions(bool onlySearchScreens = false)
    {
      IList<WorkflowAction> actions = new List<WorkflowAction>(_availableScreens.Count);
      AbstractScreenData parentScreen = _parent != null ? _parent.CurrentScreenData : null;
      IEnumerable<AbstractScreenData> screens = parentScreen != null ? _availableScreens.Where(s => s.CanFilter(parentScreen)) : _availableScreens;
      int ct = 0;
      foreach (AbstractScreenData screen in screens)
      {
        if (onlySearchScreens && !(screen is AbstractSearchScreenData))
          continue;
        if (!onlySearchScreens && (screen is AbstractSearchScreenData))
          continue;
        AbstractScreenData newScreen = screen; // Necessary to be used in closure
        WorkflowAction action = new MethodDelegateAction(Guid.NewGuid(),
            _navigationContextName + "->" + newScreen.MenuItemLabel, new Guid[] { _currentWorkflowStateId },
            LocalizationHelper.CreateResourceString(newScreen.MenuItemLabel), () =>
              {
                _currentScreenData.ReleaseScreenData();
                _currentScreenData = newScreen;

                string parent = Parent == null ? _navigationContextName : Parent.CurrentScreenData.GetType().ToString();
                // Do not save search screens as selection, they are only a "transient" state.
                if (!(newScreen is AbstractSearchScreenData))
                  SaveScreenHierarchy(parent, newScreen.GetType().ToString());

                IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
                // The last screen could have stepped into a deeper media navigation context when it had produced
                // sub views. So we first have to revert our workflow to the base workflow id before moving to the new screen.
                if (workflowManager.CurrentNavigationContext.WorkflowState.StateId == _baseWorkflowStateId)
                { // If we're already in the correct the state, update the screen manually
                  _currentScreenData.CreateScreenData(this);
                  SwitchToCurrentScreen();
                }
                else
                  // WF-Manager updates the screen for us
                  workflowManager.NavigatePopToState(_baseWorkflowStateId, false);
              })
        {
          DisplayCategory = Consts.FILTERS_WORKFLOW_CATEGORY,
          SortOrder = ct++.ToString(), // Sort in the order we have built up the filters
        };
        actions.Add(action);
      }
      return actions;
    }

    protected void SwitchToCurrentScreen()
    {
      ServiceRegistration.Get<IScreenManager>().ShowScreen(_currentScreenData.Screen);
    }
  }
}
