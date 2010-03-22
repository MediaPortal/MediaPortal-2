#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.Presentation.Workflow;

namespace UiComponents.Media.Models
{
  /// <summary>
  /// Attends the Playlist state.
  /// </summary>
  public class PlaylistModel : BaseMessageControlledUIModel, IWorkflowModel
  {
    public const string MODEL_ID_STR = "E30AA448-C1D1-4d8e-B08F-CF569624B51C";
    public static readonly Guid MODEL_ID = new Guid(MODEL_ID_STR);

    protected ItemsList _items = new ItemsList();

    public PlaylistModel()
    {
      SubscribeToMessages();
    }

    private void SubscribeToMessages()
    {
      _messageQueue.SubscribeToMessageChannel(PlaylistMessaging.CHANNEL);
      _messageQueue.SubscribeToMessageChannel(PlayerContextManagerMessaging.CHANNEL);
    }

    #region Members to be accessed from the GUI

    public ItemsList Items
    {
      get { return _items; }
    }

    #endregion

    #region IWorkflowModel implementation

    public override Guid ModelId
    {
      get { return MODEL_ID; }
    }

    public bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
    {
      IPlayerContextManager playerContextManager = ServiceScope.Get<IPlayerContextManager>();
      IPlayerContext pc = playerContextManager.GetPlayerContext(PlayerChoice.CurrentPlayer);
      return pc.MediaType == PlayerContextType.Audio || pc.MediaType == PlayerContextType.Video;
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
      // Nothing to do
    }

    public void ReActivate(NavigationContext oldContext, NavigationContext newContext)
    {
      // TODO
    }

    public void UpdateMenuActions(NavigationContext context, IDictionary<Guid, WorkflowAction> actions)
    {
      // Nothing to do
    }

    public ScreenUpdateMode UpdateScreen(NavigationContext context, ref string screen)
    {
      // Nothing to do
      return ScreenUpdateMode.AutoWorkflowManager;
    }

    #endregion
  }
}
