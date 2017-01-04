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
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.SystemResolver;
using OneTrueError.Client;
using OneTrueError.Client.Uploaders;

namespace MediaPortal.Plugins.OneTrueError
{
  public class ErrorReportingService : IPluginStateTracker
  {
    private static Uri REPO_URL = new Uri("http://onetrueerror.team-mediaportal.com/");
    private static Tuple<string, string> KEY_SERVER = new Tuple<string, string>("e654596f1e404cd7a2e6a618c60ec70d", "d3972498eb514abea732b85cb4c9c65d");
    private static Tuple<string, string> KEY_CLIENT = new Tuple<string, string>("a2c3c73343e3490ba897ff3cf7837add", "a0252fe0f059427e8161e7290e0e5060");
    private DateTime _lastErrorTime = DateTime.MinValue;
    private TimeSpan _reportTreshold = TimeSpan.FromHours(1);

    public void Activated(PluginRuntime pluginRuntime)
    {
      ISystemResolver systemResolver = ServiceRegistration.Get<ISystemResolver>();
      var appKey = systemResolver.SystemType == SystemType.Server ? KEY_SERVER : KEY_CLIENT;

      // The appkey and shared key can be found in onetrueeror.com
      OneTrue.Configuration.Credentials(REPO_URL, appKey.Item1, appKey.Item2);
      OneTrue.Configuration.CatchWinFormsExceptions();
      OneTrue.Configuration.QueueReports = true;
      //OneTrue.Configuration.Advanced.UploadReportFailed += OnUploadReportFailed;

      // Exchange the logger by the error reporting wrapper
      var currentLogger = ServiceRegistration.Get<ILogger>();
      var errorLogger = new ErrorLogWrapper(currentLogger);
      ServiceRegistration.Set<ILogger>(errorLogger);
    }

    private void OnUploadReportFailed(object sender, UploadReportFailedEventArgs uploadReportFailedEventArgs)
    {
      // As the online service or local connection can be down, we reduce the logging or errors here.
      if (DateTime.Now - _lastErrorTime > _reportTreshold)
      {
        _lastErrorTime = DateTime.Now;
        // Note: don't use the overload with Exception parameter, as this would probably trigger a new send failure.
        ServiceRegistration.Get<ILogger>().Info("ErrorReportingService: Could not send error report to service: {0}", uploadReportFailedEventArgs.Exception.Message);
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
    }
  }
}
