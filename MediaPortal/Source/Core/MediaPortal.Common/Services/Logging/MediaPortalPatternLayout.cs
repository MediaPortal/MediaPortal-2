using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using log4net.Core;
using log4net.Layout;
using log4net.Layout.Pattern;

namespace MediaPortal.Common.Services.Logging
{
  internal class LogWrapper
  {
    public Type type;
    public string relativeFilename;
  }

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

      AddConverter("M", typeof(MPMethodPatternConverter));
      AddConverter("method", typeof(MPMethodPatternConverter));
    }

    private static readonly List<LogWrapper> LOG_WRAPPERS = new List<LogWrapper>();

    public static void RegisterLogWrapper(Type type, string relativeFilename)
    {
      LogWrapper logWrapper = new LogWrapper();
      logWrapper.type = type;
      logWrapper.relativeFilename = relativeFilename;
      LOG_WRAPPERS.Add(logWrapper);
    }

    /*
     * Find the first stack element that isn't one of the known log wrappers
     */
    internal static StackFrame GetOuterStackFrame(LoggingEvent loggingEvent)
    {
      //Console.WriteLine("There are {0} stack frames", loggingEvent.LocationInformation.StackFrames.Length);
      StackFrame lastKnown = loggingEvent.LocationInformation.StackFrames[0];
      StackFrame[] stackFrames = loggingEvent.LocationInformation.StackFrames;
      foreach (StackFrame stackFrame in stackFrames)
      {
        /* Console.WriteLine("Stack frame filename={0} line={1} method={2},{3}",
          stackFrame.GetFileName() ?? "null",
          stackFrame.GetFileLineNumber(),
          stackFrame.GetMethod() != null && stackFrame.GetMethod().DeclaringType != null ? stackFrame.GetMethod().DeclaringType.FullName : "null",
          stackFrame.GetMethod()?.ToString() ?? "null"
        ); */
        if (stackFrame.GetMethod().DeclaringType != null)
        {
          if (LOG_WRAPPERS.All(x => stackFrame.GetMethod().DeclaringType != x.type))
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
      if (loggingEvent.LocationInformation.StackFrames[0].GetFileName() == null)
        return 0;

      foreach (LogWrapper logWrapper in LOG_WRAPPERS)
      {
        int offset = loggingEvent.LocationInformation.StackFrames[0].GetFileName().IndexOf(logWrapper.relativeFilename);
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
      writer.Write(filename?.Substring(MediaPortalPatternLayout.GetFullPathOffset(loggingEvent)) ?? "");
    }
  }

  internal sealed class MPLineLocationPatternConverter : PatternLayoutConverter
  {
    protected override void Convert(TextWriter writer, LoggingEvent loggingEvent)
    {
      writer.Write(MediaPortalPatternLayout.GetOuterStackFrame(loggingEvent).GetFileLineNumber());
    }
  }

  internal sealed class MPMethodPatternConverter : PatternLayoutConverter
  {
    protected override void Convert(TextWriter writer, LoggingEvent loggingEvent)
    {
      writer.Write(MediaPortalPatternLayout.GetOuterStackFrame(loggingEvent).GetMethod().Name);
    }
  }
}
