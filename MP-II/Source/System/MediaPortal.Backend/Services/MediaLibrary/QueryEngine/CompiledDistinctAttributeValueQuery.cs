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
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.MediaManagement.MLQueries;
using MediaPortal.Backend.Database;
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
    protected readonly MediaItemAspectMetadata.AttributeSpecification _selectAttribute;
    protected readonly CompiledFilter _filter;

    public CompiledDistinctAttributeValueQuery(
        MIA_Management miaManagement,
        MediaItemAspectMetadata.AttributeSpecification selectedAttribute,
        CompiledFilter filter)
    {
      _miaManagement = miaManagement;
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
        MediaItemAspectMetadata.AttributeSpecification selectAttribute,
        IFilter filter, IDictionary<Guid, MediaItemAspectMetadata> availableMIATypes)
    {
      // Raise exception if MIA types are not present, which are contained in filter condition
      CompiledFilter compiledFilter = CompiledFilter.Compile(miaManagement, filter);
      foreach (QueryAttribute qa in compiledFilter.FilterAttributes)
      {
        MediaItemAspectMetadata miam = qa.Attr.ParentMIAM;
        if (!availableMIATypes.ContainsKey(miam.AspectId))
          throw new InvalidDataException("MIA type '{0}', which is contained in filter condition, is not present in the media library", miam.Name);
      }

      return new CompiledDistinctAttributeValueQuery(miaManagement, selectAttribute, compiledFilter);
    }

    public HomogenousCollection Execute()
    {
      ISQLDatabase database = ServiceScope.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        IDbCommand command = transaction.CreateCommand();

        string valueAlias;
        if (_selectAttribute.Cardinality == Cardinality.Inline)
        {
          QueryAttribute selectAttributeQA = new QueryAttribute(_selectAttribute);
          MainQueryBuilder builder = new MainQueryBuilder(_miaManagement, new List<MediaItemAspectMetadata>(),
              new QueryAttribute[] {selectAttributeQA}, _filter);
          Namespace ns = new Namespace();
          string mediaItemIdAlias;
          IDictionary<MediaItemAspectMetadata, string> miamAliases;
          IDictionary<QueryAttribute, CompiledQueryAttribute> qa2cqa;
          command.CommandText = builder.GenerateSqlStatement(ns, true, out mediaItemIdAlias,
              out miamAliases, out qa2cqa);
          valueAlias = qa2cqa[selectAttributeQA].GetAlias(ns);
        }
        else
        {
          ComplexAttributeQueryBuilder builder = new ComplexAttributeQueryBuilder(_miaManagement, _selectAttribute,
              new List<MediaItemAspectMetadata>(), _filter);
          string mediaItemIdAlias;
          command.CommandText = builder.GenerateSqlStatement(new Namespace(),
              true, out mediaItemIdAlias, out valueAlias);
        }

        IDataReader reader = command.ExecuteReader();
        HomogenousCollection result = new HomogenousCollection(_selectAttribute.AttributeType);
        try
        {
          while (reader.Read())
            result.Add(reader.GetValue(reader.GetOrdinal(valueAlias)));
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
