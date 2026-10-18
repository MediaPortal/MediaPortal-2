#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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
using System.Globalization;
using System.IO;
using Microsoft.Data.Sqlite;
using MediaPortal.Backend.Services.Database;
using MediaPortal.Database.SQLite;
using NUnit.Framework;

namespace Tests.Server.Database
{
  /// <summary>
  /// Pins the storage contract the SQLite provider has to keep after its driver swap, measured
  /// against real database files rather than asserted about: Guids stay 16-byte BLOBs so that an
  /// existing MP2 database stays queryable, the culture-aware NOCASE collation is what a
  /// <c>COLLATE NOCASE</c> column sorts and compares with, and a connection does not inherit
  /// registrations from another one.
  /// </summary>
  /// <remarks>
  /// Binding and reading go through <see cref="DBUtils"/> where they can, so that what these tests
  /// pin is the code path the database layer actually takes. The old driver was
  /// <c>System.Data.SQLite</c> with <c>BinaryGUID=true</c>, so a Guid column is a BLOB on disk;
  /// Microsoft.Data.Sqlite's own choice is TEXT, and the difference shows up as an empty result set
  /// rather than an error - which is why it needs tests and not a comment.
  /// </remarks>
  [TestFixture]
  public class TestSQLiteValues
  {
    #region Helpers

    private string _databaseFile;
    private SqliteConnection _connection;

    [SetUp]
    public void SetUp()
    {
      _databaseFile = Path.Combine(Path.GetTempPath(),
          "mp2-sqlite-values-" + Guid.NewGuid().ToString("N") + ".db");
      _connection = OpenConnection(_databaseFile, registerCollation: true);
      Exec("CREATE TABLE t (id TEXT, title TEXT COLLATE NOCASE, stamp TEXT, letter TEXT)");
    }

    [TearDown]
    public void TearDown()
    {
      _connection?.Dispose();
      // A WAL database leaves -wal and -shm files next to it; without them SQLite would recover a
      // database nobody reads, and the temp folder would fill up with test leftovers.
      foreach (string suffix in new[] { "", "-wal", "-shm" })
      {
        string path = _databaseFile + suffix;
        if (File.Exists(path))
          File.Delete(path);
      }
    }

    private static SqliteConnection OpenConnection(string databaseFile, bool registerCollation)
    {
      // Pooling=False because that is what the provider uses: the driver's pool removes registered
      // collations when a connection is returned, measured down to "no such collation sequence:
      // NOCASE" for the next consumer.
      var builder = new SqliteConnectionStringBuilder
      {
        DataSource = databaseFile,
        Cache = SqliteCacheMode.Shared,
        ForeignKeys = true,
        Pooling = false,
      };
      var connection = new SqliteConnection(builder.ToString());
      connection.Open();
      if (registerCollation)
        connection.CreateCollation(SQLiteCultureSensitiveCollation.CollationName, SQLiteCultureSensitiveCollation.Compare);
      using (var command = connection.CreateCommand())
      {
        command.CommandText = "PRAGMA journal_mode=WAL;";
        command.ExecuteNonQuery();
      }
      return connection;
    }

    private void Exec(string sql)
    {
      using (var command = _connection.CreateCommand())
      {
        command.CommandText = sql;
        command.ExecuteNonQuery();
      }
    }

    private object Scalar(string sql)
    {
      using (var command = _connection.CreateCommand())
      {
        command.CommandText = sql;
        return command.ExecuteScalar();
      }
    }

    /// <summary>
    /// Inserts a row whose id is bound exactly the way the provider binds it.
    /// </summary>
    private void InsertRow(Guid id, string title, DateTime? stamp, string letter)
    {
      using (var command = _connection.CreateCommand())
      {
        command.CommandText = "INSERT INTO t(id, title, stamp, letter) VALUES(@id, @title, @stamp, @letter)";
        Assert.That(SQLiteValues.TryAddBinaryParameter(command, "@id", id, typeof(Guid)), Is.Not.Null,
            "the provider's binder must take Guid values");
        command.Parameters.Add(new SqliteParameter("@title", title));
        command.Parameters.Add(new SqliteParameter("@stamp", (object)stamp?.ToString("yyyy-MM-dd HH:mm:ss.fffffff", CultureInfo.InvariantCulture) ?? DBNull.Value));
        command.Parameters.Add(new SqliteParameter("@letter", (object)letter ?? DBNull.Value));
        command.ExecuteNonQuery();
      }
    }

    private int CountWhereId(Guid id)
    {
      using (var command = _connection.CreateCommand())
      {
        command.CommandText = "SELECT count(*) FROM t WHERE id=@id";
        SQLiteValues.TryAddBinaryParameter(command, "@id", id, typeof(Guid));
        return Convert.ToInt32(command.ExecuteScalar());
      }
    }

    private string[] TitlesOrdered()
    {
      using (var command = _connection.CreateCommand())
      {
        command.CommandText = "SELECT title FROM t ORDER BY title";
        var result = new System.Collections.Generic.List<string>();
        using (var reader = command.ExecuteReader())
          while (reader.Read())
            result.Add(reader.GetString(0));
        return result.ToArray();
      }
    }

