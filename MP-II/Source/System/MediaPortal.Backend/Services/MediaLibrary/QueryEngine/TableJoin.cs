#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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
  /// Encapsulates a requested table, identified by a <see cref="TableQueryData"/> instance, which is joined into a query.
  /// </summary>
  public class TableJoin
  {
    protected string _joinType;
    protected TableQueryData _table;
    protected RequestedAttribute _joinAttr1;
    protected RequestedAttribute _joinAttr2;

    public TableJoin(string joinType, TableQueryData table, RequestedAttribute joinAttr1, RequestedAttribute joinAttr2)
    {
      _joinType = joinType;
      _table = table;
      _joinAttr1 = joinAttr1;
      _joinAttr2 = joinAttr2;
    }

    /// <summary>
    /// Join type, like "inner join" or "left outer join".
    /// </summary>
    public string JoinType
    {
      get { return _joinType; }
    }

    /// <summary>
    /// Table which is joined.
    /// </summary>
    public TableQueryData JoinedTable
    {
      get { return _table; }
    }

    /// <summary>
    /// First attribute of the join condition.
    /// </summary>
    public RequestedAttribute JoinAttr1
    {
      get { return _joinAttr1; }
    }

    /// <summary>
    /// Second attribute of the join condition.
    /// </summary>
    public RequestedAttribute JoinAttr2
    {
      get { return _joinAttr2; }
    }

    public string GetJoinDeclaration(Namespace ns)
    {
      return _joinType + " " + _table.GetDeclarationWithAlias(ns) +
          " ON " + _joinAttr1.GetQualifiedName(ns) + " = " + _joinAttr2.GetQualifiedName(ns);
    }
  }
}