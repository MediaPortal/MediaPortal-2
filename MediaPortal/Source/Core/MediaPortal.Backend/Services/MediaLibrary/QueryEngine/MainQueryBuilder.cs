#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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
using System.Linq;
using System.Text;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Utilities;
using System;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;

namespace MediaPortal.Backend.Services.MediaLibrary.QueryEngine
{
  /// <summary>
  /// Builds the SQL statement for the main media item query. The main query requests all Inline and MTO attributes of
  /// media item aspects, filtered by a <see cref="CompiledFilter"/>.
  /// </summary>
  public abstract class MainQueryBuilder : BaseQueryBuilder
  {
    #region Inner classes

    protected class CompiledSortInformation
    {
      protected RequestedAttribute _sortAttribute;
      protected SortDirection _direction;

      public CompiledSortInformation(RequestedAttribute sortAttribute, SortDirection direction)
      {
        _sortAttribute = sortAttribute;
        _direction = direction;
      }

      public RequestedAttribute SortAttribute
      {
        get { return _sortAttribute; }
      }

      public SortDirection Direction
      {
        get { return _direction; }
      }

      public string GetSortDeclaration(Namespace ns)
      {
        return _sortAttribute.GetQualifiedName(ns) + " " + ToSqlOrderByDirection(_direction);
      }

      protected static string ToSqlOrderByDirection(SortDirection direction)
      {
        return direction == SortDirection.Ascending ? "ASC" : "DESC";
      }
    }

    #endregion

    protected readonly IEnumerable<MediaItemAspectMetadata> _necessaryRequestedMIAs;
    protected readonly IEnumerable<MediaItemAspectMetadata> _optionalRequestedMIAs;

    /// <summary>
    /// Attributes which are selected the in main query (=which are requested to be returned, in contrast to those
    /// attributes used in a filter).
    /// </summary>
    protected readonly IList<QueryAttribute> _selectAttributes;
    protected readonly SelectProjectionFunction _selectProjectionFunction;
    protected readonly IFilter _filter;
    protected readonly IFilter _subqueryFilter;
    protected readonly IList<ISortInformation> _sortInformation;
    protected readonly uint? _offset;
    protected readonly uint? _limit;
    protected readonly Guid? _userProfileId;

    /// <summary>
    /// Creates a new <see cref="MainQueryBuilder"/> instance.
    /// </summary>
    /// <param name="miaManagement">MIAM management instance from media library.</param>
    /// <param name="simpleSelectAttributes">Enumeration of media item aspect attributes, given as
    /// <see cref="QueryAttribute"/> instances, which should be selected by this main query. Only attributes with
    /// cardinalities of <see cref="Cardinality.Inline"/> and <see cref="Cardinality.ManyToOne"/> are allowed here.
    /// Both necessary and optional attributes are allowed in this enumeration.</param>
    /// <param name="selectProjectionFunction">This delegate function will be called for each selected attribute.
    /// It must return an SQL projection expression whose return value is the requested value for that attribute.
    /// If this delegate function is <c>null</c>, the actual attribute is selected without a projection function.</param>
    /// <param name="necessaryRequestedMIAs">MIAs which must be present for the media item to match the query.</param>
    /// <param name="optionalRequestedMIAs">MIAs which will be returned if they are attached to items which are
    /// already returned.</param>
    /// <param name="filter">Filter to restrict the result set.</param>
    /// <param name="sortInformation">List of sorting criteria.</param>
    public MainQueryBuilder(MIA_Management miaManagement, IEnumerable<QueryAttribute> simpleSelectAttributes,
        SelectProjectionFunction selectProjectionFunction,
        IEnumerable<MediaItemAspectMetadata> necessaryRequestedMIAs, IEnumerable<MediaItemAspectMetadata> optionalRequestedMIAs,
        IFilter filter, IFilter subqueryFilter, IList<ISortInformation> sortInformation, Guid? userProfileId = null, uint? limit = null, uint? offset = null)
      : base(miaManagement)
    {
      _necessaryRequestedMIAs = necessaryRequestedMIAs;
      _optionalRequestedMIAs = optionalRequestedMIAs;
      _selectAttributes = new List<QueryAttribute>(simpleSelectAttributes);
      _selectProjectionFunction = selectProjectionFunction;
      _filter = filter;
      _subqueryFilter = subqueryFilter;
      _sortInformation = sortInformation;
      _limit = limit;
      _offset = offset;
      _userProfileId = userProfileId;
    }

