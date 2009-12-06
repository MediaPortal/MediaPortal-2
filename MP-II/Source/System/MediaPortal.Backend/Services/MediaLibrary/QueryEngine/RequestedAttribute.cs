#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
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

namespace MediaPortal.Backend.Services.MediaLibrary.QueryEngine
{
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

    public string GetDeclarationWithAlias(Namespace ns, out string alias)
    {
      alias = ns.GetOrCreate(this, "A");
      return _requestedTable.GetAlias(ns) + "." + _columnName + " " + alias;
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
