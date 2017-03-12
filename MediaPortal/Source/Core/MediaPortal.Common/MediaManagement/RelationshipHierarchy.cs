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

namespace MediaPortal.Common.MediaManagement
{
  /// <summary>
  /// Holds all metadata for a the relationship extractor specified by the <see cref="IRelationshipExtractor"/>.
  /// </summary>
  public class RelationshipHierarchy
  {
    public RelationshipHierarchy(Guid childRole, MediaItemAspectMetadata.AttributeSpecification childCountAttribute, 
      Guid parentRole, MediaItemAspectMetadata.AttributeSpecification parentCountAttribute, bool updatePlayPercentage)
    {
      ChildRole = childRole;
      ChildCountAttribute = childCountAttribute;
      ParentRole = parentRole;
      ParentCountAttribute = parentCountAttribute;
      UpdatePlayPercentage = updatePlayPercentage;
    }

    /// <summary>
    /// Role of the child in the hierarchy.
    /// </summary>
    public Guid ChildRole { get; private set; }

    /// <summary>
    /// Role of the parent in the hierarchy.
    /// </summary>
    public Guid ParentRole { get; private set; }

    /// <summary>
    /// Specifies the child attribute to use to count the number of available children.
    /// </summary>
    public MediaItemAspectMetadata.AttributeSpecification ChildCountAttribute { get; private set; }

    /// <summary>
    /// Specifies the parent attribute to update with the number of available children.
    /// </summary>
    public MediaItemAspectMetadata.AttributeSpecification ParentCountAttribute { get; private set; }

    /// <summary>
    /// Specifies if the parent play percentage should be updated after children playback.
    /// </summary>
    public bool UpdatePlayPercentage { get; private set; }
  }
}
