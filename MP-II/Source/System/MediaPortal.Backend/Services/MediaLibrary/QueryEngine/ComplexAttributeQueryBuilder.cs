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

using System.Collections.Generic;
using System.Text;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Utilities.Exceptions;

namespace MediaPortal.Backend.Services.MediaLibrary.QueryEngine
{
  /// <summary>
  /// Builds the SQL statement for a complex media item aspect attribute query, filtered by a <see cref="CompiledFilter"/>.
  /// Complex attributes are all attributes which have a cardinality different from <see cref="Cardinality.Inline"/> and
  /// <see cref="Cardinality.ManyToOne"/> and thus need an explicit query for each attribute.
  /// </summary>
  public class ComplexAttributeQueryBuilder : BaseQueryBuilder
  {
    protected readonly ICollection<MediaItemAspectMetadata> _necessaryRequestedMIAs;

    protected readonly MediaItemAspectMetadata.AttributeSpecification _queryAttribute;
    protected readonly CompiledFilter _filter;

    /// <summary>
    /// Creates a new <see cref="ComplexAttributeQueryBuilder"/> instance.
    /// </summary>
    /// <param name="miaManagement">MIAM management instance from media library.</param>
    /// <param name="complexQueryAttribute">Complex attribute, which is requested by this query. Only attributes
    /// with a cardinality different from <see cref="Cardinality.Inline"/> are allowed here.</param>
    /// <param name="necessaryRequestedMIAs">MIAs which must be present for the media item to match the query.</param>
    /// <param name="filter">Filter which must be applied to the media items to match the query.</param>
    public ComplexAttributeQueryBuilder(
        MIA_Management miaManagement,
        MediaItemAspectMetadata.AttributeSpecification complexQueryAttribute,
        ICollection<MediaItemAspectMetadata> necessaryRequestedMIAs, CompiledFilter filter) : base(miaManagement)
    {
      _queryAttribute = complexQueryAttribute;
      _necessaryRequestedMIAs = necessaryRequestedMIAs;
      _filter = filter;
    }

    public MediaItemAspectMetadata.AttributeSpecification QueryAttribute
    {
      get { return _queryAttribute; }
    }

    public CompiledFilter Filter
    {
      get { return _filter; }
    }