    protected void GenerateSqlStatement(bool groupByValues,
        IDictionary<MediaItemAspectMetadata, string> miamAliases,
        out string mediaItemIdOrGroupSizeAlias,
        out IDictionary<QueryAttribute, string> attributeAliases,
        out string statementStr, out IList<BindVar> bindVars)
    {
      Namespace ns = new Namespace();
      BindVarNamespace bvNamespace = new BindVarNamespace();

      // Contains a mapping of each queried (=selected or filtered) attribute to its request attribute instance
      // data (which holds its requested query table instance)
      IDictionary<QueryAttribute, RequestedAttribute> requestedAttributes = new Dictionary<QueryAttribute, RequestedAttribute>();
      attributeAliases = new Dictionary<QueryAttribute, string>();

      // Contains a list of qualified attribute names for all select attributes - needed for GROUP BY-expressions
      ICollection<string> qualifiedGroupByAliases = new List<string>();

      // Contains a list of compiled select attribute declarations. We need this in a separate list (in contrast to using
      // the selectAttributes list together with the compiledAttributes map) because it might be the case that
      // an attribute is requested twice. In that rare case, we need a new alias name for it.
      IList<string> selectAttributeDeclarations = new List<string>();

      // Dictionary containing as key the requested MIAM instance OR attribute specification of cardinality MTO,
      // mapped to the table query data to request its contents.
      IDictionary<object, TableQueryData> tableQueries = new Dictionary<object, TableQueryData>();

      // Contains the same tables as the tableQueries variable, but in order and enriched with table join data
      IList<TableJoin> tableJoins = new List<TableJoin>();

      // Contains all table query data for MIA type tables
      IDictionary<MediaItemAspectMetadata, TableQueryData> miaTypeTableQueries = new Dictionary<MediaItemAspectMetadata, TableQueryData>();

      // Albert, 2012-01-29: Optimized query, don't join with media items table, if we have necessary requested MIAs. In that case,
      // we can use one of their media item ids.
      //// First create the request table query data for the MIA main table and the request attribute for the MIA ID.
      //// We'll need the requested attribute as join attribute soon.
      //TableQueryData miaTableQuery = new TableQueryData(MediaLibrary_SubSchema.MEDIA_ITEMS_TABLE_NAME);
      //RequestedAttribute miaIdAttribute = new RequestedAttribute(miaTableQuery, MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME);

      RequestedAttribute miaIdAttribute = null; // Lazy initialized below

      // Contains CompiledSortInformation instances for each sort information instance
      IList<CompiledSortInformation> compiledSortInformation = null;

      List<BindVar> sqlVars = new List<BindVar>();

      // Ensure that the tables for all necessary MIAs are requested first (INNER JOIN)
      foreach (MediaItemAspectMetadata miaType in _necessaryRequestedMIAs)
      {
        if (miaType.IsTransientAspect)
          continue;
        if (tableQueries.ContainsKey(miaType))
          // We only come here if miaType was already queried as necessary MIA, so optimize redundant entry
          continue;

        TableQueryData tqd = tableQueries[miaType] = TableQueryData.CreateTableQueryOfMIATable(_miaManagement, miaType);
        miaTypeTableQueries.Add(miaType, tqd);
        RequestedAttribute ra;
        // The first table join has invalid join attributes because miaIdAttribute is still null - but only the join table attribute is necessary
        // for the the first table - see below
        tableJoins.Add(new TableJoin("INNER JOIN", tqd,
            ra = new RequestedAttribute(tqd, MIA_Management.MIA_MEDIA_ITEM_ID_COL_NAME), miaIdAttribute));
        if (miaIdAttribute == null)
          miaIdAttribute = ra;
      }

      if (miaIdAttribute == null)
      { // If we didn't request any necessary MIA types, we have to add an artificial table for the miaIdAttribute
        TableQueryData miaTableQuery = new TableQueryData(MediaLibrary_SubSchema.MEDIA_ITEMS_TABLE_NAME);
        tableJoins.Add(new TableJoin("INNER JOIN", miaTableQuery, null, null)); // First table join has invalid join attributes - not needed for first table
        miaIdAttribute = new RequestedAttribute(miaTableQuery, MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME);
      }

      // Ensure that the tables for all optional MIAs are requested first (LEFT OUTER JOIN)
      // That is necessary to make empty optional MIA types available in the result
      foreach (MediaItemAspectMetadata miaType in _optionalRequestedMIAs)
      {
        if (miaType.IsTransientAspect)
          continue;
        if (tableQueries.ContainsKey(miaType))
          // We only come here if miaType was already queried as necessary or optional MIA, so optimize redundant entry
          continue;
        TableQueryData tqd = tableQueries[miaType] = TableQueryData.CreateTableQueryOfMIATable(_miaManagement, miaType);
        miaTypeTableQueries.Add(miaType, tqd);
        tableJoins.Add(new TableJoin("LEFT OUTER JOIN", tqd,
            new RequestedAttribute(tqd, MIA_Management.MIA_MEDIA_ITEM_ID_COL_NAME), miaIdAttribute));
      }

      // Build table query data for each selected Inline and MTO attribute
      // + select query attribute
      // + add alias to selectAttributeDeclarations
      foreach (QueryAttribute attr in _selectAttributes)
      {
        if (attr.Attr.ParentMIAM.IsTransientAspect)
          continue;
        RequestedAttribute ra;
        RequestSimpleAttribute(attr, tableQueries, tableJoins, "LEFT OUTER JOIN", requestedAttributes, miaTypeTableQueries,
            miaIdAttribute, out ra);
        string alias;
        selectAttributeDeclarations.Add(_selectProjectionFunction == null ?
            ra.GetDeclarationWithAlias(ns, out alias) :
            ra.GetDeclarationWithAlias(ns, _selectProjectionFunction, out alias));
        attributeAliases.Add(attr, alias);
        qualifiedGroupByAliases.Add(ra.GetAlias(ns));
      }

      CompiledFilter compiledFilter = CreateCompiledFilter(ns, bvNamespace, miaIdAttribute.GetQualifiedName(ns), tableJoins);

      // Build table query data for each Inline attribute which is part of a filter
      // + compile query attribute
      foreach (QueryAttribute attr in compiledFilter.RequiredAttributes)
      {
        if (attr.Attr.ParentMIAM.IsTransientAspect)
          continue;
        if (attr.Attr.Cardinality != Cardinality.Inline && attr.Attr.Cardinality != Cardinality.ManyToOne)
          continue;
        // Tables of Inline and MTO attributes, which are part of a filter, are joined with main table
        RequestedAttribute ra;
        RequestSimpleAttribute(attr, tableQueries, tableJoins, "LEFT OUTER JOIN", requestedAttributes,
            miaTypeTableQueries, miaIdAttribute, out ra);
      }
      // Build table query data for each sort attribute
      if (_sortInformation != null)
      {
        compiledSortInformation = new List<CompiledSortInformation>();
        BindVar userVar = null;
        foreach (ISortInformation sortInformation in _sortInformation)
        {
          if (AddAttributeSortInformation(sortInformation, miaIdAttribute, requestedAttributes, miaTypeTableQueries, tableQueries, tableJoins, compiledSortInformation))
            continue;
          if (AddChildAggregateAttributeSortInformation(sortInformation, ns, bvNamespace, miaIdAttribute, tableJoins, compiledSortInformation, sqlVars))
            continue;
          if (AddDataSortInformation(sortInformation, miaIdAttribute, tableJoins, compiledSortInformation, sqlVars, ref userVar))
            continue;
        }
      }
      StringBuilder result = new StringBuilder("SELECT ");

      string groupClause = StringUtils.Join(", ", qualifiedGroupByAliases.Select(alias => "V." + alias));
      if (groupByValues)
      {
        mediaItemIdOrGroupSizeAlias = "C";
        // Create an additional COUNT expression for the MEDIA_ITEMS.MEDIA_ITEM_ID in the GROUP BY-statement
        result.Append("COUNT(V.C) ");
        result.Append(mediaItemIdOrGroupSizeAlias);
        if (!string.IsNullOrWhiteSpace(groupClause))
        {
          result.Append(", ");
          result.Append(groupClause);
        }
        result.Append(" FROM (");
        result.Append("SELECT DISTINCT ");
        result.Append(miaIdAttribute.GetQualifiedName(ns));
        result.Append(" C");
      }
      else
      {
        // Append plain attribute MEDIA_ITEMS.MEDIA_ITEM_ID if no GROUP BY-statement is requested
        result.Append(miaIdAttribute.GetDeclarationWithAlias(ns, out mediaItemIdOrGroupSizeAlias));

        // System attributes: Necessary to evaluate if a requested MIA is present for the media item
        foreach (KeyValuePair<MediaItemAspectMetadata, TableQueryData> kvp in miaTypeTableQueries)
        {
          result.Append(", ");
          string miamColumn = kvp.Value.GetAlias(ns) + "." + MIA_Management.MIA_MEDIA_ITEM_ID_COL_NAME;
          result.Append(miamColumn);
          string miamAlias = ns.GetOrCreate(miamColumn, "A");
          result.Append(" ");
          result.Append(miamAlias);
          if (miamAlias != null)
            miamAliases.Add(kvp.Key, miamAlias);
        }
      }

      // Selected attributes
      foreach (string selectAttr in selectAttributeDeclarations)
      {
        result.Append(", ");
        result.Append(selectAttr);
      }

      string whereStr = compiledFilter.CreateSqlFilterCondition(ns, requestedAttributes, out bindVars);
      foreach(BindVar bv in sqlVars)
        bindVars.Add(bv);

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
        result.Append(" WHERE ");
        result.Append(whereStr);
      }
      if (groupByValues)
      {
        result.Append(") V");
        if (qualifiedGroupByAliases.Count > 0)
        {
          result.Append(" GROUP BY ");
          result.Append(groupClause);
        }
      }
      else
      {
        if (compiledSortInformation != null && compiledSortInformation.Count > 0)
        {
          IEnumerable<string> sortCriteria = compiledSortInformation.Select(csi => csi.GetSortDeclaration(ns));
          result.Append(" ORDER BY ");
          result.Append(StringUtils.Join(", ", sortCriteria));
        }
      }
      statementStr = result.ToString();
    }

