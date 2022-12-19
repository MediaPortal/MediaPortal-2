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

using MediaPortal.UI.Presentation.Workflow;
using System;

namespace InputDevices.Models.MappableItemProviders
{
  /// <summary>
  /// Implementation of <see cref="IMappableItemProvider"/> that provides a list of screen items that can be mapped to.
  /// </summary>
  public class ScreenActionItemProvider : AbstractWorkflowActionItemProvider
  {
    protected static readonly Guid HOME_STATE_ID = new Guid("7F702D9C-F2DD-42da-9ED8-0BA92F07787F");

    protected override bool ShouldIncludeWorkflowAction(WorkflowAction action)
    {
      return action?.SourceStateIds?.Contains(HOME_STATE_ID) == true;
    }
  }
}
