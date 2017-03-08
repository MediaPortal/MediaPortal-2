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
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;

namespace MediaPortal.Mock
{
  internal class RelationshipLookup : IRelationshipRoleExtractor
  {
    public Guid Role { get; set; }
    public Guid[] RoleAspects { get; set; }
    public Guid LinkedRole { get; set; }
    public Guid[] LinkedRoleAspects { get; set; }
    public Guid[] MatchAspects { get; set; }

    public bool TryExtractRelationships(IDictionary<Guid, IList<MediaItemAspect>> aspects, out IDictionary<IDictionary<Guid, IList<MediaItemAspect>>, Guid> extractedLinkedAspects, bool importOnly)
    {
      string id;
      MockCore.ShowMediaAspects(aspects, MockCore.Library.GetManagedMediaItemAspectMetadata());
      if (MediaItemAspect.TryGetExternalAttribute(aspects, ExternalSource, ExternalType, out id) && ExternalId == id)
      {
        ServiceRegistration.Get<ILogger>().Debug("Matched {0} / {1} / {2} / {3} / {4}", Role, LinkedRole, ExternalSource, ExternalType, ExternalId);
        extractedLinkedAspects = new Dictionary<IDictionary<Guid, IList<MediaItemAspect>>, Guid>();
        foreach (IDictionary<Guid, IList<MediaItemAspect>> data in Data)
        {
          extractedLinkedAspects.Add(data, Guid.Empty);
        }
        return true;
      }
      ServiceRegistration.Get<ILogger>().Debug("No match for {0} / {1} / {2} / {3} / {4}", Role, LinkedRole, ExternalSource, ExternalType, ExternalId);

      extractedLinkedAspects = null;
      return false;
    }

    public bool TryMatch(IDictionary<Guid, IList<MediaItemAspect>> extractedAspects, IDictionary<Guid, IList<MediaItemAspect>> existingAspects)
    {
      if (Matcher == null)
        return true;

      return Matcher(extractedAspects, existingAspects);
    }

    public bool TryGetRelationshipIndex(IDictionary<Guid, IList<MediaItemAspect>> aspects, IDictionary<Guid, IList<MediaItemAspect>> linkedAspects, out int index)
    {
      index = Index;
      return true;
    }

    public string ExternalSource { get; set; }
    public string ExternalType { get; set; }
    public string ExternalId { get; set; }

    public ICollection<IDictionary<Guid, IList<MediaItemAspect>>> Data { get; set; }

    public Func<IDictionary<Guid, IList<MediaItemAspect>>, IDictionary<Guid, IList<MediaItemAspect>>, bool> Matcher { get; set; }

    public int Index { get; set; }

    public bool BuildRelationship
    {
      get
      {
        return true;
      }
    }

    public IFilter GetSearchFilter(IDictionary<Guid, IList<MediaItemAspect>> extractedAspects)
    {
      List<IFilter> searchFilters = new List<IFilter>();
      IList<MultipleMediaItemAspect> externalAspects;
      if (MediaItemAspect.TryGetAspects(extractedAspects, ExternalIdentifierAspect.Metadata, out externalAspects))
      {
        foreach (MultipleMediaItemAspect externalAspect in externalAspects)
        {
          string source = externalAspect.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_SOURCE);
          string type = externalAspect.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_TYPE);
          string id = externalAspect.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_ID);
          if (searchFilters.Count == 0)
          {
            searchFilters.Add(new BooleanCombinationFilter(BooleanOperator.And, new[]
            {
                new RelationalFilter(ExternalIdentifierAspect.ATTR_SOURCE, RelationalOperator.EQ, source),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_TYPE, RelationalOperator.EQ, type),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_ID, RelationalOperator.EQ, id),
              }));
          }
          else
          {
            searchFilters[0] = BooleanCombinationFilter.CombineFilters(BooleanOperator.Or, searchFilters[0],
            new BooleanCombinationFilter(BooleanOperator.And, new[]
            {
                new RelationalFilter(ExternalIdentifierAspect.ATTR_SOURCE, RelationalOperator.EQ, source),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_TYPE, RelationalOperator.EQ, type),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_ID, RelationalOperator.EQ, id),
            }));
          }
        }
      }
      return BooleanCombinationFilter.CombineFilters(BooleanOperator.Or, searchFilters.ToArray());
    }

    public void CacheExtractedItem(Guid extractedItemId, IDictionary<Guid, IList<MediaItemAspect>> extractedAspects)
    {
    }
  }

  public class MockRelationshipExtractor : IRelationshipExtractor
  {
    private readonly IList<RelationshipLookup> _lookups = new List<RelationshipLookup>();

    private static readonly RelationshipExtractorMetadata METADATA = new RelationshipExtractorMetadata(Guid.Empty, "MockRelationshipExtractor");

    public RelationshipExtractorMetadata Metadata
    {
      get { return METADATA; }
    }

    public IList<IRelationshipRoleExtractor> RoleExtractors
    {
      get { return _lookups.Cast<IRelationshipRoleExtractor>().ToList(); }
    }

    public IList<RelationshipHierarchy> Hierarchies
    {
      get
      {
        return null;
      }
    }

    public void AddRelationship(
      Guid role, Guid[] roleAspectIds, Guid linkedRole, Guid[] linkedRoleAspectIds, Guid[] matchAspectIds,
      string source, string type, string id, 
      ICollection<IDictionary<Guid, IList<MediaItemAspect>>> extractedAspectData, Func<IDictionary<Guid, IList<MediaItemAspect>>, IDictionary<Guid, IList<MediaItemAspect>>, bool> matcher,
      int index)
    {
      _lookups.Add(new RelationshipLookup()
      {
        Role = role,
        RoleAspects = roleAspectIds,
        LinkedRole = linkedRole,
        LinkedRoleAspects = linkedRoleAspectIds,
        MatchAspects = matchAspectIds,
        ExternalSource = source,
        ExternalType = type,
        ExternalId = id,

        Data = extractedAspectData,
        Index = index,
      });
    }

    public IDictionary<IFilter, uint> GetLastChangedItemsFilters()
    {
      return null;
    }

    public void ResetLastChangedItems()
    {
    }
  }
}
