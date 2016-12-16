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
using MediaPortal.UiComponents.Media.Views;

namespace MediaPortal.UiComponents.Media.Actions
{
  public class SwitchBrowseModeAction : BaseTrackServerConnectionAction
  {
    #region Consts

    public const string SWITCH_BROWSE_MODE_CONTRIBUTOR_MODEL_ID_STR = "56B6B935-8972-48C2-811A-BF150A1F8F09";

    public static readonly Guid SWITCH_BROWSE_MODE_CONTRIBUTOR_MODEL_ID = new Guid(SWITCH_BROWSE_MODE_CONTRIBUTOR_MODEL_ID_STR);

    #endregion

    #region Protected fields

    protected IResourceString _displayTitle;
    protected bool _notInBrowsing = false;

    #endregion

    protected override IEnumerable<string> GetMessageChannels()
    {
      IList<string> result = new List<string>(base.GetMessageChannels()) { WorkflowManagerMessaging.CHANNEL };
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
      bool isBrowseMode;
      string screenName;
      Guid currentWorkflowState;
      if (!GetRootState(out screenName, out isBrowseMode, out currentWorkflowState))
      {
        _notInBrowsing = false;
        return;
      }

      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      _displayTitle = LocalizationHelper.CreateResourceString(isBrowseMode ? Consts.RES_SWITCH_TO_MEDIALIBRARY_VIEW : Consts.RES_SWITCH_TO_BROWSE_SHARE_VIEW);
      _notInBrowsing = !workflowManager.IsAnyStateContainedInNavigationStack(new Guid[]
        {
            Consts.WF_STATE_ID_LOCAL_MEDIA_NAVIGATION_ROOT,
            Consts.WF_STATE_ID_BROWSE_MEDIA_NAVIGATION_ROOT,
        }) && MediaNavigationModel.GetNavigationData(workflowManager.CurrentNavigationContext, false) != null;
    }

    private static bool GetRootState(out string screenName, out bool isBrowseMode, out Guid currentWorkflowState)
    {
      screenName = null;
      isBrowseMode = false;
      currentWorkflowState = Guid.Empty;
      if (!MediaNavigationModel.IsNavigationDataEnabled)
        return false;

      NavigationData current = MediaNavigationModel.GetCurrentInstance().NavigationData;
      if (current == null)
        return false;
      // Find root NavigationData
      while (current.Parent != null)
        current = current.Parent;

      screenName = current.BaseViewSpecification.ViewDisplayName;
      isBrowseMode = current.BaseViewSpecification is BrowseMediaRootProxyViewSpecification;
      currentWorkflowState = current.CurrentWorkflowStateId;
      return true;
    }

    public override IResourceString DisplayTitle
    {
      get { return _displayTitle; }
    }

    public override bool IsActionVisible(NavigationContext context)
    {
      return _isHomeServerConnected && _notInBrowsing;
    }

    public override bool IsActionEnabled(NavigationContext context)
    {
      return true;
    }

    public override void Execute()
    {
      bool isBrowseMode;
      string screenName;
      Guid currentWorkflowState;
      if (!GetRootState(out screenName, out isBrowseMode, out currentWorkflowState))
        return;

      string oldScreen = null;
      if (isBrowseMode)
        NavigationData.LoadScreenHierarchy(screenName + "_OLD", out oldScreen);

      NavigationData.SaveScreenHierarchy(screenName,
        isBrowseMode ? oldScreen : Consts.USE_BROWSE_MODE,
        !isBrowseMode /* backup when switching to browse mode */);

      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      workflowManager.StartBatchUpdate();
      workflowManager.NavigatePopToState(currentWorkflowState, true);
      workflowManager.NavigatePush(currentWorkflowState);
      workflowManager.EndBatchUpdate();
    }
  }
}