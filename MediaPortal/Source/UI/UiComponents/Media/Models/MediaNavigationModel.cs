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
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.PluginItemBuilders;
using MediaPortal.Common.PluginManager;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.SkinResources;
using MediaPortal.UiComponents.Media.Models.NavigationModel;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.UiComponents.Media.Models.ScreenData;
using MediaPortal.UI.SkinEngine.ScreenManagement;

namespace MediaPortal.UiComponents.Media.Models
{
  /// <summary>
  /// Model which holds the GUI state for the current navigation in the media views.
  /// </summary>
  /// <remarks>
  /// The media model attends all media navigation workflow states. It can handle the local media navigation as well as
  /// media library based audio, videos and images navigation. For each of those modes, there exists an own workflow
  /// state which represents the according root view. So when navigating to the root view state for the audio navigation,
  /// this model will show the audio navigation screen with a view representing the complete audio contents of the media
  /// library, for example. For the navigation, it delegates to <see cref="AbstractScreenData"/> and its sub classes.
  /// </remarks>
  public class MediaNavigationModel : IWorkflowModel
  {
    #region Consts

    // Global ID definitions and references
    public const string MEDIA_MODEL_ID_STR = "4CDD601F-E280-43b9-AD0A-6D7B2403C856";

    // ID variables
    public static readonly Guid MEDIA_MODEL_ID = new Guid(MEDIA_MODEL_ID_STR);

    protected static readonly IList<Guid> EMPTY_GUID_LIST = new List<Guid>();

    #endregion

    #region Protected fields

    // Screen data is stored in current navigation context
    protected NavigationContext _currentNavigationContext = null;
    protected static readonly IPluginItemStateTracker _mediaSkinMIATypeRegistrationStateTracker = new DefaultItemStateTracker("MediaModelNavigation");

    /// <summary>
    /// Contains the <see cref="IMediaNavigationInitializer"/>s for all media navigation root states. This list can be
    /// extended by plugins.
    /// </summary>
    protected static readonly Dictionary<Guid, IMediaNavigationInitializer> _initializers = new Dictionary<Guid, IMediaNavigationInitializer>();

    #endregion

    #region Constructor

    static MediaNavigationModel()
    {
      // Initializes the list of inbuilt IMediaNavigationInitializers.
      new List<IMediaNavigationInitializer>
      {
        new VideosNavigationInitializer(),
        new AudioNavigationInitializer(),
        new ImagesNavigationInitializer(),
        new SeriesNavigationInitializer(),
        new MoviesNavigationInitializer(),
        new LocalBrowsingNavigationInitializer(),
        new MediaLibraryBrowsingNavigationInitializer(),
      }.ForEach(i => _initializers[i.MediaNavigationRootState] = i);
    }

    #endregion

    #region Public members

    /// <summary>
    /// Gets or sets the current media navigation mode.
    /// </summary>
    /// <remarks>
    /// The media navigation mode determines the media library part which is navigated: Audio, Videos or Images. Other
    /// navigation modes are BrowseLocalMedia, which is completely decoupled from the media library, and and BrowseMediaLibrary,
    /// which lets the user browse through the media library contents.
    /// </remarks>
    public string Mode
    {
      get { return GetMode(_currentNavigationContext); }
    }

    /// <summary>
    /// Gets the navigation data which is set in the current workflow navigation context.
    /// </summary>
    public NavigationData NavigationData
    {
      get { return GetNavigationData(_currentNavigationContext, true); }
    }

    /// <summary>
    /// Provides a callable method for the skin to select an item of the media contents view.
    /// Depending on the item type, we will navigate to the choosen view, play the choosen item or filter by the item.
    /// </summary>
    /// <param name="item">The choosen item. Should contain a <see cref="ListItem.Command"/>.</param>
    public void Select(ListItem item)
    {
      if (item == null)
        return;
      if (item.Command != null)
        item.Command.Execute();
    }

    #endregion

    #region Static members which also can be used from other models

    public static MediaNavigationModel GetCurrentInstance()
    {
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      return (MediaNavigationModel) workflowManager.GetModel(MEDIA_MODEL_ID);
    }

    /// <summary>
    /// Gets the information if there is a navigation data available.
    /// </summary>
    public static bool IsNavigationDataEnabled
    {
      get
      {
        MediaNavigationModel model = GetCurrentInstance();
        NavigationData navigationData = model.NavigationData;
        return navigationData != null && navigationData.IsEnabled;
      }
    }

