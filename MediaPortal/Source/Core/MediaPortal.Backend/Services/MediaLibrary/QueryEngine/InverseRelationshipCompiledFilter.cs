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
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Utilities;

namespace MediaPortal.Backend.Services.MediaLibrary.QueryEngine
{
  public class InverseRelationshipCompiledFilter : CompiledFilter
  {
    public InverseRelationshipCompiledFilter(MIA_Management miaManagement, MediaItemIdFilter filter, Namespace ns, BindVarNamespace bvNamespace, string outerMIIDJoinVariable, ICollection<TableJoin> tableJoins)
      : base(miaManagement, filter, ns, bvNamespace, outerMIIDJoinVariable, tableJoins)
    {
    }

    protected override void CompileStatementParts(MIA_Management miaManagement, IFilter filter, Namespace ns, BindVarNamespace bvNamespace,
      ICollection<MediaItemAspectMetadata> requiredMIATypes, string outerMIIDJoinVariable, ICollection<TableJoin> tableJoins,
      IList<object> resultParts, IList<BindVar> resultBindVars)
    {
      MediaItemIdFilter mediaItemIdFilter = (MediaItemIdFilter)filter;

      bool first = true;
      foreach (IList<Guid> mediaItemIdsCluster in CollectionUtils.Cluster(mediaItemIdFilter.MediaItemIds, MAX_IN_VALUES_SIZE))
      {
        QueryAttribute qa = new QueryAttribute(RelationshipAspect.ATTR_LINKED_ID);
        IList<string> bindVarRefs = new List<string>(MAX_IN_VALUES_SIZE);
        foreach (Guid mediaItemId in mediaItemIdsCluster)
        {
          BindVar bindVar = new BindVar(bvNamespace.CreateNewBindVarName("V"), mediaItemId, typeof(Guid));
          bindVarRefs.Add("@" + bindVar.Name);
          resultBindVars.Add(bindVar);
        }
        if (!first)
          resultParts.Add(" OR ");
        first = false;
        resultParts.Add(qa);
        resultParts.Add(" IN (" + StringUtils.Join(", ", bindVarRefs) + ")");
      }
    }
  }
}
