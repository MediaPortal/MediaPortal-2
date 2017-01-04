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
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace MediaPortal.UPnPRenderer.UPnP
{
  /// <summary>
  /// Helper class to log outputs to Console in DEBUG mode. In RELEASE builds no logging is done.
  /// </summary>
  public static class TraceLogger
  {
    public static void WriteLine(string format, params object[] args)
    {
#if DEBUG
      Console.WriteLine(format, args);
#endif
    }
    public static void WriteLine(object value)
    {
#if DEBUG
      Console.WriteLine(@"{0}", value);
#endif
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static string GetCurrentMethod(int depth = 1)
    {
      StackTrace st = new StackTrace();
      StackFrame sf = st.GetFrame(depth);

      return sf.GetMethod().Name;
    }

    public static void DebugLogParams(IList<object> inParams)
    {
      WriteLine("*************");
      WriteLine("Current method: " + GetCurrentMethod(2)); // This method's parent
      WriteLine("In Params");
      foreach (var inParam in inParams)
        WriteLine(inParam);
      WriteLine("*************");
    }
  }
}
