#region Copyright (C) 2007-2021 Team MediaPortal

/*
    Copyright (C) 2007-2021 Team MediaPortal
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

using MediaPortal.Common.Logging;
using MP2BootstrapperApp.Models;
using System;
using LogLevel = WixToolset.Mba.Core.LogLevel;

namespace MP2BootstrapperApp.Logging
{
  public class Logger : ILogger
  {
    protected IBootstrapperApplicationModel _bootstrapperApplicationModel;

    public Logger(IBootstrapperApplicationModel bootstrapperApplicationModel)
    {
      _bootstrapperApplicationModel = bootstrapperApplicationModel;
    }

    public void Critical(string format, params object[] args)
    {
      LogMessage(LogLevel.Error, format, args);
    }

    public void Critical(string format, Exception ex, params object[] args)
    {
      LogMessage(LogLevel.Error, format, args);
      LogException(LogLevel.Error, ex);
    }

    public void Critical(Exception ex)
    {
      LogException(LogLevel.Error, ex);
    }

    public void Debug(string format, params object[] args)
    {
      LogMessage(LogLevel.Debug, format, args);
    }

    public void Debug(string format, Exception ex, params object[] args)
    {
      LogMessage(LogLevel.Debug, format, args);
      LogException(LogLevel.Debug, ex);
    }

    public void Error(string format, params object[] args)
    {
      LogMessage(LogLevel.Error, format, args);
    }

    public void Error(string format, Exception ex, params object[] args)
    {
      LogMessage(LogLevel.Error, format, args);
      LogException(LogLevel.Error, ex);
    }

    public void Error(Exception ex)
    {
      LogException(LogLevel.Error, ex);
    }

    public void Info(string format, params object[] args)
    {
      LogMessage(LogLevel.Standard, format, args);
    }

    public void Info(string format, Exception ex, params object[] args)
    {
      LogMessage(LogLevel.Standard, format, args);
      LogException(LogLevel.Standard, ex);
    }

    public void Warn(string format, params object[] args)
    {
      LogMessage(LogLevel.Standard, format, args);
    }

    public void Warn(string format, Exception ex, params object[] args)
    {
      LogMessage(LogLevel.Standard, format, args);
      LogException(LogLevel.Standard, ex);
    }

    protected void LogMessage(LogLevel logLevel, string format, params object[] args)
    {
      _bootstrapperApplicationModel.LogMessage(logLevel, string.Format(format, args));
    }

    protected void LogException(LogLevel logLevel, Exception ex)
    {
      _bootstrapperApplicationModel.LogMessage(logLevel, string.Format("{0}\r\n{1}", ex.Message, ex.StackTrace));
    }
  }
}
