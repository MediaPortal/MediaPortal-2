using MediaPortal.Backend.Database;
using MediaPortal.Backend.Services.Database;
using MediaPortal.Backend.Services.MediaLibrary;
using MediaPortal.Backend.Services.MediaLibrary.QueryEngine;
using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.PathManager;
using MediaPortal.Common.Services.PathManager;
using MediaPortal.Utilities.DB;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Data;

namespace Test.Backend
{
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
    public ISQLDatabase Database
    {
      get { throw new NotImplementedException(); }
    }

    public IDbConnection Connection
    {
      get { throw new NotImplementedException(); }
    }

    public void Commit()
    {
      throw new NotImplementedException();
    }

    public void Rollback()
    {
      throw new NotImplementedException();
    }

    public IDbCommand CreateCommand()
    {
      return new LoggingDbCommandWrapper(new TestCommand());
    }

    public void Dispose()
    {
    }
  }

  class TestIDbDataParameter : IDbDataParameter
  {
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
        throw new NotImplementedException();
      }
      set
      {
        throw new NotImplementedException();
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
        throw new NotImplementedException();
      }
      set
      {
        throw new NotImplementedException();
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
        throw new NotImplementedException();
      }
      set
      {
        throw new NotImplementedException();
      }
    }
  }

  class TestCommand : IDbCommand
  {
    private string _commandText;
    private IDataParameterCollection _commandParameters;

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
      throw new NotImplementedException();
    }

    public int ExecuteNonQuery()
    {
      throw new NotImplementedException();
    }

    public IDataReader ExecuteReader(CommandBehavior behavior)
    {
      throw new NotImplementedException();
    }

    public IDataReader ExecuteReader()
    {
      return new TestReader(_commandText);
    }

    public object ExecuteScalar()
    {
      throw new NotImplementedException();
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
    IList<IDictionary<int, string>> results;
    int index = -1;

    public TestReader(string commandText)
    {
      results = new List<IDictionary<int, string>>();

      if (commandText == "SELECT MIAM_ID, IDENTIFIER, DATABASE_OBJECT_NAME FROM MIA_NAME_ALIASES")
      {
        AddResult(new string[] { "", GetMIATableIdentifier(MediaAspect.Metadata), "M_MEDIAITEM" });
        AddResult(new string[] { "", GetMIAAttributeColumnIdentifier(MediaAspect.ATTR_TITLE), "TITLE" });

        AddResult(new string[] { "", GetMIATableIdentifier(AudioAspect.Metadata), "M_AUDIOITEM" });
        AddResult(new string[] { "", GetMIAAttributeColumnIdentifier(AudioAspect.ATTR_ALBUM), "ALBUM" });

        /*
        AddResult(new string[] { "", GetMIATableIdentifier(MediaLibrary_Relationships.Metadata), "M_RELATIONSHIPS" });
        AddResult(new string[] { "", GetMIAAttributeColumnIdentifier(MediaLibrary_Relationships.ATTR_LEFT_ID), "LEFTID" });
        AddResult(new string[] { "", GetMIAAttributeColumnIdentifier(MediaLibrary_Relationships.ATTR_LEFT_TYPE), "LEFTTYPE" });
        AddResult(new string[] { "", GetMIAAttributeColumnIdentifier(MediaLibrary_Relationships.ATTR_RIGHT_ID), "RIGHTID" });
        AddResult(new string[] { "", GetMIAAttributeColumnIdentifier(MediaLibrary_Relationships.ATTR_RIGHT_TYPE), "RIGHTTYPE" });
        */
      }
      else if (commandText == "SELECT MIAM_ID, MIAM_SERIALIZATION FROM MIA_TYPES")
      {

      }
      else if (commandText == "SELECT MIAM_ID, MIAM_SERIALIZATION, CREATION_DATE FROM MIA_TYPES")
      {

      }
      else if (commandText == "SELECT MIAM_ID, CREATION_DATE FROM MIA_TYPES")
      {

      }
      else
      {
        throw new NotImplementedException(string.Format("Cannot create results for {0}", commandText));
      }
    }

    private string GetMIATableIdentifier(MediaItemAspectMetadata miam)
    {
      return "T_" + SqlUtils.ToSQLIdentifier(miam.AspectId.ToString()).ToUpperInvariant();
    }

    private string GetMIAAttributeColumnIdentifier(MediaItemAspectMetadata.AttributeSpecification spec)
    {
      return "A_" + SqlUtils.ToSQLIdentifier(spec.ParentMIAM.AspectId + "_" + spec.AttributeName).ToUpperInvariant();
    }

    private void AddResult(string[] values)
    {
      IDictionary<int, string> result = new Dictionary<int, string>();
      for(int index = 0; index < values.Length; index++)
      {
        result[index] = values[index];
      }
      results.Add(result);
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
      index++;
      return index < results.Count;
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
      throw new NotImplementedException();
    }

    public long GetInt64(int i)
    {
      throw new NotImplementedException();
    }

    public string GetName(int i)
    {
      throw new NotImplementedException();
    }

    public int GetOrdinal(string name)
    {
      throw new NotImplementedException();
    }

    public string GetString(int i)
    {
      if (index >= results.Count)
      {
        throw new IndexOutOfRangeException();
      }

      string value;
      if(!results[index].TryGetValue(i, out value))
      {
        throw new KeyNotFoundException(string.Format("No key {0}", i));
      }

      return value;
    }

    public object GetValue(int i)
    {
      throw new NotImplementedException();
    }

    public int GetValues(object[] values)
    {
      throw new NotImplementedException();
    }

    public bool IsDBNull(int i)
    {
      throw new NotImplementedException();
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

  class TestDatabase : ISQLDatabase
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
      get { throw new NotImplementedException(); }
    }

    public string GetSQLType(Type dotNetType)
    {
      throw new NotImplementedException();
    }

    public string GetSQLVarLengthStringType(uint maxNumChars)
    {
      throw new NotImplementedException();
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
      throw new NotImplementedException();
    }

    public object ReadDBValue(Type type, IDataReader reader, int colIndex)
    {
      if (type == typeof(string))
      {
        return reader.GetString(colIndex);
      }

      throw new NotImplementedException();
    }

    public ITransaction BeginTransaction(IsolationLevel level)
    {
      throw new NotImplementedException();
    }

    public ITransaction BeginTransaction()
    {
      return new TestTransaction();
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
  }

  [TestClass]
  public class TestQueryEngine
  {
    [TestMethod]
    public void TestMediaItemIdFilter()
    {
      ServiceRegistration.Set<IPathManager>(new PathManager());
      ServiceRegistration.Get<IPathManager>().SetPath("LOG", ".");
      ServiceRegistration.Set<ISQLDatabase>(new TestDatabase());

      Guid itemId1 = new Guid("aaaaaaaa-1111-1111-1111-aaaaaaaaaaaa");
      Guid itemId2 = new Guid("bbbbbbbb-2222-2222-2222-bbbbbbbbbbbb");

      IList<Guid> ids = new List<Guid>();
      ids.Add(itemId1);
      ids.Add(itemId2);
      IFilter filter = new MediaItemIdFilter(ids);

      IList<object> resultParts = new List<object>();
      IList<BindVar> resultBindVars = new List<BindVar>();

      TestCompiledFilter compiledFilter = new TestCompiledFilter();
      compiledFilter.test(new MIA_Management(), filter, null, "test", null, resultParts, resultBindVars);

      Console.WriteLine("Result parts [{0}]", string.Join(",", resultParts));
      Console.WriteLine("Result bind vars [{0}]", string.Join(",", resultBindVars));
    }

    [TestMethod]
    public void TestLikeFilter()
    {
      ServiceRegistration.Set<IPathManager>(new PathManager());
      ServiceRegistration.Get<IPathManager>().SetPath("LOG", ".");
      ServiceRegistration.Set<ISQLDatabase>(new TestDatabase());

      IFilter filter = new LikeFilter(MediaAspect.ATTR_TITLE, "%", null);

      ICollection<MediaItemAspectMetadata> requiredMIATypes = new List<MediaItemAspectMetadata>();
      requiredMIATypes.Add(MediaAspect.Metadata);
      IList<object> resultParts = new List<object>();
      IList<BindVar> resultBindVars = new List<BindVar>();

      TestCompiledFilter compiledFilter = new TestCompiledFilter();
      compiledFilter.test(new MIA_Management(), filter, requiredMIATypes, "test", null, resultParts, resultBindVars);

      Console.WriteLine("Result parts [{0}]", string.Join(",", resultParts));
      Console.WriteLine("Result bind vars [{0}]", string.Join(",", resultBindVars));
    }

    [TestMethod]
    public void TestRelationshipFilter()
    {
      ServiceRegistration.Set<IPathManager>(new PathManager());
      ServiceRegistration.Set<ISQLDatabase>(new TestDatabase());

      Guid movieId = new Guid("aaaaaaaa-1111-1111-1111-aaaaaaaaaaaa");
      Guid movieType = new Guid("bbbbbbbb-2222-2222-2222-bbbbbbbbbbbb");
      Guid actorType = new Guid("cccccccc-3333-3333-3333-cccccccccccc");
      IFilter filter = new RelationshipFilter(movieId, movieType, actorType);

      ICollection<MediaItemAspectMetadata> requiredMIATypes = new List<MediaItemAspectMetadata>();

      IList<object> resultParts = new List<object>();
      IList<BindVar> resultBindVars = new List<BindVar>();
      ICollection<TableJoin> tableJoins = new List<TableJoin>();

      TestCompiledFilter compiledFilter = new TestCompiledFilter();
      compiledFilter.test(new MIA_Management(), filter, requiredMIATypes, "test", tableJoins, resultParts, resultBindVars);
    }

    [TestMethod]
    public void TestLikeQueryBuilder()
    {
      ServiceRegistration.Set<IPathManager>(new PathManager());
      ServiceRegistration.Get<IPathManager>().SetPath("LOG", ".");
      ServiceRegistration.Set<ISQLDatabase>(new TestDatabase());

      IFilter filter = new LikeFilter(MediaAspect.ATTR_TITLE, "%", null);

      ICollection<MediaItemAspectMetadata> requiredMIATypes = new List<MediaItemAspectMetadata>();
      requiredMIATypes.Add(MediaAspect.Metadata);
      requiredMIATypes.Add(AudioAspect.Metadata);

      MainQueryBuilder builder = new MainQueryBuilder(new MIA_Management(), new List<QueryAttribute>(), null, requiredMIATypes, new List<MediaItemAspectMetadata>(), filter, null);

      string mediaItemIdAlias = null;
      IDictionary<MediaItemAspectMetadata, string> miamAliases = null;
      IDictionary<QueryAttribute, string> attributeAliases = null;
      string statementStr = null;
      IList<BindVar> bindVars = null;

      builder.GenerateSqlStatement(out mediaItemIdAlias, out miamAliases, out attributeAliases, out statementStr, out bindVars);
      Console.WriteLine("mediaItemIdAlias: {0}", mediaItemIdAlias);
      Console.WriteLine("miamAliases: [{0}]", string.Join(",", miamAliases));
      Console.WriteLine("attributeAliases: [{0}]", string.Join(",", attributeAliases));
      Console.WriteLine("statementStr: {0}", statementStr);
      Console.WriteLine("bindVars: [{0}]", string.Join(",", bindVars));
    }

    [TestMethod]
    public void TestRelationshipQueryBuilder()
    {
      ServiceRegistration.Set<IPathManager>(new PathManager());
      ServiceRegistration.Set<ISQLDatabase>(new TestDatabase());

      Guid movieId = new Guid("aaaaaaaa-1111-1111-1111-aaaaaaaaaaaa");
      Guid movieType = new Guid("bbbbbbbb-2222-2222-2222-bbbbbbbbbbbb");
      Guid actorType = new Guid("cccccccc-3333-3333-3333-cccccccccccc");
      IFilter filter = new RelationshipFilter(movieId, movieType, actorType);

      ICollection<MediaItemAspectMetadata> requiredMIATypes = new List<MediaItemAspectMetadata>();
      requiredMIATypes.Add(MediaAspect.Metadata);
      requiredMIATypes.Add(AudioAspect.Metadata);

      MainQueryBuilder builder = new MainQueryBuilder(new MIA_Management(), new List<QueryAttribute>(), null, requiredMIATypes, new List<MediaItemAspectMetadata>(), filter, null);

      string mediaItemIdAlias = null;
      IDictionary<MediaItemAspectMetadata, string> miamAliases = null;
      IDictionary<QueryAttribute, string> attributeAliases = null;
      string statementStr = null;
      IList<BindVar> bindVars = null;

      builder.GenerateSqlStatement(out mediaItemIdAlias, out miamAliases, out attributeAliases, out statementStr, out bindVars);
      Console.WriteLine("mediaItemIdAlias: {0}", mediaItemIdAlias);
      Console.WriteLine("miamAliases: [{0}]", string.Join(",", miamAliases));
      Console.WriteLine("attributeAliases: [{0}]", string.Join(",", attributeAliases));
      Console.WriteLine("statementStr: {0}", statementStr);
      Console.WriteLine("bindVars: [{0}]", string.Join(",", bindVars));
    }

    [TestMethod]
    public void TestAndQueryBuilder()
    {
      //PathManager manager = new PathManager();
      //manager.SetPath("LOG", "C:\\Users\\Michael\\Dropbox\\MediaPortal-2");
      //ServiceRegistration.Set<IPathManager>(manager);
      ServiceRegistration.Set<IPathManager>(new PathManager());
      ServiceRegistration.Get<IPathManager>().SetPath("LOG", ".");
      ServiceRegistration.Set<ISQLDatabase>(new TestDatabase());

      IList<IFilter> filters = new List<IFilter>();
      filters.Add(new LikeFilter(MediaAspect.ATTR_TITLE, "%", null));
      filters.Add(new LikeFilter(AudioAspect.ATTR_ALBUM, "%", null));
      IFilter filter = new BooleanCombinationFilter(BooleanOperator.And, filters);

      ICollection<MediaItemAspectMetadata> requiredMIATypes = new List<MediaItemAspectMetadata>();
      requiredMIATypes.Add(MediaAspect.Metadata);
      requiredMIATypes.Add(AudioAspect.Metadata);

      MainQueryBuilder builder = new MainQueryBuilder(new MIA_Management(), new List<QueryAttribute>(), null, requiredMIATypes, new List<MediaItemAspectMetadata>(), filter, null);

      string mediaItemIdAlias = null;
      IDictionary<MediaItemAspectMetadata, string> miamAliases = null;
      IDictionary<QueryAttribute, string> attributeAliases = null;
      string statementStr = null;
      IList<BindVar> bindVars = null;

      builder.GenerateSqlStatement(out mediaItemIdAlias, out miamAliases, out attributeAliases, out statementStr, out bindVars);
      Console.WriteLine("mediaItemIdAlias: {0}", mediaItemIdAlias);
      Console.WriteLine("miamAliases: [{0}]", string.Join(",", miamAliases));
      Console.WriteLine("attributeAliases: [{0}]", string.Join(",", attributeAliases));
      Console.WriteLine("statementStr: {0}", statementStr);
      Console.WriteLine("bindVars: [{0}]", string.Join(",", bindVars));
    }
  }
}
