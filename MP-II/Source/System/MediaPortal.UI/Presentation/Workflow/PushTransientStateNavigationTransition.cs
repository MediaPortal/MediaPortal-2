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

using System;
using System.Collections.Generic;
using MediaPortal.Core;
using MediaPortal.Core.Localization;

namespace MediaPortal.UI.Presentation.Workflow
{
  /// <summary>
  /// When invoked, this action pushes a new, transient workflow state onto the workflow navigation stack.
  /// This can be used by workflow models which need to build workflow states at runtime.
  /// </summary>
  public class PushTransientStateNavigationTransition : WorkflowAction
  {
    #region Protected fields

    protected string _displayLabel;
    protected WorkflowState _transientState;
    protected IDictionary<string, object> _workflowNavigationContextVariables;

    #endregion

    public PushTransientStateNavigationTransition(Guid actionId, string name, string displayLabel, Guid sourceState,
        WorkflowState transientTargetState, IResourceString displayTitle) :
        base(actionId, name, sourceState, displayTitle)
    {
      _displayLabel = displayLabel;
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
    /// Additional workflow navigation context variables to be set when navigated to the transient workflow state.
    /// </summary>
    public IDictionary<string, object> WorkflowNavigationContextVariables
    {
      get { return _workflowNavigationContextVariables; }
      set { _workflowNavigationContextVariables = value; }
    }

    public override bool IsVisible
    {
      get { return true; }
    }

    public override bool IsEnabled
    {
      get { return true; }
    }

    /// <summary>
    /// Pushes the <see cref="TargetState"/> onto the workflow navigation context stack.
    /// </summary>
    public override void Execute()
    {
      IWorkflowManager workflowManager = ServiceScope.Get<IWorkflowManager>();
      NavigationContextConfig config = new NavigationContextConfig {
        NavigationContextDisplayLabel = _displayLabel,
        AdditionalContextVariables = _workflowNavigationContextVariables
      };
      workflowManager.NavigatePushTransient(_transientState, config);
    }
  }
}
