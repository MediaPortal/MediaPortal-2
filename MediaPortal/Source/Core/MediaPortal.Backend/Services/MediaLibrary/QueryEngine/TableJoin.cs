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

using System.Collections.Generic;
using System.Text;

namespace MediaPortal.Backend.Services.MediaLibrary.QueryEngine
{
  /// <summary>
  /// Encapsulates a requested table, identified by a <see cref="TableQueryData"/> instance, which is joined into a query.
  /// </summary>
  public class TableJoin
  {
    protected string _joinType;
    protected TableQueryData _table;
    protected Dictionary<object, object> _conditionPairs = new Dictionary<object, object>();

    public TableJoin(string joinType, TableQueryData table, RequestedAttribute joinAttr1, RequestedAttribute joinAttr2) :
      this(joinType, table, (object) joinAttr1, (object) joinAttr2) { }

    public TableJoin(string joinType, TableQueryData table, object joinAttr1, object joinAttr2)
    {
      _joinType = joinType;
      _table = table;
      if(joinAttr1 != null && joinAttr2 != null)
        _conditionPairs.Add(joinAttr1, joinAttr2);
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
    /// Add additional conditions
    /// </summary>
    public void AddCondition(object joinAttr1, object joinAttr2)
    {
      _conditionPairs.Add(joinAttr1, joinAttr2);
    }

    public string GetJoinDeclaration(Namespace ns)
    {
      StringBuilder result = new StringBuilder(100);
      result.Append(_joinType);
      result.Append(" ");
      result.Append(_table.GetDeclarationWithAlias(ns));
      if (_conditionPairs.Count > 0)
      {
        result.Append(" ON ");
        bool firstCondition = true;
        foreach (var condition in _conditionPairs)
        {
          if (!firstCondition)
            result.Append(" AND ");

          RequestedAttribute ra = condition.Key as RequestedAttribute;
          if (ra != null)
            result.Append(ra.GetQualifiedName(ns));
          else
            result.Append(condition.Key);
          result.Append(" = ");
          ra = condition.Value as RequestedAttribute;
          if (ra != null)
            result.Append(ra.GetQualifiedName(ns));
          else
            result.Append(condition.Value);

          firstCondition = false;
        }
      }
      return result.ToString();
    }

    public override string ToString()
    {
      return GetJoinDeclaration(new Namespace());
    }
  }
}
