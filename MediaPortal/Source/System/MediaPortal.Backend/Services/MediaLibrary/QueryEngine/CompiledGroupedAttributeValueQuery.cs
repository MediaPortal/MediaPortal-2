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
using MediaPortal.Core;
using MediaPortal.Core.General;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.MediaManagement.MLQueries;
using MediaPortal.Backend.Database;
using MediaPortal.Utilities.DB;
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
    protected readonly CompiledFilter _filter;

    public CompiledGroupedAttributeValueQuery(
        MIA_Management miaManagement,
        IEnumerable<MediaItemAspectMetadata> necessaryRequestedMIATypes,
        MediaItemAspectMetadata.AttributeSpecification selectedAttribute,
        CompiledFilter filter)
    {
      _miaManagement = miaManagement;
      _necessaryRequestedMIATypes = necessaryRequestedMIATypes;
      _selectAttribute = selectedAttribute;
      _filter = filter;
    }

    public MediaItemAspectMetadata.AttributeSpecification SelectAttribute
    {
      get { return _selectAttribute; }
    }

    public CompiledFilter Filter
    {
      get { return _filter; }
    }

    public static CompiledGroupedAttributeValueQuery Compile(MIA_Management miaManagement,
        IEnumerable<Guid> necessaryRequestedMIATypeIDs,
        MediaItemAspectMetadata.AttributeSpecification selectAttribute, IFilter filter)
    {
      IDictionary<Guid, MediaItemAspectMetadata> availableMIATypes = miaManagement.ManagedMediaItemAspectTypes;
      // Raise exception if MIA types are not present, which are contained in filter condition
      CompiledFilter compiledFilter = CompiledFilter.Compile(miaManagement, filter);
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
      return new CompiledGroupedAttributeValueQuery(miaManagement, necessaryMIATypes, selectAttribute, compiledFilter);
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
                _necessaryRequestedMIATypes, new MediaItemAspectMetadata[] {},
                new QueryAttribute[] {selectAttributeQA}, _filter, null);
            IDictionary<QueryAttribute, string> qa2a;
            builder.GenerateSqlGroupByStatement(new Namespace(), out groupSizeAlias, out qa2a, out statementStr, out bindVars);
            valueAlias = qa2a[selectAttributeQA];
          }
          else
          {
            ComplexAttributeQueryBuilder builder = new ComplexAttributeQueryBuilder(_miaManagement, _selectAttribute,
                _necessaryRequestedMIATypes, _filter);
            builder.GenerateSqlGroupByStatement(new Namespace(), out valueAlias, out groupSizeAlias,
                out statementStr, out bindVars);
          }
          command.CommandText = statementStr;
          foreach (BindVar bindVar in bindVars)
            DBUtils.AddParameter(command, bindVar.Name, bindVar.Value, DBUtils.GetDBType(bindVar.VariableType));

          HomogenousMap result = new HomogenousMap(_selectAttribute.AttributeType, typeof(int));
          using (IDataReader reader = command.ExecuteReader())
          {
            int valueCol = reader.GetOrdinal(valueAlias);
            int groupSizeCol = reader.GetOrdinal(groupSizeAlias);
            while (reader.Read())
              result.Add(DBUtils.ReadDBObject(reader, valueCol),
                  (int) DBUtils.ReadDBObject(reader, groupSizeCol));
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
