#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using MediaPortal.Core;
using MediaPortal.Presentation.DataObjects;

namespace MediaPortal.Presentation.Workflow
{
  public class PushTransientStateNavigationTransition : WorkflowStateAction
  {
    #region Protected fields

    protected WorkflowState _transientState;

    #endregion

    public PushTransientStateNavigationTransition(Guid actionId, string name, Guid sourceState,
        WorkflowState transientTargetState, IResourceString displayTitle) :
        base(actionId, name, sourceState, displayTitle)
    {
      _transientState = transientTargetState;
    }

    /// <summary>
    /// Returns the id of the workflow state which will be pushed on the navigation context stack
    /// when this action is taken.
    /// </summary>
    public WorkflowState TargetState
    {
      get { return _transientState; }
    }

    /// <summary>
    /// Pushes the <see cref="TargetState"/> onto the workflow navigation context stack.
    /// </summary>
    public override void Execute()
    {
      ServiceScope.Get<IWorkflowManager>().NavigatePushTransient(TargetState);
    }
  }
}
