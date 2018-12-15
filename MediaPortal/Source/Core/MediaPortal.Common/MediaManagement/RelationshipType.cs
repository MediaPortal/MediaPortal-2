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

using System;

namespace MediaPortal.Common.MediaManagement
{
  /// <summary>
  /// Holds all metadata for the relationship extractor specified by the <see cref="IRelationshipExtractor"/>.
  /// </summary>
  public class RelationshipType
  {
    public RelationshipType(string name, Guid childRole, Guid parentRole)
      : this(name, false, childRole, parentRole, Guid.Empty, Guid.Empty, null, null, false)
    {
    }

    public RelationshipType(string name, bool isPrimaryParent,
      Guid childRole, Guid parentRole,
      Guid childAspectId, Guid parentAspectId,
      MediaItemAspectMetadata.AttributeSpecification childCountAttribute, MediaItemAspectMetadata.SingleAttributeSpecification parentCountAttribute,
      bool updatePlayPercentage)
    {
      Name = name;
      IsPrimaryParent = isPrimaryParent;
      ChildAspectId = childAspectId;
      ParentAspectId = parentAspectId;
      ChildRole = childRole;
      ParentRole = parentRole;
      ChildCountAttribute = childCountAttribute;
      ParentCountAttribute = parentCountAttribute;
      UpdatePlayPercentage = updatePlayPercentage;
    }

    /// <summary>
    /// Name for the relationship. Mainly used for troubleshooting and logging.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Whether this relationship is hierarchical.
    /// <para>
    /// A hierarchical relationship is a relationship where the parent items maintain a count, playback and virtual
    /// state depending on the state of their children.
    /// </para>
    /// </summary>
    public bool IsHierarchical
    {
      get { return ChildCountAttribute != null && ParentCountAttribute != null; }
    }

    /// <summary>
    /// If this is a hierarchical relationship, whether this relationship represents the primary relationship in a relationship tree.
    /// <para>
    /// Media items will only be deleted from the database if the primary parent does not have any non virtual children, otherwise only
    /// the ProviderResourceAspect will be deleted and the affected media items will be marked as virtual.
    /// </para>
    /// </summary>
    public bool IsPrimaryParent { get; private set; }

    /// <summary>
    /// Role of the child in the hierarchy.
    /// </summary>
    public Guid ChildRole { get; private set; }

    /// <summary>
    /// Role of the parent in the hierarchy.
    /// </summary>
    public Guid ParentRole { get; private set; }

    /// <summary>
    /// Aspect Id of the child aspect.
    /// </summary>
    public Guid ChildAspectId { get; private set; }

    /// <summary>
    /// Aspect Id of the parent aspect.
    /// </summary>
    public Guid ParentAspectId { get; private set; }

    /// <summary>
    /// Specifies the child attribute to use to count the number of available children.
    /// </summary>
    public MediaItemAspectMetadata.AttributeSpecification ChildCountAttribute { get; private set; }

    /// <summary>
    /// Specifies the parent attribute to update with the number of available children.
    /// </summary>
    public MediaItemAspectMetadata.SingleAttributeSpecification ParentCountAttribute { get; private set; }

    /// <summary>
    /// Specifies if the parent play percentage should be updated after children playback.
    /// </summary>
    public bool UpdatePlayPercentage { get; private set; }
  }
}
