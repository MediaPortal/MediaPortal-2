#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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

using MediaPortal.Common.UPnP;
using UPnP.Infrastructure.Dv;

namespace MediaPortal.UI.Services.ServerCommunication
{
  /// <summary>
  /// Encapsulates the MediaPortal 2 UPnP frontend server device.
  /// </summary>
  public class UPnPFrontendServer : UPnPServer
  {
    public const int SSDP_ADVERTISMENT_INTERVAL = 180;
    protected readonly UPnPSystemResumeHelper _systemResumeHelper;

    public UPnPFrontendServer(string frontendServerSystemId)
    {
      AddRootDevice(new MP2FrontendServerDevice(frontendServerSystemId));
      // TODO: add UPnP standard MediaRenderer device: it's not implemented yet
      //AddRootDevice(new UPnPMediaRendererDevice(...));

      _systemResumeHelper = new UPnPSystemResumeHelper(this);
    }

    public void Start()
    {
      Bind(SSDP_ADVERTISMENT_INTERVAL);
      _systemResumeHelper.Startup();
    }

    public void Stop()
    {
      _systemResumeHelper.Shutdown();
      Close();
    }

    public override void Dispose()
    {
      _systemResumeHelper.Dispose();
      base.Dispose();
    }
  }
}
