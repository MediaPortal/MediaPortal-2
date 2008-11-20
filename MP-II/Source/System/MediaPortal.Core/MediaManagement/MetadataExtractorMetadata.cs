#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;

namespace MediaPortal.Core.MediaManagement
{
  /// <summary>
  /// Holds all metadata for a the metadata extractor specified by the <see cref="MetadataExtractorId"/>.
  /// </summary>
  /// <remarks>
  /// Every metadata extractor has to declare the <see cref="MediaItemAspect"/>s of its output.
  /// </remarks>
  public class MetadataExtractorMetadata
  {
    #region Protected fields

    protected Guid _metadataExtractorId;
    protected string _name;
    protected ICollection<string> _shareCategories;
    protected ICollection<MediaItemAspectMetadata> _extractedAspectTypes;

    #endregion

    public MetadataExtractorMetadata(Guid metadataExtractorId, string name,
        IEnumerable<string> shareCategories, IEnumerable<MediaItemAspectMetadata> extractedAspectTypes)
    {
      _metadataExtractorId = metadataExtractorId;
      _name = name;
      _shareCategories = new List<string>(shareCategories);
      _extractedAspectTypes = new List<MediaItemAspectMetadata>(extractedAspectTypes);
    }

    /// <summary>
    /// GUID which uniquely identifies the metadata extractor.
    /// </summary>
    public Guid MetadataExtractorId
    {
      get { return _metadataExtractorId; }
    }

    /// <summary>
    /// Returns a name for the metadata extractor.
    /// </summary>
    public string Name
    {
      get { return _name; }
    }

    /// <summary>
    /// Returns the categories of media items which are supported by the metadata extractor.
    /// </summary>
    /// <remarks>
    /// The categories can be used by the system to classify shares and metadata extractors. The system might
    /// offer all metadata extractors of category "Audio" for shares classified as "Audio", for example.
    /// <br/>
    /// There are default categories which can be taken from the enum <see cref="DefaultMediaCategory"/>,
    /// but also user-defined categories can be returned.
    /// </remarks>
    public ICollection<string> ShareCategories
    {
      get { return _shareCategories; }
    }

    /// <summary>
    /// Returns the format of the metadata which is provided by the extractor.
    /// Every media item aspect whose attributes might be equipped by the metadata extractor
    /// should be defined here. If the ME writes metadata for aspects whose metadata descriptors
    /// aren't returned here, these attributes can be discarded by the system.
    /// </summary>
    public ICollection<MediaItemAspectMetadata> ExtractedAspectTypes
    {
      get { return _extractedAspectTypes; }
    }
  }
}
