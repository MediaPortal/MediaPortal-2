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

using System;
using System.Collections.Generic;
using System.Data;
using MediaPortal.Backend.Services.Database;
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Backend.Database;
using MediaPortal.Utilities.Exceptions;

namespace MediaPortal.Backend.Services.MediaLibrary.QueryEngine
{
  /// <summary>
  /// Creates an SQL query for selecting a set of distinct media item aspect attribute values for a given attribute type
  /// and a given set of media items, specified by a filter.
  /// </summary>
  public class CompiledGroupedAttributeValueQuery
  {
    protected readonly MIA_Management _miaManagement = null;
    protected readonly IEnumerable<MediaItemAspectMetadata> _necessaryRequestedMIATypes = null;
    protected readonly MediaItemAspectMetadata.AttributeSpecification _selectKeyAttribute = null;
    protected readonly MediaItemAspectMetadata.AttributeSpecification _selectValueAttribute = null;
    protected readonly IAttributeFilter _selectAttributeFilter = null;
    protected readonly SelectProjectionFunction _selectProjectionFunction = null;
    protected readonly Type _projectionValueType = null;
    protected readonly IFilter _filter = null;

    public CompiledGroupedAttributeValueQuery(
        MIA_Management miaManagement,
        IEnumerable<MediaItemAspectMetadata> necessaryRequestedMIATypes,
        MediaItemAspectMetadata.AttributeSpecification selectedAttribute, IAttributeFilter selectAttributeFilter,
        SelectProjectionFunction selectProjectionFunction, Type projectionValueType,
        IFilter filter)
    {
      _miaManagement = miaManagement;
      _necessaryRequestedMIATypes = necessaryRequestedMIATypes;
      _selectValueAttribute = selectedAttribute;
      _selectAttributeFilter = selectAttributeFilter;
      _selectProjectionFunction = selectProjectionFunction;
      _projectionValueType = projectionValueType;
      _filter = filter;
    }

    public CompiledGroupedAttributeValueQuery(
        MIA_Management miaManagement,
        IEnumerable<MediaItemAspectMetadata> necessaryRequestedMIATypes,
        MediaItemAspectMetadata.AttributeSpecification selectedKeyAttribute,
        MediaItemAspectMetadata.AttributeSpecification selectedValueAttribute,
        IAttributeFilter selectAttributeFilter,
        SelectProjectionFunction selectProjectionFunction, Type projectionValueType,
        IFilter filter)
    {
      _miaManagement = miaManagement;
      _necessaryRequestedMIATypes = necessaryRequestedMIATypes;
      _selectKeyAttribute = selectedKeyAttribute;
      _selectValueAttribute = selectedValueAttribute;
      _selectAttributeFilter = selectAttributeFilter;
      _selectProjectionFunction = selectProjectionFunction;
      _projectionValueType = projectionValueType;
      _filter = filter;
    }

    public IEnumerable<MediaItemAspectMetadata> NecessaryRequestedMIATypes
    {
      get { return _necessaryRequestedMIATypes; }
    }

    public MediaItemAspectMetadata.AttributeSpecification SelectKeyAttribute
    {
      get { return _selectKeyAttribute; }
    }

    public MediaItemAspectMetadata.AttributeSpecification SelectValueAttribute
    {
      get { return _selectValueAttribute; }
    }

    public IAttributeFilter SelectAttributeFilter
    {
      get { return _selectAttributeFilter; }
    }

    public IFilter Filter
    {
      get { return _filter; }
    }

