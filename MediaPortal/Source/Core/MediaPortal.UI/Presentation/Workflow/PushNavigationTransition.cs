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

using System;
using System.Collections.Generic;
using MediaPortal.Common;
using MediaPortal.Common.Localization;

namespace MediaPortal.UI.Presentation.Workflow
{
  /// <summary>
  /// When invoked, this action pushes a workflow state onto the workflow navigation stack.
  /// </summary>
  public class PushNavigationTransition : WorkflowAction
  {
    #region Protected fields

    protected string _navigationContextDisplayLabel;
    protected Guid _targetStateId;

    #endregion

    public PushNavigationTransition(Guid actionId, string name, IEnumerable<Guid> sourceStateIds, IResourceString displayTitle,
        Guid targetStateId, string navigationContextDisplayLabel) : this(actionId, name, sourceStateIds, displayTitle, null, targetStateId, navigationContextDisplayLabel)
    {
    }

    public PushNavigationTransition(Guid actionId, string name, IEnumerable<Guid> sourceStateIds, IResourceString displayTitle, IResourceString helpText,
        Guid targetStateId, string navigationContextDisplayLabel) : base(actionId, name, sourceStateIds, displayTitle, helpText)
    {
      _navigationContextDisplayLabel = navigationContextDisplayLabel;
      _targetStateId = targetStateId;
    }

    /// <summary>
    /// Returns the id of the workflow state which will be pushed on the navigation context stack
    /// when this action is taken.
    /// </summary>
    public Guid TargetStateId
    {
      get { return _targetStateId; }
    }

    public override bool IsVisible(NavigationContext context)
    {
      return true;
    }

    public override bool IsEnabled(NavigationContext context)
    {
      return true;
    }

    /// <summary>
    /// Pushes the state with the <see cref="TargetStateId"/> onto the workflow navigation context stack.
    /// </summary>
    public override void Execute()
    {
      ServiceRegistration.Get<IWorkflowManager>().NavigatePush(TargetStateId, new NavigationContextConfig
        {
            NavigationContextDisplayLabel = _navigationContextDisplayLabel
        });
    }
  }
}
