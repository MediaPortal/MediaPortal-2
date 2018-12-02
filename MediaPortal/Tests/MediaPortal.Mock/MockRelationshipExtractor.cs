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

using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.ResourceAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MediaPortal.Mock
{
  internal class RelationshipLookup : IRelationshipRoleExtractor
  {
    public Guid Role { get; set; }
    public Guid[] RoleAspects { get; set; }
    public Guid LinkedRole { get; set; }
    public Guid[] LinkedRoleAspects { get; set; }
    public Guid[] MatchAspects { get; set; }

    public Task<bool> TryExtractRelationshipsAsync(IResourceAccessor mediaItemAccessor, IDictionary<Guid, IList<MediaItemAspect>> aspects, IList<IDictionary<Guid, IList<MediaItemAspect>>> extractedLinkedAspects)
    {
      string id;
      MockCore.ShowMediaAspects(aspects, MockCore.Library.GetManagedMediaItemAspectMetadata());
      if (MediaItemAspect.TryGetExternalAttribute(aspects, ExternalSource, ExternalType, out id) && ExternalId == id)
      {
        ServiceRegistration.Get<ILogger>().Debug("Matched {0} / {1} / {2} / {3} / {4}", Role, LinkedRole, ExternalSource, ExternalType, ExternalId);
        foreach (IDictionary<Guid, IList<MediaItemAspect>> data in Data)
        {
          extractedLinkedAspects.Add(data);
        }
        return Task.FromResult(true);
      }
      ServiceRegistration.Get<ILogger>().Debug("No match for {0} / {1} / {2} / {3} / {4}", Role, LinkedRole, ExternalSource, ExternalType, ExternalId);

      return Task.FromResult(false);
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
          searchFilters.Add(new BooleanCombinationFilter(BooleanOperator.And, new[]
          {
            new RelationalFilter(ExternalIdentifierAspect.ATTR_SOURCE, RelationalOperator.EQ, source),
            new RelationalFilter(ExternalIdentifierAspect.ATTR_TYPE, RelationalOperator.EQ, type),
            new RelationalFilter(ExternalIdentifierAspect.ATTR_ID, RelationalOperator.EQ, id)
          }));
        }
      }
      return BooleanCombinationFilter.CombineFilters(BooleanOperator.Or, searchFilters);
    }

    public ICollection<string> GetExternalIdentifiers(IDictionary<Guid, IList<MediaItemAspect>> extractedAspects)
    {
      ICollection<string> identifiers = new List<string>();
      IList<MultipleMediaItemAspect> externalAspects;
      if (MediaItemAspect.TryGetAspects(extractedAspects, ExternalIdentifierAspect.Metadata, out externalAspects))
      {
        foreach (MultipleMediaItemAspect externalAspect in externalAspects)
        {
          string source = externalAspect.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_SOURCE);
          string type = externalAspect.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_TYPE);
          string id = externalAspect.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_ID);
          identifiers.Add(string.Format("{0} | {1} | {2}", source, type, id));
        }
      }
      return identifiers;
    }
  }

  public class MockRelationshipExtractor : IRelationshipExtractor
  {
    private readonly IList<RelationshipLookup> _lookups = new List<RelationshipLookup>();

    private static readonly RelationshipExtractorMetadata METADATA = new RelationshipExtractorMetadata(Guid.Empty, "MockRelationshipExtractor", MetadataExtractorPriority.Core);

    public RelationshipExtractorMetadata Metadata
    {
      get { return METADATA; }
    }

    public IList<IRelationshipRoleExtractor> RoleExtractors
    {
      get { return _lookups.Cast<IRelationshipRoleExtractor>().ToList(); }
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

    public IDictionary<Guid, IList<MediaItemAspect>> GetBaseChildAspectsFromExistingAspects(IDictionary<Guid, IList<MediaItemAspect>> existingChildAspects, IDictionary<Guid, IList<MediaItemAspect>> existingParentAspects)
    {
      return null;
    }
  }
}
