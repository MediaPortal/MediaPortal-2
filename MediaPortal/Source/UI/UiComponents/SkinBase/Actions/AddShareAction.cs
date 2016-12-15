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
using MediaPortal.Common.Localization;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UI.ServerCommunication;
using MediaPortal.UiComponents.SkinBase.General;

namespace MediaPortal.UiComponents.SkinBase.Actions
{
  /// <summary>
  /// Action to add a share. Depending on the evaluation if our home server runs at the same machine as the GUI,
  /// we either push the "choose resource provider" or the "choose system" workflow state.
  /// In case the home server runs at the same machine, we don't provide the local shares configuration because
  /// in each case, the local server should host our shares.
  /// If the local server is not online, we don't provide any add shares action.
  /// </summary>
  public class AddShareAction : IWorkflowContributor
  {
    #region Consts

    public const string ADD_SHARE_CONTRIBUTOR_MODEL_ID_STR = "9E456C79-3FF1-4040-8CD7-4247C4C12817";
    public static readonly Guid ADD_SHARE_CONTRIBUTOR_MODEL_ID = new Guid(ADD_SHARE_CONTRIBUTOR_MODEL_ID_STR);

    #endregion

    #region IWorkflowContributor implementation

    public event ContributorStateChangeDelegate StateChanged;

    public IResourceString DisplayTitle
    {
      get { return null; }
    }

    public void Initialize()
    {
    }

    public void Uninitialize()
    {
    }

    public bool IsActionVisible(NavigationContext context)
    {
      return true;
    }

    public bool IsActionEnabled(NavigationContext context)
    {
      // We could listen for the home server's attachment and connection state and change this return value according to those states.
      // But I think that makes too much work for a function which will only be used very rarely.
      return true;
    }

    public void Execute()
    {
      IServerConnectionManager serverConnectionManager = ServiceRegistration.Get<IServerConnectionManager>();
      SystemName homeServerSystem = serverConnectionManager.LastHomeServerSystem;
      bool localHomeServer = homeServerSystem != null && homeServerSystem.IsLocalSystem();
      bool homeServerConnected = serverConnectionManager.IsHomeServerConnected;
      if (localHomeServer && !homeServerConnected)
      {
        // Our home server is local, i.e. all shares of this system must be configured at the server, but the server is not online at the moment.
        IDialogManager dialogManager = ServiceRegistration.Get<IDialogManager>();
        dialogManager.ShowDialog(Consts.RES_CANNOT_ADD_SHARES_TITLE, Consts.RES_CANNOT_ADD_SHARE_LOCAL_HOME_SERVER_NOT_CONNECTED, DialogType.OkDialog, false,
            DialogButtonType.Ok);
        return;
      }

      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      workflowManager.NavigatePush(Consts.WF_STATE_ID_SHARE_ADD_CHOOSE_SYSTEM);
    }

    #endregion
  }
}