    public static CompiledGroupedAttributeValueQuery Compile(MIA_Management miaManagement,
        IEnumerable<Guid> necessaryRequestedMIATypeIDs,
        MediaItemAspectMetadata.AttributeSpecification selectAttribute, IAttributeFilter selectAttributeFilter,
        SelectProjectionFunction selectProjectionFunction, Type projectionValueType, IFilter filter)
    {
      IDictionary<Guid, MediaItemAspectMetadata> availableMIATypes = miaManagement.ManagedMediaItemAspectTypes;

      // If we're doing a complex query, we can optimize if we have an extra select attribute filter, i.e. a restriction
      // on the result set of values. See ComplexAttributeQueryBuilder.GenerateSqlGroupByStatement().
      bool simpleQuery = selectAttribute.Cardinality == Cardinality.Inline || selectAttribute.Cardinality == Cardinality.ManyToOne;
      IFilter combinedFilter = simpleQuery ?
          BooleanCombinationFilter.CombineFilters(BooleanOperator.And, new IFilter[] {filter, selectAttributeFilter}) : filter;
      selectAttributeFilter = simpleQuery ? null : selectAttributeFilter;

      ICollection<MediaItemAspectMetadata> necessaryMIATypes = new List<MediaItemAspectMetadata>();
      // Raise exception if necessary MIA types are not present
      foreach (Guid miaTypeID in necessaryRequestedMIATypeIDs)
      {
        MediaItemAspectMetadata miam;
        if (!availableMIATypes.TryGetValue(miaTypeID, out miam))
          throw new InvalidDataException("Necessary requested MIA type of ID '{0}' is not present in the media library", miaTypeID);
        necessaryMIATypes.Add(miam);
      }
      return new CompiledGroupedAttributeValueQuery(miaManagement, necessaryMIATypes, selectAttribute, selectAttributeFilter,
          selectProjectionFunction, projectionValueType, combinedFilter);
    }

    public static CompiledGroupedAttributeValueQuery Compile(MIA_Management miaManagement,
        IEnumerable<Guid> necessaryRequestedMIATypeIDs,
        MediaItemAspectMetadata.AttributeSpecification selectKeyAttribute,
        MediaItemAspectMetadata.AttributeSpecification selectValueAttribute,
        IAttributeFilter selectAttributeFilter,
        SelectProjectionFunction selectProjectionFunction, Type projectionValueType, IFilter filter)
    {
      IDictionary<Guid, MediaItemAspectMetadata> availableMIATypes = miaManagement.ManagedMediaItemAspectTypes;

      // If we're doing a complex query, we can optimize if we have an extra select attribute filter, i.e. a restriction
      // on the result set of values. See ComplexAttributeQueryBuilder.GenerateSqlGroupByStatement().
      bool simpleQuery = selectValueAttribute.Cardinality == Cardinality.Inline || selectValueAttribute.Cardinality == Cardinality.ManyToOne;
      IFilter combinedFilter = simpleQuery ?
          BooleanCombinationFilter.CombineFilters(BooleanOperator.And, new IFilter[] { filter, selectAttributeFilter }) : filter;
      selectAttributeFilter = simpleQuery ? null : selectAttributeFilter;

      ICollection<MediaItemAspectMetadata> necessaryMIATypes = new List<MediaItemAspectMetadata>();
      // Raise exception if necessary MIA types are not present
      foreach (Guid miaTypeID in necessaryRequestedMIATypeIDs)
      {
        MediaItemAspectMetadata miam;
        if (!availableMIATypes.TryGetValue(miaTypeID, out miam))
          throw new InvalidDataException("Necessary requested MIA type of ID '{0}' is not present in the media library", miaTypeID);
        necessaryMIATypes.Add(miam);
      }
      return new CompiledGroupedAttributeValueQuery(miaManagement, necessaryMIATypes, selectKeyAttribute, selectValueAttribute, 
        selectAttributeFilter, selectProjectionFunction, projectionValueType, combinedFilter);
    }

