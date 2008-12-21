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
  public class PushNavigationTransition : WorkflowStateAction
  {
    #region Protected fields

    protected Guid _targetState;

    #endregion

    public PushNavigationTransition(Guid actionId, string name, Guid sourceState, Guid targetState,
        IResourceString displayTitle) :
        base(actionId, name, sourceState, displayTitle)
    {
      _targetState = targetState;
    }

    /// <summary>
    /// Returns the id of the workflow state which will be pushed on the navigation context stack
    /// when this action is taken.
    /// </summary>
    public Guid TargetState
    {
      get { return _targetState; }
    }

    /// <summary>
    /// Pushes the <see cref="TargetState"/> onto the workflow navigation context stack.
    /// </summary>
    public override void Execute()
    {
      ServiceScope.Get<IWorkflowManager>().NavigatePush(TargetState);
    }
  }
}
