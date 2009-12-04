#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
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
using MediaPortal.Core.MediaManagement.MLQueries;
using MediaPortal.Utilities;

namespace MediaPortal.Backend.Services.MediaLibrary.QueryEngine
{
  /// <summary>
  /// Builds the SQL statement for the main media item query. The main query requests all inline attributes of
  /// media item aspects, filtered by a <see cref="CompiledFilter"/>.
  /// </summary>
  public class MainQueryBuilder
  {
    protected readonly MIA_Management _miaManagement;

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
    /// <see cref="QueryAttribute"/> instances, which should be requested in this main query. Only attributes with
    /// a cardinality of <see cref="Cardinality.Inline"/> are allowed here.</param>
    /// <param name="filter">Filter to restrict the result set.</param>
    /// <param name="sortInformation">List of sorting criteria.</param>
    public MainQueryBuilder(MIA_Management miaManagement,
        ICollection<MediaItemAspectMetadata> necessaryRequestedMIAs,
        IEnumerable<QueryAttribute> simpleSelectAttributes, CompiledFilter filter,
        IList<SortInformation> sortInformation)
    {
      _miaManagement = miaManagement;
      _necessaryRequestedMIAs = necessaryRequestedMIAs;
      _selectAttributes = new List<QueryAttribute>(simpleSelectAttributes);
      _filter = filter;
      _sortInformation = sortInformation;
    }

    public ICollection<QueryAttribute> SelectAttributes
    {
      get { return _selectAttributes; }
    }

    public CompiledFilter Filter
    {
      get { return _filter; }
    }

    public ICollection<SortInformation> SortInformation
    {
      get { return _sortInformation; }
    }

