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
  /// Holds all metadata for a the relationship extractor specified by the <see cref="IRelationshipExtractor"/>.
  /// </summary>
  public class RelationshipExtractorMetadata
  {
    #region Protected fields

    protected Guid _relationshipExtractorId;
    protected string _name;
    MetadataExtractorPriority _priority;

    #endregion

    public RelationshipExtractorMetadata(Guid relationshipExtractorId, string name, MetadataExtractorPriority priority)
    {
      _relationshipExtractorId = relationshipExtractorId;
      _name = name;
      _priority = priority;
    }

    /// <summary>
    /// GUID which uniquely identifies the media item builder
    /// </summary>
    public Guid RelationshipExtractorId
    {
      get { return _relationshipExtractorId; }
    }

    /// <summary>
    /// Returns a name for the relationship metadata extractor.
    /// </summary>
    public string Name
    {
      get { return _name; }
    }

    /// <summary>
    /// Returns the priority of the relationship extractor.
    /// </summary>
    public MetadataExtractorPriority Priority
    {
      get { return _priority; }
    }
  }
}
