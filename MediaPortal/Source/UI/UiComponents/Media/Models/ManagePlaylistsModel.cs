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
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using MediaPortal.Core;
using MediaPortal.Core.Commands;
using MediaPortal.Core.Exceptions;
using MediaPortal.Core.General;
using MediaPortal.Core.Localization;
using MediaPortal.Core.Logging;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.MediaManagement.DefaultItemAspects;
using MediaPortal.Core.Messaging;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.UiNotifications;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UI.ServerCommunication;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.Utilities;

namespace MediaPortal.UiComponents.Media.Models
{
  public enum PlaylistLocation
  {
    None,
    Local,
    Server
  }

  public class ManagePlaylistsModel : IWorkflowModel
  {
    #region Consts

    public const string MODEL_ID_STR = "039151B6-800B-4279-A1BE-7F421EEA8C9A";
    public static readonly Guid MODEL_ID = new Guid(MODEL_ID_STR);

    #endregion

    #region Protected fields

    protected object _syncObj = new object();
    protected bool _updatingProperties = false;

    protected LocalPlaylists _localPlaylistsHandler = null;
    protected AbstractProperty _isHomeServerConnectedProperty;
    protected AbstractProperty _isPlaylistsSelectedProperty;
    protected AbstractProperty _playlistNameProperty;
    protected AbstractProperty _isPlaylistNameValidProperty;
    protected PlaylistBase _playlist = null;
    protected PlaylistLocation _playlistLocation = PlaylistLocation.None;

    protected string _message = string.Empty;
    protected ItemsList _localPlaylists = null;
    protected ItemsList _serverPlaylists = null;
    protected ItemsList _playlistLocationList = null;

    protected AsynchronousMessageQueue _messageQueue = null;

    #endregion

    #region Ctor

    public ManagePlaylistsModel()
    {
      _isHomeServerConnectedProperty = new WProperty(typeof(bool), false);
      _isPlaylistsSelectedProperty = new WProperty(typeof(bool), false);
      _playlistNameProperty = new WProperty(typeof(string), string.Empty);
      _playlistNameProperty.Attach(OnPlaylistNameChanged);
      _isPlaylistNameValidProperty = new WProperty(typeof(bool), false);
    }

    #endregion

    void OnPlaylistNameChanged(AbstractProperty prop, object oldVal)
    {
      IsPlaylistNameValid = !string.IsNullOrEmpty(PlaylistName);
      if (_playlist != null)
        _playlist.Name = PlaylistName;
    }

    void SubscribeToMessages()
    {
      AsynchronousMessageQueue messageQueue = new AsynchronousMessageQueue(this, new string[]
        {
           ServerConnectionMessaging.CHANNEL,
        });
      messageQueue.MessageReceived += OnMessageReceived;
      messageQueue.Start();
      lock (_syncObj)
        _messageQueue = messageQueue;
    }

    void UnsubscribeFromMessages()
    {
      AsynchronousMessageQueue messageQueue;
      lock (_syncObj)
      {
        messageQueue = _messageQueue;
        _messageQueue = null;
      }
      if (messageQueue == null)
        return;
      messageQueue.Shutdown();
    }

