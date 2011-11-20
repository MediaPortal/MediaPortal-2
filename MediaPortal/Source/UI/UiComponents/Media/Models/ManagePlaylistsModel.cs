#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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
using MediaPortal.Common.Exceptions;
using MediaPortal.Common.General;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.Messaging;
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
  public class ManagePlaylistsModel : IWorkflowModel
  {
    #region Consts

    public const string MODEL_ID_STR = "039151B6-800B-4279-A1BE-7F421EEA8C9A";
    public static readonly Guid MODEL_ID = new Guid(MODEL_ID_STR);

    #endregion

    #region Protected fields

    protected object _syncObj = new object();
    protected bool _updatingProperties = false;

    protected AbstractProperty _isHomeServerConnectedProperty;
    protected AbstractProperty _isPlaylistsSelectedProperty;
    protected AbstractProperty _playlistNameProperty;
    protected AbstractProperty _isPlaylistNameValidProperty;
    protected PlaylistBase _playlist = null;

    protected string _message = string.Empty;
    protected ItemsList _playlists = null;

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
      string trimmedName = PlaylistName.Trim();
      IsPlaylistNameValid = !string.IsNullOrEmpty(trimmedName);
      if (_playlist != null)
        _playlist.Name = trimmedName;
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
            if (!NavigateBackToOverview())
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
    /// List of all playlists to be displayed in the screens.
    /// </summary>
    public ItemsList Playlists
    {
      get
      {
        lock (_syncObj)
          return _playlists;
      }
    }

    public AbstractProperty IsPlaylistsSelectedProperty
    {
      get { return _isPlaylistsSelectedProperty; }
    }

    /// <summary>
    /// <c>true</c> if at least one playlist is selected.
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

    #endregion

    #region Public methods

    public void RemoveSelectedPlaylistsAndFinish()
    {
      try
      {
       ServerPlaylists.RemovePlaylists(GetSelectedPlaylists());
        UpdatePlaylists(false);
        NavigateBackToOverview();
      }
      catch (NotConnectedException)
      {
        DisconnectedError();
      }
    }

    public void SavePlaylistAndFinish()
    {
      PlaylistRawData playlistData = (PlaylistRawData) _playlist;
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      try
      {
        if (ServerPlaylists.GetPlaylists().Any(p => p.Name == _playlist.Name))
          SaveFailed(LocalizationHelper.Translate(Consts.RES_SAVE_PLAYLIST_FAILED_PLAYLIST_ALREADY_EXISTS, _playlist.Name));
        else
        {
          ServerPlaylists.SavePlaylist(playlistData);
          _message = LocalizationHelper.Translate(Consts.RES_SAVE_PLAYLIST_SUCCESSFUL_TEXT);
          workflowManager.NavigatePush(Consts.WF_STATE_ID_PLAYLIST_SAVE_SUCCESSFUL);
        }
      }
      catch (Exception e)
      {
        SaveFailed(LocalizationHelper.Translate(Consts.RES_SAVE_PLAYLIST_FAILED_TEXT, e.Message));
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
      PlaylistContents playlistContents = cd.LoadServerPlaylist(_playlist.PlaylistId, necessaryMIATypes, optionalMIATypes);
      IList<MediaItem> mediaItems = playlistContents.ItemList;
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
      if (!workflowManager.IsStateContainedInNavigationStack(Consts.WF_STATE_ID_PLAYLIST_SAVE_EDIT_NAME))
        return;
      ClearData();
      workflowManager.NavigatePopToState(Consts.WF_STATE_ID_PLAYLIST_SAVE_EDIT_NAME, true);
    }

    public bool NavigateBackToOverview()
    {
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      if (!workflowManager.IsStateContainedInNavigationStack(Consts.WF_STATE_ID_PLAYLISTS_OVERVIEW))
        return false;
      ClearData();
      workflowManager.NavigatePopToState(Consts.WF_STATE_ID_PLAYLISTS_OVERVIEW, false);
      return true;
    }

    public static void ShowPlaylistsOverview()
    {
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      workflowManager.NavigatePush(Consts.WF_STATE_ID_PLAYLISTS_OVERVIEW);
    }

    public static void ShowPlaylistInfo(PlaylistBase playlistData)
    {
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      workflowManager.NavigatePush(Consts.WF_STATE_ID_PLAYLIST_INFO, new NavigationContextConfig
        {
            AdditionalContextVariables = new Dictionary<string, object>
              {
                  {Consts.KEY_PLAYLIST_DATA, playlistData}
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
      SavePlaylist(playlistData);
    }

    public static void SavePlaylist(PlaylistRawData playlistData)
    {
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      if (ContainsLocalMediaItems(playlistData))
        SaveFailed(Consts.RES_SAVE_PLAYLIST_FAILED_LOCAL_MEDIAITEMS_TEXT);
      else
        workflowManager.NavigatePush(Consts.WF_STATE_ID_PLAYLIST_SAVE_EDIT_NAME, new NavigationContextConfig
          {
              AdditionalContextVariables = new Dictionary<string, object>
                {
                    {Consts.KEY_PLAYLIST_DATA, playlistData}
                }
          });
    }

    public static void SaveFailed(string errorMessage)
    {
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      workflowManager.NavigatePush(Consts.WF_STATE_ID_PLAYLIST_SAVE_FAILED, new NavigationContextConfig
          {
              AdditionalContextVariables = new Dictionary<string, object>
                {
                    {Consts.KEY_MESSAGE, errorMessage}
                }
          });
    }

    #endregion

    protected static bool ContainsLocalMediaItems(PlaylistRawData playlistData)
    {
      return playlistData.MediaItemIds.Any(mediaItemId => mediaItemId == Guid.Empty);
    }

    protected static AVType? ConvertPlaylistTypeToAVType(string playlistType)
    {
      return (AVType?) Enum.Parse(typeof(AVType), playlistType);
    }

    protected static string ConvertAVTypeToPlaylistType(AVType avType)
    {
      return avType.ToString();
    }

    protected ICollection<Guid> GetSelectedPlaylists()
    {
      List<Guid> result = new List<Guid>();
      lock (_syncObj)
        result.AddRange(_playlists.
            Where(playlistItem => playlistItem.Selected).
            Select(playlistItem => ((PlaylistBase) playlistItem.AdditionalProperties[Consts.KEY_PLAYLIST_DATA]).PlaylistId));
      return result;
    }

    protected void UpdateIsPlaylistSelected()
    {
      IsPlaylistsSelected = GetSelectedPlaylists().Count > 0;
    }

    protected void UpdatePlaylists(bool create)
    {
      lock (_syncObj)
      {
        if (_updatingProperties)
          return;
        _updatingProperties = true;
        if (create)
          _playlists = new ItemsList();
      }
      try
      {
        List<PlaylistBase> playlists = new List<PlaylistBase>();
        try
        {
          if (IsHomeServerConnected)
            CollectionUtils.AddAll(playlists, ServerPlaylists.GetPlaylists());
        }
        catch (NotConnectedException) { }
        int numPlaylists = playlists.Count;
        UpdatePlaylists(_playlists, playlists, numPlaylists == 1);
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

    protected void UpdatePlaylists(ItemsList list, List<PlaylistBase> playlistsData, bool selectFirstItem)
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
        PlaylistBase plCopy = playlistData;
        playlistItem.Command = new MethodDelegateCommand(() => ShowPlaylistInfo(plCopy));
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
        _playlists = null;
      }
    }

    protected void DisconnectedError()
    {
      // Called when a remote call crashes because the server was disconnected. We don't do anything here because
      // we automatically move to the overview state in the OnMessageReceived method when the server disconnects.
    }

    protected void PrepareState(NavigationContext navigationContext, bool push)
    {
      UpdateProperties();
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
      }
      if (!push)
        return;
      if (workflowStateId == Consts.WF_STATE_ID_PLAYLIST_SAVE_EDIT_NAME)
      {
        _playlist = GetCurrentPlaylist(navigationContext);
      }
      else if (workflowStateId == Consts.WF_STATE_ID_PLAYLIST_SAVE_SUCCESSFUL)
      {
        // Nothing to do
      }
      else if (workflowStateId == Consts.WF_STATE_ID_PLAYLIST_SAVE_FAILED)
      {
        _message = GetMessage(navigationContext);
      }
    }

    protected string GetMessage(NavigationContext navigationContext)
    {
      return (string) navigationContext.GetContextVariable(Consts.KEY_MESSAGE, false);
    }

    protected PlaylistBase GetCurrentPlaylist(NavigationContext navigationContext)
    {
      return (PlaylistBase) navigationContext.GetContextVariable(Consts.KEY_PLAYLIST_DATA, false);
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
        return GetCurrentPlaylist(newContext) != null;
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

    public void Reactivate(NavigationContext oldContext, NavigationContext newContext)
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
