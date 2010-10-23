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
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using MediaPortal.Core;
using MediaPortal.Core.Localization;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.UiComponents.Media.Models;

namespace MediaPortal.UiComponents.Media.Actions
{
  public class ManagePlaylistsAction : IWorkflowContributor
  {
    #region Consts

    public const string MANAGE_PLAYLISTS_ACTION_CONTRIBUTOR_MODEL_ID_STR = "2C3A747D-7FD7-408b-8843-31842A2EB6F3";

    public const string ADD_TO_PLAYLIST_RES = "[Media.ManagePlaylists]";

    #endregion

    /// <summary>
    /// Returns the information if the playlist management action should be visible in the current workflow state.
    /// </summary>
    protected bool ShowPlaylistManagement()
    {
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      return !workflowManager.IsStateContainedInNavigationStack(Consts.WF_STATE_ID_PLAYLISTS_OVERVIEW);
    }

    #region IWorkflowContributor implementation

    public event ContributorStateChangeDelegate StateChanged;

    public IResourceString DisplayTitle
    {
      get { return LocalizationHelper.CreateResourceString(ADD_TO_PLAYLIST_RES); }
    }

    public void Initialize()
    {
    }

    public void Uninitialize()
    {
    }

    public bool IsActionVisible(NavigationContext context)
    {
      return ShowPlaylistManagement();
    }

    public bool IsActionEnabled(NavigationContext context)
    {
      return true;
    }

    public void Execute()
    {
      ManagePlaylistsModel.ShowPlaylistsOverview();
    }

    #endregion
  }
}