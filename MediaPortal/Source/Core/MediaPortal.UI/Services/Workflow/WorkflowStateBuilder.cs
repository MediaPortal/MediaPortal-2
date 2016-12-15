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
using MediaPortal.Common.Logging;
using MediaPortal.Common.PluginManager;
using MediaPortal.UI.Presentation.Workflow;

namespace MediaPortal.UI.Services.Workflow
{
  /// <summary>
  /// Builds an item of type <c>WorkflowState</c> or <c>DialogState</c>.
  /// </summary>
  public class WorkflowStateBuilder : IPluginItemBuilder
  {
    protected WorkflowState BuildWorkflowState(PluginItemMetadata itemData)
    {
      IDictionary<string, string> attributes = itemData.Attributes;
      Guid id = Guid.Empty;
      try
      {
        string name;
        string displayLabel;
        bool isTemporary = false;
        string mainScreen = null;
        bool inheritMenu = false;
        Guid? workflowModelId = null;
        if (string.IsNullOrEmpty(itemData.Id))
          throw new ArgumentException(string.Format("WorkflowState: Id must be specified"));
        id = new Guid(itemData.Id);
        if (!attributes.TryGetValue("Name", out name))
          throw new ArgumentException(string.Format("WorkflowState with id '{0}': 'Name' attribute missing", id));
        if (!attributes.TryGetValue("DisplayLabel", out displayLabel))
          throw new ArgumentException(string.Format("WorkflowState with id '{0}': 'DisplayLabel' attribute missing", id));
        string tmpStr;
        if (attributes.TryGetValue("WorkflowModel", out tmpStr))
          workflowModelId = new Guid(tmpStr);
        if (!attributes.TryGetValue("MainScreen", out mainScreen))
        {
          mainScreen = null;
          if (workflowModelId == null)
            throw new ArgumentException(string.Format("WorkflowState '{0}': Either 'WorkflowModel' or 'MainScreen' atrribute must be specified", name));
        }
        if (attributes.TryGetValue("Temporary", out tmpStr) && !bool.TryParse(tmpStr, out isTemporary))
          throw new ArgumentException("'Temporary' attribute has to be of type bool");
        if (attributes.TryGetValue("InheritMenu", out tmpStr) && !bool.TryParse(tmpStr, out inheritMenu))
          throw new ArgumentException("'InheritMenu' attribute has to be of type bool");
        return new WorkflowState(id, name, displayLabel, isTemporary, mainScreen, inheritMenu, false,
            workflowModelId, WorkflowType.Workflow);
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("WorkflowStateBuilder: Workflow state '{0}' cannot be built", e, id);
        return null;
      }
    }

    private static WorkflowState BuildDialogState(PluginItemMetadata itemData)
    {
      IDictionary<string, string> attributes = itemData.Attributes;
      Guid id = Guid.Empty;
      try
      {
        string name;
        string displayLabel;
        bool isTemporary = false;
        string dialogScreen = null;
        Guid? workflowModelId = null;
        if (string.IsNullOrEmpty(itemData.Id))
          throw new ArgumentException(string.Format("WorkflowState: Id must be specified"));
        id = new Guid(itemData.Id);
        if (!attributes.TryGetValue("Name", out name))
          throw new ArgumentException(string.Format("WorkflowState with id '{0}': 'Name' attribute missing", id));
        if (!attributes.TryGetValue("DisplayLabel", out displayLabel))
          throw new ArgumentException(string.Format("WorkflowState with id '{0}': 'DisplayLabel' attribute missing", id));
        string tmpStr;
        if (attributes.TryGetValue("WorkflowModel", out tmpStr))
          workflowModelId = new Guid(tmpStr);
        if (!attributes.TryGetValue("DialogScreen", out dialogScreen))
        {
          dialogScreen = null;
          if (workflowModelId == null)
            throw new ArgumentException(string.Format("WorkflowState '{0}': Either 'WorkflowModel' or 'DialogScreen' atrribute must be specified", name));
        }
        if (attributes.TryGetValue("Temporary", out tmpStr) && !bool.TryParse(tmpStr, out isTemporary))
          throw new ArgumentException("'Temporary' attribute has to be of type bool");
        return new WorkflowState(id, name, displayLabel, isTemporary, dialogScreen, false, false,
            workflowModelId, WorkflowType.Dialog);
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("WorkflowStateBuilder: Workflow state '{0}' cannot be built", e, id);
        return null;
      }
    }

    #region IPluginItemBuilder implementation

    public object BuildItem(PluginItemMetadata itemData, PluginRuntime plugin)
    {
      if (itemData.BuilderName == "WorkflowState")
        return BuildWorkflowState(itemData);
      if (itemData.BuilderName == "DialogState")
        return BuildDialogState(itemData);
      return null;
    }

    public void RevokeItem(object item, PluginItemMetadata itemData, PluginRuntime plugin)
    {
      // Nothing to do here - the WorkflowManager will listen for workflow state withdrawals
    }

    public bool NeedsPluginActive(PluginItemMetadata itemData, PluginRuntime plugin)
    {
      return false;
    }

    #endregion
  }
}