    /// <summary>
    /// Generates the SQL statement for this query specification.
    /// </summary>
    /// <remarks>
    /// In case of <c><paramref name="distinctValue"/> == true</c>, A query of the form
    /// <code>
    ///   TODO
    /// </code>
    /// will be created. In case of <c><paramref name="distinctValue"/> == false</c>, the query will look like this:
    /// <code>
    ///   TODO
    /// </code>
    /// </remarks>
    /// <param name="ns">Namespace used to generate the SQL statement. If the generated statement should be used
    /// inside another statement, the use of a common namespace prevents name collisions.</param>
    /// <param name="distinctValue">If set to <c>true</c>, this method will create a DISTINCT result set for the requested
    /// attributes. In that case, the media item ID won't be requested and thus the parameter <paramref name="mediaItemIdAlias"/>
    /// won't have a meaningful result value. If <paramref name="distinctValue"/> is set to <c>false</c>, the query won't have
    /// a DISTINCT modifier and the result set will contain also the media item ID.</param>
    /// <param name="mediaItemIdAlias">Alias of the media item's IDs in the result set. If <paramref name="distinctValue"/>
    /// is set to <c>true</c>, the result set won't contain the media item ID and thus this parmeter is meaningless in that
    /// case.</param>
    /// <param name="miamAliases">Returns the aliases of the ID columns of the joined media item aspect tables. With this mapping,
    /// the caller can check if a MIA type was requested or not. That is needed for optional requested MIA types.</param>
    /// <param name="compiledAttributes">Returns the query descriptors for all requested attributes which can be used to
    /// determine the attribute's aliases.</param>
    /// <returns></returns>
    public string GenerateSqlStatement(Namespace ns, bool distinctValue,
        out string mediaItemIdAlias,
        out IDictionary<MediaItemAspectMetadata, string> miamAliases,
        out IDictionary<QueryAttribute, CompiledQueryAttribute> compiledAttributes)
    {
      // Contains a mapping of each queried (=selected or filtered) attribute to its compiled
      // data (with query table and attribute alias)
      compiledAttributes = new Dictionary<QueryAttribute, CompiledQueryAttribute>();

      // Contains a list of compiled select attribute declarations. We need this in a separate list (in contrast to using
      // the selectAttributes list together with the compiledAttributes map) because it might be the case that
      // an attribute is requested twice. In that rare case, we need a new alias name for it.
      IList<string> selectAttributeDeclarations = new List<string>();

      // Contains a dictionary of requested MIAM instances mapped to the table query data to request its contents.
      IDictionary<MediaItemAspectMetadata, TableQueryData> tableQueries =
          new Dictionary<MediaItemAspectMetadata, TableQueryData>();

      // Build table query data for each selected inline attribute
      // + compile query attribute to be able to produce an alias
      // + add alias to selectAttributeDeclarations
      foreach (QueryAttribute attr in _selectAttributes)
      {
        if (compiledAttributes.ContainsKey(attr)) // Attribute is requested again
          // Until now, we only have processed selected attributes - this means the attribute must have been
          // selected before, so we don't need to add it again to selectAttributeDeclarations
          continue;
        // Find query table, if already present, or create new one
        TableQueryData tqd;
        MediaItemAspectMetadata miam = attr.Attr.ParentMIAM;
        if (!tableQueries.TryGetValue(miam, out tqd))
          tqd = tableQueries[miam] = new TableQueryData(_miaManagement, miam);
        CompiledQueryAttribute cqa = new CompiledQueryAttribute(_miaManagement, attr, tqd);
        compiledAttributes.Add(attr, cqa);
        selectAttributeDeclarations.Add(cqa.GetDeclarationWithAlias(ns));
      }
      // Build table query data for each inline attribute which is part of a filter
      // + compile query attribute to be able to produce an alias
      foreach (QueryAttribute attr in _filter.FilterAttributes)
      {
        if (compiledAttributes.ContainsKey(attr))
          continue;
        if (attr.Attr.Cardinality == Cardinality.Inline)
        { // Tables of inline attributes, which are part of a filter, are joined with main table
          TableQueryData tqd;
          MediaItemAspectMetadata miam = attr.Attr.ParentMIAM;
          if (!tableQueries.TryGetValue(miam, out tqd))
            tqd = tableQueries[miam] = new TableQueryData(_miaManagement, miam);
          CompiledQueryAttribute cqa = new CompiledQueryAttribute(_miaManagement, attr, tqd);
          compiledAttributes.Add(attr, cqa);
        }
      }
      // Build table query data for each sort attribute
      if (_sortInformation != null)
        foreach (SortInformation sortInformation in _sortInformation)
        {
          MediaItemAspectMetadata.AttributeSpecification attr = sortInformation.AttributeType;
          if (attr.Cardinality == Cardinality.Inline)
          { // Sorting can only be done for inline attributes
            TableQueryData tqd;
            MediaItemAspectMetadata miam = attr.ParentMIAM;
            if (!tableQueries.TryGetValue(miam, out tqd))
              tableQueries[miam] = new TableQueryData(_miaManagement, miam);
          }
        }
      string mediaItemsTableAlias = ns.GetOrCreate(MediaLibrary_SubSchema.MEDIA_ITEMS_TABLE_NAME, "T");
      StringBuilder result = new StringBuilder("SELECT ");

      if (distinctValue)
      {
        mediaItemIdAlias = null;
        result.Append("DISTINCT ");
      }
      else
      {
        mediaItemIdAlias = ns.GetOrCreate(MIA_Management.MIA_MEDIA_ITEM_ID_COL_NAME, "A");

        // Append requested attribute MEDIA_ITEMS.MEDIA_ITEM_ID only if no DISTINCT query is made
        result.Append(mediaItemsTableAlias);
        result.Append(".");
        result.Append(MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME);
        result.Append(" ");
        result.Append(mediaItemIdAlias);
        result.Append(",");
      }

      IList<string> selectAttrStrs = new List<string>();
      // Requested attributes
      foreach (QueryAttribute attr in _selectAttributes)
        selectAttrStrs.Add(compiledAttributes[attr].GetDeclarationWithAlias(ns));
      result.Append(StringUtils.Join(",", selectAttrStrs));

      miamAliases = new Dictionary<MediaItemAspectMetadata, string>();
      // System attributes: Necessary to evaluate if a requested MIA is present for the media item
      foreach (TableQueryData tqd in tableQueries.Values)
      {
        result.Append(",");
        string miamColumn = tqd.GetAlias(ns) + "." + MIA_Management.MIA_MEDIA_ITEM_ID_COL_NAME;
        result.Append(miamColumn);
        string miamAlias = ns.GetOrCreate(miamColumn, "A");
        result.Append(" ");
        result.Append(miamAlias);
        miamAliases.Add(tqd.MIAM, miamAlias);
      }

      result.Append(" FROM ");
      // Always request the MEDIA_ITEMS table because not all aspects might be available for each requested media item
      result.Append(MediaLibrary_SubSchema.MEDIA_ITEMS_TABLE_NAME);
      result.Append(" ");
      result.Append(mediaItemsTableAlias);
      // Prepare sequence of table requests
      IList<TableQueryData> tableList = new List<TableQueryData>(tableQueries.Count);
      // First request tables of necessary requested MIAs with INNER JOINs
      foreach (MediaItemAspectMetadata miam in _necessaryRequestedMIAs)
      {
        TableQueryData tqd;
        if (!tableQueries.TryGetValue(miam, out tqd))
          tableQueries[miam] = new TableQueryData(_miaManagement, miam);
        tableList.Add(tqd);
      }
      // after that, add other tables with OUTER JOINs
      foreach (TableQueryData tqd in tableQueries.Values)
        if (!_necessaryRequestedMIAs.Contains(tqd.MIAM))
          tableList.Add(tqd);
      foreach (TableQueryData tqd in tableList)
      {
        if (_necessaryRequestedMIAs.Contains(tqd.MIAM))
          result.Append(" INNER JOIN ");
        else
          result.Append(" LEFT OUTER JOIN ");
        result.Append(tqd.GetDeclarationWithAlias(ns));
        result.Append(" ON ");
        result.Append(mediaItemsTableAlias);
        result.Append(".");
        result.Append(MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME);
        result.Append(" = ");
        result.Append(tqd.GetAlias(ns));
        result.Append(".");
        result.Append(MIA_Management.MIA_MEDIA_ITEM_ID_COL_NAME);
      }
      result.Append(" WHERE ");
      result.Append(_filter.CreateSqlFilterCondition(ns, compiledAttributes,
          mediaItemsTableAlias + "." + MediaLibrary_SubSchema.MEDIA_ITEMS_ITEM_ID_COL_NAME));
      if (_sortInformation != null)
      {
        IList<string> sortCriteria = new List<string>();
        foreach (SortInformation sortInformation in _sortInformation)
        {
          MediaItemAspectMetadata.AttributeSpecification attr = sortInformation.AttributeType;
          if (attr.Cardinality == Cardinality.Inline)
          { // Sorting can only be done for inline attributes
            MediaItemAspectMetadata miam = attr.ParentMIAM;
            TableQueryData tqd = tableQueries[miam];
            sortCriteria.Add(tqd.GetAlias(ns) + "." + _miaManagement.GetMIAAttributeColumnName(attr));
          }
        }
        result.Append(" ORDER BY ");
        result.Append(StringUtils.Join(", ", sortCriteria));
      }
      return result.ToString();
    }

    public override string ToString()
    {
      string mediaItemIdAlias2;
      IDictionary<MediaItemAspectMetadata, string> miamAliases;
      Namespace mainQueryNS = new Namespace();
      IDictionary<QueryAttribute, CompiledQueryAttribute> qa2cqa;
      return GenerateSqlStatement(mainQueryNS, false, out mediaItemIdAlias2, out miamAliases, out qa2cqa);
    }
  }
}
