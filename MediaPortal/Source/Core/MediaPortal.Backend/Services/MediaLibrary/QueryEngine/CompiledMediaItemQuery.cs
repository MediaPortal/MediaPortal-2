#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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

    public IList<MediaItem> QueryList()
    {
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        string statementStr;
        IList<BindVar> bindVars;

        // 1. Request all complex attributes
        IDictionary<Guid, IDictionary<MediaItemAspectMetadata.AttributeSpecification, ICollection<object>>> complexAttributeValues =
            new Dictionary<Guid, IDictionary<MediaItemAspectMetadata.AttributeSpecification, ICollection<object>>>();
        foreach (MediaItemAspectMetadata.AttributeSpecification attr in _explicitSelectAttributes)
        {
          ComplexAttributeQueryBuilder complexAttributeQueryBuilder = new ComplexAttributeQueryBuilder(
              _miaManagement, attr, null, _necessaryRequestedMIAs, _filter);
          using (IDbCommand command = transaction.CreateCommand())
          {
            string mediaItemIdAlias;
            string valueAlias;
            complexAttributeQueryBuilder.GenerateSqlStatement(out mediaItemIdAlias, out valueAlias,
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

        // 2. Main query
        MainQueryBuilder mainQueryBuilder = new MainQueryBuilder(_miaManagement,
            _mainSelectAttributes.Values, null, _necessaryRequestedMIAs, _optionalRequestedMIAs, _filter, _sortInformation);

        using (IDbCommand command = transaction.CreateCommand())
        {
          string mediaItemIdAlias2;
          IDictionary<MediaItemAspectMetadata, string> miamAliases;
          // Maps (selected and filtered) QueryAttributes to CompiledQueryAttributes in the SQL query
          IDictionary<QueryAttribute, string> qa2a;
          mainQueryBuilder.GenerateSqlStatement(out mediaItemIdAlias2, out miamAliases, out qa2a,
              out statementStr, out bindVars);
          command.CommandText = statementStr;
          foreach (BindVar bindVar in bindVars)
            database.AddParameter(command, bindVar.Name, bindVar.Value, bindVar.VariableType);

          IEnumerable<MediaItemAspectMetadata> selectedMIAs = _necessaryRequestedMIAs.Union(_optionalRequestedMIAs);

          ICollection<Guid> mediaItems = new HashSet<Guid>();
          using (IDataReader reader = command.ExecuteReader())
          {
            IList<MediaItem> result = new List<MediaItem>();
            while (reader.Read())
            {
              Guid mediaItemId = database.ReadDBValue<Guid>(reader, reader.GetOrdinal(mediaItemIdAlias2));
              if (mediaItems.Contains(mediaItemId))
                // Media item was already added to result - query results are not always unique because of JOINs used for filtering
                continue;
              mediaItems.Add(mediaItemId);
              IDictionary<MediaItemAspectMetadata.AttributeSpecification, ICollection<object>> attributeValues;
              if (!complexAttributeValues.TryGetValue(mediaItemId, out attributeValues))
                  attributeValues = null;
              MediaItem mediaItem = new MediaItem(mediaItemId);
              foreach (MediaItemAspectMetadata miam in selectedMIAs)
              {
                if (reader.IsDBNull(reader.GetOrdinal(miamAliases[miam])))
                  // MIAM is not available for current media item
                  continue;
                MediaItemAspect mia = new MediaItemAspect(miam);
                foreach (MediaItemAspectMetadata.AttributeSpecification attr in miam.AttributeSpecifications.Values)
                  if (attr.Cardinality == Cardinality.Inline)
                  {
                    QueryAttribute qa = _mainSelectAttributes[attr];
                    string alias = qa2a[qa];
                    mia.SetAttribute(attr, database.ReadDBValue(attr.AttributeType, reader, reader.GetOrdinal(alias)));
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
        }
      }
      finally
      {
        transaction.Dispose();
      }
    }

    public MediaItem QueryMediaItem()
    {
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        MediaItem result = null;

        // 1. Main query
        MainQueryBuilder mainQueryBuilder = new MainQueryBuilder(_miaManagement,
            _mainSelectAttributes.Values, null, _necessaryRequestedMIAs, _optionalRequestedMIAs, _filter, _sortInformation);

        using (IDbCommand mainQueryCommand = transaction.CreateCommand())
        {
          string mediaItemIdAlias2;
          IDictionary<MediaItemAspectMetadata, string> miamAliases;
          // Maps (selected and filtered) QueryAttributes to CompiledQueryAttributes in the SQL query
          IDictionary<QueryAttribute, string> qa2a;
          string statementStr;
          IList<BindVar> bindVars;
          mainQueryBuilder.GenerateSqlStatement(out mediaItemIdAlias2, out miamAliases, out qa2a,
              out statementStr, out bindVars);
          mainQueryCommand.CommandText = statementStr;
          foreach (BindVar bindVar in bindVars)
            database.AddParameter(mainQueryCommand, bindVar.Name, bindVar.Value, bindVar.VariableType);

          IEnumerable<MediaItemAspectMetadata> selectedMIAs = _necessaryRequestedMIAs.Union(_optionalRequestedMIAs);

          using (IDataReader mainReader = mainQueryCommand.ExecuteReader())
          {
            if (mainReader.Read())
            {
              Guid mediaItemId = database.ReadDBValue<Guid>(mainReader, mainReader.GetOrdinal(mediaItemIdAlias2));
              result = new MediaItem(mediaItemId);

              // Request complex attributes using media item ID
              IFilter modifiedFilter = BooleanCombinationFilter.CombineFilters(BooleanOperator.And, _filter, new MediaItemIdFilter(mediaItemId));
              IDictionary<MediaItemAspectMetadata.AttributeSpecification, ICollection<object>> complexAttributeValues =
                  new Dictionary<MediaItemAspectMetadata.AttributeSpecification, ICollection<object>>();
              foreach (MediaItemAspectMetadata.AttributeSpecification attr in _explicitSelectAttributes)
              {
                ComplexAttributeQueryBuilder complexAttributeQueryBuilder = new ComplexAttributeQueryBuilder(
                    _miaManagement, attr, null, null, modifiedFilter);
                using (IDbCommand complexQueryCommand = transaction.CreateCommand())
                {
                  string mediaItemIdAlias;
                  string valueAlias;
                  complexAttributeQueryBuilder.GenerateSqlStatement(out mediaItemIdAlias, out valueAlias,
                      out statementStr, out bindVars);
                  complexQueryCommand.CommandText = statementStr;
                  foreach (BindVar bindVar in bindVars)
                    database.AddParameter(complexQueryCommand, bindVar.Name, bindVar.Value, bindVar.VariableType);

                  Type valueType = attr.AttributeType;
                  using (IDataReader reader = complexQueryCommand.ExecuteReader())
                  {
                    if (reader.Read())
                    {
                      object value = database.ReadDBValue(valueType, reader, reader.GetOrdinal(valueAlias));
                      ICollection<object> attrValues;
                      if (!complexAttributeValues.TryGetValue(attr, out attrValues))
                        attrValues = complexAttributeValues[attr] = new List<object>();
                      attrValues.Add(value);
                    }
                  }
                }
              }

              // Put together all attributes
              foreach (MediaItemAspectMetadata miam in selectedMIAs)
              {
                if (mainReader.IsDBNull(mainReader.GetOrdinal(miamAliases[miam])))
                  // MIAM is not available for current media item
                  continue;
                MediaItemAspect mia = new MediaItemAspect(miam);
                foreach (MediaItemAspectMetadata.AttributeSpecification attr in miam.AttributeSpecifications.Values)
                  if (attr.Cardinality == Cardinality.Inline)
                  {
                    QueryAttribute qa = _mainSelectAttributes[attr];
                    string alias = qa2a[qa];
                    mia.SetAttribute(attr, database.ReadDBValue(attr.AttributeType, mainReader, mainReader.GetOrdinal(alias)));
                  }
                  else
                  {
                    ICollection<object> attrValues;
                    if (complexAttributeValues.TryGetValue(attr, out attrValues))
                      mia.SetCollectionAttribute(attr, attrValues);
                  }
                result.Aspects[miam.AspectId] = mia;
              }
            }
            return result;
          }
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
      MainQueryBuilder mainQueryBuilder = new MainQueryBuilder(_miaManagement,
          _mainSelectAttributes.Values, null, _necessaryRequestedMIAs, _optionalRequestedMIAs, _filter, _sortInformation);
      result.Append(mainQueryBuilder.ToString());
      return result.ToString();
    }
  }
}
