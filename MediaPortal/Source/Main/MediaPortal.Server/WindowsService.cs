#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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

using System.ServiceProcess;

namespace MediaPortal.Server
{
  public partial class WindowsService : ServiceBase
  {
    protected ApplicationLauncher _launcher = null;

    public WindowsService()
    {
      InitializeComponent();
      ServiceName = "MP2 Server Service";
      CanStop = true;
      CanPauseAndContinue = false;
      CanHandlePowerEvent = true;
      AutoLog = false;
    }
    
    protected override void OnStart(string[] args)
    {
      if (_launcher != null)
        return;
      _launcher = new ApplicationLauncher(null);
      _launcher.Start();
    }

    protected override void OnStop()
    {
      if (_launcher == null)
        return;
      _launcher.Stop();
    }
  }
}
