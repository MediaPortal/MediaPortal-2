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
using MediaPortal.Backend.Database;
using MediaPortal.Backend.Services.Database;

namespace MediaPortal.Mock
{
  public class MockDatabase : ISQLDatabase
  {
    public string DatabaseType
    {
      get { throw new NotImplementedException(); }
    }

    public string DatabaseVersion
    {
      get { throw new NotImplementedException(); }
    }

    public uint MaxObjectNameLength
    {
      get { return uint.MaxValue; }
    }

    public string GetSQLType(Type dotNetType)
    {
      return dotNetType.Name;
    }

    public string GetSQLVarLengthStringType(uint maxNumChars)
    {
      return "TEXT";
    }

    public string GetSQLFixedLengthStringType(uint maxNumChars)
    {
      throw new NotImplementedException();
    }

    public bool IsCLOB(uint maxNumChars)
    {
      throw new NotImplementedException();
    }

    public IDbDataParameter AddParameter(IDbCommand command, string name, object value, Type type)
    {
      //ServiceRegistration.Get<ILogger>().Info("Adding " + name + "=" + value + "(" + type + ") to " + command);
      IDbDataParameter parameter = command.CreateParameter();
      parameter.ParameterName = name;
      parameter.Value = value;
      parameter.DbType = MockDBUtils.GetType(type);
      command.Parameters.Add(parameter);

      return parameter;
    }

    public object ReadDBValue(Type type, IDataReader reader, int colIndex)
    {
      if (type == typeof(string))
        return reader.GetString(colIndex);

      else if (type == typeof(Int32))
        return reader.GetInt32(colIndex);

      else if (type == typeof(Int64))
        return reader.GetInt64(colIndex);

      else if (type == typeof(Double))
        return reader.GetDouble(colIndex);

      else if (type == typeof(Boolean))
        return reader.GetBoolean(colIndex);

      else if (type == typeof(DateTime))
        return reader.GetDateTime(colIndex);

      else if (type == typeof(Guid))
        return reader.GetGuid(colIndex);

      throw new NotImplementedException("Cannot read DB value " + type + " at " + colIndex);
    }

    public ITransaction BeginTransaction(IsolationLevel level)
    {
      return new MockTransaction(this);
    }

    public ITransaction BeginTransaction()
    {
      return new MockTransaction(this);
    }

    public ITransaction CreateTransaction()
    {
      return new MockTransaction(this);
    }

    public bool TableExists(string tableName)
    {
      throw new NotImplementedException();
    }

    public string CreateStringConcatenationExpression(string str1, string str2)
    {
      throw new NotImplementedException();
    }

    public string CreateSubstringExpression(string str1, string posExpr)
    {
      throw new NotImplementedException();
    }

    public string CreateSubstringExpression(string str1, string posExpr, string lenExpr)
    {
      throw new NotImplementedException();
    }

    public string CreateDateToYearProjectionExpression(string selectExpression)
    {
      throw new NotImplementedException();
    }

    public IDbCommand CreateCommand()
    {
      IDbCommand command;

      if (MockDBUtils.Connection != null)
      {
        command = MockDBUtils.Connection.CreateCommand();
        command = new LoggingDbCommandWrapper(command);
      }
      else
      {
        command = new MockCommand();
        MockDBUtils.AddCommand((MockCommand)command);
      }

      return command;
    }
  }
}