    public static NavigationData GetNavigationData(NavigationContext navigationContext, bool inheritFromPredecessor)
    {
      return navigationContext == null ? null :
        navigationContext.GetContextVariable(Consts.KEY_NAVIGATION_DATA, inheritFromPredecessor) as NavigationData;
    }

    /// <summary>
    /// Adds the current view to the playlist of the current player.
    /// </summary>
    /// <remarks>
    /// This action can be called from outside when there is an enabled navigation data present (<see cref="IsNavigationDataEnabled"/>.
    /// </remarks>
    public static void AddCurrentViewToPlaylist()
    {
      AddCurrentViewToPlaylist(null);
    }

    public static void AddCurrentViewToPlaylist(MediaItem selectedMediaItem)
    {
      MediaNavigationModel model = GetCurrentInstance();
      NavigationData navigationData = model.NavigationData;
      if (navigationData == null || !navigationData.IsEnabled)
      {
        ServiceRegistration.Get<ILogger>().Error("MediaNavigationModel.AddCurrentViewToPlaylist: No enabled navigation data present");
        return;
      }
      if (navigationData.CurrentScreenData.IsItemsEmpty)
      {
        ServiceRegistration.Get<IDialogManager>().ShowDialog(Consts.RES_NO_ITEMS_TO_ADD_HEADER, Consts.RES_NO_ITEMS_TO_ADD_TEXT, DialogType.OkDialog, false, DialogButtonType.Ok);
        return;
      }
      model.AddCurrentViewToPlaylistInternal(selectedMediaItem);
    }

    /// <summary>
    /// Registers a new IMediaNavigationInitializer to add a new media navigation root state.
    /// </summary>
    /// <param name="initializer"></param>
    public static void RegisterMediaNavigationInitializer(IMediaNavigationInitializer initializer)
    {
      _initializers[initializer.MediaNavigationRootState] = initializer;
    }

    #endregion

    #region Protected members

    protected internal static string GetMode(NavigationContext context)
    {
      if (context == null)
        return MediaNavigationMode.BrowseLocalMedia;
      return (context.GetContextVariable(Consts.KEY_NAVIGATION_MODE, true) as string) ?? MediaNavigationMode.BrowseLocalMedia;
    }

    protected internal static void SetMode(string mode, NavigationContext context)
    {
      if (context == null)
        return;
      context.SetContextVariable(Consts.KEY_NAVIGATION_MODE, mode);
    }

    protected static void SetNavigationData(NavigationData navigationData, NavigationContext navigationContext)
    {
      navigationContext.SetContextVariable(Consts.KEY_NAVIGATION_DATA, navigationData);
    }

    protected IEnumerable<MediaItem> GetMediaItemsFromCurrentView(MediaItem selectedMediaItem)
    {
      foreach (var mediaItem in GetMediaItemsFromCurrentView())
      {
        if (selectedMediaItem != null && IsSameMediaItem(mediaItem, selectedMediaItem))
          mediaItem.Aspects[SelectionMarkerAspect.ASPECT_ID] = null;
        yield return mediaItem;
      }
    }

    /// <summary>
    /// Helper method to check MediaItems for "identity". For MediaLibrary managed items the <see cref="MediaItem.MediaItemId"/> will be checked, otherwise the object reference.
    /// </summary>
    /// <param name="m1">MediaItem</param>
    /// <param name="m2">MediaItem</param>
    /// <returns><c>true</c> if equal</returns>
    protected bool IsSameMediaItem(MediaItem m1, MediaItem m2)
    {
      return m1 == m2 || m1.MediaItemId != Guid.Empty && m1.MediaItemId == m2.MediaItemId;
    }

    protected IEnumerable<MediaItem> GetMediaItemsFromCurrentView()
    {
      NavigationData navigationData = NavigationData;
      if (navigationData == null)
        yield break;
      foreach (MediaItem mediaItem in navigationData.CurrentScreenData.GetAllMediaItems())
        yield return mediaItem;
    }

