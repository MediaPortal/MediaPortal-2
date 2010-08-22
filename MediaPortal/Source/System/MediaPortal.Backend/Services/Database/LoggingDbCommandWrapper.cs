#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Data;
using System.Text;
using MediaPortal.Utilities.DB;
using MediaPortal.Core.Services.Logging;
using MediaPortal.Core.Services.PathManager;
using MediaPortal.Utilities;

namespace MediaPortal.Backend.Services.Database
{
  /// <summary>
  /// Wrapper class for DB commands to support debug logging.
  /// </summary>
  public class LoggingDbCommandWrapper : IDbCommand
  {
    #region Private variables

    private static readonly FileLogger sqlDebugLog = FileLogger.CreateFileLogger(new PathManager().GetPath(@"<LOG>\SQLDebug.log"), Core.Logging.LogLevel.Debug, false, true);
    private readonly IDbCommand _command = null;

    #endregion

    #region Constructor

    public LoggingDbCommandWrapper(IDbCommand command)
    {
      _command = command;
    }

    #endregion

    #region Member

    protected void DumpCommand()
    {
      DumpCommand(false, 0);
    }

    protected void DumpCommand(bool includeParameters, int timeSpanMs)
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
        _command.Connection = value;
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
        _command.Transaction = value;
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