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

using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using MediaPortal.Core;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.MediaManagement.MLQueries;
using MediaPortal.Backend.Database;
using MediaPortal.Utilities;
using MediaPortal.Utilities.DB;
using MediaPortal.Utilities.Exceptions;

namespace MediaPortal.Backend.Services.MediaLibrary.QueryEngine
{
  /// <summary>
  /// Contains compiled media item query data, creates an SQL query for the query attributes, executes the query and picks out
  /// the result.
  /// </summary>
  public class CompiledMediaItemQuery
  {
    protected readonly MIA_Management _miaManagement;
    protected readonly ICollection<MediaItemAspectMetadata> _necessaryRequestedMIAs;
    protected readonly IDictionary<MediaItemAspectMetadata.AttributeSpecification, QueryAttribute> _mainSelectAttributes;
    protected readonly ICollection<MediaItemAspectMetadata.AttributeSpecification> _explicitSelectAttributes;
    protected readonly CompiledFilter _filter;
    protected readonly IList<SortInformation> _sortInformation;

    public CompiledMediaItemQuery(
        MIA_Management miaManagement,
        ICollection<MediaItemAspectMetadata> necessaryRequestedMIAs,
        IDictionary<MediaItemAspectMetadata.AttributeSpecification, QueryAttribute> mainSelectedAttributes,
        ICollection<MediaItemAspectMetadata.AttributeSpecification> explicitSelectedAttributes,
        CompiledFilter filter, IList<SortInformation> sortInformation)
    {
      _miaManagement = miaManagement;
      _necessaryRequestedMIAs = necessaryRequestedMIAs;
      _mainSelectAttributes = mainSelectedAttributes;
      _explicitSelectAttributes = explicitSelectedAttributes;
      _filter = filter;
      _sortInformation = sortInformation;
    }

    public IDictionary<MediaItemAspectMetadata.AttributeSpecification, QueryAttribute> MainSelectAttributes
    {
      get { return _mainSelectAttributes; }
    }

    public ICollection<MediaItemAspectMetadata.AttributeSpecification> ExplicitSelectAttributes
    {
      get { return _explicitSelectAttributes; }
    }

    public CompiledFilter Filter
    {
      get { return _filter; }
    }

    public ICollection<SortInformation> SortInformation
    {
      get { return _sortInformation; }
    }

    public static CompiledMediaItemQuery Compile(MIA_Management miaManagement, MediaItemQuery query)
    {
      IDictionary<Guid, MediaItemAspectMetadata> availableMIATypes = miaManagement.ManagedMediaItemAspectTypes;
      ICollection<MediaItemAspectMetadata> necessaryMIATypes = new List<MediaItemAspectMetadata>();
      // Raise exception if necessary MIA types are not present
      foreach (Guid miaTypeID in query.NecessaryRequestedMIATypeIDs)
      {
        MediaItemAspectMetadata miam;
        if (!availableMIATypes.TryGetValue(miaTypeID, out miam))
          throw new InvalidDataException("Necessary requested MIA type '{0}' is not present in the media library", miaTypeID);
        necessaryMIATypes.Add(miam);
      }
      // Raise exception if MIA types are not present, which are contained in filter condition
      CompiledFilter filter = CompiledFilter.Compile(miaManagement, query.Filter);
      foreach (QueryAttribute qa in filter.FilterAttributes)
      {
        MediaItemAspectMetadata miam = qa.Attr.ParentMIAM;
        if (!availableMIATypes.ContainsKey(miam.AspectId))
          throw new InvalidDataException("MIA type '{0}', which is contained in filter condition, is not present in the media library", miam.Name);
      }

      // Maps (all selected main) MIAM.Attributes to QueryAttributes
      IDictionary<MediaItemAspectMetadata.AttributeSpecification, QueryAttribute> mainSelectedAttributes =
          new Dictionary<MediaItemAspectMetadata.AttributeSpecification, QueryAttribute>();

      // Attributes selected in explicit queries
      ICollection<MediaItemAspectMetadata.AttributeSpecification> explicitSelectAttributes =
          new List<MediaItemAspectMetadata.AttributeSpecification>();

      // Allocate selected attributes to main query and explicit selects
      ICollection<Guid> requestedMIATypeIDs = CollectionUtils.UnionSet(
          query.NecessaryRequestedMIATypeIDs, query.OptionalRequestedMIATypeIDs);
      foreach (Guid miaTypeID in requestedMIATypeIDs)
      {
        MediaItemAspectMetadata miam;
        if (!availableMIATypes.TryGetValue(miaTypeID, out miam))
          // If one of the necessary MIA types is not available, an exception was raised above. So we only
          // come to here if an optional MIA type is not present - simply ignore that.
          continue;
        foreach (MediaItemAspectMetadata.AttributeSpecification attr in miam.AttributeSpecifications.Values)
        {
          if (attr.Cardinality == Cardinality.Inline || attr.Cardinality == Cardinality.ManyToOne)
            mainSelectedAttributes[attr] = new QueryAttribute(attr);
          else
            explicitSelectAttributes.Add(attr);
        }
      }

      return new CompiledMediaItemQuery(miaManagement, necessaryMIATypes,
          mainSelectedAttributes, explicitSelectAttributes, filter, query.SortInformation);
    }

