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

using System;
using MediaPortal.Common.Logging;

namespace MediaPortal.PackageManager.Core
{
  public class BasicConsoleLogger : ILogger
  {
    private readonly int _logLevel;

    public BasicConsoleLogger(LogLevel logLevel)
    {
      _logLevel = (int)logLevel;
    }

    public void Debug(string format, params object[] args)
    {
      if (_logLevel >= (int)LogLevel.Debug)
      {
        Console.WriteLine(format, args);
        System.Diagnostics.Debug.Print(format, args);
      }
    }

    public void Debug(string format, Exception ex, params object[] args)
    {
      if (_logLevel >= (int)LogLevel.Debug)
      {
        Console.WriteLine(format, args);
        Console.WriteLine(ex);
        System.Diagnostics.Debug.Print(format, args);
        System.Diagnostics.Debug.Print(ex.ToString());
      }
    }

    public void Info(string format, params object[] args)
    {
      if (_logLevel >= (int)LogLevel.Information)
      {
        Console.WriteLine(format, args);
        System.Diagnostics.Debug.Print(format, args);
      }
    }

    public void Info(string format, Exception ex, params object[] args)
    {
      if (_logLevel >= (int)LogLevel.Information)
      {
        Console.WriteLine(format, args);
        Console.WriteLine(ex);
        System.Diagnostics.Debug.Print(format, args);
        System.Diagnostics.Debug.Print(ex.ToString());
      }
    }

    public void Warn(string format, params object[] args)
    {
      if (_logLevel >= (int)LogLevel.Warning)
      {
        Console.WriteLine(format, args);
        System.Diagnostics.Debug.Print(format, args);
      }
    }

    public void Warn(string format, Exception ex, params object[] args)
    {
      if (_logLevel >= (int)LogLevel.Warning)
      {
        Console.WriteLine(format, args);
        Console.WriteLine(ex);
        System.Diagnostics.Debug.Print(format, args);
        System.Diagnostics.Debug.Print(ex.ToString());
      }
    }

    public void Error(string format, params object[] args)
    {
      if (_logLevel >= (int)LogLevel.Error)
      {
        Console.WriteLine(format, args);
        System.Diagnostics.Debug.Print(format, args);
      }
    }

    public void Error(string format, Exception ex, params object[] args)
    {
      if (_logLevel >= (int)LogLevel.Error)
      {
        Console.WriteLine(format, args);
        Console.WriteLine(ex);
        System.Diagnostics.Debug.Print(format, args);
        System.Diagnostics.Debug.Print(ex.ToString());
      }
    }

    public void Error(Exception ex)
    {
      if (_logLevel >= (int)LogLevel.Error)
      {
        Console.WriteLine(ex);
        System.Diagnostics.Debug.Print(ex.ToString());
      }
    }

    public void Critical(string format, params object[] args)
    {
      if (_logLevel >= (int)LogLevel.Critical)
      {
        Console.WriteLine(format, args);
        System.Diagnostics.Debug.Print(format, args);
      }
    }

    public void Critical(string format, Exception ex, params object[] args)
    {
      if (_logLevel >= (int)LogLevel.Critical)
      {
        Console.WriteLine(format, args);
        Console.WriteLine(ex);
        System.Diagnostics.Debug.Print(format, args);
        System.Diagnostics.Debug.Print(ex.ToString());
      }
    }

    public void Critical(Exception ex)
    {
      if (_logLevel >= (int)LogLevel.Critical)
      {
        Console.WriteLine(ex);
        System.Diagnostics.Debug.Print(ex.ToString());
      }
    }
  }
}