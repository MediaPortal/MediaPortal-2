#region Copyright (C) 2007-2021 Team MediaPortal

/*
    Copyright (C) 2007-2021 Team MediaPortal
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

using InputDevices.Common.Mapping;
using MediaPortal.Common;
using MediaPortal.UI.Presentation.Workflow;
using System;

namespace InputDevices.Mapping.ActionExecutors
{
  /// <summary>
  /// Executes the workflow action defined by an <see cref="InputAction"/> with type <see cref="InputAction.WORKFLOW_ACTION_TYPE"/>.
  /// </summary>
  public class WorkflowActionExecutor : IInputActionExecutor
  {
    protected WorkflowAction _workflowAction;

    public WorkflowActionExecutor(InputAction inputAction)
    {
      if (inputAction.Type != InputAction.WORKFLOW_ACTION_TYPE)
        throw new ArgumentException($"{nameof(WorkflowActionExecutor)}: {nameof(InputAction.Type)} must be {InputAction.WORKFLOW_ACTION_TYPE}", nameof(inputAction));

      _workflowAction = GetWorkflowAction(inputAction);
    }

    protected static WorkflowAction GetWorkflowAction(InputAction inputAction)
    {
      if (!Guid.TryParse(inputAction.Action, out Guid workflowActionId))
        throw new ArgumentException($"{nameof(WorkflowActionExecutor)}: {nameof(InputAction.Action)} '{inputAction.Action}' is not a valid {nameof(Guid)}", nameof(inputAction));

      WorkflowAction workflowAction;
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      workflowManager.Lock.EnterReadLock();
      try
      {
        if (!workflowManager.MenuStateActions.TryGetValue(workflowActionId, out workflowAction))
          throw new ArgumentException($"{nameof(WorkflowActionExecutor)}: No {nameof(WorkflowAction)} found with id '{workflowActionId}'", nameof(inputAction));
      }
      finally
      {
        workflowManager.Lock.ExitReadLock();
      }

      return workflowAction;
    }

    public void Execute()
    {
      _workflowAction.Execute();
    }
  }
}
