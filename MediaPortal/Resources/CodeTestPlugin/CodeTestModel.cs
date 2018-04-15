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
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.Workflow;

namespace MediaPortal.Test.CodeTest
{
  /// <summary>
  /// Model which atends the code test workflow state.
  /// </summary>
  public class CodeTestModel : IWorkflowModel
  {
    public const string MODEL_ID_STR = "3E07F585-C3DE-4FB0-BD18-707AD9C78861";

    #region Protected fields

    #endregion

    #region Public members

    public void ShowScreenInTransientState(string screen)
    {
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      workflowManager.NavigatePushTransient(new WorkflowState(Guid.NewGuid(), screen, screen, true, screen, false, true, ModelId, WorkflowType.Workflow), null);
    }

    public void ContextMenuTest_Command()
    {
      IDialogManager dialogManager = ServiceRegistration.Get<IDialogManager>();
      string header = "Command executed";
      string text = "The command has been executed. What about the ContextMenuCommand?";
      dialogManager.ShowDialog(header, text, DialogType.OkDialog, false, DialogButtonType.Ok);
    }

    public void ContextMenuTest_ContextMenuCommand()
    {
      IDialogManager dialogManager = ServiceRegistration.Get<IDialogManager>();
      string header = "ContextMenuCommand executed";
      string text = "The ContextMenuCommand has been executed. Great.";
      dialogManager.ShowDialog(header, text, DialogType.OkDialog, false, DialogButtonType.Ok);
    }

    #endregion

    #region IWorkflowModel implementation

    public Guid ModelId
    {
      get { return new Guid(MODEL_ID_STR); }
    }

    public bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
    {
      return true;
    }

    public void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      // We could initialize some data here when entering the media navigation state
    }

    public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      // We could dispose some data here when exiting media navigation context
    }

    public void ChangeModelContext(NavigationContext oldContext, NavigationContext newContext, bool push)
    {
      // We could initialize some data here when changing the media navigation state
    }

    public void Deactivate(NavigationContext oldContext, NavigationContext newContext)
    {
    }

    public void Reactivate(NavigationContext oldContext, NavigationContext newContext)
    {
    }

    public void UpdateMenuActions(NavigationContext context, IDictionary<Guid, WorkflowAction> actions)
    {
    }

    public ScreenUpdateMode UpdateScreen(NavigationContext context, ref string screen)
    {
      return ScreenUpdateMode.AutoWorkflowManager;
    }

    #endregion
  }
}