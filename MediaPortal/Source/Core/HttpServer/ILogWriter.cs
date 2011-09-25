using System;
using System.Diagnostics;
using System.Text;

namespace HttpServer
{
  /// <summary>
  /// Priority for log entries
  /// </summary>
  /// <seealso cref="ILogWriter"/>
  public enum LogPrio
  {
    /// <summary>
    /// Very detailed logs to be able to follow the flow of the program.
    /// </summary>
    Trace,

    /// <summary>
    /// Logs to help debug errors in the application
    /// </summary>
    Debug,

    /// <summary>
    /// Information to be able to keep track of state changes etc.
    /// </summary>
    Info,

    /// <summary>
    /// Something did not go as we expected, but it's no problem.
    /// </summary>
    Warning,

    /// <summary>
    /// Something that should not fail failed, but we can still keep
    /// on going.
    /// </summary>
    Error,

    /// <summary>
    /// Something failed, and we cannot handle it properly.
    /// </summary>
    Fatal
  }

  /// <summary>
  /// Interface used to write to log files.
  /// </summary>
  public interface ILogWriter
  {
    /// <summary>
    /// Write an entry to the log file.
    /// </summary>
    /// <param name="source">object that is writing to the log</param>
    /// <param name="priority">importance of the log message</param>
    /// <param name="message">the message</param>
    void Write(object source, LogPrio priority, string message);
  }

  /// <summary>
  /// This class writes to the console. It colors the output depending on the logprio and includes a 3-level stacktrace (in debug mode)
  /// </summary>
  /// <seealso cref="ILogWriter"/>
  public sealed class ConsoleLogWriter : ILogWriter
  {
    /// <summary>
    /// The actual instance of this class.
    /// </summary>
    public static readonly ConsoleLogWriter Instance = new ConsoleLogWriter();

    /// <summary>
    /// Logwriters the specified source.
    /// </summary>
    /// <param name="source">object that wrote the logentry.</param>
    /// <param name="prio">Importance of the log message</param>
    /// <param name="message">The message.</param>
    public void Write(object source, LogPrio prio, string message)
    {
      StringBuilder sb = new StringBuilder();
      sb.Append(DateTime.Now.ToString());
      sb.Append(" ");
      sb.Append(prio.ToString().PadRight(10));
      sb.Append(" | ");
#if DEBUG
      StackTrace trace = new StackTrace();
      StackFrame[] frames = trace.GetFrames();
      int endFrame = frames.Length > 4 ? 4 : frames.Length;
      int startFrame = frames.Length > 0 ? 1 : 0;
      for (int i = startFrame; i < endFrame; ++i)
      {
        sb.Append(frames[i].GetMethod().Name);
        sb.Append(" -> ");
      }
#else
            sb.Append(System.Reflection.MethodBase.GetCurrentMethod().Name);
            sb.Append(" | ");
#endif
      sb.Append(message);

      Console.ForegroundColor = GetColor(prio);
      Console.WriteLine(sb.ToString());
      Console.ForegroundColor = ConsoleColor.Gray;
    }

    /// <summary>
    /// Get color for the specified logprio
    /// </summary>
    /// <param name="prio">prio for the log entry</param>
    /// <returns>A <see cref="ConsoleColor"/> for the prio</returns>
    public static ConsoleColor GetColor(LogPrio prio)
    {
      switch (prio)
      {
        case LogPrio.Trace:
          return ConsoleColor.DarkGray;
        case LogPrio.Debug:
          return ConsoleColor.Gray;
        case LogPrio.Info:
          return ConsoleColor.White;
        case LogPrio.Warning:
          return ConsoleColor.DarkMagenta;
        case LogPrio.Error:
          return ConsoleColor.Magenta;
        case LogPrio.Fatal:
          return ConsoleColor.Red;
      }

      return ConsoleColor.Yellow;
    }
  }

  /// <summary>
  /// Default log writer, writes everything to null (nowhere).
  /// </summary>
  /// <seealso cref="ILogWriter"/>
  public sealed class NullLogWriter : ILogWriter
  {
    /// <summary>
    /// The logging instance.
    /// </summary>
    public static readonly NullLogWriter Instance = new NullLogWriter();

    /// <summary>
    /// Writes everything to null
    /// </summary>
    /// <param name="source">object that wrote the log entry.</param>
    /// <param name="prio">Importance of the log message</param>
    /// <param name="message">The message.</param>
    public void Write(object source, LogPrio prio, string message)
    {
    }
  }
}