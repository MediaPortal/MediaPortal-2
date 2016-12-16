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

namespace UPnP.Infrastructure
{
  public class ConsoleLogger : ILogger
  {
    protected const string LOG_FORMAT_STR = "{0:G} {1}: {2}";

    protected void WriteException(Exception ex)
    {
      Console.WriteLine("Exception: " + ex);
      Console.WriteLine("  Message: " + ex.Message);
      Console.WriteLine("  Site   : " + ex.TargetSite);
      Console.WriteLine("  Source : " + ex.Source);
      Console.WriteLine("Stack Trace:");
      Console.WriteLine(ex.StackTrace);
      if (ex.InnerException != null)
      {
        Console.WriteLine("Inner Exception(s):");
        WriteException(ex.InnerException);
      }
    }

    public void Debug(string format, params object[] args)
    {
      Console.WriteLine(LOG_FORMAT_STR, DateTime.Now, "Debug", string.Format(format, args));
    }

    public void Debug(string format, Exception ex, params object[] args)
    {
      Console.WriteLine(LOG_FORMAT_STR, DateTime.Now, "Debug", string.Format(format, args));
      WriteException(ex);
    }

    public void Info(string format, params object[] args)
    {
      Console.WriteLine(LOG_FORMAT_STR, DateTime.Now, "Info.", string.Format(format, args));
    }

    public void Info(string format, Exception ex, params object[] args)
    {
      Console.WriteLine(LOG_FORMAT_STR, DateTime.Now, "Info.", string.Format(format, args));
      WriteException(ex);
    }

    public void Warn(string format, params object[] args)
    {
      Console.WriteLine(LOG_FORMAT_STR, DateTime.Now, "Warn.", string.Format(format, args));
    }

    public void Warn(string format, Exception ex, params object[] args)
    {
      Console.WriteLine(LOG_FORMAT_STR, DateTime.Now, "Warn.", string.Format(format, args));
      WriteException(ex);
    }

    public void Error(string format, params object[] args)
    {
      Console.WriteLine(LOG_FORMAT_STR, DateTime.Now, "Error", string.Format(format, args));
    }

    public void Error(string format, Exception ex, params object[] args)
    {
      Console.WriteLine(LOG_FORMAT_STR, DateTime.Now, "Error", string.Format(format, args));
      WriteException(ex);
    }

    public void Error(Exception ex)
    {
      Console.WriteLine(LOG_FORMAT_STR, DateTime.Now, "Crit.", "Error");
      WriteException(ex);
    }

    public void Critical(string format, params object[] args)
    {
      Console.WriteLine(LOG_FORMAT_STR, DateTime.Now, "Crit.", string.Format(format, args));
    }

    public void Critical(string format, Exception ex, params object[] args)
    {
      Console.WriteLine(LOG_FORMAT_STR, DateTime.Now, "Crit.", string.Format(format, args));
      WriteException(ex);
    }

    public void Critical(Exception ex)
    {
      Console.WriteLine(LOG_FORMAT_STR, DateTime.Now, "Crit.", "Critical error");
      WriteException(ex);
    }
  }
}