    void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == ServerConnectionMessaging.CHANNEL)
      {
        ServerConnectionMessaging.MessageType messageType =
            (ServerConnectionMessaging.MessageType) message.MessageType;
        switch (messageType)
        {
          case ServerConnectionMessaging.MessageType.HomeServerAttached:
          case ServerConnectionMessaging.MessageType.HomeServerDetached:
          case ServerConnectionMessaging.MessageType.HomeServerConnected:
            UpdateProperties();
            UpdatePlaylists(false);
            break;
          case ServerConnectionMessaging.MessageType.HomeServerDisconnected:
            if (_playlistLocation == PlaylistLocation.Server)
              NavigateRemovePlaylistSaveWorkflow();
            else
            {
              UpdateProperties();
              UpdatePlaylists(false);
            }
            break;
        }
      }
    }

    void OnPlaylistItemSelectionChanged(AbstractProperty prop, object oldVal)
    {
      UpdateIsPlaylistSelected();
    }

    #region Public properties

    public AbstractProperty IsHomeServerConnectedProperty
    {
      get { return _isHomeServerConnectedProperty; }
    }

    public bool IsHomeServerConnected
    {
      get { return (bool) _isHomeServerConnectedProperty.GetValue(); }
      set { _isHomeServerConnectedProperty.SetValue(value); }
    }

    /// <summary>
    /// List of all local playlists to be displayed in the screens.
    /// </summary>
    public ItemsList LocalPlaylists
    {
      get
      {
        lock (_syncObj)
          return _localPlaylists;
      }
    }

    /// <summary>
    /// List of all server playlists to be displayed in the screens.
    /// </summary>
    public ItemsList ServerPlaylists
    {
      get
      {
        lock (_syncObj)
          return _serverPlaylists;
      }
    }

    /// <summary>
    /// List of locations where the playlist can be saved.
    /// </summary>
    public ItemsList PlaylistLocationList
    {
      get
      {
        lock (_syncObj)
          return _playlistLocationList;
      }
    }

    public AbstractProperty IsPlaylistsSelectedProperty
    {
      get { return _isPlaylistsSelectedProperty; }
    }

    /// <summary>
    /// <c>true</c> if at least one playlist of the local playlists or of the server playlists is selected.
    /// </summary>
    public bool IsPlaylistsSelected
    {
      get { return (bool) _isPlaylistsSelectedProperty.GetValue(); }
      set { _isPlaylistsSelectedProperty.SetValue(value); }
    }

    public AbstractProperty PlaylistNameProperty
    {
      get { return _playlistNameProperty; }
    }

    /// <summary>
    /// Gets or sets the name of the currently edited playlist.
    /// </summary>
    public string PlaylistName
    {
      get { return (string) _playlistNameProperty.GetValue(); }
      set { _playlistNameProperty.SetValue(value); }
    }

    public AbstractProperty IsPlaylistNameValidProperty
    {
      get { return _isPlaylistNameValidProperty; }
    }

    /// <summary>
    /// Gets the information if the currently set playlist name is valid, i.e. can be used. The workflow may only continue
    /// when the name is valid.
    /// </summary>
    public bool IsPlaylistNameValid
    {
      get { return (bool) _isPlaylistNameValidProperty.GetValue(); }
      set { _isPlaylistNameValidProperty.SetValue(value); }
    }

    /// <summary>
    /// Gets the error message for screen "SavePlaylistFailed".
    /// </summary>
    public string Message
    {
      get { return _message; }
    }

    /// <summary>
    /// Returns the current playlist (in Info and Save workflows).
    /// </summary>
    public PlaylistBase Playlist
    {
      get { return _playlist; }
    }

    /// <summary>
    /// Returns the current playlist location (in Info and Save workflows).
    /// </summary>
    public PlaylistLocation PlaylistLocation
    {
      get { return _playlistLocation; }
    }

    #endregion

    #region Public methods

    public void RemoveSelectedPlaylistsAndFinish()
    {
      try
      {
        _localPlaylistsHandler.RemovePlaylists(GetSelectedLocalPlaylists());
        Models.ServerPlaylists.RemovePlaylists(GetSelectedServerPlaylists());
        UpdatePlaylists(false);
        NavigateBackToOverview();
      }
      catch (NotConnectedException)
      {
        DisconnectedError();
      }
    }

    public void SavePlaylistSetLocationAndContinue()
    {
      _playlistLocation = GetSelectedPlaylistLocation();
      if (_playlistLocation == PlaylistLocation.None)
        return;
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      workflowManager.NavigatePush(Consts.WF_STATE_ID_PLAYLIST_SAVE_EDIT_NAME);
    }

    public void SavePlaylistAndFinish()
    {
      PlaylistRawData playlistData = (PlaylistRawData) _playlist;
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      try
      {
        if (_playlistLocation == PlaylistLocation.Local)
        {
          string fileName;
          _localPlaylistsHandler.SavePlaylist(playlistData, out fileName);
          _message = LocalizationHelper.Translate(Consts.RES_SAVE_PLAYLIST_LOCAL_SUCCESSFUL_TEXT, fileName);
          workflowManager.NavigatePush(Consts.WF_STATE_ID_PLAYLIST_SAVE_SUCCESSFUL);
        }
        else
        {
          Models.ServerPlaylists.SavePlaylist(playlistData);
          _message = LocalizationHelper.Translate(Consts.RES_SAVE_PLAYLIST_SERVER_SUCCESSFUL_TEXT);
          workflowManager.NavigatePush(Consts.WF_STATE_ID_PLAYLIST_SAVE_SUCCESSFUL);
        }
      }
      catch (Exception e)
      {
        _message = LocalizationHelper.Translate(Consts.RES_SAVE_PLAYLIST_FAILED_TEXT, e.Message);
        workflowManager.NavigatePush(Consts.WF_STATE_ID_PLAYLIST_SAVE_FAILED);
      }
    }

    public void LoadPlaylist()
    {
      IDialogManager dialogManager = ServiceRegistration.Get<IDialogManager>();
      if (_playlist == null)
      {
        dialogManager.ShowDialog(SkinBase.General.Consts.RES_SYSTEM_ERROR, Consts.RES_PLAYLIST_LOAD_NO_PLAYLIST, DialogType.OkDialog, false, null);
        return;
      }
      IContentDirectory cd = ServiceRegistration.Get<IServerConnectionManager>().ContentDirectory;
      AVType? avType = ConvertPlaylistTypeToAVType(_playlist.PlaylistType);
      if (cd == null || !avType.HasValue)
      {
        dialogManager.ShowDialog(SkinBase.General.Consts.RES_SYSTEM_ERROR, Consts.RES_PLAYLIST_LOAD_ERROR_LOADING, DialogType.OkDialog, false, null);
        return;
      }
      Guid[] necessaryMIATypes = new Guid[]
          {
              ProviderResourceAspect.ASPECT_ID,
              MediaAspect.ASPECT_ID,
          };
      Guid[] optionalMIATypes = new Guid[]
          {
              AudioAspect.ASPECT_ID,
              VideoAspect.ASPECT_ID,
              PictureAspect.ASPECT_ID,
          };
      IList<MediaItem> mediaItems;
      switch (_playlistLocation)
      {
        case PlaylistLocation.Local:
          PlaylistRawData playlistData = _playlist as PlaylistRawData;
          if (playlistData == null)
            return;
          mediaItems = cd.LoadCustomPlaylist(playlistData.MediaItemIds, necessaryMIATypes, null);
          break;
        case PlaylistLocation.Server:
          PlaylistContents playlistContents = cd.LoadServerPlaylist(_playlist.PlaylistId, necessaryMIATypes, optionalMIATypes);
          mediaItems = playlistContents.ItemList;
          break;
        default:
          throw new NotImplementedException(string.Format("No handler for PlaylistLocation {0}", _playlistLocation));
      }
      INotificationService notificationService = ServiceRegistration.Get<INotificationService>();
      // Add notification if not all media items could be loaded
      if (mediaItems.Count == 0)
      {
        DefaultNotification notification = new DefaultNotification(NotificationType.Info,
            Consts.RES_PLAYLIST_LOAD_ITEMS_MISSING_TITLE, Consts.RES_PLAYLIST_LOAD_ALL_ITEMS_MISSING_TEXT)
          {
              Timeout = DateTime.Now + Consts.TS_PLAYLIST_LOAD_ITEMS_MISSING_NOTIFICATION
          };
        notificationService.EnqueueNotification(notification, false);
        // No further processing - we don't have any items to load
        return;
      }
      if (_playlist.NumItems > mediaItems.Count)
      {
        DefaultNotification notification = new DefaultNotification(NotificationType.Info,
            Consts.RES_PLAYLIST_LOAD_ITEMS_MISSING_TITLE, Consts.RES_PLAYLIST_LOAD_SOME_ITEMS_MISSING_TEXT)
          {
              Timeout = DateTime.Now + Consts.TS_PLAYLIST_LOAD_ITEMS_MISSING_NOTIFICATION
          };
        notificationService.EnqueueNotification(notification, false);
      }
      PlayItemsModel.CheckQueryPlayAction(() => mediaItems, avType.Value);
    }

    public void NavigateRemovePlaylistSaveWorkflow()
    {
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      workflowManager.NavigatePopToState(Consts.WF_STATE_ID_PLAYLIST_SAVE_CHOOSE_LOCATION, true);
    }

    public void NavigateBackToOverview()
    {
      lock (_syncObj)
      {
        _playlist = null;
        _playlistLocation = PlaylistLocation.None;
      }
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      workflowManager.NavigatePopToState(Consts.WF_STATE_ID_PLAYLISTS_OVERVIEW, false);
    }

    public static void ShowPlaylistsOverview()
    {
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      workflowManager.NavigatePush(Consts.WF_STATE_ID_MANAGE_PLAYLISTS);
    }

    public static void ShowPlaylistInfo(PlaylistBase playlistData, PlaylistLocation location)
    {
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      workflowManager.NavigatePush(Consts.WF_STATE_ID_PLAYLIST_INFO, new NavigationContextConfig
        {
            AdditionalContextVariables = new Dictionary<string, object>
              {
                  {Consts.KEY_PLAYLIST_DATA, playlistData},
                  {Consts.KEY_PLAYLIST_LOCATION, location}
              }
        });
    }

    public static void SaveCurrentPlaylist()
    {
      IPlayerContextManager pcm = ServiceRegistration.Get<IPlayerContextManager>();
      IPlayerContext pc = pcm.GetPlayerContext(PlayerChoice.CurrentPlayer);
      IPlaylist playlist = pc == null ? null : pc.Playlist;
      if (playlist == null)
      {
        ServiceRegistration.Get<ILogger>().Warn("ManagePlaylistsModel: No playlist available to save");
        return;
      }
      PlaylistRawData playlistData = new PlaylistRawData(Guid.NewGuid(), string.Empty, ConvertAVTypeToPlaylistType(pc.AVType));
      playlist.ExportPlaylistRawData(playlistData);
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      workflowManager.NavigatePush(Consts.WF_STATE_ID_PLAYLIST_SAVE_CHOOSE_LOCATION, new NavigationContextConfig
        {
            AdditionalContextVariables = new Dictionary<string, object>
              {
                  {Consts.KEY_PLAYLIST_DATA, playlistData}
              }
        });
    }

    #endregion

    protected static AVType? ConvertPlaylistTypeToAVType(string playlistType)
    {
      return (AVType?) Enum.Parse(typeof(AVType), playlistType);
    }

    protected static string ConvertAVTypeToPlaylistType(AVType avType)
    {
      return avType.ToString();
    }

    protected void UpdatePlaylistLocations()
    {
      lock (_syncObj)
      {
        if (_playlistLocationList == null)
          _playlistLocationList = new ItemsList();

        ListItem locationItem = new ListItem(Consts.KEY_NAME, Consts.RES_SAVE_PL_LOCALLY);
        locationItem.AdditionalProperties[Consts.KEY_PLAYLIST_LOCATION] = PlaylistLocation.Local;
        locationItem.Command = new MethodDelegateCommand(() => SavePlaylistChooseLocation(PlaylistLocation.Local));
        locationItem.Selected = true;
        _playlistLocationList.Add(locationItem);

        if (IsHomeServerConnected)
        {
          locationItem = new ListItem(Consts.KEY_NAME, Consts.RES_SAVE_PL_AT_SERVER);
          locationItem.AdditionalProperties[Consts.KEY_PLAYLIST_LOCATION] = PlaylistLocation.Server;
          locationItem.Command = new MethodDelegateCommand(() => SavePlaylistChooseLocation(PlaylistLocation.Server));
          _playlistLocationList.Add(locationItem);
        }

        _playlistLocationList.FireChange();
      }
    }

    protected ICollection<Guid> GetSelectedPlaylists(ItemsList playlistsList)
    {
      ICollection<Guid> result = new List<Guid>();
      lock (_syncObj)
        foreach (ListItem playlistItem in playlistsList)
          if (playlistItem.Selected)
            result.Add(((PlaylistBase) playlistItem.AdditionalProperties[Consts.KEY_PLAYLIST_DATA]).PlaylistId);
      return result;
    }

    protected ICollection<Guid> GetSelectedLocalPlaylists()
    {
      return GetSelectedPlaylists(_localPlaylists);
    }

    protected ICollection<Guid> GetSelectedServerPlaylists()
    {
      return GetSelectedPlaylists(_serverPlaylists);
    }

    protected PlaylistLocation GetSelectedPlaylistLocation()
    {
      foreach (ListItem item in _playlistLocationList)
        if (item.Selected)
          return (PlaylistLocation) item.AdditionalProperties[Consts.KEY_PLAYLIST_LOCATION];
      return PlaylistLocation.None;
    }

    protected void UpdateIsPlaylistSelected()
    {
      IsPlaylistsSelected = GetSelectedLocalPlaylists().Count > 0 || GetSelectedServerPlaylists().Count > 0;
    }

    protected void UpdatePlaylists(bool create)
    {
      lock (_syncObj)
      {
        if (_updatingProperties)
          return;
        _updatingProperties = true;
        if (create)
          _localPlaylists = new ItemsList();
        if (create)
          _serverPlaylists = new ItemsList();
      }
      try
      {
        List<PlaylistBase> localPlaylists = new List<PlaylistBase>();
        CollectionUtils.AddAll(localPlaylists, _localPlaylistsHandler.Playlists);
        List<PlaylistBase> serverPlaylists = new List<PlaylistBase>();
        try
        {
          if (IsHomeServerConnected)
            CollectionUtils.AddAll(serverPlaylists, Models.ServerPlaylists.GetPlaylists());
        }
        catch (NotConnectedException) { }
        int numPlaylists = localPlaylists.Count + serverPlaylists.Count;
        UpdatePlaylists(_localPlaylists, localPlaylists, PlaylistLocation.Local, numPlaylists == 1);
        UpdatePlaylists(_serverPlaylists, serverPlaylists, PlaylistLocation.Server, numPlaylists == 1);
        IsPlaylistsSelected = numPlaylists == 1;
      }
      finally
      {
        lock (_syncObj)
          _updatingProperties = false;
      }
    }

    protected AVType ParseAVType(string avTypeStr)
    {
      if (avTypeStr == AVType.Audio.ToString())
        return AVType.Audio;
      if (avTypeStr == AVType.Video.ToString())
        return AVType.Video;
      return AVType.None;
    }

    protected void UpdatePlaylists(ItemsList list, List<PlaylistBase> playlistsData,
        PlaylistLocation location, bool selectFirstItem)
    {
      list.Clear();
      bool selectPlaylist = selectFirstItem;
      playlistsData.Sort((a, b) => a.Name.CompareTo(b.Name));
      foreach (PlaylistBase playlistData in playlistsData)
      {
        AVType? avType = ConvertPlaylistTypeToAVType(playlistData.PlaylistType);
        if (!avType.HasValue)
          continue;
        ListItem playlistItem = new ListItem(Consts.KEY_NAME, playlistData.Name);
        playlistItem.AdditionalProperties[Consts.KEY_PLAYLIST_AV_TYPE] = avType.Value;
        playlistItem.AdditionalProperties[Consts.KEY_PLAYLIST_DATA] = playlistData;
        playlistItem.AdditionalProperties[Consts.KEY_PLAYLIST_LOCATION] = location;
        PlaylistBase plCopy = playlistData;
        playlistItem.Command = new MethodDelegateCommand(() => ShowPlaylistInfo(plCopy, location));
        if (selectPlaylist)
        {
          selectPlaylist = false;
          playlistItem.Selected = true;
        }
        playlistItem.SelectedProperty.Attach(OnPlaylistItemSelectionChanged);
        lock (_syncObj)
          list.Add(playlistItem);
      }
      list.FireChange();
    }

    protected void UpdateProperties()
    {
      lock (_syncObj)
      {
        if (_updatingProperties)
          return;
        _updatingProperties = true;
      }
      try
      {
        IServerConnectionManager serverConnectionManager = ServiceRegistration.Get<IServerConnectionManager>();
        IsHomeServerConnected = serverConnectionManager.IsHomeServerConnected;

        _localPlaylistsHandler = new LocalPlaylists();
        _localPlaylistsHandler.Refresh();

        UpdatePlaylistLocations();
      }
      finally
      {
        lock (_syncObj)
          _updatingProperties = false;
      }
    }

    protected void ClearData()
    {
      lock (_syncObj)
      {
        // Albert: Don't set this property here to avoid premature screen update
        // IsHomeServerConnected = false;
        IsPlaylistsSelected = false;
        PlaylistName = string.Empty;
        IsPlaylistsSelected = false;
        _localPlaylists = null;
        _serverPlaylists = null;
        _playlistLocationList = null;
        _localPlaylistsHandler = null;
      }
    }

    protected void DisconnectedError()
    {
      // Called when a remote call crashes because the server was disconnected. We don't do anything here because
      // we automatically move to the overview state in the OnMessageReceived method when the server disconnects.
    }

    protected void PrepareState(NavigationContext navigationContext, bool push)
    {
      Guid workflowStateId = navigationContext.WorkflowState.StateId;
      if (workflowStateId == Consts.WF_STATE_ID_PLAYLISTS_OVERVIEW)
      {
        UpdatePlaylists(true);
      }
      if (workflowStateId == Consts.WF_STATE_ID_PLAYLISTS_REMOVE)
      {
        UpdatePlaylists(true);
      }
      else if (workflowStateId == Consts.WF_STATE_ID_PLAYLIST_INFO)
      {
        _playlist = GetCurrentPlaylist(navigationContext);
        PlaylistLocation? location = GetCurrentPlaylistLocation(navigationContext);
        _playlistLocation = location.HasValue ? location.Value : PlaylistLocation.Local;
      }
      if (!push)
        return;
      if (workflowStateId == Consts.WF_STATE_ID_PLAYLIST_SAVE_CHOOSE_LOCATION)
      {
        _playlist = GetCurrentPlaylist(navigationContext);
        _playlistLocation = PlaylistLocation.None;
      }
      else if (workflowStateId == Consts.WF_STATE_ID_PLAYLIST_SAVE_EDIT_NAME)
      {
        // Nothing to do
      }
      else if (workflowStateId == Consts.WF_STATE_ID_PLAYLIST_SAVE_SUCCESSFUL)
      {
        // Nothing to do
      }
      else if (workflowStateId == Consts.WF_STATE_ID_PLAYLIST_SAVE_FAILED)
      {
        // Nothing to do
      }
    }

    protected PlaylistBase GetCurrentPlaylist(NavigationContext navigationContext)
    {
      return (PlaylistBase) navigationContext.GetContextVariable(Consts.KEY_PLAYLIST_DATA, false);
    }

    protected PlaylistLocation? GetCurrentPlaylistLocation(NavigationContext navigationContext)
    {
      return (PlaylistLocation?) navigationContext.GetContextVariable(Consts.KEY_PLAYLIST_LOCATION, false);
    }

    public void SavePlaylistChooseLocation(PlaylistLocation location)
    {
      _playlistLocation = location;
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      workflowManager.NavigatePush(Consts.WF_STATE_ID_PLAYLIST_SAVE_EDIT_NAME);
    }

    #region IWorkflowModel implementation

    public Guid ModelId
    {
      get { return MODEL_ID; }
    }

    public bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
    {
      Guid workflowStateId = newContext.WorkflowState.StateId;
      if (workflowStateId == Consts.WF_STATE_ID_PLAYLISTS_OVERVIEW)
        return true;
      if (workflowStateId == Consts.WF_STATE_ID_PLAYLISTS_REMOVE)
        return true;
      if (workflowStateId == Consts.WF_STATE_ID_PLAYLIST_INFO)
        return GetCurrentPlaylist(newContext) != null && GetCurrentPlaylistLocation(newContext) != null;
      if (workflowStateId == Consts.WF_STATE_ID_PLAYLIST_SAVE_CHOOSE_LOCATION)
        return GetCurrentPlaylist(newContext) is PlaylistRawData;
      if (workflowStateId == Consts.WF_STATE_ID_PLAYLIST_SAVE_EDIT_NAME)
        return true;
      if (workflowStateId == Consts.WF_STATE_ID_PLAYLIST_SAVE_SUCCESSFUL)
        return true;
      if (workflowStateId == Consts.WF_STATE_ID_PLAYLIST_SAVE_FAILED)
        return true;
      return false;
    }

    public void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      SubscribeToMessages();
      ClearData();
      UpdateProperties();
      PrepareState(newContext, true);
    }

    public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      UnsubscribeFromMessages();
      ClearData();
    }

    public void ChangeModelContext(NavigationContext oldContext, NavigationContext newContext, bool push)
    {
      PrepareState(newContext, push);
    }

    public void Deactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      // Nothing to do here
    }

    public void ReActivate(NavigationContext oldContext, NavigationContext newContext)
    {
      PrepareState(newContext, false);
    }

    public void UpdateMenuActions(NavigationContext context, IDictionary<Guid, WorkflowAction> actions)
    {
      // Nothing to do here
    }

    public ScreenUpdateMode UpdateScreen(NavigationContext context, ref string screen)
    {
      return ScreenUpdateMode.AutoWorkflowManager;
    }

    #endregion
  }
}
