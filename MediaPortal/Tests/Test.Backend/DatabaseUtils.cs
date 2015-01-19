#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using MediaPortal.Backend.Database;
using MediaPortal.Backend.Services.Database;
using MediaPortal.Backend.Services.MediaLibrary;
using MediaPortal.Backend.Services.MediaLibrary.QueryEngine;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Utilities.DB;

namespace Test.Backend
{
  class TestDbUtils : DBUtils
  {
    private static readonly DatabaseUtils DATABASE = new DatabaseUtils();
    private static readonly IDictionary<string, TestReader> READERS = new Dictionary<string, TestReader>();
    private static IList<TestCommand> COMMANDS = new List<TestCommand>();
    private static TestReader ALIASES_READER = null;

    static TestDbUtils()
    {
      // Override mappings to make output log more readable
      SIMPLEDOTNETTYPE2DBTYPE[typeof(DateTime)] = DbType.String;
      SIMPLEDOTNETTYPE2DBTYPE[typeof(Guid)] = DbType.String;
    }

    public static DbType GetType(Type type)
    {
      return SIMPLEDOTNETTYPE2DBTYPE[type];
    }

      public static TestReader GetAliasesReader()
    {
        return ALIASES_READER;
    }

    public static void Setup()
    {
      Base.Setup();

      ServiceRegistration.Set<ISQLDatabase>(DATABASE);

      ALIASES_READER = AddReader("SELECT MIAM_ID, IDENTIFIER, DATABASE_OBJECT_NAME FROM MIA_NAME_ALIASES");
          /*
          reader.AddResult("", GetMIATableIdentifier(MediaAspect.Metadata), "M_MEDIAITEM");
          reader.AddResult("", GetMIAAttributeColumnIdentifier(MediaAspect.ATTR_TITLE), "TITLE");
          reader.AddResult("", GetMIATableIdentifier(AudioAspect.Metadata), "M_AUDIOITEM");
          reader.AddResult("", GetMIAAttributeColumnIdentifier(AudioAspect.ATTR_ALBUM), "ALBUM");
          reader.AddResult("", GetMIATableIdentifier(RelationshipAspect.Metadata), "M_RELATIONSHIP");
          reader.AddResult("", GetMIAAttributeColumnIdentifier(RelationshipAspect.ATTR_ROLE), "ROLE");
          reader.AddResult("", GetMIAAttributeColumnIdentifier(RelationshipAspect.ATTR_LINKED_ID), "LINKEDID");
          reader.AddResult("", GetMIAAttributeColumnIdentifier(RelationshipAspect.ATTR_LINKED_ROLE), "LINKEDROLE");
          */

        AddReader("SELECT MIAM_ID, MIAM_SERIALIZATION FROM MIA_TYPES");
        AddReader("SELECT MIAM_ID, MIAM_SERIALIZATION, CREATION_DATE FROM MIA_TYPES");
        AddReader("SELECT MIAM_ID, CREATION_DATE FROM MIA_TYPES");
    }

    public static void Reset()
    {
        COMMANDS.Clear();
    }

    public static DatabaseUtils Database
    {
      get { return DATABASE; }
    }

    public static TestReader AddReader(string command, params string[] columns)
    {
      return AddReader(command, new TestReader(columns));
    }

    public static TestReader AddReader(string command, TestReader reader)
    {
      READERS[command] = reader;
      return reader;
    }

    public static IDataReader GetReader(string command)
    {
      TestReader reader = null;
      if (!READERS.TryGetValue(command, out reader))
      {
          foreach (string key in READERS.Keys)
          {
          }
          throw new NotImplementedException("No DB reader for " + command);
      }
      return reader;
    }

    public static string GetMIATableIdentifier(MediaItemAspectMetadata miam)
    {
        return "T_" + SqlUtils.ToSQLIdentifier(miam.AspectId.ToString()).ToUpperInvariant();
    }

    public static string GetMIAAttributeColumnIdentifier(MediaItemAspectMetadata.AttributeSpecification spec)
    {
        return "A_" + SqlUtils.ToSQLIdentifier(spec.ParentMIAM.AspectId + "_" + spec.AttributeName).ToUpperInvariant();
    }

      public static void AddCommand(TestCommand command)
      {
          COMMANDS.Add(command);
      }

      public static TestCommand FindCommand(string match)
      {
          foreach (TestCommand command in COMMANDS)
          {
              if (command.CommandText.StartsWith(match))
              {
                  return command;
              }
          }

          return null;
      }
  }

  class TestCompiledFilter : CompiledFilter
  {
    public TestCompiledFilter()
      : base(null, null, null, null, null, null)
    {
    }

