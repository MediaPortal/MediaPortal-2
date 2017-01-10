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
using MediaPortal.Common.Commands;
using MediaPortal.Common.Localization;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UiComponents.Media.General;

namespace MediaPortal.UiComponents.Media.Models
{
  public class MediaFilterModel
  {
    #region Consts

    public const string MF_MODEL_ID_STR = "52DFDB33-5D94-41AC-BBB7-2B070473FB48";
    public static Guid MF_MODEL_ID = new Guid(MF_MODEL_ID_STR);

    #endregion

    protected readonly ItemsList _filterItemsList = new ItemsList();

    protected NavigationData GetCurrentNavigationData()
    {
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      return MediaNavigationModel.GetNavigationData(workflowManager.CurrentNavigationContext, false);
    }

    protected void UpdateFiltersList()
    {
      _filterItemsList.Clear();
      NavigationData navigationData = GetCurrentNavigationData();
      IList<WorkflowAction> actions = navigationData.GetWorkflowActions();
      if (actions == null)
        return;

      string currentScreenTitle = LocalizationHelper.CreateResourceString(navigationData.CurrentScreenData?.MenuItemLabel)?.Evaluate();
      foreach (WorkflowAction action in actions)
      {
        WorkflowAction actionCopy = action;
        ListItem screenItem = new ListItem(Consts.KEY_NAME, action.DisplayTitle)
        {
          Command = new MethodDelegateCommand(actionCopy.Execute),
          Selected = currentScreenTitle == action.DisplayTitle?.Evaluate()
        };
        screenItem.AdditionalProperties[Consts.KEY_FILTER] = actionCopy;
        _filterItemsList.Add(screenItem);
      }
      _filterItemsList.FireChange();
    }

    #region Members to be accessed from the GUI

    public ItemsList FilterItemsList
    {
      get
      {
        UpdateFiltersList();
        return _filterItemsList;
      }
    }

    #endregion
  }
}