    protected bool AddAttributeSortInformation(ISortInformation sortInformation, RequestedAttribute miaIdAttribute, IDictionary<QueryAttribute, RequestedAttribute> requestedAttributes,
      IDictionary<MediaItemAspectMetadata, TableQueryData> miaTypeTableQueries, IDictionary<object, TableQueryData> tableQueries, IList<TableJoin> tableJoins, IList<CompiledSortInformation> compiledSortInformation)
    {
      AttributeSortInformation attributeSort = sortInformation as AttributeSortInformation;
      if (attributeSort != null)
      {
        if (attributeSort.AttributeType.ParentMIAM.IsTransientAspect)
          return false;
        if (attributeSort.AttributeType.ParentMIAM is MultipleMediaItemAspectMetadata)
          return false;

        MediaItemAspectMetadata.AttributeSpecification attr = attributeSort.AttributeType;
        if (attr.Cardinality != Cardinality.Inline && attr.Cardinality != Cardinality.ManyToOne)
          // Sorting can only be done for Inline and MTO attributes
          return false;

        RequestedAttribute ra;
        RequestSimpleAttribute(new QueryAttribute(attr), tableQueries, tableJoins, "LEFT OUTER JOIN", requestedAttributes,
            miaTypeTableQueries, miaIdAttribute, out ra);
        compiledSortInformation.Add(new CompiledSortInformation(ra, attributeSort.Direction));

        return true;
      }
      return false;
    }

