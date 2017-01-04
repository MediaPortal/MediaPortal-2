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
using System.IO;
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Settings;
using MediaPortal.Common.SystemCommunication;
using MediaPortal.Common.SystemResolver;
using MediaPortal.Common.Threading;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.Utilities;
using MediaPortal.UI.ServerCommunication;
using MediaPortal.UiComponents.Media.Models;
using MediaPortal.UiComponents.Utilities.General;
using MediaPortal.UiComponents.Utilities.Playlists;
using MediaPortal.UiComponents.Utilities.Settings;
using MediaPortal.Utilities;
using MediaPortal.UI.Services.UserManagement;

namespace MediaPortal.UiComponents.Utilities.Models
{
  public class PlaylistImportModel : IDisposable
  {
    #region Consts

    // Global ID definitions and references
    public const string STR_MODEL_ID = "D8E1EF69-1372-430D-9F36-3F163E0D12A8";

    // ID variables
    public static readonly Guid MODEL_ID = new Guid(STR_MODEL_ID);

    #endregion

    #region Classes

    public class ImportPlaylistOperation
    {
      protected AbstractProperty _numFilesProperty = new WProperty(typeof(int), 0);
      protected AbstractProperty _numProcessedProperty = new WProperty(typeof(int), 0);
      protected AbstractProperty _numMatchedProperty = new WProperty(typeof(int), 0);
      protected AbstractProperty _progressTextProperty = new WProperty(typeof(string), null);
      protected AbstractProperty _isCancelledProperty = new WProperty(typeof(bool), false);

      protected int _updateScreenCounter = 0;

      protected void CheckUpdateScreenData()
      {
        if (_updateScreenCounter++ % Consts.IMPORT_PLAYLIST_SCREEN_UPDATE_INTERVAL == 0)
          UpdateScreenData();
      }

      protected void UpdateScreenData()
      {
        int numFiles = NumFiles;
        int numProcessed = NumProcessed;
        int numMatched = NumMatched;
        ProgressText = LocalizationHelper.Translate(Consts.RES_IMPORT_PLAYLIST_PROGRESS_TEXT, numProcessed, numFiles, numProcessed - numMatched);
      }
      
      public AbstractProperty NumFilesProperty
      {
        get { return _numFilesProperty; }
      }

      public int NumFiles
      {
        get { return (int) _numFilesProperty.GetValue(); }
        set { _numFilesProperty.SetValue(value); }
      }

      public AbstractProperty NumProcessedProperty
      {
        get { return _numProcessedProperty; }
      }

      public int NumProcessed
      {
        get { return (int) _numProcessedProperty.GetValue(); }
        set { _numProcessedProperty.SetValue(value); }
      }

      public AbstractProperty NumMatchedProperty
      {
        get { return _numMatchedProperty; }
      }

      public int NumMatched
      {
        get { return (int) _numMatchedProperty.GetValue(); }
        set { _numMatchedProperty.SetValue(value); }
      }

      public AbstractProperty ProgressTextProperty
      {
        get { return _progressTextProperty; }
      }

      public string ProgressText
      {
        get { return (string) _progressTextProperty.GetValue(); }
        set { _progressTextProperty.SetValue(value); }
      }

      public AbstractProperty IsCancelledProperty
      {
        get { return _isCancelledProperty; }
      }

      public bool IsCancelled
      {
        get { return (bool) _isCancelledProperty.GetValue(); }
        set { _isCancelledProperty.SetValue(value); }
      }

      public void Cancel()
      {
        IsCancelled = true;
      }

