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

using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaPortal.Common.MediaManagement
{
  public static class RelationshipExtractorUtils
  {
    /// <summary>
    /// Creates a filter that can be used to search for media items that have an <see cref="ExternalIdentifierAspect"/> of the specified
    /// <paramref name="externalIdType"/> that are present in <paramref name="aspects"/>.
    /// </summary>
    /// <param name="aspects"></param>
    /// <param name="externalIdType"></param>
    /// <returns></returns>
    public static IFilter CreateExternalItemFilter(IDictionary<Guid, IList<MediaItemAspect>> aspects, string externalIdType)
    {
      IEnumerable<MultipleMediaItemAspect> externalAspects = GetExternalIdentifierAspectsForType(aspects, externalIdType);
      if (externalAspects == null)
        return null;

      IList<IFilter> filters = new List<IFilter>();
      foreach (MultipleMediaItemAspect externalAspect in externalAspects)
      {
        string source = externalAspect.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_SOURCE);
        string id = externalAspect.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_ID);
        filters.Add(new BooleanCombinationFilter(BooleanOperator.And, new[]
        {
          new RelationalFilter(ExternalIdentifierAspect.ATTR_SOURCE, RelationalOperator.EQ, source),
          new RelationalFilter(ExternalIdentifierAspect.ATTR_TYPE, RelationalOperator.EQ, externalIdType),
          new RelationalFilter(ExternalIdentifierAspect.ATTR_ID, RelationalOperator.EQ, id)
        }));
      }
      return filters.Count > 0 ? BooleanCombinationFilter.CombineFilters(BooleanOperator.Or, filters) : null;
    }

    /// <summary>
    /// Gets a collection of strings that can be used to identify the <see cref="ExternalIdentifierAspect"/>s with the specified
    /// <paramref name="externalIdType"/> that are present in <paramref name="aspects"/>.
    /// </summary>
    /// <param name="aspects"></param>
    /// <param name="externalIdType"></param>
    /// <returns></returns>
    public static ICollection<string> CreateExternalItemIdentifiers(IDictionary<Guid, IList<MediaItemAspect>> aspects, string externalIdType)
    {
      IEnumerable<MultipleMediaItemAspect> externalAspects = GetExternalIdentifierAspectsForType(aspects, externalIdType);
      if (externalAspects == null)
        return new List<string>();
      return externalAspects.Select(a => GetIdentifier(a, externalIdType)).ToList();
    }

    private static IEnumerable<MultipleMediaItemAspect> GetExternalIdentifierAspectsForType(IDictionary<Guid, IList<MediaItemAspect>> aspects, string externalIdType)
    {
      IList<MultipleMediaItemAspect> externalAspects;
      if (!MediaItemAspect.TryGetAspects(aspects, ExternalIdentifierAspect.Metadata, out externalAspects))
        return null;
      return externalAspects.Where(a => a.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_TYPE) == externalIdType);
    }

    private static string GetIdentifier(MultipleMediaItemAspect externalAspect, string externalIdType)
    {
      string source = externalAspect.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_SOURCE);
      string id = externalAspect.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_ID);
      return string.Format("{0} | {1} | {2}", source, externalIdType, id);
    }
  }
}
