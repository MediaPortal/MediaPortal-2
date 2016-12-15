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

namespace MediaPortal.Backend.Services.MediaLibrary.QueryEngine
{
  /// <summary>
  /// Delegate which returns an SQL expression for the given <paramref name="selectExpression"/>.
  /// </summary>
  /// <remarks>
  /// Lets say we have an SQL select statement like this:
  /// <code>
  ///   SELECT T.DATE_ATTR FROM TABLE_NAME T;
  /// </code>
  /// The projection function could evaluate the year of the date column, for example for the MS SQL Server,
  /// the function could look like this:
  /// <code>
  ///   static string GetYear(string attr)
  ///   {
  ///     return "DATEPART(YEAR, " + attr + ")";
  ///   }
  /// </code>
  /// The resulting SQL statement could be:
  /// <code>
  ///   SELECT DATEPART(YEAR, T.DATE_ATTR) FROM TABLE_NAME T;
  /// </code>
  /// </remarks>
  /// <param name="selectExpression">Column name (e.g. <c>COL</c>), qualified column name (e.g. <c>T.COL</c>),
  /// column alias (e.g. <c>C1</c>) or an other SQL expression.</param>
  /// <returns>SQL expression.</returns>
  public delegate string SelectProjectionFunction(string selectExpression);

  /// <summary>
  /// Identifies a request of a column on a given table query instance, identified by an instance of <see cref="TableQueryData"/>.
  /// Tyically, instances of this class will be used as value in a key-value map mapping <see cref="QueryAttribute"/>
  /// instances to <see cref="RequestedAttribute"/> instances.
  /// </summary>
  public class RequestedAttribute
  {
    protected readonly TableQueryData _requestedTable;
    protected readonly string _columnName;

    public RequestedAttribute(TableQueryData requestedTable, string columnName)
    {
      _requestedTable = requestedTable;
      _columnName = columnName;
    }

    public TableQueryData RequestedTable
    {
      get { return _requestedTable; }
    }

    public string ColumnName
    {
      get { return _columnName; }
    }

    public string GetQualifiedName(Namespace ns)
    {
      return _requestedTable.GetAlias(ns) + "." + _columnName;
    }

    public string GetAlias(Namespace ns)
    {
      return ns.GetOrCreate(this, "A");
    }

    public string GetDeclarationWithAlias(Namespace ns, out string alias)
    {
      alias = GetAlias(ns);
      return _requestedTable.GetAlias(ns) + "." + _columnName + " " + alias;
    }

    public string GetDeclarationWithAlias(Namespace ns, SelectProjectionFunction selectProjectionFunction, out string alias)
    {
      alias = GetAlias(ns);
      return selectProjectionFunction(_requestedTable.GetAlias(ns) + "." + _columnName) + " " + alias;
    }

    public override bool Equals(object obj)
    {
      if (!(obj is RequestedAttribute))
        return false;
      RequestedAttribute other = (RequestedAttribute) obj;
      return _requestedTable == other._requestedTable &&  _columnName == other._columnName;
    }

    public override int GetHashCode()
    {
      return _requestedTable.GetHashCode() + _columnName.GetHashCode();
    }

    public override string ToString()
    {
      return GetQualifiedName(new Namespace());
    }
  }
}