      public IList<Guid> Execute(IList<string> mediaFiles, ShareLocation shareLocation)
      {
        NumFiles = mediaFiles.Count;
        IServerConnectionManager scm = ServiceRegistration.Get<IServerConnectionManager>();
        IContentDirectory cd = scm.ContentDirectory;
        string systemId = shareLocation == ShareLocation.Local ? ServiceRegistration.Get<ISystemResolver>().LocalSystemId : scm.HomeServerSystemId;
        Guid[] necessaryAudioAspectIds = null;
        Guid[] optionalAudioAspectIds = null;

        ILogger logger = ServiceRegistration.Get<ILogger>();
        IScreenManager screenManager = ServiceRegistration.Get<IScreenManager>();
        Guid? dialogId = screenManager.ShowDialog(Consts.DIALOG_IMPORT_PLAYLIST_PROGRESS, (dialogName, dialogInstanceId) => Cancel());
        if (!dialogId.HasValue)
        {
          logger.Warn("ImportPlaylistOperation: Error showing progress dialog");
          return null;
        }

        Guid? userProfile = null;
        IUserManagement userProfileDataManagement = ServiceRegistration.Get<IUserManagement>();
        if (userProfileDataManagement != null && userProfileDataManagement.IsValidUser)
          userProfile = userProfileDataManagement.CurrentUser.ProfileId;

        IList<Guid> result = new List<Guid>();
        NumMatched = 0;
        NumProcessed = 0;
        NumMatched = 0;
        UpdateScreenData();
        try
        {
          foreach (string localMediaFile in mediaFiles)
          {
            if (IsCancelled)
              return null;
            CheckUpdateScreenData();
            MediaItem item = cd.LoadItem(systemId, LocalFsResourceProviderBase.ToResourcePath(localMediaFile), 
              necessaryAudioAspectIds, optionalAudioAspectIds, userProfile);
            NumProcessed++;
            if (item == null)
            {
              logger.Warn("ImportPlaylistOperation: Media item '{0}' was not found in the media library", localMediaFile);
              continue;
            }
            logger.Debug("ImportPlaylistOperation: Matched media item '{0}' in media library", localMediaFile);
            NumMatched++;
            result.Add(item.MediaItemId);
          }
        }
        catch (Exception e)
        {
          logger.Warn("ImportPlaylistOperation: Error importing playlist", e);
        }
        screenManager.CloseDialog(dialogId.Value);
        return result;
      }
    }

    #endregion

    #region Protected fields

    protected AbstractProperty _importFileProperty;
    protected AbstractProperty _playlistNameProperty;
    protected AbstractProperty _errorHintProperty;
    protected AbstractProperty _isDataValidProperty;

    protected ListItem _localShareLocationItem;
    protected ListItem _serverShareLocationItem;
    protected ItemsList _shareLocations;

    protected PathBrowserCloseWatcher _pathBrowserCloseWatcher = null;

    protected ImportPlaylistOperation _importPlaylistOperation = null;

    protected AsynchronousMessageQueue _queue;

    #endregion

    #region Ctor and Maintainance

    public PlaylistImportModel()
    {
      _importFileProperty = new WProperty(typeof(string), null);
      _playlistNameProperty = new WProperty(typeof(string), null);
      _errorHintProperty = new WProperty(typeof(string), null);
      _isDataValidProperty = new WProperty(typeof(bool), false);

      _importFileProperty.Attach(OnDataPropertyChanged);
      _playlistNameProperty.Attach(OnDataPropertyChanged);

      CreateShareLocations();

      _queue = new AsynchronousMessageQueue(this, new string[]
        {
            ServerConnectionMessaging.CHANNEL,
        });
      _queue.MessageReceived += OnMessageReceived;
      _queue.Start();

      LoadSettings();

      CheckDataValid();
    }

    public void Dispose()
    {
      if (_pathBrowserCloseWatcher != null)
        _pathBrowserCloseWatcher.Dispose();
      _pathBrowserCloseWatcher = null;
      if (_queue != null)
        _queue.Shutdown();
      _queue = null;
    }

