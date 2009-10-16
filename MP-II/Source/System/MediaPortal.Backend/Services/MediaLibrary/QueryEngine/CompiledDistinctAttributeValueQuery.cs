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

using System;
using System.Collections.Generic;
using System.Data;
using MediaPortal.Core;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.MediaManagement.MLQueries;
using MediaPortal.Database;
using MediaPortal.Utilities.Exceptions;

namespace MediaPortal.Services.MediaLibrary.QueryEngine
{
  /// <summary>
  /// Creates an SQL query for selecting a set of distinct media item aspect attribute values for a given attribute type
  /// and a given set of media items, specified by a filter.
  /// </summary>
  public class CompiledDistinctAttributeValueQuery
  {
    MIAM_Management _miamManagement;
    protected MediaItemAspectMetadata.AttributeSpecification _selectAttribute;
    protected CompiledFilter _filter;

    public CompiledDistinctAttributeValueQuery(
        MIAM_Management miamManagement,
        MediaItemAspectMetadata.AttributeSpecification selectedAttribute,
        CompiledFilter filter)
    {
      _miamManagement = miamManagement;
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

    public static CompiledDistinctAttributeValueQuery Compile(MIAM_Management miamManagement,
        MediaItemAspectMetadata.AttributeSpecification selectAttribute,
        IFilter filter, IDictionary<Guid, MediaItemAspectMetadata> availableMIATypes)
    {
      // Raise exception if MIA types are not present, which are contained in filter condition
      CompiledFilter compiledFilter = CompiledFilter.Compile(miamManagement, filter);
      foreach (QueryAttribute qa in compiledFilter.FilterAttributes)
      {
        MediaItemAspectMetadata miam = qa.Attr.ParentMIAM;
        if (!availableMIATypes.ContainsKey(miam.AspectId))
          throw new InvalidDataException("MIA type '{0}', which is contained in filter condition, is not present in the media library", miam.Name);
      }

      return new CompiledDistinctAttributeValueQuery(miamManagement, selectAttribute, compiledFilter);
    }

    public ICollection<object> Execute()
    {
      ISQLDatabase database = ServiceScope.Get<ISQLDatabase>();
      ITransaction transaction = database.BeginTransaction();
      try
      {
        IDbCommand command = transaction.CreateCommand();

        string valueAlias;
        if (_selectAttribute.Cardinality == Cardinality.Inline)
        {
          ComplexAttributeQueryBuilder builder = new ComplexAttributeQueryBuilder(_selectAttribute,
              new List<MediaItemAspectMetadata>(), _filter);
          string mediaItemIdAlias;
          command.CommandText = builder.GenerateSqlStatement(_miamManagement, new Namespace(),
              true, out mediaItemIdAlias, out valueAlias);
        }
        else
        {
          QueryAttribute selectAttributeQA = new QueryAttribute(_selectAttribute);
          MainQueryBuilder builder = new MainQueryBuilder(new List<MediaItemAspectMetadata>(),
              new QueryAttribute[] {selectAttributeQA}, _filter);
          Namespace ns = new Namespace();
          string mediaItemIdAlias;
          IDictionary<MediaItemAspectMetadata, string> miamAliases;
          IDictionary<QueryAttribute, CompiledQueryAttribute> qa2cqa;
          command.CommandText = builder.GenerateSqlStatement(_miamManagement, ns, true, out mediaItemIdAlias,
              out miamAliases, out qa2cqa);
          valueAlias = qa2cqa[selectAttributeQA].GetAlias(ns);
        }

        IDataReader reader = command.ExecuteReader();
        ICollection<object> result = new List<object>();
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
