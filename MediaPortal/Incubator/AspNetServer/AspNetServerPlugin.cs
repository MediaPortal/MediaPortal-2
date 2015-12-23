#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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

using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.PluginManager;
using Microsoft.AspNet.Hosting;
using System;
using MediaPortal.Common.Settings;
using MediaPortal.Plugins.AspNetServer.Logger;

namespace MediaPortal.Plugins.AspNetServer
{
  public class AspNetServerPlugin : IPluginStateTracker
  {
    private IDisposable _engine;

    public void Activated(PluginRuntime pluginRuntime)
    {
      try
      {
        var app = new WebApplicationBuilder()
          .UseStartup<Startup>()
          .UseServerFactory(ServiceRegistration.Get<ISettingsManager>().Load<AspNetServerSettings>().CheckAndGetServer())
          .ConfigureLogging(loggerFactory => loggerFactory.AddProvider(new MP2LoggerProvider("TestWebApp")))
          .Build();
        app.GetAddresses().Clear();
        app.GetAddresses().Add("http://*:5001");
        _engine = app.Start();
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("AspNetServer: Error starting server", e);
      }
    }

  public bool RequestEnd()
    {
      return true;
    }

    public void Stop()
    {
    }

    public void Continue()
    {
    }

    public void Shutdown()
    {
      if (_engine != null)
      {
        _engine.Dispose();
        _engine = null;
      }
    }
  }
}