    #endregion

    #region Guids

    [Test]
    public void TestGuidIdIsStoredAsSixteenByteBlob()
    {
      var id = Guid.Parse("b23fb013-8a3e-464f-b490-1bf0cb5ab2dd");
      InsertRow(id, "x", null, null);

      Assert.That(Scalar("SELECT typeof(id) FROM t"), Is.EqualTo("blob"),
          "Guids are stored as BLOBs, which is what System.Data.SQLite's BinaryGUID=true wrote");
      Assert.That(Scalar("SELECT length(id) FROM t"), Is.EqualTo(16L));
      Assert.That(Scalar("SELECT hex(id) FROM t"),
          Is.EqualTo(BitConverter.ToString(id.ToByteArray()).Replace("-", "")),
          "and in Guid.ToByteArray() byte order, so old rows and new rows are comparable");
    }

    [Test]
    public void TestGuidIdRoundTripsThroughTheSharedReader()
    {
      var id = Guid.NewGuid();
      InsertRow(id, "x", null, null);

      using (var command = _connection.CreateCommand())
      {
        command.CommandText = "SELECT id FROM t";
        using (var reader = command.ExecuteReader())
        {
          Assert.That(reader.Read(), Is.True);
          Assert.That(DBUtils.ReadSimpleDBValue(typeof(Guid), reader, 0), Is.EqualTo(id),
              "the shared reader is what the database layer uses, and it has to accept the BLOB");
        }
      }
    }

    /// <summary>
    /// The regression this whole swap hinges on. Rows written by the old provider hold BLOBs; a Guid
    /// bound as text does not match them, and the failure mode is an empty result, not an exception.
    /// </summary>
    [Test]
    public void TestGuidIdFromTheOldDriverIsFoundByTheProvidersBinder()
    {
      var id = Guid.NewGuid();
      using (var command = _connection.CreateCommand())
      {
        command.CommandText = "INSERT INTO t(id, title) VALUES(x'" + BitConverter.ToString(id.ToByteArray()).Replace("-", "") + "', 'old row')";
        command.ExecuteNonQuery();
      }

      Assert.That(CountWhereId(id), Is.EqualTo(1));
    }

    [Test]
    public void TestGuidIdIsNotFoundWhenBoundAsText()
    {
      var id = Guid.NewGuid();
      using (var command = _connection.CreateCommand())
      {
        command.CommandText = "INSERT INTO t(id, title) VALUES(x'" + BitConverter.ToString(id.ToByteArray()).Replace("-", "") + "', 'old row')";
        command.ExecuteNonQuery();
      }

      using (var command = _connection.CreateCommand())
      {
        command.CommandText = "SELECT count(*) FROM t WHERE id=@id";
        command.Parameters.Add(new SqliteParameter("@id", id));
        Assert.That(command.ExecuteScalar(), Is.EqualTo(0L),
            "a Guid parameter bound by the driver itself is TEXT and misses BLOB rows - if this ever " +
            "stops being true, SQLiteValues has one special case less");
      }
    }

    [Test]
    public void TestNullGuidAndNullBlobBecomeDBNull()
    {
      Assert.That(SQLiteValues.ConvertParameter(typeof(Guid), null), Is.EqualTo(DBNull.Value));
      Assert.That(SQLiteValues.ConvertParameter(typeof(Guid), DBNull.Value), Is.EqualTo(DBNull.Value));
      Assert.That(SQLiteValues.ConvertParameter(typeof(byte[]), null), Is.EqualTo(DBNull.Value));
    }

    [Test]
    public void TestOtherTypesAreLeftToTheSharedBinder()
    {
      using (var command = _connection.CreateCommand())
      {
        Assert.That(SQLiteValues.TryAddBinaryParameter(command, "@p", 7, typeof(int)), Is.Null,
            "only byte[] and Guid are this binder's business");
        Assert.That(command.Parameters.Count, Is.EqualTo(0));
      }
    }

    [Test]
    public void TestByteArrayRoundTrips()
    {
      var bytes = new byte[] { 0, 1, 2, 250, 255 };
      using (var command = _connection.CreateCommand())
      {
        command.CommandText = "INSERT INTO t(id, title) VALUES(@id, 'blob row')";
        SQLiteValues.TryAddBinaryParameter(command, "@id", bytes, typeof(byte[]));
        command.ExecuteNonQuery();
      }

      Assert.That(Scalar("SELECT typeof(id) FROM t"), Is.EqualTo("blob"));
      using (var command = _connection.CreateCommand())
      {
        command.CommandText = "SELECT id FROM t";
        using (var reader = command.ExecuteReader())
        {
          Assert.That(reader.Read(), Is.True);
          // ReadSimpleDBValue has no byte[] case (byte arrays are not one of the DB layer's simple
          // value types), so the driver's own accessor is what a blob column is read with.
          Assert.That(reader.GetValue(0), Is.EqualTo(bytes));
        }
      }
    }

