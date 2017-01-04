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
using System.Threading;
using System.Windows.Forms;
using MediaPortal.Common.Logging;

namespace MediaPortal.Common.Exceptions
{
  /// <summary>
  /// Handling methods for uncatched exceptions - can be registered in application launcher classes.
  /// </summary>
  public class LauncherExceptionHandling
  {
    public static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
    {
      string text = string.Format("Unhandled thread exception in thread '{0}'", sender);
      ILogger logger = ServiceRegistration.Get<ILogger>(false);
      if (logger ==  null)
        MessageBox.Show(text + ": " + e.Exception.Message, "Unhandled Thread Exception");
      else
        logger.Error("ApplicationLauncher: " + text, e.Exception);
    }

    public static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
      Exception exc = (Exception) e.ExceptionObject;
      ILogger logger = ServiceRegistration.Get<ILogger>(false);
      if (logger ==  null)
        MessageBox.Show("Unhandled exception in application: " + exc.Message, "Unhandled Exception");
      else
        logger.Error("ApplicationLauncher: Unhandled exception in application", exc);
    }
  }
}