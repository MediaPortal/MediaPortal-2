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

using System;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.SystemResolver;
using OneTrueError.Reporting;

namespace MediaPortal.Plugins.OneTrueError
{
  public class ErrorReportingService : IPluginStateTracker
  {
    private static Tuple<string, string> KEY_SERVER = new Tuple<string, string>("f2f6310b-1714-4112-bd6d-ec9df98ade37", "814a5a92-c6b7-473e-af9e-59bcd1d5ee35");
    private static Tuple<string, string> KEY_CLIENT = new Tuple<string, string>("9f39363e-e7c7-4e42-acc7-914ec41a52eb", "93dec981-b867-4adb-8e80-6cb86f52c034");

    public void Activated(PluginRuntime pluginRuntime)
    {
      ISystemResolver systemResolver = ServiceRegistration.Get<ISystemResolver>();
      var appKey = systemResolver.SystemType == SystemType.Server ? KEY_SERVER : KEY_CLIENT;

      // The appkey and shared key can be found in onetrueeror.com
      OneTrue.Configuration.Credentials(appKey.Item1, appKey.Item2);
      OneTrue.Configuration.CatchWinFormsExceptions();
      OneTrue.Configuration.Advanced.UploadReportFailed += OnUploadReportFailed;

      // Exchange the logger by the error reporting wrapper
      var currentLogger = ServiceRegistration.Get<ILogger>();
      var errorLogger = new ErrorLogWrapper(currentLogger);
      ServiceRegistration.Set<ILogger>(errorLogger);
    }

    private void OnUploadReportFailed(object sender, UploadReportFailedEventArgs uploadReportFailedEventArgs)
    {
      // Note: don't use the overload with Exception parameter, as this would probably trigger a new send failure.
      ServiceRegistration.Get<ILogger>().Info("ErrorReportingService: Could not send error report to service: {0}", uploadReportFailedEventArgs.Exception.ToString());
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
