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
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Utilities;
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
    protected readonly IEnumerable<MediaItemAspectMetadata> _necessaryRequestedMIAs;

    protected readonly MediaItemAspectMetadata.AttributeSpecification _queryAttribute;
    protected readonly SelectProjectionFunction _selectProjectionFunction;
    protected readonly IFilter _filter;

    /// <summary>
    /// Creates a new <see cref="ComplexAttributeQueryBuilder"/> instance.
    /// </summary>
    /// <param name="miaManagement">MIAM management instance from media library.</param>
    /// <param name="complexQueryAttribute">Complex attribute, which is requested by this query. Only attributes
    /// with a cardinality different from <see cref="Cardinality.Inline"/> are allowed here.</param>
    /// <param name="selectProjectionFunction">This delegate function will be called for the selected attribute.
    /// It must return an SQL projection expression whose return value is the requested value for that attribute.
    /// If this delegate function is <c>null</c>, the actual attribute is selected without a projection function.</param>
    /// <param name="necessaryRequestedMIAs">MIAs which must be present for the media item to match the query.</param>
    /// <param name="filter">Filter which must be applied to the media items to match the query.</param>
    public ComplexAttributeQueryBuilder(
        MIA_Management miaManagement,
        MediaItemAspectMetadata.AttributeSpecification complexQueryAttribute,
        SelectProjectionFunction selectProjectionFunction,
        IEnumerable<MediaItemAspectMetadata> necessaryRequestedMIAs, IFilter filter) : base(miaManagement)
    {
      _queryAttribute = complexQueryAttribute;
      _selectProjectionFunction = selectProjectionFunction;
      _necessaryRequestedMIAs = necessaryRequestedMIAs;
      _filter = filter;
    }

    public MediaItemAspectMetadata.AttributeSpecification QueryAttribute
    {
      get { return _queryAttribute; }
    }

    public IFilter Filter
    {
      get { return _filter; }
    }

    /// <summary>
    /// Generates a statement to query the distinct values of the complex <see cref="QueryAttribute"/>, together with their
    /// occurence count.
    /// </summary>
    /// <param name="queryAttributeFilter">Additional filter which is defined on the <see cref="QueryAttribute"/>.
    /// That filter COULD be part of the <see cref="Filter"/> but this method can highly optimize a filter on the
    /// query attribute, so a filter on that attribute should be given in this parameter and should not be part of the <see cref="Filter"/>.
    /// </param>
    /// <param name="valueAlias">Alias for the value column.</param>
    /// <param name="groupSizeAlias">Alias for the column containing the number of items in each group.</param>
    /// <param name="statementStr">Statement which was built by this method.</param>
    /// <param name="bindVars">Bind variables to be inserted into placeholders in the returned <paramref name="statementStr"/>.</param>
    public void GenerateSqlGroupByStatement(IAttributeFilter queryAttributeFilter,
        out string valueAlias, out string groupSizeAlias, out string statementStr, out IList<BindVar> bindVars)
    {
      Namespace ns = new Namespace();
      BindVarNamespace bvNamespace = new BindVarNamespace();

      // Contains a mapping of each queried (=selected or filtered) attribute to its request attribute instance
      // data (which holds its requested query table instance)
      IDictionary<QueryAttribute, RequestedAttribute> requestedAttributes = new Dictionary<QueryAttribute, RequestedAttribute>();

      // Dictionary containing as key the requested MIAM instance OR attribute specification of cardinality MTO,
      // mapped to the table query data to request its contents.
      IDictionary<object, TableQueryData> tableQueries = new Dictionary<object, TableQueryData>();

      // Contains the same tables as the tableQueries variable, but in order and enriched with table join data
      IList<TableJoin> tableJoins = new List<TableJoin>();

      // First create the request table query data for the MIA main table and the request attribute for the MIA ID.
      // We'll need the requested attribute as join attribute soon.
      MediaItemAspectMetadata queryMIAM = _queryAttribute.ParentMIAM;
      TableQueryData miaTableQuery = new TableQueryData(_miaManagement.GetMIATableName(queryMIAM));
      tableQueries.Add(queryMIAM, miaTableQuery);

      RequestedAttribute miaIdAttribute = new RequestedAttribute(miaTableQuery, MIA_Management.MIA_MEDIA_ITEM_ID_COL_NAME);

      // Ensure that the tables for all necessary MIAs are requested first (INNER JOIN)
      foreach (MediaItemAspectMetadata miaType in _necessaryRequestedMIAs)
      {
        TableQueryData tqd;
        if (!tableQueries.TryGetValue(miaType, out tqd))
          tqd = tableQueries[miaType] = TableQueryData.CreateTableQueryOfMIATable(_miaManagement, miaType);
        if (miaType != queryMIAM)
          tableJoins.Add(new TableJoin("INNER JOIN", tqd,
              new RequestedAttribute(tqd, MIA_Management.MIA_MEDIA_ITEM_ID_COL_NAME), miaIdAttribute));
      }

      CompiledFilter compiledFilter = new CompiledFilter(_miaManagement, _filter, ns, bvNamespace, miaIdAttribute.GetQualifiedName(ns), tableJoins);

      // Build table query data for each Inline attribute which is part of a filter
      // + compile query attribute
      foreach (QueryAttribute attr in compiledFilter.RequiredAttributes)
      {
        if (attr.Attr.Cardinality != Cardinality.Inline && attr.Attr.Cardinality != Cardinality.ManyToOne)
          continue;
        // Tables of Inline and MTO attributes, which are part of a filter, are joined with main table
        RequestedAttribute ra;
        RequestSimpleAttribute(attr, tableQueries, tableJoins, "LEFT OUTER JOIN", requestedAttributes,
            null, miaIdAttribute, out ra);
      }

      TableQueryData joinTableQuery;
      RequestedAttribute valueAttribute;

      // Build join table for value attribute
      switch (_queryAttribute.Cardinality)
      {
        case Cardinality.OneToMany:
          joinTableQuery = new TableQueryData(_miaManagement.GetMIACollectionAttributeTableName(_queryAttribute));
          tableJoins.Add(new TableJoin("LEFT OUTER JOIN", joinTableQuery,
              new RequestedAttribute(joinTableQuery, MIA_Management.FOREIGN_COLL_ATTR_ID_COL_NAME),
              new RequestedAttribute(miaTableQuery, MIA_Management.MIA_MEDIA_ITEM_ID_COL_NAME)));
          valueAttribute = new RequestedAttribute(joinTableQuery, MIA_Management.COLL_ATTR_VALUE_COL_NAME);
          break;
        case Cardinality.ManyToMany:
          joinTableQuery = new TableQueryData(_miaManagement.GetMIACollectionAttributeNMTableName(_queryAttribute));
          tableJoins.Add(new TableJoin("LEFT OUTER JOIN", joinTableQuery,
              new RequestedAttribute(joinTableQuery, MIA_Management.MIA_MEDIA_ITEM_ID_COL_NAME),
              new RequestedAttribute(miaTableQuery, MIA_Management.MIA_MEDIA_ITEM_ID_COL_NAME)));
          TableQueryData collAttrTableQuery = new TableQueryData(_miaManagement.GetMIACollectionAttributeTableName(_queryAttribute));
          tableJoins.Add(new TableJoin("LEFT OUTER JOIN", collAttrTableQuery,
              new RequestedAttribute(joinTableQuery, MIA_Management.FOREIGN_COLL_ATTR_ID_COL_NAME),
              new RequestedAttribute(collAttrTableQuery, MIA_Management.FOREIGN_COLL_ATTR_ID_COL_NAME)));
          valueAttribute = new RequestedAttribute(collAttrTableQuery, MIA_Management.COLL_ATTR_VALUE_COL_NAME);
          break;
        default:
          throw new IllegalCallException("Media item aspect attributes of cardinality '{0}' cannot be requested via the {1}",
              _queryAttribute.Cardinality, GetType().Name);
      }

      // Selected attributes
      string valueDeclaration = _selectProjectionFunction == null ?
          valueAttribute.GetDeclarationWithAlias(ns, out valueAlias) :
          valueAttribute.GetDeclarationWithAlias(ns, _selectProjectionFunction, out valueAlias);
      groupSizeAlias = "C";
      StringBuilder result = new StringBuilder("SELECT COUNT(V.ID) ");
      result.Append(groupSizeAlias);
      result.Append(", V.");
      result.Append(valueAlias);
      result.Append(" FROM (");
      result.Append("SELECT DISTINCT ");
      result.Append(miaIdAttribute.GetQualifiedName(ns));
      result.Append(" ID, ");
      result.Append(valueDeclaration);

      string whereStr = compiledFilter.CreateSqlFilterCondition(ns, requestedAttributes, out bindVars);

      result.Append(" FROM ");

      // Always request the mia table
      result.Append(miaTableQuery.GetDeclarationWithAlias(ns));
      result.Append(' ');

      // Other joined tables
      foreach (TableJoin tableJoin in tableJoins)
      {
        result.Append(tableJoin.GetJoinDeclaration(ns));
        result.Append(' ');
      }

      if (!string.IsNullOrEmpty(whereStr) || queryAttributeFilter != null)
      {
        result.Append("WHERE ");
        IList<string> filters = new List<string>(2);
        if (!string.IsNullOrEmpty(whereStr))
          filters.Add(whereStr);
        if (queryAttributeFilter != null)
        {
          IList<object> resultParts = new List<object>();
          CompiledFilter.BuildAttributeFilterExpression(queryAttributeFilter, valueAttribute.GetQualifiedName(ns), bvNamespace,
              resultParts, bindVars);
          string filterStr = CompiledFilter.CreateSimpleSqlFilterCondition(ns, resultParts, requestedAttributes);
          filters.Add(filterStr);
        }
        result.Append(StringUtils.Join(" AND ", filters));
      }

      result.Append(") V GROUP BY V.");
      result.Append(valueAlias);

      statementStr = result.ToString();
    }

    /// <summary>
    /// Generates a statement to query the values of the complex <see cref="QueryAttribute"/>.
    /// </summary>
    /// <param name="valueAlias">Alias for the value column.</param>
    /// <param name="mediaItemIdAlias">Alias for the ID of the media item to which the value belongs.</param>
    /// <param name="statementStr">Statement which was built by this method.</param>
    /// <param name="bindVars">Bind variables to be inserted into placeholders in the returned <paramref name="statementStr"/>.</param>
    public void GenerateSqlStatement(out string mediaItemIdAlias, out string valueAlias,
        out string statementStr, out IList<BindVar> bindVars)
    {
      Namespace ns = new Namespace();
      BindVarNamespace bvNamespace = new BindVarNamespace();

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
      RequestedAttribute orderAttribute;

      // Build main join table
      switch (_queryAttribute.Cardinality)
      {
        case Cardinality.OneToMany:
          mainJoinTableQuery = new TableQueryData(_miaManagement.GetMIACollectionAttributeTableName(_queryAttribute));
          miaIdAttribute = new RequestedAttribute(mainJoinTableQuery, MIA_Management.MIA_MEDIA_ITEM_ID_COL_NAME);
          valueAttribute = new RequestedAttribute(mainJoinTableQuery, MIA_Management.COLL_ATTR_VALUE_COL_NAME);
          orderAttribute = new RequestedAttribute(mainJoinTableQuery, MIA_Management.COLL_ATTR_VALUE_ORDER_COL_NAME);
          break;
        case Cardinality.ManyToMany:
          mainJoinTableQuery = new TableQueryData(_miaManagement.GetMIACollectionAttributeNMTableName(_queryAttribute));
          miaIdAttribute = new RequestedAttribute(mainJoinTableQuery, MIA_Management.MIA_MEDIA_ITEM_ID_COL_NAME);
          TableQueryData collAttrTableQuery = new TableQueryData(_miaManagement.GetMIACollectionAttributeTableName(_queryAttribute));
          tableJoins.Add(new TableJoin("INNER JOIN", collAttrTableQuery,
              new RequestedAttribute(mainJoinTableQuery, MIA_Management.FOREIGN_COLL_ATTR_ID_COL_NAME),
              new RequestedAttribute(collAttrTableQuery, MIA_Management.FOREIGN_COLL_ATTR_ID_COL_NAME)));
          valueAttribute = new RequestedAttribute(collAttrTableQuery, MIA_Management.COLL_ATTR_VALUE_COL_NAME);
          orderAttribute = new RequestedAttribute(collAttrTableQuery, MIA_Management.COLL_ATTR_VALUE_ORDER_COL_NAME);
          break;
        default:
          throw new IllegalCallException("Media item aspect attributes of cardinality '{0}' cannot be requested via the {1}",
              _queryAttribute.Cardinality, GetType().Name);
      }
      tableJoins.Insert(0, new TableJoin("INNER JOIN", mainJoinTableQuery, null, null)); // The first table join doesn't need the join attributes - see below

      // Ensure that the tables for all necessary MIAs are requested first (INNER JOIN)
      foreach (MediaItemAspectMetadata miaType in _necessaryRequestedMIAs)
      {
        if (tableQueries.ContainsKey(miaType))
          // We only come here if miaType is the MIA of the requested attribute itself or if it was already queried as necessary MIA, so optimize redundant entry
          continue;
        TableQueryData tqd = tableQueries[miaType] = TableQueryData.CreateTableQueryOfMIATable(_miaManagement, miaType);
        tableJoins.Add(new TableJoin("INNER JOIN", tqd,
            new RequestedAttribute(tqd, MIA_Management.MIA_MEDIA_ITEM_ID_COL_NAME), miaIdAttribute));
      }

      CompiledFilter compiledFilter = new CompiledFilter(_miaManagement, _filter, ns, bvNamespace, miaIdAttribute.GetQualifiedName(ns), tableJoins);

      // Build table query data for each Inline attribute which is part of a filter
      // + compile query attribute
      foreach (QueryAttribute attr in compiledFilter.RequiredAttributes)
      {
        if (attr.Attr.Cardinality != Cardinality.Inline && attr.Attr.Cardinality != Cardinality.ManyToOne)
          continue;
        // Tables of Inline and MTO attributes, which are part of a filter, are joined with main table
        RequestedAttribute ra;
        RequestSimpleAttribute(attr, tableQueries, tableJoins, "LEFT OUTER JOIN", requestedAttributes,
            null, miaIdAttribute, out ra);
      }
      StringBuilder result = new StringBuilder("SELECT ");

      // Append MIA ID attribute only if no DISTINCT query is made
      result.Append(_selectProjectionFunction == null ?
          miaIdAttribute.GetDeclarationWithAlias(ns, out mediaItemIdAlias) :
          miaIdAttribute.GetDeclarationWithAlias(ns, _selectProjectionFunction, out mediaItemIdAlias));
      result.Append(", ");

      // Selected attributes
      result.Append(valueAttribute.GetDeclarationWithAlias(ns, out valueAlias));

      string whereStr = compiledFilter.CreateSqlFilterCondition(ns, requestedAttributes, out bindVars);

      result.Append(" FROM ");

      bool firstJoinTable = true;
      // Other joined tables
      foreach (TableJoin tableJoin in tableJoins)
      {
        if (firstJoinTable)
        {
          result.Append(tableJoin.JoinedTable.GetDeclarationWithAlias(ns));
          firstJoinTable = false;
        }
        else
          result.Append(tableJoin.GetJoinDeclaration(ns));
        result.Append(' ');
      }

      if (!string.IsNullOrEmpty(whereStr))
      {
        result.Append("WHERE ");
        result.Append(whereStr);
      }

      if(orderAttribute != null)
      {
        result.Append(" ORDER BY ");
        result.Append(orderAttribute.GetQualifiedName(ns));
      }

      statementStr = result.ToString();
    }

    public override string ToString()
    {
      string mediaItemIdAlias;
      string valueAlias;
      string statementStr;
      IList<BindVar> bindVars;
      GenerateSqlStatement(out mediaItemIdAlias, out valueAlias, out statementStr, out bindVars);
      return statementStr;
    }
  }
}
