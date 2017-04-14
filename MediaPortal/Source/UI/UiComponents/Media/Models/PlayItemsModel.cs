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
using MediaPortal.Common.Commands;
using MediaPortal.Common.General;
using MediaPortal.Common.Localization;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.Settings;
using MediaPortal.Common.Threading;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.Presentation.Players.ResumeState;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UI.Services.Players;
using MediaPortal.UI.Services.UserManagement;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.UiComponents.Media.Settings;

namespace MediaPortal.UiComponents.Media.Models
{
  public delegate IEnumerable<MediaItem> GetMediaItemsDlgt();

  /// <summary>
  /// Workflow model which attends all play items workflow states. This model provides static methods to start playing
  /// of media items.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This class is the central starting point for all callers that have one or more media items which should be played and who want
  /// to use the play infrastructure of this Media plugin. Items will be played using default play menus and default CP and FSC states.
  /// </para>
  /// <para>
  /// There are methods available with several signatures to provide a maximum of flexibility for the caller. The user can be
  /// asked if he wants to play or enqueue the item(s) or the function can be choosen explicitly by the caller. The user
  /// can also be asked which kind of media items (audio/video/images) he wants to play/enqueue.
  /// </para>
  /// <para>
  /// As entrance point see the static methods <see cref="PlayItem"/>, <see cref="PlayItems"/>,
  /// <see cref="PlayOrEnqueueItem"/>, <see cref="PlayOrEnqueueItems"/>,
  /// <see cref="CheckQueryPlayAction(MediaItem)"/>, <see cref="CheckQueryPlayAction(GetMediaItemsDlgt,AVType)"/> and
  /// <see cref="CheckQueryPlayAction(GetMediaItemsDlgt)"/>.
  /// </para>
  /// </remarks>
  public class PlayItemsModel : IWorkflowModel
  {
    #region Consts

    public const string STR_MODEL_ID = "3750D3FE-CA2A-4c8a-97B3-A08EF305C084";
    public static readonly Guid MODEL_ID = new Guid(STR_MODEL_ID);

    // Constants for passing parameters into workflow states
    public const string KEY_GET_MEDIA_ITEMS_FUNCTION = "PlayItemsModel: GetMediaItemsFunction";
    public const string KEY_AV_TYPE = "PlayItemsModel: AVType";
    public const string KEY_DO_PLAY = "PlayItemsModel: DoPlay";
    public const string KEY_CONCURRENCY_MODE = "PlayItemsModel: ConcurrencyMode";
    public const string KEY_MEDIA_ITEM = "PlayItemsModel: MediaItem";

    #endregion

    #region Protected fields

    protected DialogCloseWatcher _dialogCloseWatcher = null;

    // Choice dialog for media type for untyped media navigation
    protected ItemsList _mediaTypeChoiceMenuItems = null;

    // Play menu
    protected ItemsList _playMenuItems = null;

    protected AbstractProperty _numItemsAddedToPlaylistTextProperty = new WProperty(typeof(string), string.Empty);
    protected bool _stopAddToPlaylist;

    #endregion

    #region Public members

    /// <summary>
    /// Provides a list of items to be shown in the choice dialog for the AV type.
    /// </summary>
    public ItemsList MediaTypeChoiceMenuItems
    {
      get { return _mediaTypeChoiceMenuItems; }
    }

    /// <summary>
    /// Provides a list of items to be shown in the play menu.
    /// </summary>
    public ItemsList PlayMenuItems
    {
      get { return _playMenuItems; }
    }

    public AbstractProperty NumItemsAddedToPlaylistTextProperty
    {
      get { return _numItemsAddedToPlaylistTextProperty; }
    }

    /// <summary>
    /// Exposes the number of items already added to playlist for screen <c>DialogAddToPlaylistProgress</c>.
    /// The text is of the form "57 items added".
    /// </summary>
    public string NumItemsAddedToPlaylistText
    {
      get { return (string) _numItemsAddedToPlaylistTextProperty.GetValue(); }
      set { _numItemsAddedToPlaylistTextProperty.SetValue(value); }
    }

