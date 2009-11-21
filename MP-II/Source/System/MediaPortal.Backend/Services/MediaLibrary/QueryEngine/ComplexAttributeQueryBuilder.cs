#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
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

namespace MediaPortal.Services.MediaLibrary.QueryEngine
{
  /// <summary>
  /// Builds the SQL statement for a complex media item aspect attribute query, filtered by a <see cref="CompiledFilter"/>.
  /// Complex attributes are all attributes which have a cardinality different from <see cref="Cardinality.Inline"/>.
  /// </summary>
  public class ComplexAttributeQueryBuilder
  {
    protected readonly MIA_Management _miaManagement;
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
        ICollection<MediaItemAspectMetadata> necessaryRequestedMIAs, CompiledFilter filter)
    {
      _miaManagement = miaManagement;
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
    public string GenerateSqlStatement(Namespace ns, bool distinctValue,
        out string mediaItemIdAlias, out string valueAlias)
    {
      // Contains a mapping of each queried (=selected or filtered) attribute to its compiled
      // data (with query table and attribute alias)
      IDictionary<QueryAttribute, CompiledQueryAttribute> compiledAttributes =
          new Dictionary<QueryAttribute, CompiledQueryAttribute>();

      // Contains a dictionary of requested MIAM instances mapped to the table query data to request its contents.
      IDictionary<MediaItemAspectMetadata, TableQueryData> tableQueries = new Dictionary<MediaItemAspectMetadata, TableQueryData>();

      // Build table query data for each inline attribute which is part of a filter
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
          compiledAttributes.Add(attr, new CompiledQueryAttribute(_miaManagement, attr, tqd));
        }
      }
      string queryAttributeTableName = _miaManagement.GetMIACollectionAttributeTableName(_queryAttribute);
      string queryAttributeTableAlias = ns.GetOrCreate(queryAttributeTableName, "T");
      valueAlias = ns.GetOrCreate(MIA_Management.COLL_MIA_VALUE_COL_NAME, "A");

      StringBuilder result = new StringBuilder("SELECT ");

      if (distinctValue)
      {
        mediaItemIdAlias = null;
        result.Append("DISTINCT ");
      }
      else
      {
        mediaItemIdAlias = ns.GetOrCreate(MIA_Management.MIA_MEDIA_ITEM_ID_COL_NAME, "A");

        // Request media item id only if no DISTINCT query is made
        result.Append(queryAttributeTableAlias);
        result.Append(".");
        result.Append(MIA_Management.MIA_MEDIA_ITEM_ID_COL_NAME);
        result.Append(" ");
        result.Append(mediaItemIdAlias);
        result.Append(", ");
      }

      // Value column of extern attribute table
      result.Append(queryAttributeTableAlias);
      result.Append(".");
      result.Append(MIA_Management.COLL_MIA_VALUE_COL_NAME);
      result.Append(" ");
      result.Append(valueAlias);

      result.Append(" FROM ");
      // Always request the extern attribute table first
      result.Append(queryAttributeTableName);
      result.Append(" ");
      result.Append(queryAttributeTableAlias);

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
        result.Append(queryAttributeTableAlias);
        result.Append(".");
        result.Append(MIA_Management.MIA_MEDIA_ITEM_ID_COL_NAME);
        result.Append(" = ");
        result.Append(tqd.GetAlias(ns));
        result.Append(".");
        result.Append(MIA_Management.MIA_MEDIA_ITEM_ID_COL_NAME);
      }
      result.Append(" WHERE ");
      result.Append(_filter.CreateSqlFilterCondition(ns, compiledAttributes,
          queryAttributeTableAlias + "." + MIA_Management.MIA_MEDIA_ITEM_ID_COL_NAME));
      return result.ToString();
    }

    public override string ToString()
    {
      string mediaItemIdAlias;
      string valueAlias;
      return GenerateSqlStatement(new Namespace(), false, out mediaItemIdAlias, out valueAlias);
    }
  }
}
