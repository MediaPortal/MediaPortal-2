#region Copyright (C) 2007-2013 Team MediaPortal

/*
    Copyright (C) 2007-2013 Team MediaPortal
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
using System.Linq;
using System.Text;
using MediaPortal.Core;
using MediaPortal.Utilities.DB;

namespace MediaPortal.Backend.Services.Logging
{
  /// <summary>
  /// Helper class to output formatted SQL statement to log.
  /// Output is currently only enabled for DEBUG builds.
  /// </summary>
  public class SqlDebugLogger
  {
    /// <summary>
    /// Formats and writes a SQL statement to log.
    /// </summary>
    /// <param name="unformattedSQL"></param>
    public static void Write(String unformattedSQL)
    {
#if DEBUG
      ServiceScope.Get<MediaPortal.Core.Logging.ILogger>().Debug(SqlUtils.FormatSQL(unformattedSQL));
#endif
    }
  }
}
