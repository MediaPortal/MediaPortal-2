#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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
using MediaPortal.Common;
using MediaPortal.Common.Commands;
using MediaPortal.Common.General;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.Settings;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UI.ServerCommunication;
using MediaPortal.UiComponents.Media.Models;
using MediaPortal.UiComponents.PartyMusicPlayer.General;
using MediaPortal.UiComponents.PartyMusicPlayer.Settings;

namespace MediaPortal.UiComponents.PartyMusicPlayer.Models
{
  // TODO:
  // - Avoid that the user chooses RepeatMode "None" because then, the music could end and the party mode would be left
  public class PartyMusicPlayerModel : IWorkflowModel
  {
    #region Consts

    // Global ID definitions and references
    public const string STR_MODEL_ID = "6B3B9024-5B7A-44C0-9B9A-C83FB49FB8D6";

    // ID variables
    public static readonly Guid MODEL_ID = new Guid(STR_MODEL_ID);

    #endregion

    #region Protected fields

    protected AbstractProperty _useEscapePasswordProperty;
    protected AbstractProperty _escapePasswordProperty;
    protected AbstractProperty _disableScreenSaverProperty;
    protected AbstractProperty _playlistNameProperty;
    protected AbstractProperty _playModeProperty;
    protected AbstractProperty _repeatModeProperty;

    protected ItemsList _playlists;
    protected IList<MediaItem>  _mediaItems = null;
    protected Guid _playlistId = Guid.Empty;

    protected ScreenSaverController _screenSaverController = null;
    protected IPlayerContext _playerContext = null;

    #endregion

    public PartyMusicPlayerModel()
    {
      _useEscapePasswordProperty = new WProperty(typeof(bool), true);
      _escapePasswordProperty = new WProperty(typeof(string), null);
      _disableScreenSaverProperty = new WProperty(typeof(bool), true);
      _playlistNameProperty = new WProperty(typeof(string), null);
      _playModeProperty = new WProperty(typeof(PlayMode), PlayMode.Continuous);
      _repeatModeProperty = new WProperty(typeof(RepeatMode), RepeatMode.All);

      _playlists = new ItemsList();

      LoadSettings();
    }

    #region Public members

    public ItemsList Playlists
    {
      get { return _playlists; }
    }

    public AbstractProperty UseEscapePasswordProperty
    {
      get { return _useEscapePasswordProperty; }
    }

    public bool UseEscapePassword
    {
      get { return (bool) _useEscapePasswordProperty.GetValue(); }
      set { _useEscapePasswordProperty.SetValue(value); }
    }

    public AbstractProperty EscapePasswordProperty
    {
      get { return _escapePasswordProperty; }
    }

    public string EscapePassword
    {
      get { return (string) _escapePasswordProperty.GetValue(); }
      set { _escapePasswordProperty.SetValue(value); }
    }

    public AbstractProperty DisableScreenSaverProperty
    {
      get { return _disableScreenSaverProperty; }
    }

    public bool DisableScreenSaver
    {
      get { return (bool) _disableScreenSaverProperty.GetValue(); }
      set { _disableScreenSaverProperty.SetValue(value); }
    }

    public AbstractProperty PlaylistNameProperty
    {
      get { return _playlistNameProperty; }
    }

    public string PlaylistName
    {
      get { return (string) _playlistNameProperty.GetValue(); }
      set { _playlistNameProperty.SetValue(value); }
    }

    public AbstractProperty PlayModeProperty
    {
      get { return _playModeProperty; }
    }

    public PlayMode PlayMode
    {
      get { return (PlayMode) _playModeProperty.GetValue(); }
      set { _playModeProperty.SetValue(value); }
    }

    public AbstractProperty RepeatModeProperty
    {
      get { return _repeatModeProperty; }
    }

    public RepeatMode RepeatMode
    {
      get { return (RepeatMode) _repeatModeProperty.GetValue(); }
      set { _repeatModeProperty.SetValue(value); }
    }

    public void ChoosePlaylist()
    {
      if (!UpdatePlaylists())
        return;
      IScreenManager screenManager = ServiceRegistration.Get<IScreenManager>();
      screenManager.ShowDialog(Consts.DIALOG_CHOOSE_PLAYLIST);
    }