    #endregion

    #region Collation

    [Test]
    public void TestNoCaseColumnSortsCaseInsensitivelyAndNumbersByValue()
    {
      InsertRow(Guid.NewGuid(), "Item10", null, null);
      InsertRow(Guid.NewGuid(), "item9", null, null);
      InsertRow(Guid.NewGuid(), "Item100", null, null);
      InsertRow(Guid.NewGuid(), "apple", null, null);

      // SQLite's built-in NOCASE would order these "apple, Item10, Item100, item9" - the number in
      // the name would be compared digit by digit. MP2 wants "Item9" before "Item10".
      Assert.That(TitlesOrdered(), Is.EqualTo(new[] { "apple", "item9", "Item10", "Item100" }));
    }

    [Test]
    public void TestNoCaseColumnComparesIgnoringCase()
    {
      InsertRow(Guid.NewGuid(), "Item9", null, null);

      using (var command = _connection.CreateCommand())
      {
        command.CommandText = "SELECT count(*) FROM t WHERE title=@t";
        command.Parameters.Add(new SqliteParameter("@t", "item9"));
        Assert.That(command.ExecuteScalar(), Is.EqualTo(1L));
      }
    }

    /// <summary>
    /// Registrations are per connection, which is why the provider registers on every connection it
    /// opens and why it does not let the driver pool them.
    /// </summary>
    [Test]
    public void TestCollationIsNotInheritedByAnotherConnection()
    {
      InsertRow(Guid.NewGuid(), "Item10", null, null);
      InsertRow(Guid.NewGuid(), "item9", null, null);

      using (var other = OpenConnection(_databaseFile, registerCollation: false))
      using (var command = other.CreateCommand())
      {
        command.CommandText = "SELECT title FROM t ORDER BY title";
        var found = new System.Collections.Generic.List<string>();
        using (var reader = command.ExecuteReader())
          while (reader.Read())
            found.Add(reader.GetString(0));

        Assert.That(found, Is.EqualTo(new[] { "Item10", "item9" }),
            "an unregistered connection gets SQLite's built-in NOCASE, which sorts 'Item10' before 'item9'");
      }

      Assert.That(TitlesOrdered(), Is.EqualTo(new[] { "item9", "Item10" }));
    }

    #endregion

    #region Other storage classes the database layer relies on

    [Test]
    public void TestDateTimeIsStoredAsTextTheOldDriverWrote()
    {
      var moment = new DateTime(2026, 10, 18, 11, 30, 6, DateTimeKind.Unspecified).AddTicks(1750763);
      InsertRow(Guid.NewGuid(), "x", moment, null);

      string stored = (string)Scalar("SELECT stamp FROM t");
      Assert.That(stored, Does.Match(@"^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{1,7}$"),
          "the format MP2's existing rows use, e.g. '2026-10-18 11:30:06.1750763'");
      Assert.That(stored, Is.EqualTo("2026-10-18 11:30:06.1750763"));

      using (var command = _connection.CreateCommand())
      {
        command.CommandText = "SELECT stamp FROM t";
        using (var reader = command.ExecuteReader())
        {
          Assert.That(reader.Read(), Is.True);
          Assert.That(DBUtils.ReadSimpleDBValue(typeof(DateTime), reader, 0), Is.EqualTo(moment));
        }
      }
    }

    [Test]
    public void TestCharRoundTripsThroughTheSharedBinder()
    {
      using (var command = _connection.CreateCommand())
      {
        command.CommandText = "INSERT INTO t(id, letter) VALUES(@id, @letter)";
        SQLiteValues.TryAddBinaryParameter(command, "@id", Guid.NewGuid(), typeof(Guid));
        DBUtils.AddSimpleParameter(command, "@letter", 'A', typeof(char));
        command.ExecuteNonQuery();
      }

      Assert.That(Scalar("SELECT typeof(letter) FROM t"), Is.EqualTo("text"));
      using (var command = _connection.CreateCommand())
      {
        command.CommandText = "SELECT letter FROM t";
        using (var reader = command.ExecuteReader())
        {
          Assert.That(reader.Read(), Is.True);
          Assert.That(DBUtils.ReadSimpleDBValue(typeof(char), reader, 0), Is.EqualTo('A'));
        }
      }
    }

    [Test]
    public void TestPragmaFromTheProvidersInitializationCommandIsApplied()
    {
      // What the provider runs on every connection: the settings' InitializationCommand. A pragma
      // batch has to survive being executed as one command text.
      using (var command = _connection.CreateCommand())
      {
        command.CommandText = "PRAGMA wal_autocheckpoint=32768;PRAGMA temp_store=MEMORY;PRAGMA threads=1;";
        command.ExecuteNonQuery();
      }
      Assert.That(Scalar("PRAGMA temp_store"), Is.Not.Null);
      Assert.That(Scalar("PRAGMA journal_mode"), Is.EqualTo("wal"));
    }

    #endregion
  }
}