    /// <summary>
    /// Generates a statement to query the values of the complex <see cref="QueryAttribute"/>.
    /// </summary>
    /// <param name="ns">Namespace used to generate the SQL statement. If the generated statement should be used
    /// inside another statement, the use of a common namespace prevents name collisions.</param>
    /// <param name="distinctValue">If set to <c>true</c>, the returned statement will request a set of distinct values.
    /// If set to <c>false</c>, the returned statement will correlate media item IDs to values of the
    /// <see cref="QueryAttribute"/>.</param>
    /// <param name="mediaItemIdAlias">Alias for the media item ID column.
    /// If <paramref name="distinctValue"/> is set to <c>true</c>, the returned statement won't contain a column for
    /// the media item id and thus the output variable <paramref name="mediaItemIdAlias"/> won't have a meaningful
    /// value.</param>
    /// <param name="valueAlias">Alias for the value column.</param>
    /// <param name="statementStr">Statement which was built by this method.</param>
    /// <param name="values">Values to be inserted into placeholders in the returned <paramref name="statementStr"/>.</param>
    public void GenerateSqlStatement(Namespace ns, bool distinctValue,
        out string mediaItemIdAlias, out string valueAlias, out string statementStr, out IList<object> values)
    {
      // Contains a mapping of each queried (=selected or filtered) attribute to its request attribute instance
      // data (which holds its requested query table instance)
      IDictionary<QueryAttribute, RequestedAttribute> requestedAttributes = new Dictionary<QueryAttribute, RequestedAttribute>();

      // Dictionary containing as key the requested MIAM instance OR attribute specification of cardinality MTO,
      // mapped to the table query data to request its contents.
      IDictionary<object, TableQueryData> tableQueries = new Dictionary<object, TableQueryData>();

      // Contains the same tables as the tableQueries variable, but in order and enriched with table join data
      IList<TableJoin> tableJoins = new List<TableJoin>();

      // First create the request table query data for the external attribute table, which contains the foreign key
      // to the MIA ID, and the request attribute for that MIA ID.
      // We'll need the requested attribute as join attribute soon.
      TableQueryData mainJoinTableQuery;
      RequestedAttribute miaIdAttribute;
      RequestedAttribute valueAttribute;

      // Build main join table
      switch (_queryAttribute.Cardinality)
      {
        case Cardinality.OneToMany:
          mainJoinTableQuery = new TableQueryData(_miaManagement.GetMIACollectionAttributeTableName(_queryAttribute));
          miaIdAttribute = new RequestedAttribute(mainJoinTableQuery, MIA_Management.MIA_MEDIA_ITEM_ID_COL_NAME);
          valueAttribute = new RequestedAttribute(mainJoinTableQuery, MIA_Management.COLL_ATTR_VALUE_COL_NAME);
          break;
        case Cardinality.ManyToMany:
          mainJoinTableQuery = new TableQueryData(_miaManagement.GetMIACollectionAttributeNMTableName(_queryAttribute));
          miaIdAttribute = new RequestedAttribute(mainJoinTableQuery, MIA_Management.MIA_MEDIA_ITEM_ID_COL_NAME);
          TableQueryData collAttrTableQuery = new TableQueryData(_miaManagement.GetMIACollectionAttributeTableName(_queryAttribute));
          tableJoins.Add(new TableJoin("INNER JOIN", collAttrTableQuery,
              new RequestedAttribute(mainJoinTableQuery, MIA_Management.FOREIGN_COLL_ATTR_ID_COL_NAME),
              new RequestedAttribute(collAttrTableQuery, MIA_Management.FOREIGN_COLL_ATTR_ID_COL_NAME)));
          valueAttribute = new RequestedAttribute(collAttrTableQuery, MIA_Management.COLL_ATTR_VALUE_COL_NAME);
          break;
        default:
          throw new IllegalCallException("Media item aspect attributes of cardinality '{0}' cannot be requested via the {1}",
              _queryAttribute.Cardinality, GetType().Name);
      }

      // Ensure that the tables for all necessary MIAs are requested first (INNER JOIN)
      foreach (MediaItemAspectMetadata miaType in _necessaryRequestedMIAs)
      {
        TableQueryData tqd;
        if (!tableQueries.TryGetValue(miaType, out tqd))
          tqd = tableQueries[miaType] = TableQueryData.CreateTableQueryOfMIATable(_miaManagement, miaType);
        tableJoins.Add(new TableJoin("INNER JOIN", tqd,
            new RequestedAttribute(tqd, MIA_Management.MIA_MEDIA_ITEM_ID_COL_NAME), miaIdAttribute));
      }

      // Build table query data for each Inline attribute which is part of a filter
      // + compile query attribute
      foreach (QueryAttribute attr in _filter.FilterAttributes)
      {
        if (attr.Attr.Cardinality != Cardinality.Inline && attr.Attr.Cardinality != Cardinality.ManyToOne)
          continue;
        // Tables of Inline and MTO attributes, which are part of a filter, are joined with main table
        RequestedAttribute ra;
        RequestSimpleAttribute(attr, tableQueries, tableJoins, "LEFT OUTER JOIN", requestedAttributes,
            null, miaIdAttribute, out ra);
      }
      StringBuilder result = new StringBuilder("SELECT ");

      if (distinctValue)
      {
        mediaItemIdAlias = null;
        result.Append("DISTINCT ");
      }
      else
      {
        // Append MIA ID attribute only if no DISTINCT query is made
        result.Append(miaIdAttribute.GetDeclarationWithAlias(ns, out mediaItemIdAlias));
        result.Append(", ");
      }

      // Selectedattributes
      result.Append(valueAttribute.GetDeclarationWithAlias(ns, out valueAlias));

      result.Append(" FROM ");

      // Always request the main join table (depending on the cardinality of query attribute)
      result.Append(mainJoinTableQuery.GetDeclarationWithAlias(ns));
      result.Append(' ');

      // Other joined tables
      foreach (TableJoin tableJoin in tableJoins)
      {
        result.Append(tableJoin.GetJoinDeclaration(ns));
        result.Append(' ');
      }

      string whereStr;
      _filter.CreateSqlFilterCondition(ns, requestedAttributes,
          miaIdAttribute.GetQualifiedName(ns), out whereStr, out values);
      if (!string.IsNullOrEmpty(whereStr))
      {
        result.Append("WHERE ");
        result.Append(whereStr);
      }

      statementStr = result.ToString();
    }

    public override string ToString()
    {
      string mediaItemIdAlias;
      string valueAlias;
      string statementStr;
      IList<object> values;
      GenerateSqlStatement(new Namespace(), false, out mediaItemIdAlias, out valueAlias, out statementStr, out values);
      return statementStr;
    }
  }
}
