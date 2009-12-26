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
  public class CompiledDistinctAttributeValueQuery
  {
    protected readonly MIA_Management _miaManagement;
    protected readonly ICollection<MediaItemAspectMetadata> _necessaryRequestedMIATypes;
    protected readonly MediaItemAspectMetadata.AttributeSpecification _selectAttribute;
    protected readonly CompiledFilter _filter;

    public CompiledDistinctAttributeValueQuery(
        MIA_Management miaManagement,
        ICollection<MediaItemAspectMetadata> necessaryRequestedMIATypes,
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

    public static CompiledDistinctAttributeValueQuery Compile(MIA_Management miaManagement,
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

      return new CompiledDistinctAttributeValueQuery(miaManagement, necessaryMIATypes, selectAttribute, compiledFilter);
    }

    public HomogenousCollection Execute()
    {
      ISQLDatabase database = ServiceScope.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        IDbCommand command = transaction.CreateCommand();

        string valueAlias;
        string statementStr;
        IList<object> values;
        if (_selectAttribute.Cardinality == Cardinality.Inline || _selectAttribute.Cardinality == Cardinality.ManyToOne)
        {
          QueryAttribute selectAttributeQA = new QueryAttribute(_selectAttribute);
          MainQueryBuilder builder = new MainQueryBuilder(_miaManagement, _necessaryRequestedMIATypes,
              new QueryAttribute[] {selectAttributeQA}, _filter, null);
          string mediaItemIdAlias;
          IDictionary<MediaItemAspectMetadata, string> miamAliases;
          IDictionary<QueryAttribute, string> qa2a;
          builder.GenerateSqlStatement(new Namespace(), true, out mediaItemIdAlias, out miamAliases, out qa2a,
              out statementStr, out values);
          valueAlias = qa2a[selectAttributeQA];
        }
        else
        {
          ComplexAttributeQueryBuilder builder = new ComplexAttributeQueryBuilder(_miaManagement, _selectAttribute,
              _necessaryRequestedMIATypes, _filter);
          string mediaItemIdAlias;
          builder.GenerateSqlStatement(new Namespace(), true, out mediaItemIdAlias, out valueAlias,
              out statementStr, out values);
        }
        command.CommandText = statementStr;
        foreach (object value in values)
        {
          IDbDataParameter param = command.CreateParameter();
          param.Value = value;
          command.Parameters.Add(param);
        }

        IDataReader reader = command.ExecuteReader();
        HomogenousCollection result = new HomogenousCollection(_selectAttribute.AttributeType);
        try
        {
          while (reader.Read())
            result.Add(DBUtils.ReadDBObject(reader, reader.GetOrdinal(valueAlias)));
        }
        finally
        {
          reader.Close();
        }
        return result;
      }
      finally
      {
        transaction.Dispose();
      }
    }
  }
}
