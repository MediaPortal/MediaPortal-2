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
using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Backend.Database;
using MediaPortal.Utilities.Exceptions;

namespace MediaPortal.Backend.Services.MediaLibrary.QueryEngine
{
  /// <summary>
  /// Creates an SQL query for evaluating the count of media items which contain the given necessary MIA types and which match
  /// the given filter.
  /// </summary>
  public class CompiledCountItemsQuery
  {
    protected readonly MIA_Management _miaManagement;
    protected readonly IEnumerable<MediaItemAspectMetadata> _necessaryRequestedMIATypes;
    protected readonly IFilter _filter;

    public CompiledCountItemsQuery(MIA_Management miaManagement, IEnumerable<MediaItemAspectMetadata> necessaryRequestedMIATypes, IFilter filter)
    {
      _miaManagement = miaManagement;
      _necessaryRequestedMIATypes = necessaryRequestedMIATypes;
      _filter = filter;
    }

    public IFilter Filter
    {
      get { return _filter; }
    }

    public static CompiledCountItemsQuery Compile(MIA_Management miaManagement,
        IEnumerable<Guid> necessaryRequestedMIATypeIDs, IFilter filter)
    {
      IDictionary<Guid, MediaItemAspectMetadata> availableMIATypes = miaManagement.ManagedMediaItemAspectTypes;
      ICollection<MediaItemAspectMetadata> necessaryMIATypes = new List<MediaItemAspectMetadata>();
      // Raise exception if necessary MIA types are not present
      foreach (Guid miaTypeID in necessaryRequestedMIATypeIDs)
      {
        MediaItemAspectMetadata miam;
        if (!availableMIATypes.TryGetValue(miaTypeID, out miam))
          throw new InvalidDataException("Necessary requested MIA type of ID '{0}' is not present in the media library", miaTypeID);
        necessaryMIATypes.Add(miam);
      }
      return new CompiledCountItemsQuery(miaManagement, necessaryMIATypes, filter);
    }

    public int Execute()
    {
      ISQLDatabase database = ServiceRegistration.Get<ISQLDatabase>();
      ITransaction transaction = database.CreateTransaction();
      try
      {
        using (IDbCommand command = transaction.CreateCommand())
        {
          string countAlias;
          string statementStr;
          IList<BindVar> bindVars;
          MIAQueryBuilder builder = new MIAQueryBuilder(_miaManagement, new QueryAttribute[] { }, null,
              _necessaryRequestedMIATypes, new MediaItemAspectMetadata[] {}, _filter, null);
          IDictionary<QueryAttribute, string> qa2a;
          builder.GenerateSqlGroupByStatement(out countAlias, out qa2a, out statementStr, out bindVars);

          command.CommandText = statementStr;
          foreach (BindVar bindVar in bindVars)
            database.AddParameter(command, bindVar.Name, bindVar.Value, bindVar.VariableType);

          return Convert.ToInt32(command.ExecuteScalar());
        }
      }
      finally
      {
        transaction.Dispose();
      }
    }
  }
}
