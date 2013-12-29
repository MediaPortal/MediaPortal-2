#region Copyright (C) 2007-2013 Team MediaPortal

/*
    Copyright (C) 2007-2013 Team MediaPortal
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
using MediaPortal.Plugins.SlimTv.Client.Models;
using MediaPortal.Plugins.SlimTv.Interfaces.Extensions;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.UI.Presentation.Workflow;

namespace MediaPortal.Plugins.SlimTv.Client.Extensions
{
  /// <summary>
  /// <see cref="ExtendedSchedule"/> provides extended scheduling options like series recordings.
  /// </summary>
  class ExtendedSchedule: IProgramAction
  {
    public bool ShowExtendedRecordingcScreen(IProgram program)
    {
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      NavigationContextConfig navigationContextConfig = new NavigationContextConfig();
      navigationContextConfig.AdditionalContextVariables = new Dictionary<string, object>();
      navigationContextConfig.AdditionalContextVariables[SlimTvClientModel.KEY_PROGRAM] = program;
      workflowManager.NavigatePush(new Guid("3C6081CB-88DC-44A7-9E17-8D7BFE006EE5"), navigationContextConfig);
      return true;
    }

    public bool IsAvailable (IProgram program)
    {
      return true;
    }

    public ProgramActionDelegate ProgramAction
    {
      get { return ShowExtendedRecordingcScreen; }
    }
  }
}