    public void StopAddToPlaylist()
    {
      _stopAddToPlaylist = true;
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

    #region Static methods which also can be called from other models

    /// <summary>
    /// Checks if we need to show a menu for playing all items provided by the given <paramref name="getMediaItemsFunction"/>
    /// and shows that menu or adds all items to the playlist at once, starting playing, if no player is active and thus
    /// no menu needs to be shown.
    /// </summary>
    /// <param name="getMediaItemsFunction">Function which returns the media items to be added to the playlist. This function
    /// might take some time to return the items; in that case, a progress dialog will be shown.</param>
    /// <param name="avType">AV type of media items to be played.</param>
    public static void CheckQueryPlayAction(GetMediaItemsDlgt getMediaItemsFunction, AVType avType)
    {
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      workflowManager.NavigatePush(Consts.WF_STATE_ID_CHECK_QUERY_PLAYACTION_MULTIPLE_ITEMS, new NavigationContextConfig
        {
            AdditionalContextVariables = new Dictionary<string, object>
              {
                  {KEY_GET_MEDIA_ITEMS_FUNCTION, getMediaItemsFunction},
                  {KEY_AV_TYPE, avType},
              }
        });
    }

    /// <summary>
    /// First shows a menu to choose which media type should be played (Video, Audio, Image), then
    /// filters the items returned from the given <paramref name="getMediaItemsFunction"/>, checks if we need to show a
    /// menu for playing those items and shows that menu or adds all items to the playlist at once, starting playing,
    /// if no player is active and thus no menu needs to be shown.
    /// </summary>
    /// <param name="getMediaItemsFunction">Function which returns the media items to be added to the playlist. This function
    /// might take some time to return the items; in that case, a progress dialog will be shown.</param>
    public static void CheckQueryPlayAction(GetMediaItemsDlgt getMediaItemsFunction)
    {
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      workflowManager.NavigatePush(Consts.WF_STATE_ID_QUERY_AV_TYPE_CHECK_QUERY_PLAYACTION_MULTIPLE_ITEMS, new NavigationContextConfig
        {
            AdditionalContextVariables = new Dictionary<string, object>
              {
                  {KEY_GET_MEDIA_ITEMS_FUNCTION, getMediaItemsFunction},
              }
        });
    }

    /// <summary>
    /// Checks if we need to show a menu for playing the specified <paramref name="item"/> and shows that
    /// menu or plays the item, if no player is active and thus no menu needs to be shown.
    /// </summary>
    /// <param name="item">The item which should be played.</param>
    public static void CheckQueryPlayAction(MediaItem item)
    {
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      workflowManager.NavigatePush(Consts.WF_STATE_ID_CHECK_QUERY_PLAYACTION_SINGLE_ITEM, new NavigationContextConfig
        {
            AdditionalContextVariables = new Dictionary<string, object>
              {
                  {KEY_MEDIA_ITEM, item},
              }
        });
    }

    /// <summary>
    /// Checks if we need to show a menu for resuming the playback of the specified <paramref name="item"/> and shows that
    /// menu or plays the item.
    /// </summary>
    /// <param name="item">The item which should be played.</param>
    public static void CheckResumeAction(MediaItem item)
    {
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      workflowManager.NavigatePush(Consts.WF_STATE_ID_CHECK_RESUME_SINGLE_ITEM, new NavigationContextConfig
        {
            AdditionalContextVariables = new Dictionary<string, object>
              {
                  {KEY_MEDIA_ITEM, item},
              }
        });
    }

    /// <summary>
    /// Discards any current player and plays the specified media <paramref name="item"/>.
    /// </summary>
    /// <param name="item">Media item to be played.</param>
    /// <param name="resumeState">Contains optional information for players to resume playback.</param>
    public static void PlayItem(MediaItem item, IResumeState resumeState = null)
    {
      CloseSecondaryPlayerContext();
      PlayOrEnqueueItem(item, true, PlayerContextConcurrencyMode.None, resumeState);
    }

    /// <summary>
    /// Depending on parameter <paramref name="play"/>, plays or enqueues the specified media <paramref name="item"/>.
    /// </summary>
    /// <param name="item">Media item to be played.</param>
    /// <param name="play">If <c>true</c>, plays the specified <paramref name="item"/>, else enqueues it.</param>
    /// <param name="concurrencyMode">Determines if the media item will be played or enqueued in concurrency mode.</param>
    /// <param name="resumeState">Contains optional information for players to resume playback.</param>
    public static void PlayOrEnqueueItem(MediaItem item, bool play, PlayerContextConcurrencyMode concurrencyMode, IResumeState resumeState = null)
    {
      IPlayerContextManager pcm = ServiceRegistration.Get<IPlayerContextManager>();
      AVType avType = pcm.GetTypeOfMediaItem(item);
      IPlayerContext pc = PreparePlayerContext(avType, play, concurrencyMode);
      if (pc == null)
        return;

      // Always add items to playlist. This allows audio playlists as well as video/image playlists.
      pc.Playlist.Add(item);

      ServiceRegistration.Get<IThreadPool>().Add(() => CompletePlayOrEnqueue(pc, play, resumeState));
    }

    /// <summary>
    /// Discards any current player and plays the media items of type <paramref name="avType"/> returned by the given
    /// <paramref name="getMediaItemsFunction"/>.
    /// </summary>
    /// <param name="getMediaItemsFunction">Function returning the media items to be played.</param>
    /// <param name="avType">AV type of media items returned.</param>
    public static void PlayItems(GetMediaItemsDlgt getMediaItemsFunction, AVType avType)
    {
      CloseSecondaryPlayerContext();
      PlayOrEnqueueItems(getMediaItemsFunction, avType, true, PlayerContextConcurrencyMode.None);
    }

    /// <summary>
    /// Depending on parameter <paramref name="play"/>, plays or enqueues the media items of type <paramref name="avType"/>
    /// returned by the given <paramref name="getMediaItemsFunction"/>.
    /// This method can also be called from other models.
    /// </summary>
    /// <param name="getMediaItemsFunction">Function returning the media items to be played.</param>
    /// <param name="avType">AV type of media items to be played.</param>
    /// <param name="play">If <c>true</c>, plays the specified items, else enqueues it.</param>
    /// <param name="concurrencyMode">Determines if the media item will be played or enqueued in concurrency mode.</param>
    public static void PlayOrEnqueueItems(GetMediaItemsDlgt getMediaItemsFunction, AVType avType,
        bool play, PlayerContextConcurrencyMode concurrencyMode)
    {
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      workflowManager.NavigatePush(Consts.WF_STATE_ID_PLAY_OR_ENQUEUE_ITEMS, new NavigationContextConfig
        {
            AdditionalContextVariables = new Dictionary<string, object>
              {
                  {KEY_GET_MEDIA_ITEMS_FUNCTION, getMediaItemsFunction},
                  {KEY_AV_TYPE, avType},
                  {KEY_DO_PLAY, play},
                  {KEY_CONCURRENCY_MODE, concurrencyMode},
              }
        });
    }

    #endregion

    #region Protected members

    protected static void CloseSecondaryPlayerContext()
    {
      IPlayerContextManager playerContextManager = ServiceRegistration.Get<IPlayerContextManager>();
      IPlayerContext pcSecondary = playerContextManager.SecondaryPlayerContext;
      if (pcSecondary != null)
        pcSecondary.Close();
    }

    protected static void CompletePlayOrEnqueue(IPlayerContext pc, bool play, IResumeState resumeState = null)
    {
      IPlayerContextManager pcm = ServiceRegistration.Get<IPlayerContextManager>();
      MediaModelSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<MediaModelSettings>();
      pc.CloseWhenFinished = settings.ClosePlayerWhenFinished; // Has to be done before starting the media item, else the slot will not close in case of an error / when the media item cannot be played
      if (play)
      {
        if (resumeState != null)
          pc.SetContextVariable(PlayerContext.KEY_RESUME_STATE, resumeState);
        pc.Play();
        if (pc.AVType == AVType.Video || pc.AVType == AVType.Audio)
          pcm.ShowFullscreenContent(true);
      }
    }

    protected static IPlayerContext PreparePlayerContext(AVType avType, bool play, PlayerContextConcurrencyMode concurrencyMode)
    {
      IPlayerContextManager pcm = ServiceRegistration.Get<IPlayerContextManager>();
      string contextName;
      if (!GetPlayerContextNameForMediaType(avType, out contextName))
        return null;
      IPlayerContext pc = null;
      if (!play)
      {
        // !play means enqueue - so find our first player context of the correct media type
        IList<IPlayerContext> playerContexts = new List<IPlayerContext>(
            pcm.GetPlayerContextsByMediaModuleId(Consts.MODULE_ID_MEDIA).Where(playerContext => playerContext.AVType == avType));
        // In case the media type is audio, we have max. one player context of that type. In case media type is
        // video, we might have two. But we handle enqueue only for the first video player context.
        pc = playerContexts.FirstOrDefault();
      }
      if (pc == null)
        // No player context to reuse - so open a new one
        if (avType == AVType.Video)
          pc = pcm.OpenVideoPlayerContext(Consts.MODULE_ID_MEDIA, contextName, concurrencyMode,
              Consts.WF_STATE_ID_CURRENTLY_PLAYING_VIDEO, Consts.WF_STATE_ID_FULLSCREEN_VIDEO);
        else if (avType == AVType.Audio)
          pc = pcm.OpenAudioPlayerContext(Consts.MODULE_ID_MEDIA, contextName, concurrencyMode == PlayerContextConcurrencyMode.ConcurrentVideo,
              Consts.WF_STATE_ID_CURRENTLY_PLAYING_AUDIO, Consts.WF_STATE_ID_FULLSCREEN_AUDIO);
      if (pc == null)
        return null;
      if (play)
        pc.Playlist.Clear();
      return pc;
    }

    protected void LeaveQueryAVTypeState()
    {
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      workflowManager.NavigatePopToState(Consts.WF_STATE_ID_QUERY_AV_TYPE_CHECK_QUERY_PLAYACTION_MULTIPLE_ITEMS, true);
    }

    protected void LeaveCheckQueryPlayActionMultipleItemsState()
    {
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      workflowManager.NavigatePopToState(Consts.WF_STATE_ID_CHECK_QUERY_PLAYACTION_MULTIPLE_ITEMS, true);
    }

    protected void LeaveCheckResumePlaybackSingleItemState()
    {
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      workflowManager.NavigatePopToState(Consts.WF_STATE_ID_CHECK_RESUME_SINGLE_ITEM, true);
    }

    protected void LeaveCheckQueryPlayActionSingleItemState()
    {
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      workflowManager.NavigatePopToState(Consts.WF_STATE_ID_CHECK_QUERY_PLAYACTION_SINGLE_ITEM, true);
    }

    protected void CheckPlayMenuInternal(GetMediaItemsDlgt getMediaItemsFunction, AVType avType)
    {
      IPlayerContextManager pcm = ServiceRegistration.Get<IPlayerContextManager>();
      int numOpen = pcm.NumActivePlayerContexts;
      if (numOpen == 0)
      {
        // Asynchronously leave the current workflow state because we're called from a workflow model method
        IThreadPool threadPool = ServiceRegistration.Get<IThreadPool>();
        threadPool.Add(() =>
          {
            LeaveCheckQueryPlayActionMultipleItemsState();
            PlayItems(getMediaItemsFunction, avType);
          });
        return;
      }
      _playMenuItems = new ItemsList();
      int numAudio = pcm.GetPlayerContextsByAVType(AVType.Audio).Count();
      int numVideo = pcm.GetPlayerContextsByAVType(AVType.Video).Count();
      switch (avType)
      {
        case AVType.Audio:
          {
            ListItem playItem = new ListItem(Consts.KEY_NAME, Consts.RES_PLAY_AUDIO_ITEMS)
              {
                  Command = new MethodDelegateCommand(() =>
                    {
                      LeaveCheckQueryPlayActionMultipleItemsState();
                      PlayItems(getMediaItemsFunction, avType);
                    })
              };
            _playMenuItems.Add(playItem);
            if (numAudio > 0)
            {
              ListItem enqueueItem = new ListItem(Consts.KEY_NAME, Consts.RES_ENQUEUE_AUDIO_ITEMS)
                {
                    Command = new MethodDelegateCommand(() =>
                      {
                        LeaveCheckQueryPlayActionMultipleItemsState();
                        PlayOrEnqueueItems(getMediaItemsFunction, avType, false, PlayerContextConcurrencyMode.None);
                      })
                };
              _playMenuItems.Add(enqueueItem);
            }
            if (numVideo > 0)
            {
              ListItem playItemConcurrently = new ListItem(Consts.KEY_NAME, Consts.RES_MUTE_VIDEO_PLAY_AUDIO_ITEMS)
                {
                    Command = new MethodDelegateCommand(() =>
                      {
                        LeaveCheckQueryPlayActionMultipleItemsState();
                        PlayOrEnqueueItems(getMediaItemsFunction, avType, true, PlayerContextConcurrencyMode.ConcurrentVideo);
                      })
                };
              _playMenuItems.Add(playItemConcurrently);
            }
          }
          break;
        case AVType.Video:
          {
            ListItem playItem = new ListItem(Consts.KEY_NAME, Consts.RES_PLAY_VIDEO_IMAGE_ITEMS)
              {
                  Command = new MethodDelegateCommand(() =>
                    {
                      LeaveCheckQueryPlayActionMultipleItemsState();
                      PlayItems(getMediaItemsFunction, avType);
                    })
              };
            _playMenuItems.Add(playItem);
            if (numVideo > 0)
            {
              ListItem enqueueItem = new ListItem(Consts.KEY_NAME, Consts.RES_ENQUEUE_VIDEO_IMAGE_ITEMS)
                {
                    Command = new MethodDelegateCommand(() =>
                      {
                        LeaveCheckQueryPlayActionMultipleItemsState();
                        PlayOrEnqueueItems(getMediaItemsFunction, avType, false, PlayerContextConcurrencyMode.None);
                      })
                };
              _playMenuItems.Add(enqueueItem);
            }
            if (numAudio > 0)
            {
              ListItem playItem_A = new ListItem(Consts.KEY_NAME, Consts.RES_PLAY_VIDEO_IMAGE_ITEMS_MUTED_CONCURRENT_AUDIO)
                {
                    Command = new MethodDelegateCommand(() =>
                      {
                        LeaveCheckQueryPlayActionMultipleItemsState();
                        PlayOrEnqueueItems(getMediaItemsFunction, avType, true, PlayerContextConcurrencyMode.ConcurrentAudio);
                      })
                };
              _playMenuItems.Add(playItem_A);
            }
            if (numVideo > 0)
            {
              ListItem playItem_V = new ListItem(Consts.KEY_NAME, Consts.RES_PLAY_VIDEO_IMAGE_ITEMS_PIP)
                {
                    Command = new MethodDelegateCommand(() =>
                      {
                        LeaveCheckQueryPlayActionMultipleItemsState();
                        PlayOrEnqueueItems(getMediaItemsFunction, avType, true, PlayerContextConcurrencyMode.ConcurrentVideo);
                      })
                };
              _playMenuItems.Add(playItem_V);
            }
          }
          break;
        default:
          {
            IDialogManager dialogManager = ServiceRegistration.Get<IDialogManager>();
            Guid dialogHandleId = dialogManager.ShowDialog(Consts.RES_SYSTEM_INFORMATION, Consts.RES_CANNOT_PLAY_ITEMS_DIALOG_TEXT,
                DialogType.OkDialog, false, DialogButtonType.Ok);
            _dialogCloseWatcher = new DialogCloseWatcher(this, dialogHandleId, dialogResult => LeaveCheckQueryPlayActionMultipleItemsState());
          }
          break;
      }
      IScreenManager screenManager = ServiceRegistration.Get<IScreenManager>();
      screenManager.ShowDialog(Consts.DIALOG_PLAY_MENU, (dialogName, dialogInstanceId) =>
          LeaveCheckQueryPlayActionMultipleItemsState());
    }

    protected void CheckResumeMenuInternal(MediaItem item)
    {
      IResumeState resumeState = null;
      IUserManagement userProfileDataManagement = ServiceRegistration.Get<IUserManagement>();
      if (userProfileDataManagement.IsValidUser)
      {
        string resumeStateString;
        if (userProfileDataManagement.UserProfileDataManagement.GetUserMediaItemData(userProfileDataManagement.CurrentUser.ProfileId, item.MediaItemId, PlayerContext.KEY_RESUME_STATE, out resumeStateString))
          resumeState = ResumeStateBase.Deserialize(resumeStateString);
      }

      if (resumeState == null)
      {
        // Asynchronously leave the current workflow state because we're called from a workflow model method
        IThreadPool threadPool = ServiceRegistration.Get<IThreadPool>();
        threadPool.Add(() =>
        {
          LeaveCheckResumePlaybackSingleItemState();
          PlayItem(item);
        });
        return;
      }
      _playMenuItems = new ItemsList();
      ListItem resumeItem = new ListItem(Consts.KEY_NAME, Consts.RES_PLAYBACK_RESUME)
      {
        Command = new MethodDelegateCommand(() =>
        {
          LeaveCheckResumePlaybackSingleItemState();
          PlayItem(item, resumeState);
        })
      };
      _playMenuItems.Add(resumeItem);
      ListItem playItem = new ListItem(Consts.KEY_NAME, Consts.RES_PLAYBACK_FROMSTART)
      {
        Command = new MethodDelegateCommand(() =>
        {
          LeaveCheckResumePlaybackSingleItemState();
          PlayItem(item);
        })
      };
      _playMenuItems.Add(playItem);
      IScreenManager screenManager = ServiceRegistration.Get<IScreenManager>();
      screenManager.ShowDialog(Consts.DIALOG_PLAY_MENU, (dialogName, dialogInstanceId) => LeaveCheckResumePlaybackSingleItemState());
    }

    protected void CheckPlayMenuInternal(MediaItem item)
    {
      IPlayerContextManager pcm = ServiceRegistration.Get<IPlayerContextManager>();
      int numOpen = pcm.NumActivePlayerContexts;
      if (numOpen == 0)
      {
        // Asynchronously leave the current workflow state because we're called from a workflow model method
        IThreadPool threadPool = ServiceRegistration.Get<IThreadPool>();
        threadPool.Add(() =>
          {
            LeaveCheckQueryPlayActionSingleItemState();
            CheckResumeAction(item);
          });
        return;
      }
      _playMenuItems = new ItemsList();
      AVType avType = pcm.GetTypeOfMediaItem(item);
      int numAudio = pcm.GetPlayerContextsByAVType(AVType.Audio).Count();
      int numVideo = pcm.GetPlayerContextsByAVType(AVType.Video).Count();
      switch (avType)
      {
        case AVType.Audio:
          {
            ListItem playItem = new ListItem(Consts.KEY_NAME, Consts.RES_PLAY_AUDIO_ITEM)
              {
                  Command = new MethodDelegateCommand(() =>
                    {
                      LeaveCheckQueryPlayActionSingleItemState();
                      CheckResumeAction(item);
                    })
              };
            _playMenuItems.Add(playItem);
            if (numAudio > 0)
            {
              ListItem enqueueItem = new ListItem(Consts.KEY_NAME, Consts.RES_ENQUEUE_AUDIO_ITEM)
                {
                    Command = new MethodDelegateCommand(() =>
                      {
                        LeaveCheckQueryPlayActionSingleItemState();
                        PlayOrEnqueueItem(item, false, PlayerContextConcurrencyMode.None);
                      })
                };
              _playMenuItems.Add(enqueueItem);
            }
            if (numVideo > 0)
            {
              ListItem playItemConcurrently = new ListItem(Consts.KEY_NAME, Consts.RES_MUTE_VIDEO_PLAY_AUDIO_ITEM)
                {
                    Command = new MethodDelegateCommand(() =>
                      {
                        LeaveCheckQueryPlayActionSingleItemState();
                        PlayOrEnqueueItem(item, true, PlayerContextConcurrencyMode.ConcurrentVideo);
                      })
                };
              _playMenuItems.Add(playItemConcurrently);
            }
          }
          break;
        case AVType.Video:
          {
            ListItem playItem = new ListItem(Consts.KEY_NAME, Consts.RES_PLAY_VIDEO_IMAGE_ITEM)
              {
                  Command = new MethodDelegateCommand(() =>
                    {
                      LeaveCheckQueryPlayActionSingleItemState();
                      CheckResumeAction(item);
                    })
              };
            _playMenuItems.Add(playItem);
            if (numVideo > 0)
            {
              ListItem enqueueItem = new ListItem(Consts.KEY_NAME, Consts.RES_ENQUEUE_VIDEO_IMAGE_ITEM)
                {
                    Command = new MethodDelegateCommand(() =>
                      {
                        LeaveCheckQueryPlayActionSingleItemState();
                        PlayOrEnqueueItem(item, false, PlayerContextConcurrencyMode.None);
                      })
                };
              _playMenuItems.Add(enqueueItem);
            }
            if (numAudio > 0)
            {
              ListItem playItem_A = new ListItem(Consts.KEY_NAME, Consts.RES_PLAY_VIDEO_IMAGE_ITEM_MUTED_CONCURRENT_AUDIO)
                {
                    Command = new MethodDelegateCommand(() =>
                      {
                        LeaveCheckQueryPlayActionSingleItemState();
                        PlayOrEnqueueItem(item, true, PlayerContextConcurrencyMode.ConcurrentAudio);
                      })
                };
              _playMenuItems.Add(playItem_A);
            }
            if (numVideo > 0)
            {
              ListItem playItem_V = new ListItem(Consts.KEY_NAME, Consts.RES_PLAY_VIDEO_IMAGE_ITEM_PIP)
                {
                    Command = new MethodDelegateCommand(() =>
                      {
                        LeaveCheckQueryPlayActionSingleItemState();
                        PlayOrEnqueueItem(item, true, PlayerContextConcurrencyMode.ConcurrentVideo);
                      })
                };
              _playMenuItems.Add(playItem_V);
            }
          }
          break;
        default:
          {
            IDialogManager dialogManager = ServiceRegistration.Get<IDialogManager>();
            Guid dialogHandleId = dialogManager.ShowDialog(Consts.RES_SYSTEM_INFORMATION, Consts.RES_CANNOT_PLAY_ITEM_DIALOG_TEXT,
                DialogType.OkDialog, false, DialogButtonType.Ok);
            _dialogCloseWatcher = new DialogCloseWatcher(this, dialogHandleId, dialogResult => LeaveCheckQueryPlayActionSingleItemState());
          }
          break;
      }
      IScreenManager screenManager = ServiceRegistration.Get<IScreenManager>();
      screenManager.ShowDialog(Consts.DIALOG_PLAY_MENU, (dialogName, dialogInstanceId) =>
          LeaveCheckQueryPlayActionSingleItemState());
    }

    protected static bool GetPlayerContextNameForMediaType(AVType avType, out string contextName)
    {
      // No locking necessary
      if (avType == AVType.Video)
      {
        contextName = LocalizationHelper.Translate(Consts.RES_VIDEO_IMAGE_CONTEXT_NAME);
        return true;
      }
      if (avType == AVType.Audio)
      {
        contextName = LocalizationHelper.Translate(Consts.RES_AUDIO_CONTEXT_NAME);
        return true;
      }
      contextName = null;
      return false;
    }

    protected void SetNumItemsAddedToPlaylist(int numItems)
    {
      if (numItems % Consts.ADD_TO_PLAYLIST_UPDATE_INTERVAL == 0)
        NumItemsAddedToPlaylistText = LocalizationHelper.Translate(Consts.RES_N_ITEMS_ADDED, numItems);
    }

    protected void AsyncAddToPlaylist(IPlayerContext pc, GetMediaItemsDlgt getMediaItemsFunction, bool play)
    {
      IScreenManager screenManager = ServiceRegistration.Get<IScreenManager>();
      Guid? dialogInstanceId = screenManager.ShowDialog(Consts.DIALOG_ADD_TO_PLAYLIST_PROGRESS,
          (dialogName, instanceId) => StopAddToPlaylist());
      try
      {
        int numItems = 0;
        _stopAddToPlaylist = false;
        SetNumItemsAddedToPlaylist(0);
        ICollection<MediaItem> items = new List<MediaItem>();
        foreach (MediaItem item in getMediaItemsFunction())
        {
          SetNumItemsAddedToPlaylist(++numItems);
          if (_stopAddToPlaylist)
            break;
          items.Add(item);
        }
        pc.Playlist.AddAll(items);
      }
      finally
      {
        if (dialogInstanceId.HasValue)
          screenManager.CloseDialog(dialogInstanceId.Value);
        IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
        workflowManager.NavigatePopToState(Consts.WF_STATE_ID_PLAY_OR_ENQUEUE_ITEMS, true);
      }
      // Must be done after the dialog is closed
      CompletePlayOrEnqueue(pc, play);
    }

    protected void PlayOrEnqueueItemsInternal(GetMediaItemsDlgt getMediaItemsFunction, AVType avType,
        bool play, PlayerContextConcurrencyMode concurrencyMode)
    {
      IPlayerContext pc = PreparePlayerContext(avType, play, concurrencyMode);
      if (pc == null)
        return;

      // Adding items to playlist must be executed asynchronously - we will show a progress dialog where we aren't allowed
      // to block the input thread.
      IThreadPool threadPool = ServiceRegistration.Get<IThreadPool>();
      threadPool.Add(() => AsyncAddToPlaylist(pc, getMediaItemsFunction, play));
    }

    protected IEnumerable<MediaItem> FilterMediaItems(GetMediaItemsDlgt getMediaItemsFunction,
        ICollection<Guid> consideredMediaItemAspectTypes)
    {
      return getMediaItemsFunction().Where(mediaItem => consideredMediaItemAspectTypes.Any(aspectType => mediaItem.Aspects.ContainsKey(aspectType)));
    }

    protected void CheckQueryPlayAction_Continue(GetMediaItemsDlgt getMediaItemsFunction,
        ICollection<Guid> consideredMediaItemAspectTypes, AVType avType)
    {
      LeaveQueryAVTypeState();
      CheckQueryPlayAction(() => FilterMediaItems(getMediaItemsFunction, consideredMediaItemAspectTypes), avType);
    }

    protected void CheckQueryPlayAction_ShowMediaTypeChoice(GetMediaItemsDlgt getMediaItemsFunction)
    {
      _mediaTypeChoiceMenuItems = new ItemsList
        {
            new ListItem(Consts.KEY_NAME, Consts.RES_ADD_ALL_AUDIO)
              {
                  Command = new MethodDelegateCommand(() => CheckQueryPlayAction_Continue(
                      getMediaItemsFunction, new Guid[] {AudioAspect.Metadata.AspectId}, AVType.Audio))
              },
            new ListItem(Consts.KEY_NAME, Consts.RES_ADD_ALL_VIDEOS)
              {
                  Command = new MethodDelegateCommand(() => CheckQueryPlayAction_Continue(
                      getMediaItemsFunction, new Guid[] {VideoAspect.Metadata.AspectId}, AVType.Video))
              },
            new ListItem(Consts.KEY_NAME, Consts.RES_ADD_ALL_IMAGES)
              {
                  Command = new MethodDelegateCommand(() => CheckQueryPlayAction_Continue(
                      getMediaItemsFunction, new Guid[] {ImageAspect.Metadata.AspectId}, AVType.Video))
              },
            new ListItem(Consts.KEY_NAME, Consts.RES_ADD_VIDEOS_AND_IMAGES)
              {
                  Command = new MethodDelegateCommand(() => CheckQueryPlayAction_Continue(
                      getMediaItemsFunction, new Guid[] {VideoAspect.Metadata.AspectId, ImageAspect.Metadata.AspectId}, AVType.Video))
              },
        };
      IScreenManager screenManager = ServiceRegistration.Get<IScreenManager>();
      screenManager.ShowDialog(Consts.DIALOG_CHOOSE_AV_TYPE, (dialogName, dialogInstanceId) => LeaveQueryAVTypeState());
    }

    protected void PrepareState(NavigationContext context)
    {
      Guid workflowStateId = context.WorkflowState.StateId;
      if (workflowStateId == Consts.WF_STATE_ID_PLAY_OR_ENQUEUE_ITEMS)
      {
        AVType avType = (AVType) context.GetContextVariable(KEY_AV_TYPE, false);
        GetMediaItemsDlgt getMediaItemsFunction = (GetMediaItemsDlgt) context.GetContextVariable(KEY_GET_MEDIA_ITEMS_FUNCTION, false);
        bool doPlay = (bool) context.GetContextVariable(KEY_DO_PLAY, false);
        PlayerContextConcurrencyMode concurrencyMode = (PlayerContextConcurrencyMode) context.GetContextVariable(KEY_CONCURRENCY_MODE, false);
        PlayOrEnqueueItemsInternal(getMediaItemsFunction, avType, doPlay, concurrencyMode);
      }
      else if (workflowStateId == Consts.WF_STATE_ID_CHECK_RESUME_SINGLE_ITEM)
      {
        MediaItem item = (MediaItem) context.GetContextVariable(KEY_MEDIA_ITEM, false);
        CheckResumeMenuInternal(item);
      }
      else if (workflowStateId == Consts.WF_STATE_ID_CHECK_QUERY_PLAYACTION_SINGLE_ITEM)
      {
        MediaItem item = (MediaItem) context.GetContextVariable(KEY_MEDIA_ITEM, false);
        CheckPlayMenuInternal(item);
      }
      else if (workflowStateId == Consts.WF_STATE_ID_CHECK_QUERY_PLAYACTION_MULTIPLE_ITEMS)
      {
        GetMediaItemsDlgt getMediaItemsFunction = (GetMediaItemsDlgt) context.GetContextVariable(KEY_GET_MEDIA_ITEMS_FUNCTION, false);
        AVType avType = (AVType) context.GetContextVariable(KEY_AV_TYPE, false);
        CheckPlayMenuInternal(getMediaItemsFunction, avType);
      }
      else if (workflowStateId == Consts.WF_STATE_ID_QUERY_AV_TYPE_CHECK_QUERY_PLAYACTION_MULTIPLE_ITEMS)
      {
        GetMediaItemsDlgt getMediaItemsFunction = (GetMediaItemsDlgt) context.GetContextVariable(KEY_GET_MEDIA_ITEMS_FUNCTION, false);
        CheckQueryPlayAction_ShowMediaTypeChoice(getMediaItemsFunction);
      }
    }

    protected void ReleaseModelData()
    {
      _mediaTypeChoiceMenuItems = null;
      _playMenuItems = null;
      if (_dialogCloseWatcher != null)
      {
        _dialogCloseWatcher.Dispose();
        _dialogCloseWatcher = null;
      }
    }

    #endregion

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
      PrepareState(newContext);
    }

    public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      ReleaseModelData();
    }

    public void ChangeModelContext(NavigationContext oldContext, NavigationContext newContext, bool push)
    {
      PrepareState(newContext);
    }

    public void Deactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      // Nothing to do
    }

    public void Reactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      // Nothing to do
    }

    public void UpdateMenuActions(NavigationContext context, IDictionary<Guid, WorkflowAction> actions)
    {
      // Nothing to do
    }

    public ScreenUpdateMode UpdateScreen(NavigationContext context, ref string screen)
    {
      return ScreenUpdateMode.ManualWorkflowModel; // Avoid automatic screen update - we only show dialogs if necessary
    }

    #endregion
  }
}