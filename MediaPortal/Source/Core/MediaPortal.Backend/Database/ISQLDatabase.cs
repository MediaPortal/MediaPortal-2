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
using MediaPortal.Backend.Services.MediaLibrary;

namespace MediaPortal.Backend.Database
{
  /// <summary>
  /// Holds a single transaction. After usage, each instance has to be either disposed (<see cref="IDisposable.Dispose"/>),
  /// committed (<see cref="Commit"/>) or rolled back (<see cref="Rollback"/>).
  /// </summary>
  public interface ITransaction : IDisposable
  {
    /// <summary>
    /// Returns the parent database instance of this transaction.
    /// </summary>
    ISQLDatabase Database { get; }

    /// <summary>
    /// Returns the connection this transaction uses.
    /// </summary>
    IDbConnection Connection { get; }

    /// <summary>
    /// Begins this transaction on the current connection.
    /// </summary>
    /// <remarks>
    /// Calling <see cref="IDisposable.Dispose"/> after calling this method has no effect.
    /// </remarks>
    void Begin(IsolationLevel level);

    /// <summary>
    /// Commits and disposes this transaction. This transaction becomes invalid after calling this method.
    /// </summary>
    /// <remarks>
    /// Calling <see cref="IDisposable.Dispose"/> after calling this method has no effect.
    /// </remarks>
    void Commit();

    /// <summary>
    /// Rolls this transaction back and disposes it. This transaction becomes invalid after calling this method.
    /// </summary>
    /// <remarks>
    /// Calling <see cref="IDisposable.Dispose"/> after calling this method has no effect.
    /// </remarks>
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
  /// <remarks>
  /// Implementors must support simple type mappings for at least the following types:
  /// <list>
  /// <item><see cref="DateTime"/></item>
  /// <item><see cref="Char"/></item>
  /// <item><see cref="Boolean"/></item>
  /// <item><see cref="Single"/></item>
  /// <item><see cref="Double"/></item>
  /// <item><see cref="Int32"/></item>
  /// <item><see cref="Int64"/></item>
  /// <item><see cref="Guid"/></item>
  /// <item><c>byte[]</c></item>
  /// </list>
  /// Furthermore, string types must be supported. See the defined methods for more information.
  /// </remarks>
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
    /// Gets the maximum length of an object name (table, column, sequence, ...) in this database.
    /// </summary>
    uint MaxObjectNameLength { get; }

    /// <summary>
    /// Returns the name of an SQL type (to be used in SQL scripts) which can store values of the specified .net type
    /// without truncation or loss of data.
    /// </summary>
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
    /// Returns the information if for the given number of characters, the SQL type for strings is a CLOB.
    /// </summary>
    /// <param name="maxNumChars">Number of characters for the string size.</param>
    /// <returns><c>true</c>, if method <see cref="GetSQLVarLengthStringType"/> returns a CLOB type for the given
    /// <paramref name="maxNumChars"/>, else <c>false</c>.</returns>
    bool IsCLOB(uint maxNumChars);

    /// <summary>
    /// Adds a parameter with the given <paramref name="name"/> to the given <paramref name="command"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Command parameters are used to fill named parameters in the command's <see cref="IDbCommand.CommandText"/>.
    /// Command parameters are prefixed with an <c>@</c> character.
    /// If the command text is for example <c>SELECT * FROM MY_TABLE WHERE MY_COLUMN=@COL_VAL</c>, this method should be
    /// used to add the parameter of name <c>"COL_VAL"</c>.
    /// </para>
    /// <para>
    /// It is necessary to implement this method on the database class because for some types, we cannot use the generic
    /// type mapping system via <see cref="DbType"/>. For example, the SQL server uses an enhanced type mapping enum
    /// <see cref="SqlDbType"/>.
    /// </para>
    /// </remarks>
    /// <param name="command">Command to add the parameter.</param>
    /// <param name="name">Name of the parameter.</param>
    /// <param name="value">Current value of the parameter for the current query.</param>
    /// <param name="type">Type of the value. This parameter will affect the type which is used to marshal the given
    /// value in the DB driver. Typically, this type is the type of the given <paramref name="value"/> parameter.</param>
    /// <returns>The created parameter instance.</returns>
    IDbDataParameter AddParameter(IDbCommand command, string name, object value, Type type);

    /// <summary>
    /// Reads a value from the given DB data <paramref name="reader"/> from the column of the given <paramref name="colIndex"/>.
    /// </summary>
    /// <param name="type">Type of the parameter to read.</param>
    /// <param name="reader">Reader to take the value from.</param>
    /// <param name="colIndex">Index of the column in the query result which is represented by the
    /// <paramref name="reader"/>.</param>
    /// <returns>Read value.</returns>
    object ReadDBValue(Type type, IDataReader reader, int colIndex);

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
    /// Gets a database connection from the connection pool and prepares a new transaction on that connection without starting it.
    /// </summary>
    /// <remarks>
    /// The returned transaction instance can be started with <see cref="ITransaction.Begin"/> and has to be closed with 
    /// using of the methods <see cref="ITransaction.Commit"/>, <see cref="ITransaction.Rollback"/> or <see cref="ITransaction.Dispose"/>, 
    /// because this is needed for maintaining the connection pool management in the background.
    /// </remarks>
    /// <returns>Transaction instance.</returns>
    ITransaction CreateTransaction();

    /// <summary>
    /// Returns the information if a table with the given <paramref name="tableName"/> exists in the database.
    /// </summary>
    /// <param name="tableName">Name of the table to check.</param>
    /// <returns><c>true</c>, if a table with the given name exists, else <c>false</c>.</returns>
    bool TableExists(string tableName);

    /// <summary>
    /// Creates an expression to concatenate the two given string expressions. For Oracle, that will be
    /// <c>str1 + "||" + str2</c>, for example. For MS SQL Server, that will be <c>str1 + "+" + str2</c>.
    /// </summary>
    /// <param name="str1">First string to concatenate.</param>
    /// <param name="str2">Second string to concatenate.</param>
    /// <returns>Expression which concatenates the two strings.</returns>
    string CreateStringConcatenationExpression(string str1, string str2);

    string CreateSubstringExpression(string str1, string posExpr);
    string CreateSubstringExpression(string str1, string posExpr, string lenExpr);
    string CreateDateToYearProjectionExpression(string selectExpression);
  }

  /// <summary>
  /// Extension interface for processing SQL side paging clauses.
  /// </summary>
  public interface ISQLDatabasePaging : ISQLDatabase
  {
    /// <summary>
    /// Modifies the given <paramref name="statementStr"/> and <paramref name="bindVars"/> to add paging clause if required.
    /// If <paramref name="offset"/> or <paramref name="limit"/> was processed by this method, their values will be set to <c>null</c>
    /// to avoid handling those parameters twice.
    /// </summary>
    /// <param name="statementStr">Reference to SQL query.</param>
    /// <param name="bindVars">Reference to list of BindVars.</param>
    /// <param name="offset">Reference to offset.</param>
    /// <param name="limit">Reference to limit.</param>
    /// <returns></returns>
    bool Process(ref string statementStr, ref IList<BindVar> bindVars, ref uint? offset, ref uint? limit);
  }
}
