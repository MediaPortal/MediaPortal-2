#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using MediaPortal.Common.MediaManagement.Helpers;
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

    /// <summary>
    /// Tries to create a list of objects derived from <see cref="BaseInfo"/> from the
    /// specified enumeration of <see cref="MediaItemAspect"/> dictionaries.
    /// </summary>
    /// <typeparam name="T">The type of the objects to create.</typeparam>
    /// <param name="aspects">Enumeration of <see cref="MediaItemAspect"/> dictionaries.</param>
    /// <param name="infos">If successful, a list of objects created from the <see cref="MediaItemAspect"/> dictionaries.</param>
    /// <returns><c>true</c> if the objects were successfully created.</returns>
    public static bool TryCreateInfoFromLinkedAspects<T>(IEnumerable<IDictionary<Guid, IList<MediaItemAspect>>> aspects, out List<T> infos) where T : BaseInfo
    {
      infos = null;
      if (aspects == null)
        return false;
      foreach (IDictionary<Guid, IList<MediaItemAspect>> aspect in aspects)
      {
        T info = Activator.CreateInstance<T>();
        if (!info.FromLinkedMetadata(aspect))
          continue;
        if (infos == null)
          infos = new List<T>();
        infos.Add(info);
      }
      return infos != null && infos.Count > 0;
    }

    /// <summary>
    /// Tries to get the id of the first relation with the specified linked role
    /// from the <see cref="RelationshipAspect"/>s contained in a <see cref="MediaItemAspect"/> dictionary.
    /// </summary>
    /// <param name="linkedRole">The role of the relation.</param>
    /// <param name="aspects"><see cref="MediaItemAspect"/> dictionary containing the <see cref="RelationshipAspect"/>s.</param>
    /// <param name="linkedId">If successful, the id of the first relation with the specified linked role.</param>
    /// <returns>True, if a relation with the specified linked role was found.</returns>
    public static bool TryGetLinkedId(Guid linkedRole, IDictionary<Guid, IList<MediaItemAspect>> aspects, out Guid linkedId)
    {
      IList<MediaItemAspect> relationshipAspects;
      if (aspects.TryGetValue(RelationshipAspect.ASPECT_ID, out relationshipAspects))
        return TryGetLinkedId(linkedRole, relationshipAspects, out linkedId);
      linkedId = Guid.Empty;
      return false;
    }

    /// <summary>
    /// Tries to get the id of the first relation with the specified linked role
    /// in the specified enumeration of <see cref="RelationshipAspect"/>s.
    /// </summary>
    /// <param name="linkedRole">The role of the relation.</param>
    /// <param name="relationshipAspects">Enumeration of <see cref="RelationshipAspect"/>s.</param>
    /// <param name="linkedId">If successful, the id of the first relation with the specified linked role.</param>
    /// <returns>True, if a relation with the specified linked role was found.</returns>
    public static bool TryGetLinkedId(Guid linkedRole, IEnumerable<MediaItemAspect> relationshipAspects, out Guid linkedId)
    {
      foreach (MediaItemAspect aspect in relationshipAspects)
        if (aspect.GetAttributeValue<Guid?>(RelationshipAspect.ATTR_LINKED_ROLE) == linkedRole)
        {
          Guid? possibleLinkedId = aspect.GetAttributeValue<Guid?>(RelationshipAspect.ATTR_LINKED_ID);
          if (possibleLinkedId.HasValue)
          {
            linkedId = possibleLinkedId.Value;
            return true;
          }
        }
      linkedId = Guid.Empty;
      return false;
    }

    /// <summary>
    /// Tries to get the ids of all relations with the specified linked role, sorted by relationship index,
    /// from the <see cref="RelationshipAspect"/>s contained in a <see cref="MediaItemAspect"/> dictionary.
    /// </summary>
    /// <param name="linkedRole">The role of the relations.</param>
    /// <param name="aspects"><see cref="MediaItemAspect"/> dictionary containing the <see cref="RelationshipAspect"/>s.</param>
    /// <param name="linkedIds">If successful, a list of ids of all relations with the specified linked role, sorted by relationship index.</param>
    /// <returns>True, if relations with the specified linked role were found.</returns>
    public static bool TryGetLinkedIds(Guid linkedRole, IDictionary<Guid, IList<MediaItemAspect>> aspects, out IList<Guid> linkedIds)
    {
      IList<MediaItemAspect> relationshipAspects;
      if (aspects.TryGetValue(RelationshipAspect.ASPECT_ID, out relationshipAspects))
        return TryGetLinkedIds(linkedRole, relationshipAspects, out linkedIds);
      linkedIds = null;
      return false;
    }

    /// <summary>
    /// Tries to get the ids of all relations with the specified linked role, sorted by relationship index,
    /// from the specified enumeration of <see cref="RelationshipAspect"/>s.
    /// </summary>
    /// <param name="linkedRole">The role of the relations.</param>
    /// <param name="relationshipAspects">Enumeration of <see cref="RelationshipAspect"/>s.</param>
    /// <param name="linkedIds">If successful, a list of ids of all relations with the specified linked role, sorted by relationship index.</param>
    /// <returns>True, if relations with the specified linked role were found.</returns>
    public static bool TryGetLinkedIds(Guid linkedRole, IEnumerable<MediaItemAspect> relationshipAspects, out IList<Guid> linkedIds)
    {
      List<Tuple<Guid, int>> roleAspects = new List<Tuple<Guid, int>>();
      foreach (MediaItemAspect aspect in relationshipAspects)
        if (aspect.GetAttributeValue<Guid?>(RelationshipAspect.ATTR_LINKED_ROLE) == linkedRole)
        {
          Guid? linkedId = aspect.GetAttributeValue<Guid?>(RelationshipAspect.ATTR_LINKED_ID);
          if (linkedId.HasValue)
          {
            int? index = aspect.GetAttributeValue<int?>(RelationshipAspect.ATTR_RELATIONSHIP_INDEX);
            roleAspects.Add(new Tuple<Guid, int>(linkedId.Value, index.HasValue ? index.Value : 0));
          }
        }

      if (roleAspects.Count > 0)
      {
        linkedIds = roleAspects.OrderBy(a => a.Item2).Select(a => a.Item1).ToList();
        return true;
      }
      linkedIds = null;
      return false;
    }

    /// <summary>
    /// Tries to get a map of the ids of all relations with the specified linked role and the values
    /// specified in <paramref name="valuesToMap"/>, mapped by indes, from the specified <see cref="MediaItemAspect"/> dictionary.
    /// </summary>
    /// <param name="linkedRole">The role of the relations.</param>
    /// <param name="aspects"><see cref="MediaItemAspect"/> dictionary containing the <see cref="RelationshipAspect"/>s.</param>
    /// <param name="valuesToMap">List of values that should be mapped to the linked ids</param>
    /// <param name="mappedLinkedIds">If successful, a list of Tuples of all relations with the specified linked role and the corresponding value.</param>
    /// <returns>True, if relations with the specified linked role were found.</returns>
    public static bool TryGetMappedLinkedIds<T>(Guid linkedRole, IDictionary<Guid, IList<MediaItemAspect>> aspects, IList<T> valuesToMap, out IList<Tuple<Guid, T>> mappedLinkedIds)
    {
      IList<MediaItemAspect> relationshipAspects;
      if (aspects.TryGetValue(RelationshipAspect.ASPECT_ID, out relationshipAspects))
        return TryGetMappedLinkedIds(linkedRole, relationshipAspects, valuesToMap, out mappedLinkedIds);
      mappedLinkedIds = null;
      return false;
    }

    /// <summary>
    /// Tries to get a map of the ids of all relations with the specified linked role and the values
    /// specified in <paramref name="valuesToMap"/>, mapped by indes, from the specified enumeration of <see cref="RelationshipAspect"/>s.
    /// </summary>
    /// <param name="linkedRole">The role of the relations.</param>
    /// <param name="relationshipAspects">Enumeration of <see cref="RelationshipAspect"/>s.</param>
    /// <param name="valuesToMap">List of values that should be mapped to the linked ids</param>
    /// <param name="mappedLinkedIds">If successful, a list of Tuples of all relations with the specified linked role and the corresponding value.</param>
    /// <returns>True, if relations with the specified linked role were found.</returns>
    public static bool TryGetMappedLinkedIds<T>(Guid linkedRole, IEnumerable<MediaItemAspect> relationshipAspects, IList<T> valuesToMap, out IList<Tuple<Guid, T>> mappedLinkedIds)
    {
      IList<Guid> linkedIds;
      if (!TryGetLinkedIds(linkedRole, relationshipAspects, out linkedIds))
      {
        mappedLinkedIds = null;
        return false;
      }

      mappedLinkedIds = new List<Tuple<Guid, T>>();
      int i = 0;
      while (i < linkedIds.Count && i < valuesToMap.Count)
      {
        mappedLinkedIds.Add(new Tuple<Guid, T>(linkedIds[i], valuesToMap[i]));
        i++;
      }
      return true;
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
