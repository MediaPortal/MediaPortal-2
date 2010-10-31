#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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

using MediaPortal.Core;
using MediaPortal.Core.Localization;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.UiComponents.Media.Models;

namespace MediaPortal.UiComponents.Media.Actions
{
  public class SavePlaylistAction : IWorkflowContributor
  {
    #region Consts

    public const string SAVE_PLAYLISTS_ACTION_CONTRIBUTOR_MODEL_ID_STR = "02848CDD-34F0-4719-9A52-DA959E848409";

    public const string SAVE_PLAYLIST_RES = "[Media.SavePlaylistAction]";
    public const string SAVE_CURRENT_PLAYLIST_RES = "[Media.SaveCurrentPlaylistAction]";

    #endregion

    protected bool ShowSavePLAction()
    {
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      return workflowManager.CurrentNavigationContext.WorkflowState.StateId == Consts.WF_STATE_ID_SHOW_PLAYLIST ||
          workflowManager.CurrentNavigationContext.WorkflowState.StateId == Consts.WF_STATE_ID_EDIT_PLAYLIST;
    }

    protected bool ShowSaveCurrentPLAction()
    {
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      return workflowManager.CurrentNavigationContext.WorkflowState.StateId == Consts.WF_STATE_ID_PLAYLISTS_OVERVIEW;
    }

    #region IWorkflowContributor implementation

    public event ContributorStateChangeDelegate StateChanged;

    public IResourceString DisplayTitle
    {
      get
      {
        return ShowSavePLAction() ? LocalizationHelper.CreateResourceString(SAVE_PLAYLIST_RES) :
            LocalizationHelper.CreateResourceString(SAVE_CURRENT_PLAYLIST_RES);
      }
    }

    public void Initialize()
    {
    }

    public void Uninitialize()
    {
    }

    public bool IsActionVisible(NavigationContext context)
    {
      return (ShowSavePLAction() || ShowSaveCurrentPLAction()) &&
          ServiceRegistration.Get<IPlayerContextManager>().CurrentPlayerIndex > -1;
    }

    public bool IsActionEnabled(NavigationContext context)
    {
      return true;
    }

    public void Execute()
    {
      ManagePlaylistsModel.SaveCurrentPlaylist();
    }

    #endregion
  }
}