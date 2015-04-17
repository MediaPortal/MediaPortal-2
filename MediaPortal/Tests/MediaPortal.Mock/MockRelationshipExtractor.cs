#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.Services.Logging;

namespace MediaPortal.Mock
{
  internal class RelationshipLookup
  {
    public Guid Role { get; set; }
    public Guid LinkedRole { get; set; }
    public string ExternalSource { get; set; }
    public string ExternalType { get; set; }
    public string ExternalId { get; set; }

    public ICollection<IDictionary<Guid, IList<MediaItemAspect>>> Data { get; set; }
  }

  public class MockRelationshipExtractor : IRelationshipExtractor
  {
    private IList<RelationshipLookup> lookups = new List<RelationshipLookup>();
    private static readonly RelationshipExtractorMetadata _METADATA = new RelationshipExtractorMetadata(Guid.Empty, "MockRelationshipExtractor");

    public RelationshipExtractorMetadata Metadata
    {
      get { return _METADATA; }
    }

    public void AddRelationship(Guid role, Guid linkedRole, string source, string type, string id, ICollection<IDictionary<Guid, IList<MediaItemAspect>>> extractedAspectData)
    {
      lookups.Add(new RelationshipLookup()
      {
        Role = role,
        LinkedRole = linkedRole,
        ExternalSource = source,
        ExternalType = type,
        ExternalId = id,

        Data = extractedAspectData
      });
    }

    public bool TryExtractRelationships(IDictionary<Guid, IList<MediaItemAspect>> aspects, Guid role, Guid linkedRole, out ICollection<IDictionary<Guid, IList<MediaItemAspect>>> extractedAspectData, bool forceQuickMode)
    {
      ServiceRegistration.Get<ILogger>().Debug("Extracting {0} / {1}", role, linkedRole);

      foreach(RelationshipLookup lookup in lookups)
      {
        string id = null;
        ServiceRegistration.Get<ILogger>().Debug("Checking {0} / {1} / {2} / {3} / {4}", lookup.Role, lookup.LinkedRole, lookup.ExternalSource, lookup.ExternalType, lookup.ExternalId);
        if (lookup.Role == role && lookup.LinkedRole == linkedRole && MediaItemAspect.TryGetExternalAttribute(aspects, lookup.ExternalSource, lookup.ExternalType, out id) && lookup.ExternalId == id)
        {
          ServiceRegistration.Get<ILogger>().Debug("Matched");
          extractedAspectData = lookup.Data;
          return true;
        }
        else
        {
          ServiceRegistration.Get<ILogger>().Debug("No match for {0}", id);
        }
      }

      extractedAspectData = null;

      return false;
    }
  }
}