    protected bool AddChildAggregateAttributeSortInformation(ISortInformation sortInformation, Namespace ns, BindVarNamespace bvNamespace, RequestedAttribute miaIdAttribute, IList<TableJoin> tableJoins,
      IList<CompiledSortInformation> compiledSortInformation, IList<BindVar> bindVars)
    {
      ChildAggregateAttributeSortInformation childAttributeSort = sortInformation as ChildAggregateAttributeSortInformation;
      if (childAttributeSort != null)
      {
        if (childAttributeSort.ChildAttributeType.ParentMIAM.IsTransientAspect)
          return false;

        var relation = _miaManagement.LocallyKnownRelationshipTypes.FirstOrDefault(r => (r.ChildRole == childAttributeSort.ChildRole && r.ParentRole == childAttributeSort.ParentRole));
        if (relation == null)
          return false;

        if (!_miaManagement.ManagedMediaItemAspectTypes.TryGetValue(relation.ChildAspectId, out var childMetadata))
          return false;

        BindVar roleVar = new BindVar(bvNamespace.CreateNewBindVarName("V"), childAttributeSort.ParentRole, typeof(Guid));
        bindVars.Add(roleVar);
        BindVar linkedRoleVar = new BindVar(bvNamespace.CreateNewBindVarName("V"), childAttributeSort.ChildRole, typeof(Guid));
        bindVars.Add(linkedRoleVar);

        var table = new TableQueryData($"({BuildRelationshipPart(childAttributeSort, childMetadata, false, roleVar, linkedRoleVar)} UNION {BuildRelationshipPart(childAttributeSort, childMetadata, true, roleVar, linkedRoleVar)})");
        var tableName = table.GetAlias(ns);
        tableJoins.Add(new TableJoin("INNER JOIN", table, $"{tableName}.ID", miaIdAttribute.GetQualifiedName(ns)));

        RequestedAttribute ra = new RequestedAttribute(table, "SORT");
        compiledSortInformation.Add(new CompiledSortInformation(ra, childAttributeSort.Direction));
        return true;
      }
      return false;
    }

