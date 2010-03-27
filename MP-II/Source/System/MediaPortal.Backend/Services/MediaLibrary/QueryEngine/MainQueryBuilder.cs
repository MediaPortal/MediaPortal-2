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
using System.Text;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.MediaManagement.MLQueries;
using MediaPortal.Utilities;

namespace MediaPortal.Backend.Services.MediaLibrary.QueryEngine
{
  /// <summary>
  /// Builds the SQL statement for the main media item query. The main query requests all Inline and MTO attributes of
  /// media item aspects, filtered by a <see cref="CompiledFilter"/>.
  /// </summary>
  public class MainQueryBuilder : BaseQueryBuilder
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

    protected readonly ICollection<MediaItemAspectMetadata> _necessaryRequestedMIAs;

    /// <summary>
    /// Attributes which are selected the in main query (=which are requested to be returned, in contrast to those
    /// attributes used in a filter).
    /// </summary>
    protected readonly IList<QueryAttribute> _selectAttributes;
    protected readonly CompiledFilter _filter;
    protected readonly IList<SortInformation> _sortInformation;

    /// <summary>
    /// Creates a new <see cref="MainQueryBuilder"/> instance.
    /// </summary>
    /// <param name="miaManagement">MIAM management instance from media library.</param>
    /// <param name="necessaryRequestedMIAs">MIAs which must be present for the media item to match the query.</param>
    /// <param name="simpleSelectAttributes">Enumeration of media item aspect attributes, given as
    /// <see cref="QueryAttribute"/> instances, which should be selected by this main query. Only attributes with
    /// cardinalities of <see cref="Cardinality.Inline"/> and <see cref="Cardinality.ManyToOne"/> are allowed here.
    /// Both necessary and optional attributes are contained in this enumeration.</param>
    /// <param name="filter">Filter to restrict the result set.</param>
    /// <param name="sortInformation">List of sorting criteria.</param>
    public MainQueryBuilder(MIA_Management miaManagement,
        ICollection<MediaItemAspectMetadata> necessaryRequestedMIAs,
        IEnumerable<QueryAttribute> simpleSelectAttributes, CompiledFilter filter,
        IList<SortInformation> sortInformation) : base(miaManagement)
    {
      _necessaryRequestedMIAs = necessaryRequestedMIAs;
      _selectAttributes = new List<QueryAttribute>(simpleSelectAttributes);
      _filter = filter;
      _sortInformation = sortInformation;
    }

