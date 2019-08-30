#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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

using MediaPortal.Common.PluginManager;
using MediaPortal.Common.Services.PluginManager.Builders;
using System;

namespace SkinSettings
{
  public class WorkflowStateViewModesBuilder : IPluginItemBuilder
  {
    public const string WF_VIEWMODES_PROVIDER_PATH = "/Workflow/ViewModes";

    #region IPluginItemBuilder Member

    public object BuildItem(PluginItemMetadata itemData, PluginRuntime plugin)
    {
      BuilderHelper.CheckParameter("Skin", itemData);
      BuilderHelper.CheckParameter("ViewModes", itemData);
      BuilderHelper.CheckParameter("StateId", itemData);
      return new WorkflowStateViewModesRegistration(itemData.Attributes["Skin"], itemData.Attributes["StateId"], itemData.Attributes["ViewModes"]);
    }

    public void RevokeItem(object item, PluginItemMetadata itemData, PluginRuntime plugin)
    {
      // Noting to do
    }

    public bool NeedsPluginActive(PluginItemMetadata itemData, PluginRuntime plugin)
    {
      return false;
    }

    #endregion
  }

  public class WorkflowStateViewModesRegistration
  {
    /// <summary>
    /// Unique ID of Workflow state.
    /// </summary>
    public Guid StateId { get; private set; }

    /// <summary>
    /// Skin name for which this view mode is offered.
    /// </summary>
    public string Skin { get; private set; }

    /// <summary>
    /// Comma-separated list of view modes.
    /// </summary>
    public string ViewModes { get; private set; }

    public WorkflowStateViewModesRegistration(string skinName, string stateId, string viewModes)
    {
      Skin = skinName;
      StateId = new Guid(stateId);
      ViewModes = viewModes;
    }
  }
}