    private string BuildRelationshipPart(ChildAggregateAttributeSortInformation childAttributeSort, MediaItemAspectMetadata childMetadata, bool reverse, BindVar roleVar, BindVar linkedRoleVar)
    {
      string selectColumn;
      string roleColumn;
      string linkedRoleColumn;
      string linkedIdColumn;
      if (reverse)
      {
        //if this is the reverse part, reverse the column names
        selectColumn = _miaManagement.GetMIAAttributeColumnName(RelationshipAspect.ATTR_LINKED_ID);
        roleColumn = _miaManagement.GetMIAAttributeColumnName(RelationshipAspect.ATTR_LINKED_ROLE);
        linkedRoleColumn = _miaManagement.GetMIAAttributeColumnName(RelationshipAspect.ATTR_ROLE);
        linkedIdColumn = MIA_Management.MIA_MEDIA_ITEM_ID_COL_NAME;
      }
      else
      {
        selectColumn = MIA_Management.MIA_MEDIA_ITEM_ID_COL_NAME;
        roleColumn = _miaManagement.GetMIAAttributeColumnName(RelationshipAspect.ATTR_ROLE);
        linkedRoleColumn = _miaManagement.GetMIAAttributeColumnName(RelationshipAspect.ATTR_LINKED_ROLE);
        linkedIdColumn = _miaManagement.GetMIAAttributeColumnName(RelationshipAspect.ATTR_LINKED_ID);
      }

      StringBuilder joinSql = new StringBuilder();
      joinSql.Append($"SELECT R.{selectColumn} ID, ");
      if (childAttributeSort.AggregateFunction == AggregateFunction.Avg)
        joinSql.Append("AVG");
      else if (childAttributeSort.AggregateFunction == AggregateFunction.Count)
        joinSql.Append("COUNT");
      else if (childAttributeSort.AggregateFunction == AggregateFunction.Max)
        joinSql.Append("MAX");
      else if (childAttributeSort.AggregateFunction == AggregateFunction.Min)
        joinSql.Append("MIN");
      else if (childAttributeSort.AggregateFunction == AggregateFunction.Sum)
        joinSql.Append("SUM");

      if (childMetadata.AspectId != childAttributeSort.ChildAttributeType.ParentMIAM.AspectId)
        joinSql.Append($"(S.{_miaManagement.GetMIAAttributeColumnName(childAttributeSort.ChildAttributeType)}) SORT");
      else
        joinSql.Append($"(C.{_miaManagement.GetMIAAttributeColumnName(childAttributeSort.ChildAttributeType)}) SORT");
      joinSql.Append(" FROM ");
      joinSql.Append(_miaManagement.GetMIATableName(childMetadata));
      joinSql.Append(" C");
      joinSql.Append(" INNER JOIN ");
      joinSql.Append(_miaManagement.GetMIATableName(RelationshipAspect.Metadata));
      joinSql.Append(" R ON R.");
      joinSql.Append(linkedIdColumn);
      joinSql.Append("=C.");
      joinSql.Append(MIA_Management.MIA_MEDIA_ITEM_ID_COL_NAME);
      joinSql.Append(" AND R.");
      joinSql.Append(linkedRoleColumn);
      joinSql.Append("=@" + linkedRoleVar.Name);
      joinSql.Append(" AND R.");
      joinSql.Append(roleColumn);
      joinSql.Append("=@" + roleVar.Name);

      //If sort property is from a different aspect we must also join the sort aspect
      if (childMetadata.AspectId != childAttributeSort.ChildAttributeType.ParentMIAM.AspectId)
      {
        joinSql.Append(" LEFT OUTER JOIN ");
        joinSql.Append(_miaManagement.GetMIATableName(childAttributeSort.ChildAttributeType.ParentMIAM));
        joinSql.Append($" S ON S.");
        joinSql.Append(MIA_Management.MIA_MEDIA_ITEM_ID_COL_NAME);
        joinSql.Append("=C.");
        joinSql.Append(MIA_Management.MIA_MEDIA_ITEM_ID_COL_NAME);
      }
      joinSql.Append($" GROUP BY R.{selectColumn}");

      return joinSql.ToString();
    }

