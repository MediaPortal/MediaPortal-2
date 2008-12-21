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
using System.Collections.Generic;
using MediaPortal.Presentation.Workflow;

namespace MediaPortal.Presentation.Models
{
  /// <summary>
  /// A workflow model is a special GUI model which is able to attend some states of a GUI workflow.
  /// It provides methods to track the current workflow state and to enrich the state with
  /// special state content like special menu- and context-menu-actions.
  /// </summary>
  public interface IWorkflowModel
  {
    /// <summary>
    /// Returns the id of this model. The returned id has to be the same as the id used to register
    /// this model in the plugin descriptor.
    /// </summary>
    Guid ModelId { get; }

    /// <summary>
    /// Informs this model about the entance of a new workflow navigation step.
    /// </summary>
    /// <param name="context">The workflow navigation context which was entered.</param>
    void StartModelContext(NavigationContext context);

    /// <summary>
    /// Informs this model about the quitting of the specified current navigation <paramref name="oldContext"/>.
    /// </summary>
    /// <param name="oldContext">The old navigation context which will be quit.</param>
    /// <param name="newContext">The new navigation context which will be entered.</param>
    void ExitModelContext(NavigationContext oldContext, NavigationContext newContext);

    /// <summary>
    /// Adds additional menu actions which are created dynamically for the state of the specified
    /// navigation <paramref name="context"/>, or updates/removes existing actions.
    /// </summary>
    /// <remarks>
    /// The updated collection of actions should remain valid while the specified navigation
    /// <paramref name="context"/> is valid.
    /// </remarks>
    /// <param name="context">Current navigation context, which should be enriched with additional
    /// dynamic menu actions.</param>
    /// <param name="actions">Collection where this model can add additional menu actions valid for
    /// the specified navigation <paramref name="context"/>.</param>
    void UpdateMenuActions(NavigationContext context, ICollection<WorkflowStateAction> actions);

    /// <summary>
    /// Adds additional context menu actions which are created dynamically for the state of the specified
    /// navigation <paramref name="context"/>, or updates/removes existing actions.
    /// </summary>
    /// <remarks>
    /// The updated collection of actions should remain valid while the specified navigation
    /// <paramref name="context"/> is valid.
    /// </remarks>
    /// <param name="context">Current navigation context, which should be enriched with additional
    /// dynamic context menu actions.</param>
    /// <param name="actions">Collection where this model can add additional context menu actions valid for
    /// the specified navigation <paramref name="context"/>.</param>
    void UpdateContextMenuActions(NavigationContext context, ICollection<WorkflowStateAction> actions);
  }
}