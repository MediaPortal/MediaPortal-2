using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using FirebirdSql.Data.FirebirdClient;
using MediaPortal.Core;
using MediaPortal.Utilities.DB;
using MediaPortal.Core.Services.Logging;
using MediaPortal.Core.Services.PathManager;
using MediaPortal.Utilities;

namespace MediaPortal.BackendComponents.Database.Firebird
{
  /// <summary>
  /// Wrapper class for DB commands to support debug logging.
  /// </summary>
  public class LoggingDbCommandWrapper : IDbCommand
  {
    #region Private variables

    private static FileLogger sqlDebugLog = FileLogger.CreateFileLogger(new PathManager().GetPath(@"<LOG>\SQLDebug.log"), MediaPortal.Core.Logging.LogLevel.Debug, false, true);
    private FbCommand _command = null;

    #endregion

    #region Constructor

    public LoggingDbCommandWrapper(FbCommand command)
    {
      _command = command;
    }

    #endregion

    #region Member


    private void DumpCommand()
    {
      DumpCommand(false, 0);
    }

    private void DumpCommand(bool includeParameters, int timeSpanMs)
    {
      StringBuilder sbLogText = new StringBuilder();
      sbLogText.Append("\r\n-------------------------------------------------------");
      sbLogText.Append(SqlUtils.FormatSQL(_command.CommandText));
      if (includeParameters)
      {
        sbLogText.Append("\r\n-------------------------------------------------------");
        sbLogText.Append(SqlUtils.FormatSQLParameters(_command.Parameters));
      }
      sbLogText.AppendFormat("\r\n Query time {0} ms", timeSpanMs);
      sbLogText.Append("\r\n=======================================================");
      sqlDebugLog.Debug(StringUtils.EscapeCurlyBraces(sbLogText.ToString()));
    }

    #endregion

    #region IDbCommand Member

    public void Cancel()
    {
      _command.Cancel();
    }

    public string CommandText
    {
      get
      {
        return _command.CommandText;
      }
      set
      {
        _command.CommandText = value;
      }
    }

    public int CommandTimeout
    {
      get
      {
        return _command.CommandTimeout;
      }
      set
      {
        _command.CommandTimeout = value;
      }
    }

    public CommandType CommandType
    {
      get
      {
        return _command.CommandType;
      }
      set
      {
        _command.CommandType = value;
      }
    }

    public IDbConnection Connection
    {
      get
      {
        return _command.Connection;
      }
      set
      {
        _command.Connection = (FbConnection)value;
      }
    }

    public IDbDataParameter CreateParameter()
    {
      return _command.CreateParameter();
    }

    public int ExecuteNonQuery()
    {
      DateTime start = DateTime.Now;
      try
      {
        return _command.ExecuteNonQuery();
      }
      finally
      {
        DumpCommand(true, (DateTime.Now - start).Milliseconds);
      }
    }

    public IDataReader ExecuteReader(CommandBehavior behavior)
    {
      DateTime start = DateTime.Now;
      try
      {
        return _command.ExecuteReader(behavior);
      }
      finally
      {
        DumpCommand(true, (DateTime.Now - start).Milliseconds);
      }
    }

    public IDataReader ExecuteReader()
    {
      DateTime start = DateTime.Now;
      try
      {
        return _command.ExecuteReader();
      }
      finally
      {
        DumpCommand(true, (DateTime.Now - start).Milliseconds);
      }
    }

    public object ExecuteScalar()
    {
      DateTime start = DateTime.Now;
      try
      {
        return _command.ExecuteScalar();
      }
      finally
      {
        DumpCommand(true, (DateTime.Now - start).Milliseconds);
      }
    }

    public IDataParameterCollection Parameters
    {
      get { return _command.Parameters; }
    }

    public void Prepare()
    {
      _command.Prepare();
    }

    public IDbTransaction Transaction
    {
      get
      {
        return _command.Transaction;
      }
      set
      {
        _command.Transaction = (FbTransaction)value;
      }
    }

    public UpdateRowSource UpdatedRowSource
    {
      get
      {
        return _command.UpdatedRowSource;
      }
      set
      {
        _command.UpdatedRowSource = value;
      }
    }

    #endregion

    #region IDisposable Member

    public void Dispose()
    {
      _command.Dispose();
    }

    #endregion
  }
}
