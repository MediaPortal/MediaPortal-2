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
using MediaPortal.Common.Messaging;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.UiComponents.Media.Models;

namespace MediaPortal.UiComponents.Media.Actions
{
  public class ManagePlaylistsAction : VisibilityDependsOnServerConnectStateAction
  {
    protected bool _insideMedia;
    protected static Guid WF_HOME_ID = new Guid("7F702D9C-F2DD-42da-9ED8-0BA92F07787F");


    #region Consts

    public const string MANAGE_PLAYLISTS_ACTION_CONTRIBUTOR_MODEL_ID_STR = "2C3A747D-7FD7-408b-8843-31842A2EB6F3";

    #endregion

    public ManagePlaylistsAction() : base(true, null, Consts.RES_ADD_TO_PLAYLIST_MENU_ITEM) {}

    void SubscribeToMessages()
    {
      _messageQueue.SubscribeToMessageChannel(WorkflowManagerMessaging.CHANNEL);
      _messageQueue.MessageReceived += OnMessageReceived;
    }

    void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == WorkflowManagerMessaging.CHANNEL)
      {
        WorkflowManagerMessaging.MessageType messageType = (WorkflowManagerMessaging.MessageType) message.MessageType;
        switch (messageType)
        {
          case WorkflowManagerMessaging.MessageType.NavigationComplete:
            Update();
            break;
        }
      }
    }

    protected override void Update()
    {
      base.Update();
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      // Playlists should appear only inside home and all derived media browsing models
      _insideMedia = workflowManager.CurrentNavigationContext.WorkflowState.StateId == WF_HOME_ID ||
        workflowManager.IsAnyStateContainedInNavigationStack(Consts.WF_STATE_IDS_MEDIA_ROOTS) && 
        MediaNavigationModel.GetNavigationData(workflowManager.CurrentNavigationContext, false) != null;
    }

    public override bool IsActionVisible(NavigationContext context)
    {
      return _insideMedia && base.IsActionVisible(context);
    }

    /// <summary>
    /// Returns the information if the playlist management action should be visible in the current workflow state.
    /// </summary>
    protected bool ShowPlaylistManagement()
    {
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      return !workflowManager.IsStateContainedInNavigationStack(Consts.WF_STATE_ID_PLAYLISTS_OVERVIEW);
    }

    public override void Initialize()
    {
      base.Initialize();
      SubscribeToMessages();
    }

    protected override bool IsVisibleOverride
    {
      get { return ShowPlaylistManagement(); }
    }

    #region IWorkflowContributor implementation

    public override void Execute()
    {
      ManagePlaylistsModel.ShowPlaylistsOverview();
    }

    #endregion
  }
}