    public Tuple<HomogenousMap, HomogenousMap> Execute()
    {
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      ITransaction transaction = database.CreateTransaction();
      try
      {
        using (IDbCommand command = transaction.CreateCommand())
        {
          string keyAlias = null;
          string valueAlias = null;
          string groupSizeAlias = null;
          string statementStr = null;
          IList<BindVar> bindVars = null;
          if (_selectValueAttribute.Cardinality == Cardinality.Inline || _selectValueAttribute.Cardinality == Cardinality.ManyToOne)
          {
            List<QueryAttribute> qAttributes = new List<QueryAttribute>();
            QueryAttribute selectValueAttributeQA = new QueryAttribute(_selectValueAttribute);
            qAttributes.Add(selectValueAttributeQA);
            QueryAttribute selectKeyAttributeQA = null;
            if (_selectKeyAttribute != null)
            {
              selectKeyAttributeQA = new QueryAttribute(_selectKeyAttribute);
              qAttributes.Add(selectKeyAttributeQA);
            }
            MIAQueryBuilder builder = new MIAQueryBuilder(_miaManagement, qAttributes, _selectProjectionFunction,
                _necessaryRequestedMIATypes, new MediaItemAspectMetadata[] {}, _filter, null);
            IDictionary<QueryAttribute, string> qa2a;
            builder.GenerateSqlGroupByStatement(out groupSizeAlias, out qa2a, out statementStr, out bindVars);
            valueAlias = qa2a[selectValueAttributeQA];
            if (_selectKeyAttribute != null)
              keyAlias = qa2a[selectKeyAttributeQA];
          }
          else
          {
            if (_selectKeyAttribute != null)
            {
              throw new InvalidDataException("Value attribute '{0}' does not support key value grouping", _selectValueAttribute.AttributeName);
            }
            ComplexAttributeQueryBuilder builder = new ComplexAttributeQueryBuilder(_miaManagement, _selectValueAttribute,
                _selectProjectionFunction, _necessaryRequestedMIATypes, _filter);
            builder.GenerateSqlGroupByStatement(_selectAttributeFilter, out valueAlias, out groupSizeAlias,
                out statementStr, out bindVars);
          }
          command.CommandText = statementStr;
          foreach (BindVar bindVar in bindVars)
            database.AddParameter(command, bindVar.Name, bindVar.Value, bindVar.VariableType);

          Tuple<HomogenousMap, HomogenousMap> result = null;
          if (_selectKeyAttribute != null)
          {
            Type valueType = _projectionValueType ?? _selectValueAttribute.AttributeType;
            Type keyType = _selectKeyAttribute.AttributeType;
            HomogenousMap valueMap = new HomogenousMap(valueType, typeof(int));
            HomogenousMap keyMap = new HomogenousMap(valueType, keyType);
            using (IDataReader reader = command.ExecuteReader())
            {
              int keyCol = reader.GetOrdinal(keyAlias);
              int valueCol = reader.GetOrdinal(valueAlias);
              int groupSizeCol = reader.GetOrdinal(groupSizeAlias);
              while (reader.Read())
              {
                if (!keyMap.ContainsKey(database.ReadDBValue(valueType, reader, valueCol)))
                {
                  keyMap.Add(database.ReadDBValue(valueType, reader, valueCol), database.ReadDBValue(keyType, reader, keyCol));
                  valueMap.Add(database.ReadDBValue(valueType, reader, valueCol), database.ReadDBValue<int>(reader, groupSizeCol));
                }
              }
            }
            result = new Tuple<HomogenousMap, HomogenousMap>(valueMap, keyMap);
          }
          else
          {
            Type valueType = _projectionValueType ?? _selectValueAttribute.AttributeType;
            HomogenousMap valueMap = new HomogenousMap(valueType, typeof(int));
            using (IDataReader reader = command.ExecuteReader())
            {
              int valueCol = reader.GetOrdinal(valueAlias);
              int groupSizeCol = reader.GetOrdinal(groupSizeAlias);
              while (reader.Read())
                valueMap.Add(database.ReadDBValue(valueType, reader, valueCol),
                    database.ReadDBValue<int>(reader, groupSizeCol));
            }
            result = new Tuple<HomogenousMap, HomogenousMap>(valueMap, null);
          }
          return result;
        }
      }
      finally
      {
        transaction.Dispose();
      }
    }
  }
}