    public void test(MIA_Management miaManagement, IFilter filter,
      ICollection<MediaItemAspectMetadata> requiredMIATypes, string outerMIIDJoinVariable, ICollection<TableJoin> tableJoins,
      IList<object> resultParts, IList<BindVar> resultBindVars)
    {
      CompileStatementParts(miaManagement, filter, null, new BindVarNamespace(),
        requiredMIATypes, outerMIIDJoinVariable, tableJoins,
        resultParts, resultBindVars);
    }
  }

  class TestTransaction : ITransaction
  {
    private DatabaseUtils _database;

    public TestTransaction(DatabaseUtils database)
    {
      this._database = database;
    }

    public ISQLDatabase Database
    {
      get { return _database; }
    }

    public IDbConnection Connection
    {
      get { throw new NotImplementedException(); }
    }

    public void Commit()
    {
      ServiceRegistration.Get<ILogger>().Info("Committing");
    }

    public void Rollback()
    {
      ServiceRegistration.Get<ILogger>().Info("Rolling back");
    }

    public IDbCommand CreateCommand()
    {
      return _database.CreateCommand();
    }

    public void Dispose()
    {
    }
  }

  class TestDataParameter : IDbDataParameter
  {
    private string _name;
    private object _value;
    private DbType _dbType;

    public byte Precision
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

    public byte Scale
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

    public int Size
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

    public DbType DbType
    {
      get
      {
        return _dbType;
      }
      set
      {
        _dbType = value;
      }
    }

    public ParameterDirection Direction
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

    public bool IsNullable
    {
      get { throw new NotImplementedException(); }
    }

    public string ParameterName
    {
      get
      {
        return _name;
      }
      set
      {
        _name = value;
      }
    }

    public string SourceColumn
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

    public DataRowVersion SourceVersion
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

    public object Value
    {
      get
      {
        return _value;
      }
      set
      {
        _value = value;
      }
    }
  }

  class TestDataParameterCollection : IDataParameterCollection
  {
    private IList<IDbDataParameter> _parameters = new List<IDbDataParameter>();

    public bool Contains(string parameterName)
    {
      throw new NotImplementedException();
    }

    public int IndexOf(string parameterName)
    {
      throw new NotImplementedException();
    }

    public void RemoveAt(string parameterName)
    {
      throw new NotImplementedException();
    }

    public object this[string parameterName]
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

    public int Add(object value)
    {
      IDbDataParameter parameter = (IDbDataParameter)value;
      _parameters.Add(parameter);
      return _parameters.Count;
    }

    public void Clear()
    {
      throw new NotImplementedException();
    }

    public bool Contains(object value)
    {
      throw new NotImplementedException();
    }

    public int IndexOf(object value)
    {
      throw new NotImplementedException();
    }

    public void Insert(int index, object value)
    {
      throw new NotImplementedException();
    }

    public bool IsFixedSize
    {
      get { throw new NotImplementedException(); }
    }

    public bool IsReadOnly
    {
      get { throw new NotImplementedException(); }
    }

    public void Remove(object value)
    {
      throw new NotImplementedException();
    }

    public void RemoveAt(int index)
    {
      throw new NotImplementedException();
    }

    public object this[int index]
    {
      get
      {
        return _parameters[index];
      }
      set
      {
        throw new NotImplementedException();
      }
    }

    public void CopyTo(Array array, int index)
    {
      throw new NotImplementedException();
    }

    public int Count
    {
      get { return _parameters.Count; }
    }

    public bool IsSynchronized
    {
      get { throw new NotImplementedException(); }
    }

    public object SyncRoot
    {
      get { throw new NotImplementedException(); }
    }

    public IEnumerator GetEnumerator()
    {
      return _parameters.GetEnumerator();
    }
  }

  class TestCommand : IDbCommand
  {
    private string _commandText;
    private TestDataParameterCollection _commandParameters = new TestDataParameterCollection();

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
      return new TestDataParameter();
    }