    void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == ServerConnectionMessaging.CHANNEL)
      {
        ServerConnectionMessaging.MessageType messageType = (ServerConnectionMessaging.MessageType) message.MessageType;
        switch (messageType)
        {
          case ServerConnectionMessaging.MessageType.HomeServerConnected:
          case ServerConnectionMessaging.MessageType.HomeServerDisconnected:
            CheckDataValid();
            break;
        }
      }
    }

    void OnDataPropertyChanged(AbstractProperty property, object oldValue)
    {
      CheckDataValid();
    }

    #endregion

    #region Public members

    public AbstractProperty ImportFileProperty
    {
      get { return _importFileProperty; }
    }

    public string ImportFile
    {
      get { return (string) _importFileProperty.GetValue(); }
      set { _importFileProperty.SetValue(value); }
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

    public ItemsList ShareLocations
    {
      get { return _shareLocations; }
    }

    public AbstractProperty IsDataValidProperty
    {
      get { return _isDataValidProperty; }
    }

    public bool IsDataValid
    {
      get { return (bool) _isDataValidProperty.GetValue(); }
      set { _isDataValidProperty.SetValue(value); }
    }

    public AbstractProperty ErrorHintProperty
    {
      get { return _errorHintProperty; }
    }

    public string ErrorHint
    {
      get { return (string) _errorHintProperty.GetValue(); }
      set { _errorHintProperty.SetValue(value); }
    }

    public ImportPlaylistOperation CurrentImportOperation
    {
      get { return _importPlaylistOperation; }
    }

    public void ChooseImportFile()
    {
      string importFile = ImportFile;
      string initialPath = string.IsNullOrEmpty(importFile) ? null : DosPathHelper.GetDirectory(importFile);
      Guid dialogHandle = ServiceRegistration.Get<IPathBrowser>().ShowPathBrowser(Consts.RES_CHOOSE_IMPORT_FILE_DIALOG_HEADER, true, false,
          string.IsNullOrEmpty(initialPath) ? null : LocalFsResourceProviderBase.ToResourcePath(initialPath),
          path =>
            {
              string choosenPath = LocalFsResourceProviderBase.ToDosPath(path.LastPathSegment.Path);
              if (string.IsNullOrEmpty(choosenPath))
                return false;
              string extension = StringUtils.TrimToEmpty(DosPathHelper.GetExtension(choosenPath)).ToLowerInvariant();
              return (extension == ".m3u" || extension == ".m3u8") && File.Exists(choosenPath);
            });
      if (_pathBrowserCloseWatcher != null)
        _pathBrowserCloseWatcher.Dispose();
      _pathBrowserCloseWatcher = new PathBrowserCloseWatcher(this, dialogHandle, choosenPath =>
          {
            ImportFile = LocalFsResourceProviderBase.ToDosPath(choosenPath);
          },
          null);
    }

    public void ImportPlaylist()
    {
      ILogger logger = ServiceRegistration.Get<ILogger>();
      SaveSettings();
      string importFile = ImportFile;
      if (!File.Exists(importFile))
      {
        logger.Warn("PlaylistImportModel: Cannot import playlist, playlist file '{0}' does not exist", importFile);
        return;
      }
      logger.Info("PlaylistImportModel: Importing playlist '{0}'", importFile);
      IServerConnectionManager scm = ServiceRegistration.Get<IServerConnectionManager>();
      IContentDirectory cd = scm.ContentDirectory;
      if (cd == null)
      {
        logger.Warn("PlaylistImportModel: Cannot import playlist, the server is not connected");
        return;
      }
      IList<string> mediaFiles = M3U.ExtractFileNamesFromPlaylist(importFile);
      IThreadPool threadPool = ServiceRegistration.Get<IThreadPool>();
      threadPool.Add(() => RunImportOperationAsync(cd, mediaFiles));
    }

    #endregion

    #region Protected members

    protected ShareLocation GetShareLocation()
    {
      return _localShareLocationItem.Selected ? ShareLocation.Local : ShareLocation.Server;
    }

    protected void CreateShareLocations()
    {
      _shareLocations = new ItemsList();
      _localShareLocationItem = new ListItem(Consts.KEY_NAME, Consts.RES_LOCAL_SHARE_LOCATION);
      _serverShareLocationItem = new ListItem(Consts.KEY_NAME, Consts.RES_SERVER_SHARE_LOCATION);
      _shareLocations.Add(_localShareLocationItem);
      _shareLocations.Add(_serverShareLocationItem);
      // Initial selection will be filled by LoadSettings()
    }

    protected void CheckDataValid()
    {
      bool result = true;
      ErrorHint = null;
      IServerConnectionManager scm = ServiceRegistration.Get<IServerConnectionManager>();
      IContentDirectory cd = scm.ContentDirectory;
      if (cd == null)
      {
        ErrorHint = Consts.RES_SERVER_NOT_CONNECTED;
        result = false;
      }
      else if (string.IsNullOrEmpty(PlaylistName))
      {
        ErrorHint = Consts.RES_PLAYLIST_NAME_EMPTY;
        result = false;
      }
      else if (PlaylistNameExists(cd, PlaylistName))
      {
        ErrorHint = Consts.RES_PLAYLIST_NAME_EXISTS;
        result = false;
      }
      else if (!File.Exists(ImportFile))
      {
        ErrorHint = Consts.RES_IMPORT_FILE_INVALID;
        result = false;
      }
      IsDataValid = result;
    }

    protected void LoadSettings()
    {
      ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();
      PlaylistImportSettings settings = settingsManager.Load<PlaylistImportSettings>();
      ImportFile = settings.LastImportFile;
      if (settings.ShareLocation == ShareLocation.Local)
      {
        _localShareLocationItem.Selected = true;
        _serverShareLocationItem.Selected = false;
      }
      else
      {
        _localShareLocationItem.Selected = false;
        _serverShareLocationItem.Selected = true;
      }
    }

    protected void SaveSettings()
    {
      ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();
      PlaylistImportSettings settings = settingsManager.Load<PlaylistImportSettings>();
      settings.LastImportFile = ImportFile;
      settingsManager.Save(settings);
      settings.ShareLocation = GetShareLocation();
    }

    protected void RunImportOperationAsync(IContentDirectory cd, IList<string> mediaFiles)
    {
      ILogger logger = ServiceRegistration.Get<ILogger>();
      _importPlaylistOperation = new ImportPlaylistOperation();
      IList<Guid> items = _importPlaylistOperation.Execute(mediaFiles, GetShareLocation());
      if (items == null)
      {
        logger.Info("PlaylistImportModel: Playlist import cancelled");
        return;
      }
      IDialogManager dialogManager = ServiceRegistration.Get<IDialogManager>();
      if (items.Count == 0)
      {
        dialogManager.ShowDialog(Consts.RES_PLAYLIST_SAVE_FAILED_TITLE,
            Consts.RES_NO_ITEMS_WERE_IMPORTED_SKIPPING_SAVE_TEXT, DialogType.OkDialog, false, DialogButtonType.Ok);
        return;
      }
      string playlistName = PlaylistName;
      PlaylistRawData playlistRawData = new PlaylistRawData(Guid.NewGuid(), playlistName, ManagePlaylistsModel.ConvertAVTypeToPlaylistType(AVType.Audio), items);
      try
      {
        cd.SavePlaylist(playlistRawData);
        dialogManager.ShowDialog(Consts.RES_PLAYLIST_SAVED_SUCCESSFULLY_TITLE,
            LocalizationHelper.Translate(Consts.RES_PLAYLIST_SAVED_SUCCESSFULLY_TEXT, playlistName), DialogType.OkDialog, false, DialogButtonType.Ok);
      }
      catch (Exception e)
      {
        dialogManager.ShowDialog(Consts.RES_PLAYLIST_SAVE_FAILED_TITLE, e.Message, DialogType.OkDialog, false, DialogButtonType.Ok);
      }
      _importPlaylistOperation = null;
    }

    protected bool PlaylistNameExists(IContentDirectory cd, string playlistName)
    {
      return cd.GetPlaylists().FirstOrDefault(playlistData => playlistData.Name == playlistName) != null;
    }

    #endregion
  }
}
