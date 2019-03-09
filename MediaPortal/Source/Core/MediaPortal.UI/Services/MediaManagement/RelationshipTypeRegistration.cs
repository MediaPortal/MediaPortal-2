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

using MediaPortal.Common.MediaManagement;
using System.Collections.Generic;
using System.Linq;

namespace MediaPortal.UI.Services.MediaManagement
{
  /// <summary>
  /// Relationship type registration class for the MediaPortal client. Stores all registered relationship types
  /// and automatically registers them at the connected server.
  /// </summary>
  public class RelationshipTypeRegistration : IRelationshipTypeRegistration
  {
    protected object _syncObj = new object();

    protected ICollection<RelationshipType> _locallyKnownRelationshipTypes =
        new List<RelationshipType>();

    protected ICollection<RelationshipType> _locallyKnownHierarchicalRelationshipTypes =
        new List<RelationshipType>();

    public ICollection<RelationshipType> LocallyKnownRelationshipTypes
    {
      get
      {
        lock (_syncObj)
          return new List<RelationshipType>(_locallyKnownRelationshipTypes);
      }
    }

    public ICollection<RelationshipType> LocallyKnownHierarchicalRelationshipTypes
    {
      get
      {
        lock (_syncObj)
          return new List<RelationshipType>(_locallyKnownHierarchicalRelationshipTypes);
      }
    }

    public void RegisterLocallyKnownRelationshipType(RelationshipType relationshipType, bool isChildPrimaryResource)
    {
      lock (_syncObj)
      {
        if (_locallyKnownRelationshipTypes.Any(r => r.ChildRole == relationshipType.ChildRole && r.ParentRole == relationshipType.ParentRole))
          return;

        _locallyKnownRelationshipTypes.Add(relationshipType);
        if (relationshipType.IsHierarchical)
          _locallyKnownHierarchicalRelationshipTypes.Add(relationshipType);
      }

      //TODO: Register on server
    }
  }
}
