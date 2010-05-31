#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System.Collections.Generic;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Utilities.Exceptions;

namespace MediaPortal.Backend.Services.MediaLibrary.QueryEngine
{
  public class BaseQueryBuilder
  {
    protected readonly MIA_Management _miaManagement;

    public BaseQueryBuilder(MIA_Management miaManagement)
    {
      _miaManagement = miaManagement;
    }

    protected void RequestSimpleAttribute(QueryAttribute queryAttribute,
        IDictionary<object, TableQueryData> tableQueries, IList<TableJoin> tableJoins, string miaJoinType,
        IDictionary<QueryAttribute, RequestedAttribute> requestedAttributes,
        IDictionary<MediaItemAspectMetadata, TableQueryData> miaTypeTableQueries,
        RequestedAttribute miaIdAttribute, out RequestedAttribute requestedAttribute)
    {
      if (requestedAttributes.TryGetValue(queryAttribute, out requestedAttribute))
        // Already requested
        return;
      MediaItemAspectMetadata.AttributeSpecification spec = queryAttribute.Attr;
      MediaItemAspectMetadata miaType = spec.ParentMIAM;
      TableQueryData tqd;
      switch (spec.Cardinality)
      {
        case Cardinality.Inline:
          // For Inline queries, we request the Inline attribute's column name at the MIA main table, which gets joined
          // with the MIA ID
          if (!tableQueries.TryGetValue(miaType, out tqd))
          {
            tqd = tableQueries[miaType] = TableQueryData.CreateTableQueryOfMIATable(_miaManagement, miaType);
            if (miaTypeTableQueries != null)
              miaTypeTableQueries.Add(miaType, tqd);
            tableJoins.Add(new TableJoin(miaJoinType, tqd, miaIdAttribute,
                new RequestedAttribute(tqd, MIA_Management.MIA_MEDIA_ITEM_ID_COL_NAME)));
          }
          requestedAttribute = new RequestedAttribute(tqd, _miaManagement.GetMIAAttributeColumnName(queryAttribute.Attr));
          break;
        case Cardinality.ManyToOne:
          // For MTO queries, we request both the MIA main table and the MTO table
          TableQueryData miaTqd;
          if (!tableQueries.TryGetValue(miaType, out miaTqd))
          {
            miaTqd = tableQueries[miaType] = TableQueryData.CreateTableQueryOfMIATable(_miaManagement, miaType);
            if (miaTypeTableQueries != null)
              miaTypeTableQueries.Add(miaType, miaTqd);
            // Add MIA main table to list of table joins
            tableJoins.Add(new TableJoin(miaJoinType, miaTqd, miaIdAttribute,
                new RequestedAttribute(miaTqd, MIA_Management.MIA_MEDIA_ITEM_ID_COL_NAME)));
          }
          if (!tableQueries.TryGetValue(spec, out tqd))
          {
            tqd = tableQueries[spec] = TableQueryData.CreateTableQueryOfMTOTable(_miaManagement, spec);
            // We must use left outer joins for MTO value tables, because if the value is null, the association FK is null
            tableJoins.Add(new TableJoin("LEFT OUTER JOIN", tqd,
                new RequestedAttribute(miaTqd, _miaManagement.GetMIAAttributeColumnName(queryAttribute.Attr)),
                new RequestedAttribute(tqd, MIA_Management.FOREIGN_COLL_ATTR_ID_COL_NAME)));
          }
          requestedAttribute = new RequestedAttribute(tqd, MIA_Management.COLL_ATTR_VALUE_COL_NAME);
          break;
        default:
          throw new IllegalCallException("Attributes of cardinality '{0}' cannot be queried via the {1}", spec.Cardinality, GetType().Name);
      }
      requestedAttributes.Add(queryAttribute, requestedAttribute);
    }
  }
}
