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
