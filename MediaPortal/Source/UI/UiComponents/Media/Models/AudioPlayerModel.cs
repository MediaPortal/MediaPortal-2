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
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.MediaManagement;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.UI.Control.InputManager;

namespace MediaPortal.UiComponents.Media.Models
{
  /// <summary>
  /// Attends the CurrentlyPlaying and FullscreenContent states for audio players.
  /// </summary>
  public class AudioPlayerModel : BasePlayerModel
  {
    public const string MODEL_ID_STR = "D8998340-DA2D-42be-A29B-6D7A72AEA2DC";
    public static readonly Guid MODEL_ID = new Guid(MODEL_ID_STR);
    protected AbstractProperty _mediaItemProperty;
    protected AbstractProperty _isOSDVisibleProperty;
    protected MediaItem _currentMediaItem;
    private bool _isOsdOpenOnDemand;
    private DateTime _lastAudioInfoDemand = DateTime.MinValue;

    public AudioPlayerModel() : base(Consts.WF_STATE_ID_CURRENTLY_PLAYING_AUDIO, Consts.WF_STATE_ID_FULLSCREEN_AUDIO)
    {
      _mediaItemProperty = new SProperty(typeof(MediaItem), null);
      _isOSDVisibleProperty = new WProperty(typeof(bool), false);
    }

    protected override Type GetPlayerUIContributorType(IPlayer player, MediaWorkflowStateType stateType)
    {
      if (!(player is IAudioPlayer))
        return null;

      // First check if the player provides an own UI contributor.
      IUIContributorPlayer uicPlayer = player as IUIContributorPlayer;
      if (uicPlayer != null)
        return uicPlayer.UIContributorType;

      // TODO: Specific UI contributor implementations for specific players
      return typeof(DefaultAudioPlayerUIContributor);
    }

    protected override void Update()
    {
      // base.Update is abstract
      IPlayerContextManager playerContextManager = ServiceRegistration.Get<IPlayerContextManager>();
      IPlayerContext playerContext = playerContextManager.CurrentPlayerContext;
      _currentMediaItem = playerContext == null ? null : playerContext.CurrentMediaItem;
      IInputManager inputManager = ServiceRegistration.Get<IInputManager>();

      if (!_isOsdOpenOnDemand)
      {
        if (DateTime.Now - _lastAudioInfoDemand > DateTime.Now.AddSeconds(5) - DateTime.Now)
        {
          IsOSDVisible = inputManager.IsMouseUsed;
        }
      }
      MediaItem = _currentMediaItem;
    }

    #region Members to be accessed from the GUI

    public void ToggleAudioInfo()
    {
      IsOSDVisible = !IsOSDVisible;
      _isOsdOpenOnDemand = IsOSDVisible;
      _lastAudioInfoDemand = DateTime.Now;
    }

    public void CloseAudioInfo()
    {
      IsOSDVisible = false;
      _lastAudioInfoDemand = DateTime.Now;
      _isOsdOpenOnDemand = false;
    }

    public AbstractProperty MediaItemProperty
    {
      get { return _mediaItemProperty; }
    }

    public MediaItem MediaItem
    {
      get { return (MediaItem)_mediaItemProperty.GetValue(); }
      internal set { _mediaItemProperty.SetValue(value); }
    }

    public AbstractProperty IsOSDVisibleProperty
    {
      get { return _isOSDVisibleProperty; }
    }

    public bool IsOSDVisible
    {
      get { return (bool)_isOSDVisibleProperty.GetValue(); }
      set { _isOSDVisibleProperty.SetValue(value); }
    }

    #endregion

    #region IWorkflowModel implementation

    public override Guid ModelId
    {
      get { return MODEL_ID; }
    }

    #endregion
  }
}
