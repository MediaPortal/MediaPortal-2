#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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
using Microsoft.Deployment.WindowsInstaller;

namespace CustomActions
{
  public class LAVFilters
  {
    [CustomAction]
    public static ActionResult InstallLAVFilters(Session session)
    {
      CustomActionRunner customActionRunner = new CustomActionRunner(new RunnerHelper());

      try
      {
        session.Log("LAVFilters: Checking if LAVFilters already installed");
        if (customActionRunner.IsLavFiltersAlreadyInstalled())
        {
          session.Log("LAVFilters: Already installed.");
          return ActionResult.NotExecuted;
        }

        session.Log("LAVFilters: Downloading installer...");
        if (!customActionRunner.IsLavFiltersDownloaded())
        {
          session.Log("LAVFilters: Download failed.");
          return ActionResult.Failure;
        }

        session.Log("LAVFilters: Run the installer.");
        if (!customActionRunner.InstallLavFilters())
        {
          session.Log("LAVFilters: Installation failed.");
          return ActionResult.Failure;
        }
        session.Log("LAVFilters: Successfully installed LAVFilters.");
        return ActionResult.Success;
      }
      catch (Exception ex)
      {
        session.Log("LAVFilters: Error: " + ex.Message);
        return ActionResult.Failure;
      }
    }
  }
}
