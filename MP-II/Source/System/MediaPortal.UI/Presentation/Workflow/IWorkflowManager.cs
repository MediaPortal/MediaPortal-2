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

namespace MediaPortal.Presentation.Workflow
{
  /// <summary>
  /// Component for tracking application states and managing application workflows.
  /// </summary>
  public interface IWorkflowManager
  {
    /// <summary>
    /// Returns all currently known workflow states.
    /// </summary>
    /// <remarks>
    /// This collection will change when plugins are added or removed.
    /// </remarks>
    ICollection<WorkflowState> States { get; }

    /// <summary>
    /// Returns all currently known menu state actions.
    /// </summary>
    /// <remarks>
    /// This collection maybe change when plugins are added or removed.
    /// </remarks>
    ICollection<WorkflowStateAction> MenuStateActions { get; }

    /// <summary>
    /// Returns all currently known context menu state actions.
    /// </summary>
    /// <remarks>
    /// This collection maybe change when plugins are added or removed.
    /// </remarks>
    ICollection<WorkflowStateAction> ContextMenuStateActions { get; }

    /// <summary>
    /// Returns the navigation structure consisting of a stack of currently active navigation contexts.
    /// </summary>
    Stack<NavigationContext> NavigationContextStack { get; }

    /// <summary>
    /// Returns the current navigation context in the <see cref="NavigationContextStack"/>.
    /// </summary>
    /// <remarks>
    /// This is a convenience property for calling <c><see cref="NavigationContextStack"/>.Peek()</c>.
    /// </remarks>
    NavigationContext CurrentNavigationContext { get; }

    /// <summary>
    /// Navigates to the specified state. This will push a new navigation context entry containing
    /// the specified state on top of the navigation context stack. This realizes a forward navigation.
    /// </summary>
    /// <param name="stateId">Id of the state to enter.</param>
    void NavigatePush(Guid stateId);

    /// <summary>
    /// Removes the <paramref name="count"/> youngest navigation context levels from the
    /// <see cref="NavigationContextStack"/>. This realizes a "back" navigation.
    /// </summary>
    /// <param name="count">Number of navigation levels to remove.</param>
    void NavigatePop(int count);

    // TODO: State and model context listeners
  }
}