    protected bool AddDataSortInformation(ISortInformation sortInformation, RequestedAttribute miaIdAttribute, IList<TableJoin> tableJoins, IList<CompiledSortInformation> compiledSortInformation,
      IList<BindVar> bindVars, ref BindVar userVar)
    {
      DataSortInformation dataSort = sortInformation as DataSortInformation;
      if (dataSort != null && _userProfileId.HasValue)
      {
        TableQueryData tqd = new TableQueryData(UserProfileDataManagement.UserProfileDataManagement_SubSchema.USER_MEDIA_ITEM_DATA_TABLE_NAME);
        RequestedAttribute ra = new RequestedAttribute(tqd, UserProfileDataManagement.UserProfileDataManagement_SubSchema.USER_DATA_VALUE_COL_NAME);
        compiledSortInformation.Add(new CompiledSortInformation(ra, dataSort.Direction));

        if (userVar == null)
        {
          userVar = new BindVar("UID", _userProfileId.Value, typeof(Guid));
          bindVars.Add(userVar);
        }
        TableJoin join = new TableJoin("LEFT OUTER JOIN", tqd, new RequestedAttribute(tqd, UserProfileDataManagement.UserProfileDataManagement_SubSchema.USER_PROFILE_ID_COL_NAME), "@" + userVar.Name);
        join.AddCondition(new RequestedAttribute(tqd, MIA_Management.MIA_MEDIA_ITEM_ID_COL_NAME), miaIdAttribute);
        join.AddCondition(new RequestedAttribute(tqd, UserProfileDataManagement.UserProfileDataManagement_SubSchema.USER_DATA_KEY_COL_NAME), $"'{dataSort.UserDataKey}'");
        tableJoins.Add(join);

        return true;
      }
      return false;
    }

    protected virtual CompiledFilter CreateCompiledFilter(Namespace ns, BindVarNamespace bvNamespace, string outerMIIDJoinVariable, IList<TableJoin> tableJoins)
    {
      return new CompiledFilter(_miaManagement, _filter, _subqueryFilter, ns, bvNamespace, outerMIIDJoinVariable, tableJoins);
    }

    /// <summary>
    /// Generates the SQL statement for the underlaying query specification.
    /// </summary>
    /// <param name="mediaItemIdAlias">Alias of the media item's IDs in the result set.</param>
    /// <param name="miamAliases">Returns the aliases of the ID columns of the joined media item aspect tables. With this mapping,
    /// the caller can check if a MIA type was requested or not. That is needed for optional requested MIA types.</param>
    /// <param name="attributeAliases">Returns the aliases for all selected attributes.</param>
    /// <param name="statementStr">SQL statement which was built by this method.</param>
    /// <param name="bindVars">Bind variables to be inserted into the returned <paramref name="statementStr"/>.</param>
    public void GenerateSqlStatement(out string mediaItemIdAlias,
        out IDictionary<MediaItemAspectMetadata, string> miamAliases,
        out IDictionary<QueryAttribute, string> attributeAliases,
        out string statementStr, out IList<BindVar> bindVars)
    {
      miamAliases = new Dictionary<MediaItemAspectMetadata, string>();
      GenerateSqlStatement(false, miamAliases, out mediaItemIdAlias, out attributeAliases, out statementStr, out bindVars);
    }

    public override string ToString()
    {
      string mediaItemIdAlias2;
      IDictionary<MediaItemAspectMetadata, string> miamAliases;
      IDictionary<QueryAttribute, string> qa2a;
      string statementStr;
      IList<BindVar> bindVars;
      GenerateSqlStatement(out mediaItemIdAlias2, out miamAliases, out qa2a, out statementStr, out bindVars);
      return statementStr;
    }
  }
}

