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
using System.Collections.Generic;
using System.Linq;

namespace InputDevices.Models.MappableItemProviders
{
  /// <summary>
  /// Base implementation of <see cref="IMappableItemProvider"/> for providers that provide a subset of workflow actions that can be mapped to.
  /// </summary>
  public abstract class AbstractWorkflowActionItemProvider : IMappableItemProvider
  {
    public IEnumerable<MappableItem> GetMappableItems()
    {
      WorkflowManager.Lock.EnterReadLock();
      try
      {
        return WorkflowManager.MenuStateActions.Values
          .Where(a => ShouldIncludeWorkflowAction(a))
          .Select(a => new MappableItem(
            a.DisplayTitle,
            InputAction.CreateWorkflowAction(a.ActionId)
          )).ToList();
      }
      finally
      {
        WorkflowManager.Lock.ExitReadLock();
      }
    }

    /// <summary>
    /// Called for each workflow action when getting the list of mappable items. Implementations should return whether
    /// the specified <see cref="WorkflowAction"/> should be included in the list.
    /// </summary>
    /// <param name="action">The <see cref="WorkflowAction"/> to determine whether to include.</param>
    /// <returns><c>true</c> if the action should be included; else <c>false</c>.</returns>
    protected abstract bool ShouldIncludeWorkflowAction(WorkflowAction action);

    protected IWorkflowManager WorkflowManager
    {
      get { return ServiceRegistration.Get<IWorkflowManager>(); }
    }
  }
}
