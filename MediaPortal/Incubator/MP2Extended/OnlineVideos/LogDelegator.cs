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
using OnlineVideos;

namespace MediaPortal.Plugins.MP2Extended.OnlineVideos
{
  public class LogDelegator : MarshalByRefObject, ILog
  {
    #region MarshalByRefObject overrides

    public override object InitializeLifetimeService()
    {
      // In order to have the lease across appdomains live forever, we return null.
      return null;
    }

    #endregion

    private const string PREFIX = "[OnlineVideos] ";

    public void Debug(string format, params object[] arg)
    {
      ServiceRegistration.Get<ILogger>().Debug(PREFIX + format, arg);
    }

    public void Error(Exception ex)
    {
      ServiceRegistration.Get<ILogger>().Error(ex);
    }

    public void Error(string format, params object[] arg)
    {
      ServiceRegistration.Get<ILogger>().Error(PREFIX + format, arg);
    }

    public void Info(string format, params object[] arg)
    {
      ServiceRegistration.Get<ILogger>().Info(PREFIX + format, arg);
    }

    public void Warn(string format, params object[] arg)
    {
      ServiceRegistration.Get<ILogger>().Warn(PREFIX + format, arg);
    }
  }
}