    protected void GenerateSqlStatement(Namespace ns, bool groupByValues,
        IDictionary<MediaItemAspectMetadata, string> miamAliases,
        out string mediaItemIdOrGroupSizeAlias,
        out IDictionary<QueryAttribute, string> attributeAliases,
        out string statementStr, out IList<object> values)
    {
      // Contains a mapping of each queried (=selected or filtered) attribute to its request attribute instance
      // data (which holds its requested query table instance)
      IDictionary<QueryAttribute, RequestedAttribute> requestedAttributes = new Dictionary<QueryAttribute, RequestedAttribute>();
      attributeAliases = new Dictionary<QueryAttribute, string>();

      // Contains a list of compiled select attribute declarations. We need this in a separate list (in contrast to using
      // the selectAttributes list together with the compiledAttributes map) because it might be the case that
      // an attribute is requested twice. In that rare case, we need a new alias name for it.
      IList<string> selectAttributeDeclarations = new List<string>();

      // Dictionary containing as key the requested MIAM instance OR attribute specification of cardinality MTO,
      // mapped to the table query data to request its contents.
      IDictionary<object, TableQueryData> tableQueries =
          new Dictionary<object, TableQueryData>();

      // Contains the same tables as the tableQueries variable, but in order and enriched with table join data
      IList<TableJoin> tableJoins = new List<TableJoin>();

      // Contains all table query data for MIA type tables
      IDictionary<MediaItemAspectMetadata, TableQueryData> miaTypeTableQueries = new Dictionary<MediaItemAspectMetadata, TableQueryData>();

      // First create the request table query data for the MIA main table and the request attribute for the MIA ID.
      // We'll need the requested attribute as join attribute soon.
      TableQueryData miaTableQuery = new TableQueryData(MediaLibrary_SubSchema.MEDIA_ITEMS_TABLE_NAME);
      RequestedAttribute miaIdAttribute = new RequestedAttribute(miaTableQuery, MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME);

      // Contains CompiledSortInformation instances for each sort information instance
      IList<CompiledSortInformation> compiledSortInformation = null;

      // Ensure that the tables for all necessary MIAs are requested first (INNER JOIN)
      foreach (MediaItemAspectMetadata miaType in _necessaryRequestedMIAs)
      {
        TableQueryData tqd;
        if (!tableQueries.TryGetValue(miaType, out tqd))
        {
          tqd = tableQueries[miaType] = TableQueryData.CreateTableQueryOfMIATable(_miaManagement, miaType);
          miaTypeTableQueries.Add(miaType, tqd);
        }
        tableJoins.Add(new TableJoin("INNER JOIN", tqd,
            new RequestedAttribute(tqd, MIA_Management.MIA_MEDIA_ITEM_ID_COL_NAME), miaIdAttribute));
      }

      // Build table query data for each selected Inline and MTO attribute
      // + select query attribute
      // + add alias to selectAttributeDeclarations
      foreach (QueryAttribute attr in _selectAttributes)
      {
        RequestedAttribute ra;
        RequestSimpleAttribute(attr, tableQueries, tableJoins, "LEFT OUTER JOIN", requestedAttributes, miaTypeTableQueries,
            miaIdAttribute, out ra);
        string alias;
        selectAttributeDeclarations.Add(ra.GetDeclarationWithAlias(ns, out alias));
        attributeAliases.Add(attr, alias);
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
            miaTypeTableQueries, miaIdAttribute, out ra);
      }
      // Build table query data for each sort attribute
      if (_sortInformation != null)
      {
        compiledSortInformation = new List<CompiledSortInformation>();
        foreach (SortInformation sortInformation in _sortInformation)
        {
          MediaItemAspectMetadata.AttributeSpecification attr = sortInformation.AttributeType;
          if (attr.Cardinality != Cardinality.Inline && attr.Cardinality != Cardinality.ManyToOne)
            // Sorting can only be done for Inline and MTO attributes
            continue;
          RequestedAttribute ra;
          RequestSimpleAttribute(new QueryAttribute(attr), tableQueries, tableJoins, "LEFT OUTER JOIN", requestedAttributes,
              miaTypeTableQueries, miaIdAttribute, out ra);
          compiledSortInformation.Add(new CompiledSortInformation(ra, sortInformation.Direction));
        }
      }
      StringBuilder result = new StringBuilder("SELECT ");

      if (groupByValues)
      {
        // Append a COUNT expression for the MEDIA_ITEMS.MEDIA_ITEM_ID for the GROUP BY-statement
        string countAttribute = "COUNT(" + miaIdAttribute.GetQualifiedName(ns) + ")";
        result.Append(countAttribute);
        result.Append(" ");
        mediaItemIdOrGroupSizeAlias = ns.GetOrCreate(countAttribute, "C");
        result.Append(mediaItemIdOrGroupSizeAlias);
        result.Append(", ");
      }
      else
      {
        // Append plain attribute MEDIA_ITEMS.MEDIA_ITEM_ID if no GROUP BY-statement is requested
        result.Append(miaIdAttribute.GetDeclarationWithAlias(ns, out mediaItemIdOrGroupSizeAlias));

        // System attributes: Necessary to evaluate if a requested MIA is present for the media item
        foreach (KeyValuePair<MediaItemAspectMetadata, TableQueryData> kvp in miaTypeTableQueries)
        {
          result.Append(",");
          string miamColumn = kvp.Value.GetAlias(ns) + "." + MIA_Management.MIA_MEDIA_ITEM_ID_COL_NAME;
          result.Append(miamColumn);
          string miamAlias = ns.GetOrCreate(miamColumn, "A");
          result.Append(" ");
          result.Append(miamAlias);
          if (miamAlias != null)
            miamAliases.Add(kvp.Key, miamAlias);
        }

        result.Append(", ");
      }

      // Selected attributes
      result.Append(StringUtils.Join(", ", selectAttributeDeclarations));

