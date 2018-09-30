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
using MediaPortal.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MediaPortal.Backend.Services.MediaLibrary.QueryEngine
{
    public class MIAQueryBuilder : MainQueryBuilder
    {
        public MIAQueryBuilder(MIA_Management miaManagement, IEnumerable<QueryAttribute> simpleSelectAttributes,
            SelectProjectionFunction selectProjectionFunction,
            IEnumerable<MediaItemAspectMetadata> necessaryRequestedMIAs, IEnumerable<MediaItemAspectMetadata> optionalRequestedMIAs,
            IFilter filter, IFilter subqueryFilter, IList<ISortInformation> sortInformation, Guid? userProfileId = null) : base(miaManagement, simpleSelectAttributes,
            selectProjectionFunction,
            necessaryRequestedMIAs, optionalRequestedMIAs,
            filter, subqueryFilter, sortInformation, userProfileId)
        {
        }

    /// <summary>
    /// Generates an SQL statement for the underlaying query specification which contains groups of the same attribute
    /// values and a count column containing the size of each group.
    /// </summary>
    /// <param name="groupSizeAlias">Alias of the groups sizes in the result set.</param>
    /// <param name="attributeAliases">Returns the aliases for all selected attributes.</param>
    /// <param name="statementStr">SQL statement which was built by this method.</param>
    /// <param name="bindVars">Bind variables to be inserted into the returned <paramref name="statementStr"/>.</param>
    public void GenerateSqlGroupByStatement(out string groupSizeAlias, out IDictionary<QueryAttribute, string> attributeAliases,
        out string statementStr, out IList<BindVar> bindVars)
    {
      GenerateSqlStatement(true, null, out groupSizeAlias, out attributeAliases, out statementStr, out bindVars);
    }

    public void GenerateSingleMIASqlGroupByStatement(out string groupSizeAlias, out IDictionary<QueryAttribute, string> attributeAliases,
        out string statementStr, out IList<BindVar> bindVars)
    {
      GenerateSingleMIASqlStatement(true, null, out groupSizeAlias, out attributeAliases, out statementStr, out bindVars);
    }

    public void GenerateSingleMIASqlStatement(out string mediaItemIdAlias,
        out IDictionary<MediaItemAspectMetadata, string> miamAliases,
        out IDictionary<QueryAttribute, string> attributeAliases,
        out string statementStr, out IList<BindVar> bindVars)
    {
      miamAliases = new Dictionary<MediaItemAspectMetadata, string>();
      GenerateSingleMIASqlStatement(false, miamAliases, out mediaItemIdAlias, out attributeAliases, out statementStr, out bindVars);
    }

    protected void GenerateSingleMIASqlStatement(bool groupByValues,
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

      //Used to check whether a required MulpleMIA exists
      IList<TableQueryData> miaExistsTables = new List<TableQueryData>();

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
        if (miaType is MultipleMediaItemAspectMetadata)
        {
          miaExistsTables.Add(TableQueryData.CreateTableQueryOfMIATable(_miaManagement, miaType));
          continue;
        }

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
        if (miaType is MultipleMediaItemAspectMetadata)
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
        //Multiple MIAs will be handled later
        if (attr.Attr.ParentMIAM is MultipleMediaItemAspectMetadata)
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

      string subSql;
      GenerateSubSelectSqlStatement(bvNamespace, out subSql, out bindVars);

      // Build table query data for each sort attribute
      if (_sortInformation != null)
      {
        compiledSortInformation = new List<CompiledSortInformation>();
        BindVar userVar = null;
        foreach (ISortInformation sortInformation in _sortInformation)
        {
          AttributeSortInformation attributeSort = sortInformation as AttributeSortInformation;
          if (attributeSort != null)
          {
            if (attributeSort.AttributeType.ParentMIAM.IsTransientAspect)
              continue;
            if (attributeSort.AttributeType.ParentMIAM is MultipleMediaItemAspectMetadata)
              continue;

            MediaItemAspectMetadata.AttributeSpecification attr = attributeSort.AttributeType;
            if (attr.Cardinality != Cardinality.Inline && attr.Cardinality != Cardinality.ManyToOne)
              // Sorting can only be done for Inline and MTO attributes
              continue;

            RequestedAttribute ra;
            RequestSimpleAttribute(new QueryAttribute(attr), tableQueries, tableJoins, "LEFT OUTER JOIN", requestedAttributes,
                miaTypeTableQueries, miaIdAttribute, out ra);
            compiledSortInformation.Add(new CompiledSortInformation(ra, attributeSort.Direction));
            continue;
          }

          DataSortInformation dataSort = sortInformation as DataSortInformation;
          if (dataSort != null && _userProfileId.HasValue)
          {
            TableQueryData tqd = new TableQueryData(UserProfileDataManagement.UserProfileDataManagement_SubSchema.USER_MEDIA_ITEM_DATA_TABLE_NAME);
            RequestedAttribute ra = new RequestedAttribute(tqd, UserProfileDataManagement.UserProfileDataManagement_SubSchema.USER_DATA_VALUE_COL_NAME);
            compiledSortInformation.Add(new CompiledSortInformation(ra, dataSort.Direction));

            if (userVar == null)
            {
              userVar = new BindVar("UID", _userProfileId.Value, typeof(Guid));
              sqlVars.Add(userVar);
            }
            TableJoin join = new TableJoin("LEFT OUTER JOIN", tqd, new RequestedAttribute(tqd, UserProfileDataManagement.UserProfileDataManagement_SubSchema.USER_PROFILE_ID_COL_NAME), "@" + userVar.Name);
            join.AddCondition(new RequestedAttribute(tqd, MIA_Management.MIA_MEDIA_ITEM_ID_COL_NAME), miaIdAttribute);
            join.AddCondition(new RequestedAttribute(tqd, UserProfileDataManagement.UserProfileDataManagement_SubSchema.USER_DATA_KEY_COL_NAME), $"'{dataSort.UserDataKey}'");
            tableJoins.Add(join);
          }
        }
      }
      foreach (BindVar bv in sqlVars)
        bindVars.Add(bv);

      //Build sql statement
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

      bool whereAdded = false;
      if (!string.IsNullOrEmpty(subSql))
      {
        result.Append(" WHERE ");
        result.Append(miaIdAttribute.GetQualifiedName(ns));
        result.Append($" IN({subSql}) ");
        whereAdded = true;
      }

      if(miaExistsTables.Count > 0)
      {
        foreach (TableQueryData tqd in miaExistsTables)
        {
          if (!whereAdded)
            result.Append(" WHERE ");
          else
            result.Append(" AND ");
          whereAdded = true;
          result.Append("EXISTS(SELECT 1 FROM ");
          result.Append(tqd.TableName);
          result.Append(" WHERE ");
          result.Append(MIA_Management.MIA_MEDIA_ITEM_ID_COL_NAME);
          result.Append("=");
          result.Append(miaIdAttribute.GetQualifiedName(ns));
          result.Append(")");
        }
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

    protected void GenerateSubSelectSqlStatement(BindVarNamespace bvNamespace,
        out string statementStr, out IList<BindVar> bindVars)
    {
      Namespace ns = new Namespace();
      IDictionary<MediaItemAspectMetadata, string> miamAliases = new Dictionary<MediaItemAspectMetadata, string>();

      // Contains a mapping of each queried (=selected or filtered) attribute to its request attribute instance
      // data (which holds its requested query table instance)
      IDictionary<QueryAttribute, RequestedAttribute> requestedAttributes = new Dictionary<QueryAttribute, RequestedAttribute>();

      // Dictionary containing as key the requested MIAM instance OR attribute specification of cardinality MTO,
      // mapped to the table query data to request its contents.
      IDictionary<object, TableQueryData> tableQueries = new Dictionary<object, TableQueryData>();

      // Contains the same tables as the tableQueries variable, but in order and enriched with table join data
      IList<TableJoin> tableJoins = new List<TableJoin>();

      // Contains all table query data for MIA type tables
      IDictionary<MediaItemAspectMetadata, TableQueryData> miaTypeTableQueries = new Dictionary<MediaItemAspectMetadata, TableQueryData>();

      RequestedAttribute miaIdAttribute = null; // Lazy initialized below

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
      
      StringBuilder result = new StringBuilder("SELECT ");

      result.Append(miaIdAttribute.GetQualifiedName(ns));

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
        result.Append(" WHERE ");
        result.Append(whereStr);
      }

      statementStr = result.ToString();
    }
  }
}
