#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;

namespace MediaPortal.Database
{
  public interface ITransaction : IDisposable
  {
    IDbConnection Connection { get; }
    void Commit();
    void Rollback();

    /// <summary>
    /// Creates a database command for this transaction.
    /// </summary>
    /// <remarks>
    /// The returned command can be reused multiple times during the runtime of this transaction, i.e. before the transaction
    /// was closed using <see cref="Commit"/> or <see cref="Rollback"/>.
    /// </remarks>
    /// <returns>Database command instance.</returns>
    IDbCommand CreateCommand();
  }

  /// <summary>
  /// Provides access to the system's database.
  /// </summary>
  public interface ISQLDatabase
  {
    /// <summary>
    /// Returns a string which identifies the type of database to use. For some SQL constructs, it might depend on the
    /// database system if they are supported.
    /// </summary>
    string DatabaseType { get; }

    /// <summary>
    /// Returns the version of the underlaying database implementation.
    /// </summary>
    string DatabaseVersion { get; }

    /// <summary>
    /// Gets the maximum length of a table name in this database.
    /// </summary>
    uint MaxTableNameLength { get; }

    /// <summary>
    /// Returns the name of an SQL type (to be used in SQL scripts) which can store values of the specified .net type
    /// without truncation or loss of data.
    /// </summary>
    /// <remarks>
    /// Implementors must support mappings for at least the following types:
    /// <list>
    /// <item><see cref="DateTime"/></item>
    /// <item><see cref="Char"/></item>
    /// <item><see cref="Boolean"/></item>
    /// <item><see cref="Single"/></item>
    /// <item><see cref="Double"/></item>
    /// <item><see cref="Int32"/></item>
    /// <item><see cref="Int64"/></item>
    /// </list>
    /// </remarks>
    /// <param name="dotNetType">Type to get the SQL type pendant for.</param>
    /// <returns>SQL type which can hold an object of the specified <paramref name="dotNetType"/>. The result value is
    /// <c>null</c>, if the database doesn't support the given <paramref name="dotNetType"/>.</returns>
    string GetSQLType(Type dotNetType);

    /// <summary>
    /// Returns the type for variable length unicode strings of the specified maximum length in this database.
    /// </summary>
    /// <param name="maxNumChars">Maximum number of chars which should fit into the returned string type.</param>
    /// <returns>SQL string type for a unicode string of the given length.</returns>
    string GetSQLVarLengthStringType(uint maxNumChars);

    /// <summary>
    /// Returns the type for unicode strings of the given fixed length in this database.
    /// </summary>
    /// <param name="maxNumChars">Number of chars which should fit into the returned string type.</param>
    /// <returns>SQL string type for a fixed-length string of the given length.</returns>
    string GetSQLFixedLengthStringType(uint maxNumChars);

    /// <summary>
    /// Gets a database connection from the connection pool and starts a new transaction on that connection
    /// with the specified isolation level.
    /// </summary>
    /// <remarks>
    /// The returned transaction instance has to be closed with using of the methods <see cref="ITransaction.Commit"/>,
    /// <see cref="ITransaction.Rollback"/> or <see cref="ITransaction.Dispose"/>, because this is needed for maintaining
    /// the connection pool management in the background.
    /// </remarks>
    /// <param name="level">Transaction level to use.</param>
    /// <returns>Transaction instance.</returns>
    ITransaction BeginTransaction(IsolationLevel level);

    /// <summary>
    /// Gets a database connection from the connection pool and starts a new transaction on that connection
    /// with the isolation level <see cref="IsolationLevel.ReadCommitted"/>.
    /// </summary>
    /// <remarks>
    /// The returned transaction instance has to be closed with using of the methods <see cref="ITransaction.Commit"/>,
    /// <see cref="ITransaction.Rollback"/> or <see cref="ITransaction.Dispose"/>, because this is needed for maintaining
    /// the connection pool management in the background.
    /// </remarks>
    /// <returns>Transaction instance.</returns>
    ITransaction BeginTransaction();

    /// <summary>
    /// Returns the information if a table with the given <paramref name="tableName"/> exists in the database.
    /// </summary>
    /// <param name="tableName">Name of the table to check.</param>
    /// <param name="caseSensitiveName">If set to <c>true</c>, the given <paramref name="tableName"/> will be checked
    /// case-sensitive, i.e. the table must have been created case-sensitive. If the table was not explicitly created
    /// case-sensitive, leave this parameter <c>false</c>.</param>
    /// <returns><c>true</c>, if a table with the given name exists, else <c>false</c>.</returns>
    bool TableExists(string tableName, bool caseSensitiveName);

    /// <summary>
    /// Executes an SQL batch script given in the <paramref name="sqlScript"/> parameter.
    /// </summary>
    /// <remarks>
    /// Statement terminator char is ';'.
    /// </remarks>
    /// <param name="sqlScript">String containing the sequence of SQL commands.</param>
    /// <param name="autoCommit">If set to <c>true</c>, the engine will automatically commit the transaction after each
    /// DDL command.</param>
    void ExecuteScript(string sqlScript, bool autoCommit);

    /// <summary>
    /// Executes a sequence of <paramref name="sqlStatements"/>.
    /// </summary>
    /// <param name="sqlStatements">Sequence of SQL commands.</param>
    /// <param name="autoCommit">If set to <c>true</c>, the engine will automatically commit the transaction after each
    /// DDL command.</param>
    void ExecuteBatch(IList<string> sqlStatements, bool autoCommit);

    /// <summary>
    /// Executes an SQL script file located at the given <paramref name="sqlScriptFilePath"/>.
    /// </summary>
    /// <param name="sqlScriptFilePath">File system path to the SQL script.</param>
    /// <param name="autoCommit">If set to <c>true</c>, the engine will automatically commit the transaction after each
    /// DDL command.</param>
    void ExecuteBatch(string sqlScriptFilePath, bool autoCommit);
    
    /// <summary>
    /// Executes an SQL script provided by the given <paramref name="reader"/>.
    /// </summary>
    /// <param name="reader">Reader which provides the script contents.</param>
    /// <param name="autoCommit">If set to <c>true</c>, the engine will automatically commit the transaction after each
    /// DDL command.</param>
    void ExecuteBatch(TextReader reader, bool autoCommit);
  }
}
