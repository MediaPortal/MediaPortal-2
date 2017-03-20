using System.IO;
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
  }

  internal sealed class MPTypeNamePatternConverter : PatternLayoutConverter
  {
    protected override void Convert(TextWriter writer, LoggingEvent loggingEvent)
    {
      if (loggingEvent.LocationInformation.StackFrames[1].GetMethod().DeclaringType != null)
        writer.Write(loggingEvent.LocationInformation.StackFrames[1].GetMethod().DeclaringType.FullName);
    }
  }

  internal sealed class MPFileLocationPatternConverter : PatternLayoutConverter
  {
    protected override void Convert(TextWriter writer, LoggingEvent loggingEvent)
    {
      string filename = loggingEvent.LocationInformation.StackFrames[0].GetFileName();
      int strip = filename.LastIndexOf(@"\Source\Core\MediaPortal.Common\Services\Logging\log4netLogger.cs");
      if (strip < filename.Length - 1)
      {
        strip++;
      }
      writer.Write(loggingEvent.LocationInformation.StackFrames[1].GetFileName().Substring(strip));
    }
  }

  internal sealed class MPLineLocationPatternConverter : PatternLayoutConverter
  {
    protected override void Convert(TextWriter writer, LoggingEvent loggingEvent)
    {
      writer.Write(loggingEvent.LocationInformation.StackFrames[1].GetFileLineNumber());
    }
  }

  internal sealed class MPMethodLocationPatternConverter : PatternLayoutConverter
  {
    protected override void Convert(TextWriter writer, LoggingEvent loggingEvent)
    {
      writer.Write(loggingEvent.LocationInformation.StackFrames[1].GetMethod().Name);
    }
  }
}
