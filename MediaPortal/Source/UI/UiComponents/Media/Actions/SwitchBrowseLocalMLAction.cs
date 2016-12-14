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
using MediaPortal.Common;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Messaging;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.UiComponents.Media.Models;
using MediaPortal.UiComponents.Media.Models.ScreenData;

namespace MediaPortal.UiComponents.Media.Actions
{
  public class SwitchBrowseLocalMLAction : BaseTrackServerConnectionAction
  {
    #region Consts

    public const string SWITCH_BROWSE_LOCAL_ML_CONTRIBUTOR_MODEL_ID_STR = "F1DBEDCB-CB95-4CF5-8787-07390869DE44";

    public static readonly Guid SWITCH_BROWSE_LOCAL_ML_CONTRIBUTOR_MODEL_ID = new Guid(SWITCH_BROWSE_LOCAL_ML_CONTRIBUTOR_MODEL_ID_STR);

    #endregion

    #region Protected fields

    protected IResourceString _displayTitle;
    protected bool _isInBrowseState = false;

    #endregion

    protected override IEnumerable<string> GetMessageChannels()
    {
      IList<string> result = new List<string>(base.GetMessageChannels()) {WorkflowManagerMessaging.CHANNEL};
      return result;
    }

    protected override void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      base.OnMessageReceived(queue, message);
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
      _displayTitle = LocalizationHelper.CreateResourceString(workflowManager.IsStateContainedInNavigationStack(Consts.WF_STATE_ID_LOCAL_MEDIA_NAVIGATION_ROOT) ?
          Consts.RES_SWITCH_TO_BROWSE_ML_VIEW : Consts.RES_SWITCH_TO_LOCAL_MEDIA_VIEW);
      _isInBrowseState = workflowManager.IsAnyStateContainedInNavigationStack(new Guid[]
        {
            Consts.WF_STATE_ID_LOCAL_MEDIA_NAVIGATION_ROOT,
            Consts.WF_STATE_ID_BROWSE_MEDIA_NAVIGATION_ROOT,
        }) && MediaNavigationModel.GetNavigationData(workflowManager.CurrentNavigationContext, false) != null;
    }

    public override IResourceString DisplayTitle
    {
      get { return _displayTitle; }
    }

    public override bool IsActionVisible(NavigationContext context)
    {
      return _isHomeServerConnected && _isInBrowseState;
    }

    public override bool IsActionEnabled(NavigationContext context)
    {
      return true;
    }

    public override void Execute()
    {
      AbstractBrowseMediaNavigationScreenData.NavigateToSiblingState();
    }
  }
}