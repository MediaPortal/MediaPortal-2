#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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
using System.Data;
using System.Linq;
using System.Text;
using MediaPortal.Backend.Services.Database;
using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Backend.Database;
using MediaPortal.Utilities;
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
    protected readonly ICollection<MediaItemAspectMetadata> _optionalRequestedMIAs;
    protected readonly IDictionary<MediaItemAspectMetadata.AttributeSpecification, QueryAttribute> _mainSelectAttributes;
    protected readonly ICollection<MediaItemAspectMetadata.AttributeSpecification> _explicitSelectAttributes;
    protected readonly IFilter _filter;
    protected readonly IList<SortInformation> _sortInformation;

    public CompiledMediaItemQuery(
        MIA_Management miaManagement,
        ICollection<MediaItemAspectMetadata> necessaryRequestedMIAs,
        ICollection<MediaItemAspectMetadata> optionalRequestedMIAs,
        IDictionary<MediaItemAspectMetadata.AttributeSpecification, QueryAttribute> mainSelectedAttributes,
        ICollection<MediaItemAspectMetadata.AttributeSpecification> explicitSelectedAttributes,
        IFilter filter, IList<SortInformation> sortInformation)
    {
      _miaManagement = miaManagement;
      _necessaryRequestedMIAs = necessaryRequestedMIAs;
      _optionalRequestedMIAs = optionalRequestedMIAs;
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

    public IFilter Filter
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
      ICollection<MediaItemAspectMetadata> optionalMIATypes = new List<MediaItemAspectMetadata>();
      // Raise exception if necessary MIA types are not present
      foreach (Guid miaTypeID in query.NecessaryRequestedMIATypeIDs)
      {
        MediaItemAspectMetadata miam;
        if (!availableMIATypes.TryGetValue(miaTypeID, out miam))
          throw new InvalidDataException("Necessary requested MIA type '{0}' is not present in the media library", miaTypeID);
        necessaryMIATypes.Add(miam);
      }
      // For optional MIA types, we don't raise an exception if the type is not present
      foreach (Guid miaTypeID in query.OptionalRequestedMIATypeIDs)
      {
        MediaItemAspectMetadata miam;
        if (!availableMIATypes.TryGetValue(miaTypeID, out miam))
          continue;
        optionalMIATypes.Add(miam);
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

      return new CompiledMediaItemQuery(miaManagement, necessaryMIATypes, optionalMIATypes,
          mainSelectedAttributes, explicitSelectAttributes, query.Filter, query.SortInformation);
    }

    private IList<MediaItem> GetMediaItems(ISQLDatabase database, ITransaction transaction, bool singleMode, IEnumerable<MediaItemAspectMetadata> selectedMIAs, out IList<Guid> mediaItemIds)
    {
          IList<MediaItem> mediaItems = new List<MediaItem>();
          mediaItemIds = new List<Guid>();

        string statementStr;
        IList<BindVar> bindVars;

        SingleMIAQueryBuilder builder = new SingleMIAQueryBuilder(_miaManagement,
            _mainSelectAttributes.Values, null, _necessaryRequestedMIAs, _optionalRequestedMIAs, _filter, _sortInformation);

        using (IDbCommand command = transaction.CreateCommand())
        {
          string mediaItemIdAlias2;
          IDictionary<MediaItemAspectMetadata, string> miamAliases;
          // Maps (selected and filtered) QueryAttributes to CompiledQueryAttributes in the SQL query
          IDictionary<QueryAttribute, string> qa2a;
          builder.GenerateSqlStatement(out mediaItemIdAlias2, out miamAliases, out qa2a,
              out statementStr, out bindVars);
          command.CommandText = statementStr;
          foreach (BindVar bindVar in bindVars)
            database.AddParameter(command, bindVar.Name, bindVar.Value, bindVar.VariableType);

          using (IDataReader reader = command.ExecuteReader())
          {
            while (reader.Read())
            {
              Guid mediaItemId = database.ReadDBValue<Guid>(reader, reader.GetOrdinal(mediaItemIdAlias2));
              if (mediaItemIds.Contains(mediaItemId))
                // Media item was already added to result - query results are not always unique because of JOINs used for filtering
                continue;
              mediaItemIds.Add(mediaItemId);
              MediaItem mediaItem = new MediaItem(mediaItemId);
              foreach (SingleMediaItemAspectMetadata miam in selectedMIAs.Where(x => x is SingleMediaItemAspectMetadata))
              {
                string name;
                if (!miamAliases.TryGetValue(miam, out name) || reader.IsDBNull(reader.GetOrdinal(name)))
                  // MIAM is not available for current media item
                  continue;
                SingleMediaItemAspect mia = new SingleMediaItemAspect(miam);
                foreach (MediaItemAspectMetadata.AttributeSpecification attr in miam.AttributeSpecifications.Values)
                  if (attr.Cardinality == Cardinality.Inline)
                  {
                    QueryAttribute qa = _mainSelectAttributes[attr];
                    string alias = qa2a[qa];
                    mia.SetAttribute(attr, database.ReadDBValue(attr.AttributeType, reader, reader.GetOrdinal(alias)));
                  }
                MediaItemAspect.SetAspect(mediaItem.Aspects, mia);
              }
              mediaItems.Add(mediaItem);
              if (singleMode)
              {
                  break;
              }
            }
          }
        }

        return mediaItems;
    }

    private IDictionary<Guid, IDictionary<MediaItemAspectMetadata.AttributeSpecification, ICollection<object>>> GetComplexAttributes(ISQLDatabase database, ITransaction transaction, IEnumerable<Guid> ids)
    {
        string statementStr;
        IList<BindVar> bindVars;

        IDictionary<Guid, IDictionary<MediaItemAspectMetadata.AttributeSpecification, ICollection<object>>> complexAttributeValues =
            new Dictionary<Guid, IDictionary<MediaItemAspectMetadata.AttributeSpecification, ICollection<object>>>();
        foreach (MediaItemAspectMetadata.AttributeSpecification attr in _explicitSelectAttributes)
        {
            ComplexAttributeQueryBuilder builder = new ComplexAttributeQueryBuilder(
                _miaManagement, attr, null, _necessaryRequestedMIAs, new MediaItemIdFilter(ids));
            using (IDbCommand command = transaction.CreateCommand())
            {
                string mediaItemIdAlias;
                string valueAlias;
                builder.GenerateSqlStatement(out mediaItemIdAlias, out valueAlias,
                    out statementStr, out bindVars);
                command.CommandText = statementStr;
                foreach (BindVar bindVar in bindVars)
                    database.AddParameter(command, bindVar.Name, bindVar.Value, bindVar.VariableType);

                Type valueType = attr.AttributeType;
                using (IDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Guid mediaItemId = database.ReadDBValue<Guid>(reader, reader.GetOrdinal(mediaItemIdAlias));
                        object value = database.ReadDBValue(valueType, reader, reader.GetOrdinal(valueAlias));
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
            }
        }

        return complexAttributeValues;
    }

    private IDictionary<Guid, ICollection<MultipleMediaItemAspect>> GetMultipleMIAs(ISQLDatabase database, ITransaction transaction, IEnumerable<MediaItemAspectMetadata> selectedMIAs, IEnumerable<Guid> ids)
    {
        IDictionary<Guid, ICollection<MultipleMediaItemAspect>> multipleMiaValues =
            new Dictionary<Guid, ICollection<MultipleMediaItemAspect>>();

        foreach (MultipleMediaItemAspectMetadata miam in selectedMIAs.Where(x => x is MultipleMediaItemAspectMetadata))
        {
            MultipleMIAQueryBuilder builder = new MultipleMIAQueryBuilder(_miaManagement,
              _mainSelectAttributes.Values, null, miam, ids.ToArray(), _sortInformation);
            using (IDbCommand command = transaction.CreateCommand())
            {
                string mediaItemIdAlias;
                string indexAlias;
                // Maps (selected and filtered) QueryAttributes to CompiledQueryAttributes in the SQL query
                IDictionary<QueryAttribute, string> qa2a;
                string statementStr;
                IList<BindVar> bindVars;
                builder.GenerateSqlStatement(out mediaItemIdAlias, out indexAlias, out qa2a,
                  out statementStr, out bindVars);
                command.CommandText = statementStr;
                foreach (BindVar bindVar in bindVars)
                    database.AddParameter(command, bindVar.Name, bindVar.Value, bindVar.VariableType);

                using (IDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Guid itemId = database.ReadDBValue<Guid>(reader, reader.GetOrdinal(mediaItemIdAlias));
                        ICollection<MultipleMediaItemAspect> values = null;
                        if (!multipleMiaValues.TryGetValue(itemId, out values))
                        {
                            values = new List<MultipleMediaItemAspect>();
                            multipleMiaValues[itemId] = values;
                        }
                        int index = database.ReadDBValue<Int32>(reader, reader.GetOrdinal(indexAlias));
                        MultipleMediaItemAspect mia = new MultipleMediaItemAspect(index, miam);
                        foreach (MediaItemAspectMetadata.AttributeSpecification attr in miam.AttributeSpecifications.Values)
                        {
                            if (attr.Cardinality == Cardinality.Inline)
                            {
                                QueryAttribute qa = _mainSelectAttributes[attr];
                                string alias = qa2a[qa];
                                Console.WriteLine("Reading multiple MIA attibute " + attr.AttributeName + " #" + index + " from column " + alias);
                                mia.SetAttribute(attr, database.ReadDBValue(attr.AttributeType, reader, reader.GetOrdinal(alias)));
                            }
                        }
                        values.Add(mia);
                    }
                }
            }
        }

        return multipleMiaValues;
    }

    public IList<MediaItem> QueryList()
    {
        return Query(false);
    }

    public IList<MediaItem> Query(bool singleMode)
    {
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();

      try
      {
          IEnumerable<MediaItemAspectMetadata> selectedMIAs = _necessaryRequestedMIAs.Union(_optionalRequestedMIAs);

          IList<Guid> mediaItemIds;
          IList<MediaItem> mediaItems = GetMediaItems(database, transaction, singleMode, selectedMIAs, out mediaItemIds);

          Console.WriteLine("Got media items " + string.Join(",", mediaItemIds));

          IDictionary<Guid, IDictionary<MediaItemAspectMetadata.AttributeSpecification, ICollection<object>>> complexAttributeValues =
              GetComplexAttributes(database, transaction, mediaItemIds);
          foreach (MediaItem mediaItem in mediaItems)
            {
                foreach (SingleMediaItemAspectMetadata miam in selectedMIAs.Where(x => x is SingleMediaItemAspectMetadata))
                {
                    IDictionary<MediaItemAspectMetadata.AttributeSpecification, ICollection<object>> attributeValues;
                    if (!complexAttributeValues.TryGetValue(mediaItem.MediaItemId, out attributeValues))
                        continue;
                    SingleMediaItemAspect mia = MediaItemAspect.GetOrCreateAspect(mediaItem.Aspects, miam);
                    foreach (MediaItemAspectMetadata.AttributeSpecification attr in miam.AttributeSpecifications.Values)
                        if (attr.Cardinality != Cardinality.Inline)
                        {
                            ICollection<object> attrValues;
                            if (attributeValues != null && attributeValues.TryGetValue(attr, out attrValues))
                                mia.SetCollectionAttribute(attr, attrValues);
                        }
                }
            }

          IDictionary<Guid, ICollection<MultipleMediaItemAspect>> multipleMiaValues =
            GetMultipleMIAs(database, transaction, selectedMIAs, mediaItemIds);
          Console.WriteLine("Got multiple MIAs for " + string.Join(",", multipleMiaValues.Keys));
          foreach (MediaItem mediaItem in mediaItems)
          {
              ICollection<MultipleMediaItemAspect> values;
              if (!multipleMiaValues.TryGetValue(mediaItem.MediaItemId, out values))
                  continue;
              foreach (MultipleMediaItemAspect value in values)
              {
                  Console.WriteLine("Adding MIA " + value);
                  MediaItemAspect.AddAspect(mediaItem.Aspects, value);
              }
          }

          return mediaItems;
      }
      finally
      {
        transaction.Dispose();
      }
    }

    public MediaItem QueryMediaItem()
    {
        IList<MediaItem> mediaItems = Query(true);
        if(mediaItems.Count == 1)
        {
            return mediaItems[0];
        }
        return null;
    }

    public override string ToString()
    {
      StringBuilder result = new StringBuilder();
      result.Append("CompiledMediaItemQuery\r\n");
      foreach (MediaItemAspectMetadata.AttributeSpecification attr in _explicitSelectAttributes)
      {
        ComplexAttributeQueryBuilder complexAttributeQueryBuilder = new ComplexAttributeQueryBuilder(
            _miaManagement, attr, null, _necessaryRequestedMIAs, _filter);
        result.Append("External attribute query for ");
        result.Append(attr.ParentMIAM.Name);
        result.Append(".");
        result.Append(attr.AttributeName);
        result.Append(":\r\n");
        result.Append(complexAttributeQueryBuilder.ToString());
        result.Append("\r\n\r\n");
      }
      result.Append("Main query:\r\n");
      SingleMIAQueryBuilder mainQueryBuilder = new SingleMIAQueryBuilder(_miaManagement,
          _mainSelectAttributes.Values, null, _necessaryRequestedMIAs, _optionalRequestedMIAs, _filter, _sortInformation);
      result.Append(mainQueryBuilder.ToString());
      return result.ToString();
    }
  }
}