    protected void DumpCommand()
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
      ServiceRegistration.Get<ILogger>().Info(sbLogText.ToString());
    }

    public int ExecuteNonQuery()
    {
      DumpCommand();
      return 0;
    }

    public IDataReader ExecuteReader(CommandBehavior behavior)
    {
      DumpCommand();
      return TestDbUtils.GetReader(_commandText);
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

  class TestReader : IDataReader
  {
      private IList<string> _columns = new List<string>();
    private readonly IList<IDictionary<int, string>> _results = new List<IDictionary<int, string>>();
    private int _index = -1;

    public TestReader(string[] columns)
    {
        this._columns = columns.ToArray();
    }

    public void AddResult(params string[] values)
    {
      IDictionary<int, string> result = new Dictionary<int, string>();
      for (int index = 0; index < values.Length; index++)
      {
        result[index] = values[index];
      }
      _results.Add(result);
    }

    public void Close()
    {
      throw new NotImplementedException();
    }

    public int Depth
    {
      get { throw new NotImplementedException(); }
    }

    public DataTable GetSchemaTable()
    {
      throw new NotImplementedException();
    }

    public bool IsClosed
    {
      get { throw new NotImplementedException(); }
    }

    public bool NextResult()
    {
      throw new NotImplementedException();
    }

    public bool Read()
    {
      _index++;
      return _index < _results.Count;
    }

    public int RecordsAffected
    {
      get { throw new NotImplementedException(); }
    }

    public void Dispose()
    {
    }

    public int FieldCount
    {
      get { throw new NotImplementedException(); }
    }

    public bool GetBoolean(int i)
    {
      throw new NotImplementedException();
    }

    public byte GetByte(int i)
    {
      throw new NotImplementedException();
    }

    public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
    {
      throw new NotImplementedException();
    }

    public char GetChar(int i)
    {
      throw new NotImplementedException();
    }

    public long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
    {
      throw new NotImplementedException();
    }

    public IDataReader GetData(int i)
    {
      throw new NotImplementedException();
    }

    public string GetDataTypeName(int i)
    {
      throw new NotImplementedException();
    }

    public DateTime GetDateTime(int i)
    {
      throw new NotImplementedException();
    }

    public decimal GetDecimal(int i)
    {
      throw new NotImplementedException();
    }

    public double GetDouble(int i)
    {
      throw new NotImplementedException();
    }

    public Type GetFieldType(int i)
    {
      throw new NotImplementedException();
    }

    public float GetFloat(int i)
    {
      throw new NotImplementedException();
    }

    public Guid GetGuid(int i)
    {
      throw new NotImplementedException();
    }

    public short GetInt16(int i)
    {
      throw new NotImplementedException();
    }

    public int GetInt32(int i)
    {
        try
        {
            return Int32.Parse(_results[_index][i]);
        }
        catch (KeyNotFoundException e)
        {
            throw new KeyNotFoundException("Column " + i + " not found", e);
        }
        catch (FormatException e)
        {
            throw new FormatException("Cannot parse " + _results[_index][i] + " as integer", e);
        }
    }

    public long GetInt64(int i)
    {
      throw new NotImplementedException();
    }

    public string GetName(int i)
    {
        return _columns[i];
    }

    public int GetOrdinal(string name)
    {
        int ordinal = _columns.IndexOf(name);
        if (ordinal == -1)
        {
            throw new KeyNotFoundException(string.Format("No ordinal for {0}", name));
        }
        return ordinal;
    }

    public string GetString(int i)
    {
      if (_index >= _results.Count)
      {
        throw new IndexOutOfRangeException();
      }

      string value;
      if (!_results[_index].TryGetValue(i, out value))
      {
        throw new KeyNotFoundException(string.Format("No key {0}", i));
      }

      return value;
    }

    public object GetValue(int i)
    {
        try
        {
            return _results[_index][i];
        }
        catch (KeyNotFoundException e)
        {
            throw new KeyNotFoundException("Column " + i + " not found", e);
        }
    }

    public int GetValues(object[] values)
    {
      throw new NotImplementedException();
    }

    public bool IsDBNull(int i)
    {
        return _results[_index][i] == null;
    }

    public object this[string name]
    {
      get { throw new NotImplementedException(); }
    }

    public object this[int i]
    {
      get { throw new NotImplementedException(); }
    }
  }

  class DatabaseUtils : ISQLDatabase
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
      parameter.DbType = TestDbUtils.GetType(type);
      command.Parameters.Add(parameter);

      return parameter;
    }

    public object ReadDBValue(Type type, IDataReader reader, int colIndex)
    {
      if (type == typeof(string))
      {
        return reader.GetString(colIndex);
      }
      else if (type == typeof(Int32))
      {
          return reader.GetInt32(colIndex);
      }
      else if (type == typeof(Guid))
      {
          return new Guid(reader.GetString(colIndex));
      }

      throw new NotImplementedException("Cannot read DB value " + type + " at " + colIndex);
    }

    public ITransaction BeginTransaction(IsolationLevel level)
    {
      return new TestTransaction(this);
    }

    public ITransaction BeginTransaction()
    {
      return new TestTransaction(this);
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
      TestCommand command = new TestCommand();
      TestDbUtils.AddCommand(command);
      return command;
    }
  }
}
