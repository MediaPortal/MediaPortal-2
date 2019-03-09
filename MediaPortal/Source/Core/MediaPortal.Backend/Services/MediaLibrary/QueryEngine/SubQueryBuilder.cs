#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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

using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.MLQueries;
using System;
using System.Collections.Generic;

namespace MediaPortal.Backend.Services.MediaLibrary.QueryEngine
{
  public class SubQueryBuilder : MainQueryBuilder
  {
    private int _bindVarCount = 0;

    public SubQueryBuilder(MIA_Management miaManagement, IEnumerable<QueryAttribute> simpleSelectAttributes,
      ICollection<MediaItemAspectMetadata> requiredMIATypes, IFilter filter, IFilter subQueryFilter, int bindVarCount, Guid? userProfileId = null)
      : base(miaManagement, simpleSelectAttributes, null, requiredMIATypes, new List<MediaItemAspectMetadata> { },
          filter, subQueryFilter, null, userProfileId)
    {
      _bindVarCount = bindVarCount;
    }

    protected override CompiledFilter CreateCompiledFilter(Namespace ns, BindVarNamespace bvNamespace, string outerMIIDJoinVariable, IList<TableJoin> tableJoins)
    {
      bvNamespace.BindVarCounter += _bindVarCount;
      return new CompiledFilter(_miaManagement, CreateSubQueryFilter(), _subqueryFilter, ns, bvNamespace, outerMIIDJoinVariable, tableJoins);
    }

    protected IFilter CreateSubQueryFilter()
    {
      if (_subqueryFilter == null)
        return _filter;
      if (_filter == null)
        return _subqueryFilter;
      return BooleanCombinationFilter.CombineFilters(BooleanOperator.And, _filter, _subqueryFilter);
    }

    /// <summary>
    /// Generates the SQL statement for the underlaying query specification.
    /// </summary>
    /// <param name="mediaItemIdAlias">Alias of the media item's IDs in the result set.</param>
    /// <param name="statementStr">SQL statement which was built by this method.</param>
    /// <param name="bindVars">Bind variables to be inserted into the returned <paramref name="statementStr"/>.</param>
    public void GenerateSqlStatement(out string mediaItemIdAlias, out string statementStr, out IList<BindVar> bindVars)
    {
      IDictionary<MediaItemAspectMetadata, string> miamAliases = new Dictionary<MediaItemAspectMetadata, string>();
      IDictionary<QueryAttribute, string> attributeAliases = new Dictionary<QueryAttribute, string>();
      GenerateSqlStatement(false, miamAliases, out mediaItemIdAlias, out attributeAliases, out statementStr, out bindVars);
    }
  }
}
