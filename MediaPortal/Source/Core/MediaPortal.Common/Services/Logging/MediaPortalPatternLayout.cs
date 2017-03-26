using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using log4net.Core;
using log4net.Layout;
using log4net.Layout.Pattern;

namespace MediaPortal.Common.Services.Logging
{
  /**
   * We know the stack frame will have log4netLogger as the source of the logging event
   * so we need to go one level up
   */
  public class MediaPortalPatternLayout : PatternLayout
  {
    public MediaPortalPatternLayout()
    {
      AddConverter("C", typeof(MPTypeNamePatternConverter));
      AddConverter("class", typeof(MPTypeNamePatternConverter));
      AddConverter("type", typeof(MPTypeNamePatternConverter));

      AddConverter("F", typeof(MPFileLocationPatternConverter));
      AddConverter("file", typeof(MPFileLocationPatternConverter));

      AddConverter("L", typeof(MPLineLocationPatternConverter));
      AddConverter("line", typeof(MPLineLocationPatternConverter));

      AddConverter("M", typeof(MPMethodLocationPatternConverter));
      AddConverter("method", typeof(MPMethodLocationPatternConverter));
    }

    private static readonly List<string> LOG_WRAPPERS = new List<string>();

    public static void RegisterLogWrapper(string logger)
    {
      LOG_WRAPPERS.Add(logger);
    }

    /*
     * Find the first stack element that isn't one of the known log wrappers
     */
    internal static StackFrame GetOuterStackFrame(LoggingEvent loggingEvent)
    {
      StackFrame lastKnown = loggingEvent.LocationInformation.StackFrames[0];
      StackFrame[] stackFrames = loggingEvent.LocationInformation.StackFrames;
      foreach (StackFrame stackFrame in stackFrames)
      {
        if (stackFrame.GetFileName() != null)
        {
          if (LOG_WRAPPERS.All(x => stackFrame.GetFileName().IndexOf(x) == -1))
            return stackFrame;
          lastKnown = stackFrame;
        }
      }

      return lastKnown;
    }

    /*
     * Find the path prefix (assuming everything is in the same root directory)
     */
    internal static int GetFullPathOffset(LoggingEvent loggingEvent)
    {
      foreach (string logWrapper in LOG_WRAPPERS)
      {
        int offset = loggingEvent.LocationInformation.StackFrames[0].GetFileName().IndexOf(logWrapper);
        if (offset >= 0)
          return offset + 1;
      }

      return 0;
    }
  }

  internal sealed class MPTypeNamePatternConverter : PatternLayoutConverter
  {
    protected override void Convert(TextWriter writer, LoggingEvent loggingEvent)
    {
      StackFrame stackFrame = MediaPortalPatternLayout.GetOuterStackFrame(loggingEvent);
      if (stackFrame.GetMethod().DeclaringType != null)
        writer.Write(stackFrame.GetMethod().DeclaringType.FullName);
    }
  }

  internal sealed class MPFileLocationPatternConverter : PatternLayoutConverter
  {
    protected override void Convert(TextWriter writer, LoggingEvent loggingEvent)
    {
      string filename = MediaPortalPatternLayout.GetOuterStackFrame(loggingEvent).GetFileName();
      writer.Write(filename.Substring(MediaPortalPatternLayout.GetFullPathOffset(loggingEvent)));
    }
  }

  internal sealed class MPLineLocationPatternConverter : PatternLayoutConverter
  {
    protected override void Convert(TextWriter writer, LoggingEvent loggingEvent)
    {
      writer.Write(MediaPortalPatternLayout.GetOuterStackFrame(loggingEvent).GetFileLineNumber());
    }
  }

  internal sealed class MPMethodLocationPatternConverter : PatternLayoutConverter
  {
    protected override void Convert(TextWriter writer, LoggingEvent loggingEvent)
    {
      writer.Write(MediaPortalPatternLayout.GetOuterStackFrame(loggingEvent).GetMethod().Name);
    }
  }
}