    public void StartPartyMode()
    {
      ServiceRegistration.Get<ILogger>().Info("PartyMusicPlayerModel: Starting party mode");
      SaveSettings();
      if (!LoadPlaylist())
        return;

      LoadPlayRepeatMode();
      
      IPlayerContextManager pcm = ServiceRegistration.Get<IPlayerContextManager>();
      IPlayerContext audioPlayerContext = pcm.OpenAudioPlayerContext(Consts.MODULE_ID_PARTY_MUSIC_PLAYER, Consts.RES_PLAYER_CONTEXT_NAME, false,
          Consts.WF_STATE_ID_PARTY_MUSIC_PLAYER, Consts.WF_STATE_ID_PARTY_MUSIC_PLAYER);
      IPlaylist playlist = audioPlayerContext.Playlist;
      playlist.StartBatchUpdate();
      try
      {
        playlist.Clear();
        foreach (MediaItem mediaItem in _mediaItems)
          playlist.Add(mediaItem);
        playlist.PlayMode = PlayMode;
        playlist.RepeatMode = RepeatMode;
        _playerContext = audioPlayerContext;
      }
      finally
      {
        playlist.EndBatchUpdate();
      }

      audioPlayerContext.Play();

      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      workflowManager.NavigatePushAsync(Consts.WF_STATE_ID_PARTY_MUSIC_PLAYER);
    }

    public void ShowPlaylist()
    {
      ShowPlaylistModel.ShowPlaylist(true);
    }

    /// <summary>
    /// Called from the GUI when the user chooses to leave the party mode.
    /// </summary>
    public void QueryLeavePartyMode()
    {
      ServiceRegistration.Get<ILogger>().Info("PartyMusicPlayerModel: Request to leave party mode");
      if (UseEscapePassword)
      {
        IScreenManager screenManager = ServiceRegistration.Get<IScreenManager>();
        screenManager.ShowDialog(Consts.DIALOG_QUERY_ESCAPE_PASSWORD);
      }
      else
        LeavePartyMode();
    }

    /// <summary>
    /// Called from the GUI to leave the party mode with the given password.
    /// </summary>
    /// <param name="escapePassword">The password which was entered and which is checked against the <see cref="EscapePassword"/>.</param>
    public void TryLeavePartyMode(string escapePassword)
    {
      if (EscapePassword == escapePassword)
        LeavePartyMode();
      else
      {
        IDialogManager dialogManager = ServiceRegistration.Get<IDialogManager>();
        dialogManager.ShowDialog(Consts.RES_WRONG_ESCAPE_PASSWORD_DIALOG_HEADER, Consts.RES_WRONG_ESCAPE_PASSWORD_DIALOG_TEXT, DialogType.OkDialog, false, null);
      }
    }

