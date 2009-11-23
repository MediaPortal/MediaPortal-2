using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaPortal.Core.Logging;

namespace MediaPortal.Core.Services.Logging
{
  class GroupLogger : ILogger
  {
    protected List<ILogger> _loggerList = new List<ILogger>();
  
     /// <summary>
    /// Creates a new <see cref="GroupLogger"/> instance and initializes it with the given
    /// <paramref name="logger"/> .
    /// </summary>
    /// <param name="logger">The first logger in the list.</param>
    /// <remarks>
    /// <para><b><u>Warning!</u></b></para>
    /// <para>Turning on logging of method names causes a severe performance degradation. Each call to the
    /// logger will add an extra amount of time, for example 10 to 40 milliseconds for a file output,
    /// depending on the length of the stack trace.</para>
    /// </remarks>
    public GroupLogger(ILogger logger)
    {
      if (logger == null)
      {
        throw new ArgumentNullException();
      }
      Add(logger);
    }

    public void Add(ILogger logger)
    {
      _loggerList.Add(logger);
    }

    #region Implementation of ILogger

    /// <summary>
    /// Indicates whether the logger will append the calling classname and method before each log line.
    /// </summary>
    /// <remarks>
    /// Warning!! Turning this option on causes a severe performance degradation!!!</remarks>
    public bool LogMethodNames
    {
      get { return _loggerList[0].LogMethodNames; }
      set { foreach (ILogger logger in _loggerList) logger.LogMethodNames = value; }
    }

    /// <summary>
    /// Gets or sets the log level.
    /// </summary>
    /// <value>A <see cref="LogLevel"/> value that indicates the minimum level messages must have to be 
    /// written to the logger.</value>
    public LogLevel Level
    {
      get { return _loggerList[0].Level; }
      set { foreach (ILogger logger in _loggerList) logger.Level = value; }
    }

    /// <summary>
    /// Writes a debug message to the log.
    /// </summary>
    /// <param name="format">A composite format string.</param>
    /// <param name="args">An array of objects to write using format.</param>
    public void Debug(string format, params object[] args)
    {
      foreach (ILogger logger in _loggerList)
      {
        logger.Debug(format, args);
      }
    }

    /// <summary>
    /// Writes a debug message to the log.
    /// </summary>
    /// <param name="format">A composite format string.</param>
    /// <param name="ex">The <see cref="Exception"/> that caused the message.</param>
    /// <param name="args">An array of objects to write using format.</param>
    public void Debug(string format, Exception ex, params object[] args)
    {
      foreach (ILogger logger in _loggerList)
      {
        logger.Debug(format, ex, args);
      }
    }

    /// <summary>
    /// Writes an informational message to the log.
    /// </summary>
    /// <param name="format">A composite format string.</param>
    /// <param name="args">An array of objects to write using format.</param>
    public void Info(string format, params object[] args)
    {
      foreach (ILogger logger in _loggerList)
      {
        logger.Info(format, args);
      }
    }

    /// <summary>
    /// Writes an informational message to the log.
    /// </summary>
    /// <param name="format">A composite format string.</param>
    /// <param name="ex">The <see cref="Exception"/> that caused the message.</param>
    /// <param name="args">An array of objects to write using format.</param>
    public void Info(string format, Exception ex, params object[] args)
    {
      foreach (ILogger logger in _loggerList)
      {
        logger.Info(format, ex, args);
      }
    }

    /// <summary>
    /// Writes a warning to the log.
    /// </summary>
    /// <param name="format">A composite format string.</param>
    /// <param name="args">An array of objects to write using format.</param>
    public void Warn(string format, params object[] args)
    {
      foreach (ILogger logger in _loggerList)
      {
        logger.Warn(format, args);
      }
    }

    /// <summary>
    /// Writes a warning to the log, passing the original <see cref="Exception"/>.
    /// </summary>
    /// <param name="format">A composite format string.</param>
    /// <param name="ex">The <see cref="Exception"/> that caused the message.</param>
    /// <param name="args">An array of objects to write using format.</param>
    public void Warn(string format, Exception ex, params object[] args)
    {
      foreach (ILogger logger in _loggerList)
      {
        logger.Warn(format, ex, args);
      }
    }

    /// <summary>
    /// Writes an error message to the log.
    /// </summary>
    /// <param name="format">A composite format string.</param>
    /// <param name="args">An array of objects to write using format.</param>
    public void Error(string format, params object[] args)
    {
      foreach (ILogger logger in _loggerList)
      {
        logger.Error(format, args);
      }
    }

    /// <summary>
    /// Writes an error message to the log, passing the original <see cref="Exception"/>.
    /// </summary>
    /// <param name="format">A composite format string.</param>
    /// <param name="ex">The <see cref="Exception"/> that caused the message.</param>
    /// <param name="args">An array of objects to write using format.</param>
    public void Error(string format, Exception ex, params object[] args)
    {
      foreach (ILogger logger in _loggerList)
      {
        logger.Error(format, ex, args);
      }
    }

    /// <summary>
    /// Writes an Error <see cref="Exception"/> to the log.
    /// </summary>
    /// <param name="ex">The <see cref="Exception"/> to write.</param>
    public void Error(Exception ex)
    {
      foreach (ILogger logger in _loggerList)
      {
        logger.Error(ex);
      }
    }

    /// <summary>
    /// Writes a critical error system message to the log.
    /// </summary>
    /// <param name="format">A composite format string.</param>
    /// <param name="args">An array of objects to write using format.</param>
    public void Critical(string format, params object[] args)
    {
      foreach (ILogger logger in _loggerList)
      {
        logger.Critical(format, args);
      }
    }

    /// <summary>
    /// Writes a critical error system message to the log, passing the original <see cref="Exception"/>.
    /// </summary>
    /// <param name="format">A composite format string.</param>
    /// <param name="ex">The <see cref="Exception"/> that caused the message.</param>
    /// <param name="args">An array of objects to write using format.</param>
    public void Critical(string format, Exception ex, params object[] args)
    {
      foreach (ILogger logger in _loggerList)
      {
        logger.Critical(format, ex, args);
      }
    }

    /// <summary>
    /// Writes an Critical error <see cref="Exception"/> to the log.
    /// </summary>
    /// <param name="ex">The <see cref="Exception"/> to write.</param>
    public void Critical(Exception ex)
    {
      foreach (ILogger logger in _loggerList)
      {
        logger.Critical(ex);
      }
    }

    #endregion
  }
}