      result.Append(" FROM ");
      // Always request the MEDIA_ITEMS table because if no necessary aspects are given and the optional aspects aren't
      // present, we could miss the ID for requested media items
      result.Append(miaTableQuery.GetDeclarationWithAlias(ns));
      result.Append(' ');

      // Other joined tables
      foreach (TableJoin tableJoin in tableJoins)
      {
        result.Append(tableJoin.GetJoinDeclaration(ns));
        result.Append(' ');
      }

      string whereStr;
      _filter.CreateSqlFilterCondition(ns, requestedAttributes, miaIdAttribute.GetQualifiedName(ns), out whereStr, out values);
      if (!string.IsNullOrEmpty(whereStr))
      {
        result.Append("WHERE ");
        result.Append(whereStr);
      }
      if (groupByValues)
      {
        result.Append("GROUP BY ");
        result.Append(StringUtils.Join(", ", attributeAliases.Values));
      }
      else
      {
        if (compiledSortInformation != null && compiledSortInformation.Count > 0)
        {
          IList<string> sortCriteria = new List<string>();
          foreach (CompiledSortInformation csi in compiledSortInformation)
            sortCriteria.Add(csi.GetSortDeclaration(ns));
          result.Append("ORDER BY ");
          result.Append(StringUtils.Join(", ", sortCriteria));
        }
      }

      statementStr = result.ToString();
    }

    /// <summary>
    /// Generates an SQL statement for the underlaying query specification which contains groups of the same attribute
    /// values and a count column containing the size of each group.
    /// </summary>
    /// <param name="ns">Namespace used to generate the SQL statement. If the generated statement should be used
    /// inside another statement, the use of a common namespace prevents name collisions.</param>
    /// <param name="groupSizeAlias">Alias of the groups sizes in the result set.</param>
    /// <param name="attributeAliases">Returns the aliases for all selected attributes.</param>
    /// <param name="statementStr">SQL statement which was built by this method.</param>
    /// <param name="values">Values to be inserted into the returned <paramref name="statementStr"/>.</param>
    public void GenerateSqlGroupByStatement(Namespace ns, out string groupSizeAlias,
        out IDictionary<QueryAttribute, string> attributeAliases,
        out string statementStr, out IList<object> values)
    {
      GenerateSqlStatement(ns, true, null, out groupSizeAlias, out attributeAliases, out statementStr, out values);
    }

    /// <summary>
    /// Generates the SQL statement for the underlaying query specification.
    /// </summary>
    /// <param name="ns">Namespace used to generate the SQL statement. If the generated statement should be used
    /// inside another statement, the use of a common namespace prevents name collisions.</param>
    /// <param name="mediaItemIdAlias">Alias of the media item's IDs in the result set.</param>
    /// <param name="miamAliases">Returns the aliases of the ID columns of the joined media item aspect tables. With this mapping,
    /// the caller can check if a MIA type was requested or not. That is needed for optional requested MIA types.</param>
    /// <param name="attributeAliases">Returns the aliases for all selected attributes.</param>
    /// <param name="statementStr">SQL statement which was built by this method.</param>
    /// <param name="values">Values to be inserted into the returned <paramref name="statementStr"/>.</param>
    public void GenerateSqlStatement(Namespace ns, out string mediaItemIdAlias,
        out IDictionary<MediaItemAspectMetadata, string> miamAliases,
        out IDictionary<QueryAttribute, string> attributeAliases,
        out string statementStr, out IList<object> values)
    {
      miamAliases = new Dictionary<MediaItemAspectMetadata, string>();
      GenerateSqlStatement(ns, false, miamAliases, out mediaItemIdAlias, out attributeAliases, out statementStr, out values);
    }

    public override string ToString()
    {
      string mediaItemIdAlias2;
      IDictionary<MediaItemAspectMetadata, string> miamAliases;
      Namespace mainQueryNS = new Namespace();
      IDictionary<QueryAttribute, string> qa2a;
      string statementStr;
      IList<object> values;
      GenerateSqlStatement(mainQueryNS, out mediaItemIdAlias2, out miamAliases, out qa2a, out statementStr, out values);
      return statementStr;
    }
  }
}
