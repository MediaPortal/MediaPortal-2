#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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
    protected readonly MIA_Management _miaManagement;
    protected readonly IEnumerable<MediaItemAspectMetadata> _necessaryRequestedMIATypes;
    protected readonly MediaItemAspectMetadata.AttributeSpecification _selectAttribute;
    protected readonly IAttributeFilter _selectAttributeFilter;
    protected readonly SelectProjectionFunction _selectProjectionFunction;
    protected readonly Type _projectionValueType;
    protected readonly CompiledFilter _filter;
    protected readonly BindVarNamespace _bvNamespace;

    public CompiledGroupedAttributeValueQuery(
        MIA_Management miaManagement,
        IEnumerable<MediaItemAspectMetadata> necessaryRequestedMIATypes,
        MediaItemAspectMetadata.AttributeSpecification selectedAttribute, IAttributeFilter selectAttributeFilter,
        SelectProjectionFunction selectProjectionFunction, Type projectionValueType, BindVarNamespace bvNamespace,
        CompiledFilter filter)
    {
      _miaManagement = miaManagement;
      _necessaryRequestedMIATypes = necessaryRequestedMIATypes;
      _selectAttribute = selectedAttribute;
      _selectAttributeFilter = selectAttributeFilter;
      _selectProjectionFunction = selectProjectionFunction;
      _projectionValueType = projectionValueType;
      _bvNamespace = bvNamespace;
      _filter = filter;
    }

    public IEnumerable<MediaItemAspectMetadata> NecessaryRequestedMIATypes
    {
      get { return _necessaryRequestedMIATypes; }
    }

    public MediaItemAspectMetadata.AttributeSpecification SelectAttribute
    {
      get { return _selectAttribute; }
    }

    public IAttributeFilter SelectAttributeFilter
    {
      get { return _selectAttributeFilter; }
    }

    public CompiledFilter Filter
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

      BindVarNamespace bvNamespace = new BindVarNamespace();
      // Raise exception if MIA types are not present, which are contained in filter condition
      CompiledFilter compiledFilter = CompiledFilter.Compile(miaManagement, combinedFilter, bvNamespace);
      foreach (QueryAttribute qa in compiledFilter.FilterAttributes)
      {
        MediaItemAspectMetadata miam = qa.Attr.ParentMIAM;
        if (!availableMIATypes.ContainsKey(miam.AspectId))
          throw new InvalidDataException("MIA type '{0}', which is contained in filter condition, is not present in the media library", miam.Name);
      }
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
          selectProjectionFunction, projectionValueType, bvNamespace, compiledFilter);
    }

    public HomogenousMap Execute()
    {
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        using (IDbCommand command = transaction.CreateCommand())
        {
          string valueAlias;
          string groupSizeAlias;
          string statementStr;
          IList<BindVar> bindVars;
          if (_selectAttribute.Cardinality == Cardinality.Inline || _selectAttribute.Cardinality == Cardinality.ManyToOne)
          {
            QueryAttribute selectAttributeQA = new QueryAttribute(_selectAttribute);
            MainQueryBuilder builder = new MainQueryBuilder(_miaManagement,
                new QueryAttribute[] {selectAttributeQA}, _selectProjectionFunction,
                _necessaryRequestedMIATypes, new MediaItemAspectMetadata[] {}, _filter, null);
            IDictionary<QueryAttribute, string> qa2a;
            builder.GenerateSqlGroupByStatement(new Namespace(), out groupSizeAlias, out qa2a, out statementStr, out bindVars);
            valueAlias = qa2a[selectAttributeQA];
          }
          else
          {
            ComplexAttributeQueryBuilder builder = new ComplexAttributeQueryBuilder(_miaManagement, _selectAttribute,
                _selectProjectionFunction, _necessaryRequestedMIATypes, _filter);
            builder.GenerateSqlGroupByStatement(new Namespace(), _selectAttributeFilter, _bvNamespace, out valueAlias, out groupSizeAlias,
                out statementStr, out bindVars);
          }
          command.CommandText = statementStr;
          foreach (BindVar bindVar in bindVars)
            database.AddParameter(command, bindVar.Name, bindVar.Value, bindVar.VariableType);

          Type valueType = _projectionValueType ?? _selectAttribute.AttributeType;
          HomogenousMap result = new HomogenousMap(valueType, typeof(int));
          using (IDataReader reader = command.ExecuteReader())
          {
            int valueCol = reader.GetOrdinal(valueAlias);
            int groupSizeCol = reader.GetOrdinal(groupSizeAlias);
            while (reader.Read())
              result.Add(database.ReadDBValue(valueType, reader, valueCol),
                  database.ReadDBValue<int>(reader, groupSizeCol));
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