    protected void AddCurrentViewToPlaylistInternal(MediaItem selectedMediaItem)
    {
      NavigationData navigationData = NavigationData;
      if (navigationData == null || !navigationData.IsEnabled)
      {
        ServiceRegistration.Get<ILogger>().Error("MediaNavigationModel: Cannot add current view to playlist - There is no enabled navigation data available");
      }
      string mode = Mode;
      GetMediaItemsDlgt getMediaItemsWithSelection = () => GetMediaItemsFromCurrentView(selectedMediaItem);

      switch (mode)
      {
        case MediaNavigationMode.Audio:
          PlayItemsModel.CheckQueryPlayAction(getMediaItemsWithSelection, AVType.Audio);
          break;
        case MediaNavigationMode.Movies:
        case MediaNavigationMode.Series:
        case MediaNavigationMode.Videos:
        case MediaNavigationMode.Images:
          PlayItemsModel.CheckQueryPlayAction(getMediaItemsWithSelection, AVType.Video);
          break;
        case MediaNavigationMode.BrowseLocalMedia:
        case MediaNavigationMode.BrowseMediaLibrary:
          PlayItemsModel.CheckQueryPlayAction(getMediaItemsWithSelection);
          break;
      }
    }

    private static void GetSkinAndThemeName(ISkinResourceBundle resourceBundle, out string skinName, out string themeName)
    {
      ISkin skin = resourceBundle as ISkin;
      if (skin != null)
      {
        skinName = skin.Name;
        themeName = null;
        return;
      }
      ITheme theme = resourceBundle as ITheme;
      if (theme == null)
      {
        skinName = null;
        themeName = null;
        return;
      }
      themeName = theme.Name;
      skin = theme.ParentSkin;
      skinName = skin == null ? string.Empty : skin.Name;
    }

    // Currently, we don't track skin changes while we're in the media navigation. Normally, that should not be necessary because to switch the skin,
    // the user has to navigate out of media navigation. If we wanted to track skin changes and then update all our navigation data,
    // we would need to register a plugin item registration change listener, which would need to trigger an update of all active media state data.
    public static IEnumerable<Guid> GetMediaSkinOptionalMIATypes(string navigationMode)
    {
      IScreenManager screenManager = ServiceRegistration.Get<IScreenManager>();
      return GetMediaSkinOptionalMIATypes(navigationMode, screenManager.CurrentSkinResourceBundle);
    }

    public static IEnumerable<Guid> GetMediaSkinOptionalMIATypes(string navigationMode, ISkinResourceBundle bundle)
    {
      if (bundle == null)
        return EMPTY_GUID_LIST;
      string skinName;
      string themeName;
      GetSkinAndThemeName(bundle, out skinName, out themeName);
      IPluginManager pluginManager = ServiceRegistration.Get<IPluginManager>();
      string registrationLocation = Consts.MEDIA_SKIN_SETTINGS_REGISTRATION_PATH + "/" + skinName + "/";
      if (!string.IsNullOrEmpty(themeName))
        registrationLocation += themeName + "/";
      registrationLocation += navigationMode + "/" + Consts.MEDIA_SKIN_SETTINGS_REGISTRATION_OPTIONAL_TYPES_PATH;
      IEnumerable<Guid> result = pluginManager.RequestAllPluginItems<MIATypeRegistration>(
          registrationLocation, _mediaSkinMIATypeRegistrationStateTracker).Select(registration => registration.MediaItemAspectTypeId);
      pluginManager.RevokeAllPluginItems(registrationLocation, _mediaSkinMIATypeRegistrationStateTracker);
      return result.Union(GetMediaSkinOptionalMIATypes(navigationMode, bundle.InheritedSkinResources));
    }

    /// <summary>
    /// Returns context variables to be set for the given workflow state id.
    /// </summary>
    /// <param name="workflowStateId">Workflow state which determines the root media navigation state.</param>
    /// <returns>Mapping of context variable keys to values.</returns>
    protected static IDictionary<string, object> PrepareRootState(Guid workflowStateId)
    {
      IDictionary<string, object> result = new Dictionary<string, object>();
      // The initial state ID determines the media model "part" to initialize: Browse local media, browse media library, audio, videos or images.
      // The media model part determines the media navigation mode and the view contents to be set.
      if (!_initializers.ContainsKey(workflowStateId)) 
        return result;

      // Use the IMediaNavigationInitializer that is associated with our root workflow state.
      IMediaNavigationInitializer initializer = _initializers[workflowStateId];
      string mode;
      NavigationData navigationData;
      initializer.InitMediaNavigation(out mode, out navigationData);
      result.Add(Consts.KEY_NAVIGATION_MODE, mode);
      result.Add(Consts.KEY_NAVIGATION_DATA, navigationData);
      return result;
    }

