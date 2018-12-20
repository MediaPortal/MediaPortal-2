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

using System.Collections.Generic;

namespace MediaPortal.Common.MediaManagement
{
  public interface IRelationshipTypeRegistration
  {
    /// <summary>
    /// Returns all relationship types which were registered in this registration instance.
    /// </summary>
    /// <value>Collection of relationship types.</value>
    ICollection<RelationshipType> LocallyKnownRelationshipTypes { get; }

    /// <summary>
    /// Returns all hierarchical relationship types which were registered in this registration instance.
    /// </summary>
    /// <value>Collection of relationship types.</value>
    ICollection<RelationshipType> LocallyKnownHierarchicalRelationshipTypes { get; }

    /// <summary>
    /// Registration method for all relationship types which are known by the local system. Relationships must either belong
    /// to a primary resource, by specifying <paramref name="isChildPrimaryResource"/>, or the child role must be a parent role
    /// in an existing relationship to ensure that all relationships can be traversed to from a primary resource.
    /// <para>
    /// Each module, which brings in new relationship types, must register them at each system start
    /// (or at least before working with them). Only relationships that are registered will be stored in the database and 
    /// relationships will only be stored in the media item with the child role.
    /// </para>
    /// </summary>
    /// <param name="relationshipType">Relationship type to register.</param>
    /// <param name="isChildPrimaryResource">Whether the child role represents a primary resource (usually a physical file on disk).</param>
    void RegisterLocallyKnownRelationshipType(RelationshipType relationshipType, bool isChildPrimaryResource);
  }
}
