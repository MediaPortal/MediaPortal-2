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
using InputDevices.Mapping.ActionExecutors;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using System;

namespace InputDevices.Mapping
{
  public class InputActionExecutorFactory
  {
    public bool TryCreate(InputAction inputAction, out IInputActionExecutor inputActionExecutor)
    {
      inputActionExecutor = null;
      try
      {
        if (inputAction.Type == InputAction.KEY_ACTION_TYPE)
          inputActionExecutor = new KeyActionExecutor(inputAction);
        else if (inputAction.Type == InputAction.WORKFLOW_ACTION_TYPE)
          inputActionExecutor = new WorkflowActionExecutor(inputAction);
        else if (inputAction.Type == InputAction.CONFIG_LOCATION_TYPE)
          inputActionExecutor = new ConfigSectionExecutor(inputAction);
        else
          inputActionExecutor = null;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error($"{nameof(InputActionExecutorFactory)}: Error creating executor for input action", ex);
      }
      return inputActionExecutor != null;
    }
  }
}