    public IList<MediaItem> Execute()
    {
      ISQLDatabase database = ServiceScope.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        IDbCommand command;
        string statementStr;
        IList<object> values;

        // 1. Request all complex attributes
        IDictionary<long, IDictionary<MediaItemAspectMetadata.AttributeSpecification, ICollection<object>>> complexAttributeValues =
            new Dictionary<long, IDictionary<MediaItemAspectMetadata.AttributeSpecification, ICollection<object>>>();
        foreach (MediaItemAspectMetadata.AttributeSpecification attr in _explicitSelectAttributes)
        {
          ComplexAttributeQueryBuilder complexAttributeQueryBuilder = new ComplexAttributeQueryBuilder(
              _miaManagement, attr, _necessaryRequestedMIAs, _filter);
          command = transaction.CreateCommand();
          string mediaItemIdAlias;
          string valueAlias;
          complexAttributeQueryBuilder.GenerateSqlStatement(new Namespace(), out mediaItemIdAlias, out valueAlias,
              out statementStr, out values);
          command.CommandText = statementStr;
          foreach (object value in values)
          {
            IDbDataParameter param = command.CreateParameter();
            param.Value = value;
            command.Parameters.Add(param);
          }

          IDataReader reader = command.ExecuteReader();
          try
          {
            while (reader.Read())
            {
              Int64 mediaItemId = DBUtils.ReadDBValue<Int64>(reader, reader.GetOrdinal(mediaItemIdAlias));
              object value = DBUtils.ReadDBObject(reader, reader.GetOrdinal(valueAlias));
              IDictionary<MediaItemAspectMetadata.AttributeSpecification, ICollection<object>> attributeValues;
              if (!complexAttributeValues.TryGetValue(mediaItemId, out attributeValues))
                attributeValues = complexAttributeValues[mediaItemId] =
                    new Dictionary<MediaItemAspectMetadata.AttributeSpecification, ICollection<object>>();
              ICollection<object> attrValues;
              if (!attributeValues.TryGetValue(attr, out attrValues))
                attrValues = attributeValues[attr] = new List<object>();
              attrValues.Add(value);
            }
          }
          finally
          {
            reader.Close();
          }
        }

        // 2. Main query
        MainQueryBuilder mainQueryBuilder = new MainQueryBuilder(_miaManagement,
            _necessaryRequestedMIAs, _mainSelectAttributes.Values, _filter, _sortInformation);

        command = transaction.CreateCommand();
        string mediaItemIdAlias2;
        IDictionary<MediaItemAspectMetadata, string> miamAliases;
        Namespace mainQueryNS = new Namespace();
        // Maps (selected and filtered) QueryAttributes to CompiledQueryAttributes in the SQL query
        IDictionary<QueryAttribute, string> qa2a;
        mainQueryBuilder.GenerateSqlStatement(mainQueryNS, out mediaItemIdAlias2, out miamAliases, out qa2a,
            out statementStr, out values);
        command.CommandText = statementStr;
        foreach (object value in values)
        {
          IDbDataParameter param = command.CreateParameter();
          param.Value = value;
          command.Parameters.Add(param);
        }

        ICollection<MediaItemAspectMetadata> selectedMIAs = new HashSet<MediaItemAspectMetadata>();
        foreach (MediaItemAspectMetadata.AttributeSpecification attr in CollectionUtils.UnionList(_mainSelectAttributes.Keys, _explicitSelectAttributes))
          selectedMIAs.Add(attr.ParentMIAM);

        IDataReader reader2 = command.ExecuteReader();
        try
        {
          IList<MediaItem> result = new List<MediaItem>();
          while (reader2.Read())
          {
            long mediaItemId = DBUtils.ReadDBValue<Int64>(reader2, reader2.GetOrdinal(mediaItemIdAlias2));
            IDictionary<MediaItemAspectMetadata.AttributeSpecification, ICollection<object>> attributeValues;
            if (!complexAttributeValues.TryGetValue(mediaItemId, out attributeValues))
                attributeValues = null;
            MediaItem mediaItem = new MediaItem();
            foreach (MediaItemAspectMetadata miam in selectedMIAs)
            {
              if (reader2.IsDBNull(reader2.GetOrdinal(miamAliases[miam])))
                // MIAM is not available for current media item
                continue;
              MediaItemAspect mia = new MediaItemAspect(miam);
              foreach (MediaItemAspectMetadata.AttributeSpecification attr in miam.AttributeSpecifications.Values)
                if (attr.Cardinality == Cardinality.Inline)
                {
                  QueryAttribute qa = _mainSelectAttributes[attr];
                  string alias = qa2a[qa];
                  mia.SetAttribute(attr, DBUtils.ReadDBObject(reader2, reader2.GetOrdinal(alias)));
                }
                else
                {
                  ICollection<object> attrValues;
                  if (attributeValues != null && attributeValues.TryGetValue(attr, out attrValues))
                    mia.SetCollectionAttribute(attr, attrValues);
                }
              mediaItem.Aspects[miam.AspectId] = mia;
            }
            result.Add(mediaItem);
          }
          return result;
        }
        finally
        {
          reader2.Close();
        }
      }
      finally
      {
        transaction.Dispose();
      }
    }

    public override string ToString()
    {
      StringBuilder result = new StringBuilder();
      result.Append("CompiledMediaItemQuery\r\n");
      foreach (MediaItemAspectMetadata.AttributeSpecification attr in _explicitSelectAttributes)
      {
        ComplexAttributeQueryBuilder complexAttributeQueryBuilder = new ComplexAttributeQueryBuilder(
            _miaManagement, attr, _necessaryRequestedMIAs, _filter);
        result.Append("External attribute query for ");
        result.Append(attr.ParentMIAM.Name);
        result.Append(".");
        result.Append(attr.AttributeName);
        result.Append(":\r\n");
        result.Append(complexAttributeQueryBuilder.ToString());
        result.Append("\r\n\r\n");
      }
      result.Append("Main query:\r\n");
      MainQueryBuilder mainQueryBuilder = new MainQueryBuilder(_miaManagement, _necessaryRequestedMIAs,
          _mainSelectAttributes.Values, _filter, _sortInformation);
      result.Append(mainQueryBuilder.ToString());
      return result.ToString();
    }
  }
}
