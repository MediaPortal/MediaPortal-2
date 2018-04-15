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

using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace MediaPortal.Server
{
  [RunInstaller(true)]
  public class WindowsServiceInstaller : Installer
  {
    private readonly ServiceInstaller _serviceInstaller;
    private readonly ServiceProcessInstaller _serviceProcessInstaller;

    public WindowsServiceInstaller()
    {
      _serviceInstaller = new ServiceInstaller
                            {
                              ServiceName = "MP2 Server Service", 
                              Description = "Provides MediaPortal2 Server Service for all Clients",
                              StartType = ServiceStartMode.Manual
                            };

      _serviceProcessInstaller = new ServiceProcessInstaller { Account = ServiceAccount.LocalSystem }; // Local system is required for using impersonation!
      
      Installers.Add(_serviceInstaller);
      Installers.Add(_serviceProcessInstaller);
    }
  }
}