    public bool LoadPlaylist()
    {
      IServerConnectionManager scm = ServiceRegistration.Get<IServerConnectionManager>();
      IContentDirectory cd = scm.ContentDirectory;
      if (cd == null)
      {
        ShowServerNotConnectedDialog();
        return false;
      }
      PlaylistRawData playlistData = cd.ExportPlaylist(_playlistId);
      _mediaItems = cd.LoadCustomPlaylist(playlistData.MediaItemIds, Consts.NECESSARY_AUDIO_MIAS, Consts.EMPTY_GUID_ENUMERATION);
      return true;
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

    #region Protected members

    protected void LoadSettings()
    {
      ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();
      PartyMusicPlayerSettings settings = settingsManager.Load<PartyMusicPlayerSettings>();
      UseEscapePassword = settings.UseEscapePassword;
      EscapePassword = settings.EscapePassword;
      DisableScreenSaver = settings.DisableScreenSaver;
      PlaylistName = settings.PlaylistName;
      _playlistId = settings.PlaylistId;
      // Don't load the play- and repeat mode here, it's loaded separately
    }

    protected void SaveSettings()
    {
      ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();
      PartyMusicPlayerSettings settings = settingsManager.Load<PartyMusicPlayerSettings>();
      settings.UseEscapePassword = UseEscapePassword;
      settings.EscapePassword = EscapePassword;
      settings.DisableScreenSaver = DisableScreenSaver;
      settings.PlaylistName = PlaylistName;
      settings.PlaylistId = _playlistId;
      // Don't save the play- and repeat mode here, it's saved separately
      settingsManager.Save(settings);
    }

    protected void LoadPlayRepeatMode()
    {
      ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();
      PartyMusicPlayerSettings settings = settingsManager.Load<PartyMusicPlayerSettings>();
      PlayMode = settings.PlayMode;
      RepeatMode = settings.RepeatMode;
    }

    protected void SavePlayRepeatMode()
    {
      ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();
      PartyMusicPlayerSettings settings = settingsManager.Load<PartyMusicPlayerSettings>();
      settings.PlayMode = PlayMode;
      settings.RepeatMode = RepeatMode;
      settingsManager.Save(settings);
    }

    protected bool UpdatePlaylists()
    {
      IServerConnectionManager scm = ServiceRegistration.Get<IServerConnectionManager>();
      IContentDirectory cd = scm.ContentDirectory;
      if (cd == null)
      {
        ShowServerNotConnectedDialog();
        return false;
      }
      UpdatePlaylists(cd);
      return true;
    }

    protected void ShowServerNotConnectedDialog()
    {
      IDialogManager dialogManager = ServiceRegistration.Get<IDialogManager>();
      dialogManager.ShowDialog(Consts.RES_SERVER_NOT_CONNECTED_DIALOG_HEADER, Consts.RES_SERVER_NOT_CONNECTED_DIALOG_TEXT, DialogType.OkDialog, false, null);
    }

    protected void UpdatePlaylists(IContentDirectory cd)
    {
      _playlists.Clear();
      ICollection<PlaylistInformationData> playlists = cd.GetPlaylists();
      foreach (PlaylistInformationData playlist in playlists)
      {
        Guid playlistId = playlist.PlaylistId;
        string playlistName = playlist.Name;
        ListItem playlistItem = new ListItem(Consts.KEY_NAME, playlistName)
          {
              Command = new MethodDelegateCommand(() => SetPlaylist(playlistId, playlistName))
          };
        playlistItem.AdditionalProperties[Consts.KEY_PLAYLIST_ID] = playlistId;
        _playlists.Add(playlistItem);
      }
    }

    protected void SetPlaylist(Guid playlistId, string playlistName)
    {
      _playlistId = playlistId;
      PlaylistName = playlistName;
    }

    protected void LeavePartyMode()
    {
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      workflowManager.NavigatePopToStateAsync(Consts.WF_STATE_ID_PARTY_MUSIC_PLAYER, true);
    }

    protected void CheckScreenSaver()
    {
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      if (workflowManager.IsStateContainedInNavigationStack(Consts.WF_STATE_ID_PARTY_MUSIC_PLAYER))
      {
        if (DisableScreenSaver)
        {
          if (_screenSaverController == null)
          {
            IScreenControl screenControl = ServiceRegistration.Get<IScreenControl>();
            _screenSaverController = screenControl.GetScreenSaverController();
          }
          if (_screenSaverController != null)
            _screenSaverController.IsScreenSaverDisabled = true;
        }
      }
      else
      {
        if (_screenSaverController != null)
        {
          _screenSaverController.Dispose();
          _screenSaverController = null;
        }
      }
    }

    #endregion

    protected void PrepareState(NavigationContext oldContext, NavigationContext newContext, bool push)
    {
      Guid oldStateId = oldContext.WorkflowState.StateId;
      if (oldStateId == Consts.WF_STATE_ID_PARTY_MUSIC_PLAYER && !push)
        SavePlayRepeatMode();
    }

    #region IWorkflowModel implementation

    public Guid ModelId
    {
      get { return MODEL_ID; }
    }

    public bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
    {
      return true;
    }

    public void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      CheckScreenSaver();
    }

    public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      CheckScreenSaver();
    }

    public void ChangeModelContext(NavigationContext oldContext, NavigationContext newContext, bool push)
    {
      CheckScreenSaver();
    }

    public void Deactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      CheckScreenSaver();
    }

    public void Reactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      CheckScreenSaver();
    }

    public void UpdateMenuActions(NavigationContext context, IDictionary<Guid, WorkflowAction> actions)
    {
      // Nothing to do
    }

    public ScreenUpdateMode UpdateScreen(NavigationContext context, ref string screen)
    {
      return ScreenUpdateMode.AutoWorkflowManager;
    }

    #endregion
  }
}