    /// <summary>
    /// Prepares the given workflow navigation <paramref name="context"/>, i.e. prepares the view data and the
    /// available filter criteria to be used in the menu.
    /// </summary>
    protected void PrepareState(NavigationContext context)
    {
      _currentNavigationContext = context;
      NavigationData navigationData = GetNavigationData(context, false);
      if (navigationData != null)
        return;
      // Initialize root media navigation state. We will set up all sub processes for each media model "part", i.e.
      // audio, videos, images, browse local media and browse media library.
      IDictionary<string, object> contextVariables = PrepareRootState(context.WorkflowState.StateId);
      foreach (KeyValuePair<string, object> variable in contextVariables)
        context.SetContextVariable(variable.Key, variable.Value);
    }

    protected void ReleaseModelData()
    {
      _currentNavigationContext = null;
    }

    /// <summary>
    /// Custom handling of UI state: because the layout changes before the screen tranition happens, the UI state will not be stored
    /// at correct time (see WorkflowSaveRestoreStateAction). So we save it here before changing the layout and leaving the screen.
    /// </summary>
    /// <param name="context">NavigationContext</param>
    protected void SaveUIState(NavigationContext context)
    {
      // Mapping of context variable name -> UI state
      string contextVariable = "Root";
      IDictionary<string, IDictionary<string, object>> state =
          (IDictionary<string, IDictionary<string, object>>)context.GetContextVariable(contextVariable, false) ??
          new Dictionary<string, IDictionary<string, object>>(10);

      var sm = ServiceRegistration.Get<IScreenManager>() as ScreenManager;
      if (sm == null)
        return;
      var screen = sm.FocusedScreen;
      if (screen == null)
        return;

      // Mapping of element paths -> element states
      IDictionary<string, object> screenStateDictionary = new Dictionary<string, object>(1000);
      string screenName = screen.ResourceName;
      state[screenName] = screenStateDictionary;
      screen.SaveUIState(screenStateDictionary, string.Empty);
      context.SetContextVariable(contextVariable, state);
      // Indicate that state is already persisted
      context.SetContextVariable("Root_persisted", (bool?)true);
    }

    #endregion

    #region IWorkflowModel implementation

    public Guid ModelId
    {
      get { return MEDIA_MODEL_ID; }
    }

    public bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
    {
      return true;
    }

    public void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      PrepareState(newContext);
      NavigationData navigationData = GetNavigationData(newContext, false);
      navigationData.Enable();
    }

    public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      NavigationData navigationData = GetNavigationData(oldContext, false);
      navigationData.Dispose();
      ReleaseModelData();
    }

    public void ChangeModelContext(NavigationContext oldContext, NavigationContext newContext, bool push)
    {
      PrepareState(newContext);
      if (push)
      {
        NavigationData navigationData = GetNavigationData(oldContext, false);
        navigationData.Disable();
        SaveUIState(oldContext);
        navigationData = GetNavigationData(newContext, false);
        navigationData.Enable();
      }
      else
      {
        NavigationData navigationData = GetNavigationData(oldContext, false);
        navigationData.Dispose();
        navigationData = GetNavigationData(newContext, false);
        navigationData.Enable();
      }
    }

    public void Deactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      // Don't disable the current navigation data when we leave our model - the last navigation data must be
      // available in sub workflows, for example to make the GetMediaItemsFromCurrentView method work
    }

    public void Reactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      // The last navigation data was not disabled so we don't need to enable it here
    }

    public void UpdateMenuActions(NavigationContext context, IDictionary<Guid, WorkflowAction> actions)
    {
      PrepareState(context);
      ICollection<WorkflowAction> dynamicWorkflowActions = NavigationData.DynamicWorkflowActions;
      if (dynamicWorkflowActions != null)
        foreach (WorkflowAction action in dynamicWorkflowActions)
          actions.Add(action.ActionId, action);
    }

    public ScreenUpdateMode UpdateScreen(NavigationContext context, ref string screen)
    {
      screen = NavigationData.CurrentScreenData.Screen;
      return ScreenUpdateMode.AutoWorkflowManager;
    }

    #endregion
  }
}
