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
using System.Data;
using System.Text;
using MediaPortal.Common;
using MediaPortal.Common.Logging;

namespace MediaPortal.Mock
{
  public class MockCommand : IDbCommand
  {
    private string _commandText;
    private MockDataParameterCollection _commandParameters = new MockDataParameterCollection();

    public void Cancel()
    {
      throw new NotImplementedException();
    }

    public string CommandText
    {
      get
      {
        return _commandText;
      }
      set
      {
        _commandText = value;
      }
    }

    public int CommandTimeout
    {
      get
      {
        throw new NotImplementedException();
      }
      set
      {
        throw new NotImplementedException();
      }
    }

    public CommandType CommandType
    {
      get
      {
        throw new NotImplementedException();
      }
      set
      {
        throw new NotImplementedException();
      }
    }

    public IDbConnection Connection
    {
      get
      {
        throw new NotImplementedException();
      }
      set
      {
        throw new NotImplementedException();
      }
    }

    public IDbDataParameter CreateParameter()
    {
      return new MockDataParameter();
    }

    protected string GetFormattedSql()
    {
      StringBuilder sbLogText = new StringBuilder(_commandText);
      foreach (IDbDataParameter param in _commandParameters)
      {
        String quoting = "";
        String pv = "[NULL]";
        if (param.Value != null)
          pv = param.Value.ToString().Replace("{", "{{").Replace("}", "}}");

        if (param.DbType == DbType.String)
          quoting = "'";

        pv = String.Format("{0}{1}{2}", quoting, pv, quoting);

        sbLogText = sbLogText.Replace("@" + param.ParameterName, pv);
      }
      return sbLogText.ToString();
    }

    public int ExecuteNonQuery()
    {
      ServiceRegistration.Get<ILogger>().Info(GetFormattedSql());
      return 0;
    }

    public IDataReader ExecuteReader(CommandBehavior behavior)
    {
      return MockDBUtils.GetReader(_commandText, GetFormattedSql());
    }

    public IDataReader ExecuteReader()
    {
      return ExecuteReader(CommandBehavior.Default);
    }

    public object ExecuteScalar()
    {
      IDataReader reader = ExecuteReader();
      if (!reader.Read())
      {
        return null;
      }
      return reader.GetValue(0);
    }

    public IDataParameterCollection Parameters
    {
      get { return _commandParameters; }
    }

    public void Prepare()
    {
      throw new NotImplementedException();
    }

    public IDbTransaction Transaction
    {
      get
      {
        throw new NotImplementedException();
      }
      set
      {
        throw new NotImplementedException();
      }
    }

    public UpdateRowSource UpdatedRowSource
    {
      get
      {
        throw new NotImplementedException();
      }
      set
      {
        throw new NotImplementedException();
      }
    }

    public void Dispose()
    {
    }
  }
}