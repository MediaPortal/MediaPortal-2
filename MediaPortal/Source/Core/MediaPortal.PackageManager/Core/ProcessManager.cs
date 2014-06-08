#region Copyright (C) 2007-2014 Team MediaPortal
/*
    Copyright (C) 2007-2014 Team MediaPortal
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

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using MediaPortal.Common.Logging;

namespace MediaPortal.PackageManager.Core
{
	internal class ProcessManager
	{
	  private readonly ILogger _log;
	  private readonly List<string> _sharedProcessNames = new List<string> { "TvSetup.exe" };
	  private readonly List<string> _clientProcessNames = new List<string> { "MP2-Client.exe" };
	  private readonly List<string> _serverProcessNames = new List<string> { "MP2-Server.exe" };
    
    public ProcessManager( ILogger log )
    {
      _log = log;
    }

	  public bool Stop(bool clientOnly)
	  {
	    var processNames = _sharedProcessNames.Concat(clientOnly ? _clientProcessNames : _serverProcessNames);
	    foreach (var processName in processNames)
	    {
	      var processes = Process.GetProcessesByName(processName);
	      foreach (var process in processes)
	      {
	        process.Close();
	        if (!process.WaitForExit(5000))
            process.Kill();	        
	      }
	    }
	    return true;
		}

	  public bool Start(bool clientOnly)
		{
	    var processNames = clientOnly ? _clientProcessNames : _serverProcessNames;
	    foreach (var processName in processNames)
	    {
	      var basePath = ""; // TODO we need the base path to the executables here
	      var processPath = Path.Combine(basePath, processName);
	      var process = Process.Start(processPath);
        if (process == null || process.HasExited)
	        return false;
	    }
	    return true;
	  }
	}
}
