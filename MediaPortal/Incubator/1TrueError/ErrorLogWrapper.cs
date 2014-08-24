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
using MediaPortal.Common.Logging;
using OneTrueError.Reporting;

namespace MediaPortal.Plugins.OneTrueError
{
  public class ErrorLogWrapper : ILogger
  {
    private readonly ILogger _logger;

    /// <summary>
    /// Creates a new <see cref="ErrorLogWrapper"/> instance and initializes it with the given <paramref name="parentLogger"/>.
    /// All logging calls that contain an exception will be reported.
    /// </summary>
    /// <param name="parentLogger">Current logger to be wrapped around.</param>
    public ErrorLogWrapper(ILogger parentLogger)
    {
      _logger = parentLogger;
    }

    #region ILogger implementation

    public void Debug(string format, params object[] args)
    {
      _logger.Debug(format, args);
    }

    public void Debug(string format, Exception ex, params object[] args)
    {
      _logger.Debug(string.Format(format, args), ex);
      OneTrue.Report(ex);
    }

    public void Info(string format, params object[] args)
    {
      _logger.Info(format, args);
    }

    public void Info(string format, Exception ex, params object[] args)
    {
      _logger.Info(string.Format(format, args), ex);
      OneTrue.Report(ex);
    }

    public void Warn(string format, params object[] args)
    {
      _logger.Warn(format, args);
    }

    public void Warn(string format, Exception ex, params object[] args)
    {
      _logger.Warn(string.Format(format, args), ex);
      OneTrue.Report(ex);
    }

    public void Error(string format, params object[] args)
    {
      _logger.Error(format, args);
    }

    public void Error(string format, Exception ex, params object[] args)
    {
      _logger.Error(string.Format(format, args), ex);
      OneTrue.Report(ex);
    }

    public void Error(Exception ex)
    {
      _logger.Error("", ex);
      OneTrue.Report(ex);
    }

    public void Critical(string format, params object[] args)
    {
      _logger.Critical(format, args);
    }

    public void Critical(string format, Exception ex, params object[] args)
    {
      _logger.Critical(string.Format(format, args), ex);
      OneTrue.Report(ex);
    }

    public void Critical(Exception ex)
    {
      _logger.Critical("", ex);
      OneTrue.Report(ex);
    }

    #endregion
  }
}
