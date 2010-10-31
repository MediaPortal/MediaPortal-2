#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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
using System.Collections.Generic;
using MediaPortal.Core.Logging;

namespace MediaPortal.Core.Services.Logging
{
  public class GroupLogger : ILogger
  {
    protected List<ILogger> _loggerList = new List<ILogger>();

     /// <summary>
    /// Creates a new <see cref="GroupLogger"/> instance and initializes it with the given
    /// <paramref name="loggers"/>.
    /// </summary>
    /// <param name="loggers">The loggers to add.</param>
    public GroupLogger(params ILogger[] loggers)
    {
       foreach (ILogger logger in loggers)
        Add(logger);
    }

    public void Add(ILogger logger)
    {
      _loggerList.Add(logger);
    }

    #region Implementation of ILogger

    public bool LogMethodNames
    {
      get { return _loggerList[0].LogMethodNames; }
      set
      {
        foreach (ILogger logger in _loggerList)
          logger.LogMethodNames = value;
      }
    }

    public LogLevel Level
    {
      get { return _loggerList[0].Level; }
      set
      {
        foreach (ILogger logger in _loggerList)
          logger.Level = value;
      }
    }

    public void Debug(string format, params object[] args)
    {
      foreach (ILogger logger in _loggerList)
        logger.Debug(format, args);
    }

    public void Debug(string format, Exception ex, params object[] args)
    {
      foreach (ILogger logger in _loggerList)
        logger.Debug(format, ex, args);
    }

    public void Info(string format, params object[] args)
    {
      foreach (ILogger logger in _loggerList)
        logger.Info(format, args);
    }

    public void Info(string format, Exception ex, params object[] args)
    {
      foreach (ILogger logger in _loggerList)
        logger.Info(format, ex, args);
    }

    public void Warn(string format, params object[] args)
    {
      foreach (ILogger logger in _loggerList)
        logger.Warn(format, args);
    }

    public void Warn(string format, Exception ex, params object[] args)
    {
      foreach (ILogger logger in _loggerList)
        logger.Warn(format, ex, args);
    }

    public void Error(string format, params object[] args)
    {
      foreach (ILogger logger in _loggerList)
        logger.Error(format, args);
    }

    public void Error(string format, Exception ex, params object[] args)
    {
      foreach (ILogger logger in _loggerList)
        logger.Error(format, ex, args);
    }

    public void Error(Exception ex)
    {
      foreach (ILogger logger in _loggerList)
        logger.Error(ex);
    }

    public void Critical(string format, params object[] args)
    {
      foreach (ILogger logger in _loggerList)
        logger.Critical(format, args);
    }

    public void Critical(string format, Exception ex, params object[] args)
    {
      foreach (ILogger logger in _loggerList)
        logger.Critical(format, ex, args);
    }

    public void Critical(Exception ex)
    {
      foreach (ILogger logger in _loggerList)
        logger.Critical(ex);
    }

    #endregion
  }
}
