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
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using MediaPortal.Backend.Database;
using MediaPortal.Backend.Services.Database;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Utilities.DB;
using NUnit.Framework;

namespace MediaPortal.Mock
{
  public class MockDBUtils : DBUtils
  {
    private static readonly MockDatabase DATABASE = new MockDatabase();
    private static readonly MockDatabaseManager DATABASE_MANAGER = new MockDatabaseManager();
    private static readonly IDictionary<string, IList<MockReader>> READERS = new Dictionary<string, IList<MockReader>>();
    private static IList<MockCommand> COMMANDS = new List<MockCommand>();
    private static MockReader ALIASES_READER = null;
    private static SQLiteConnection CONNECTION;

    static MockDBUtils()
    {
      // Override mappings to make output log more readable
      SIMPLEDOTNETTYPE2DBTYPE[typeof(DateTime)] = DbType.String;
      SIMPLEDOTNETTYPE2DBTYPE[typeof(Guid)] = DbType.String;

      ServiceRegistration.Set<ISQLDatabase>(DATABASE);
      ServiceRegistration.Set<IDatabaseManager>(DATABASE_MANAGER);

      Reset();
    }

    public static DbType GetType(Type type)
    {
      return SIMPLEDOTNETTYPE2DBTYPE[type];
    }

    public static MockReader GetAliasesReader()
    {
      return ALIASES_READER;
    }

    public static void Reset()
    {
      COMMANDS.Clear();
      READERS.Clear();
      ALIASES_READER = AddReader("SELECT MIAM_ID, IDENTIFIER, DATABASE_OBJECT_NAME FROM MIA_NAME_ALIASES");
      AddReader("SELECT MIAM_ID, MIAM_SERIALIZATION FROM MIA_TYPES");
      AddReader("SELECT MIAM_ID, MIAM_SERIALIZATION, CREATION_DATE FROM MIA_TYPES");
      AddReader("SELECT MIAM_ID, CREATION_DATE FROM MIA_TYPES");
      MockCore.Reset();
    }

    public static MockDatabase Database
    {
      get { return DATABASE; }
    }

    public static SQLiteConnection Connection
    {
      get {  return CONNECTION; }
    }

    public static MockDatabaseManager DatabaseManager
    {
      get { return DATABASE_MANAGER; }
    }

    private static string NormalizeSQL(string sql)
    {
      for (int i = 0; i < 10; i++)
        sql = sql.Replace("_" + i, "");

      return sql;
    }

    public static MockReader AddReader(string command, params string[] columns)
    {
      return AddReader(-1, command, columns);
    }

    public static MockReader AddReader(int index, string command, params string[] columns)
    {
      return AddReader(command, new MockReader(index, columns));
    }

    public static MockReader AddReader(string command, MockReader reader)
    {
      command = NormalizeSQL(command);
      IList<MockReader> readerList;
      if (!READERS.TryGetValue(command, out readerList))
      {
        readerList = new List<MockReader>();
        READERS[command] = readerList;
      }
      readerList.Add(reader);
      return reader;
    }

    public static MockReader GetReader(string sql, string formatterSql)
    {
      sql = NormalizeSQL(sql);
      IList<MockReader> readerList;
      if (!READERS.TryGetValue(sql, out readerList))
      {
        Assert.Fail("No DB reader for " + sql + " -> " + formatterSql);
      }
      if (readerList.Count == 0)
      {
        Assert.Fail("DB readers exhausted for " + sql + " -> " + formatterSql);
      }
      MockReader reader = readerList[0];
      readerList.RemoveAt(0);
      ServiceRegistration.Get<ILogger>().Info("Using reader" + (reader.Id > 0 ? " #" + reader.Id : "") + " for " + sql + " -> " + formatterSql);
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

    public static void AddCommand(MockCommand command)
    {
      COMMANDS.Add(command);
    }

    public static MockCommand FindCommand(string match)
    {
      foreach (MockCommand command in COMMANDS)
      {
        if (command.CommandText.StartsWith(match))
        {
          return command;
        }
      }

      return null;
    }
  }
}