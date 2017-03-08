#region Copyright (C) 2011-2012 MPExtended
// Copyright (C) 2011-2012 MPExtended Developers, http://mpextended.github.com/
// 
// MPExtended is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 2 of the License, or
// (at your option) any later version.
// 
// MPExtended is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with MPExtended. If not, see <http://www.gnu.org/licenses/>.
#endregion

using System;
using System.Diagnostics;
using System.Security.Principal;
using MediaPortal.Common;
using MediaPortal.ServiceMonitor.ViewModel;

namespace MediaPortal.ServiceMonitor.Utilities
{
  internal class UacServiceHelper
  {
    public static bool IsAdmin()
    {
      WindowsIdentity id = WindowsIdentity.GetCurrent();
      WindowsPrincipal p = new WindowsPrincipal(id);
      return p.IsInRole(WindowsBuiltInRole.Administrator);
    }

    public static bool StartService()
    {
      return RunUacServiceHandler("-m --command=StartService");
    }

    public static bool StopService()
    {
      return RunUacServiceHandler("-m --command=StopService");
    }

    public static bool RestartService()
    {
      return RunUacServiceHandler("-m --command=RestartService");
    }

    public static bool RunUacServiceHandler(string parameters)
    {
      // Launch itself as administrator
      ProcessStartInfo proc = new ProcessStartInfo();
      proc.UseShellExecute = true;
      proc.WorkingDirectory = Environment.CurrentDirectory;
      proc.FileName = System.Reflection.Assembly.GetExecutingAssembly().Location;
      proc.Verb = "runas";
      proc.Arguments = parameters;

      try
      {
        Process.Start(proc);
      }
      catch
      {
        // The user refused to allow privileges elevation.
        // Do nothing and return directly ...
        return false;
      }

      ServiceRegistration.Get<IAppController>().CloseMainApplication(); // Quit itself
      return true;
    }
  }
}
