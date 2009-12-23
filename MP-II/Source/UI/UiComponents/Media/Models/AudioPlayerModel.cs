#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
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
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.Presentation.Workflow;

namespace UiComponents.Media.Models
{
  /// <summary>
  /// Attends the CurrentlyPlaying and FullscreenContent states for audio players.
  /// </summary>
  public class AudioPlayerModel : BaseTimerControlledUIModel, IWorkflowModel
  {
    public const string MODEL_ID_STR = "D8998340-DA2D-42be-A29B-6D7A72AEA2DC";
    public static readonly Guid MODEL_ID = new Guid(MODEL_ID_STR);

    public const string CURRENTLY_PLAYING_STATE_ID_STR = "4596B758-CE2B-4e31-9CB9-6C30215831ED";
    public const string FULLSCREEN_CONTENT_STATE_ID_STR = "82E8C050-0318-41a3-86B8-FC14FB85338B";
    public const string PLAYER_CONFIGURATION_DIALOG_STATE_ID = "D0B79345-69DF-4870-B80E-39050434C8B3"; // From SkinBase

    public static readonly Guid CURRENTLY_PLAYING_STATE_ID = new Guid(CURRENTLY_PLAYING_STATE_ID_STR);
    public static readonly Guid FULLSCREEN_CONTENT_STATE_ID = new Guid(FULLSCREEN_CONTENT_STATE_ID_STR);
    public static readonly Guid PLAYER_CONFIGURATION_DIALOG_STATE = new Guid(PLAYER_CONFIGURATION_DIALOG_STATE_ID);

    public const string FULLSCREENAUDIO_SCREEN_NAME = "FullscreenContentAudio"; // TODO: Create screen
    public const string CURRENTLY_PLAYING_SCREEN_NAME = "CurrentlyPlayingAudio"; // TODO: Create screen

    public AudioPlayerModel() : base(300)
    {
      // TODO
    }

    protected override void Update()
    {
      // TODO
    }

    protected static bool CanHandlePlayer(IPlayer player)
    {
      return player is IAudioPlayer;
    }

    #region IWorkflowModel implementation

    public override Guid ModelId
    {
      get { return MODEL_ID; }
    }

    public bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
    {
      IPlayerContextManager playerContextManager = ServiceScope.Get<IPlayerContextManager>();
      if (newContext.WorkflowState.StateId == CURRENTLY_PLAYING_STATE_ID)
      {
        IPlayerContext pc = playerContextManager.CurrentPlayerContext;
        // The "currently playing" screen is always bound to the "current player"
        return pc != null && CanHandlePlayer(pc.CurrentPlayer);
      }
      else if (newContext.WorkflowState.StateId == FULLSCREEN_CONTENT_STATE_ID)
      {
        // The "fullscreen content" screen is always bound to the "primary player"
        IPlayerContext pc = playerContextManager.GetPlayerContext(PlayerManagerConsts.PRIMARY_SLOT);
        return pc != null && CanHandlePlayer(pc.CurrentPlayer);
      }
      else
        return false;
    }

    public void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      // TODO
    }

    public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      // TODO
    }

    public void ChangeModelContext(NavigationContext oldContext, NavigationContext newContext, bool push)
    {
      // TODO
    }

    public void Deactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      // TODO
    }

    public void ReActivate(NavigationContext oldContext, NavigationContext newContext)
    {
      // TODO
    }

    public void UpdateMenuActions(NavigationContext context, IDictionary<Guid, WorkflowAction> actions)
    {
      // TODO
    }

    public ScreenUpdateMode UpdateScreen(NavigationContext context, ref string screen)
    {
      // TODO
      return ScreenUpdateMode.AutoWorkflowManager;
    }

    #endregion

    #region Overrides of BaseTimerControlledUIModel

    #endregion
  }
}
