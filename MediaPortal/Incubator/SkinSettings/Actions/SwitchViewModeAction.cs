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

using MediaPortal.Common;
using MediaPortal.Common.Localization;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.Workflow;

namespace SkinSettings.Actions
{
  public class SwitchViewModeAction : IWorkflowContributor
  {
    #region IWorkflowContributor implementation

    public event ContributorStateChangeDelegate StateChanged;

    public IResourceString DisplayTitle
    {
      get
      {
        const string RES_SWITCH_VIEW_MODE = "[Media.SwitchViewModeMenuItem]";
        return LocalizationHelper.CreateResourceString(RES_SWITCH_VIEW_MODE);
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
      var wfVmModel = (WorkflowStateViewModeModel)ServiceRegistration.Get<IWorkflowManager>().GetModel(WorkflowStateViewModeModel.VM_MODEL_ID);
      wfVmModel.Update();
      return wfVmModel.ViewModeItemsList.Count > 1;
    }

    public bool IsActionEnabled(NavigationContext context)
    {
      return true;
    }

    public void Execute()
    {
      const string DIALOG_SWITCH_VIEW_MODE = "DialogSwitchGenericViewMode";
      IScreenManager screenManager = ServiceRegistration.Get<IScreenManager>();
      screenManager.ShowDialog(DIALOG_SWITCH_VIEW_MODE);
    }

    #endregion
  }
}
