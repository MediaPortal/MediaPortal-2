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

namespace MediaPortal.Common.MediaManagement
{
  /// <summary>
  /// <see cref="MetadataExtractorPriority"/> defines the different levels of <see cref="IMetadataExtractor"/>s in terms of availability,
  /// quantity and quality of information they can extract. Metadata extractors will be run in ascending order from lowest (<see cref="Core"/>) 
  /// to highest (<see cref="External"/>). This way a higher level extractor can work with the already extracted metadata, i.e. to do an 
  /// online lookup.
  /// </summary>
  public enum MetadataExtractorPriority
  {
    /// <summary>
    /// Lowest level for metadata extractors, like detecting audio and video files, reading stream informationen and metadata of files.
    /// </summary>
    Core,
    /// <summary>
    /// Extended level extractors provide additional information, that can be read of additional files (.xml, .nfo, ...).
    /// </summary>
    Extended,
    /// <summary>
    /// External extractors provide information that can be retrieved from online sources (like IMDB, TMDB, TvDB, ...). They can already
    /// use all metadata that were extracted before to do a successful online lookup.
    /// </summary>
    External,
    /// <summary>
    /// Fall back extractors provide information that could not be retrieved from any other source as a last resort.
    /// </summary>
    FallBack,
  }

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
    protected bool _processesNonFiles;
    protected MetadataExtractorPriority _metadataExtractorPriority;
    protected ICollection<MediaCategory> _mediaCategories;
    protected IDictionary<Guid, MediaItemAspectMetadata> _extractedAspectTypes;

    #endregion

    public MetadataExtractorMetadata(Guid metadataExtractorId, string name, MetadataExtractorPriority metadataExtractorPriority, bool processesNonFiles,
        IEnumerable<MediaCategory> shareCategories, IEnumerable<MediaItemAspectMetadata> extractedAspectTypes)
    {
      _metadataExtractorId = metadataExtractorId;
      _name = name;
      _processesNonFiles = processesNonFiles;
      _metadataExtractorPriority = metadataExtractorPriority;
      _mediaCategories = new List<MediaCategory>(shareCategories);
      _extractedAspectTypes = new Dictionary<Guid, MediaItemAspectMetadata>();
      foreach (MediaItemAspectMetadata aspectMetadata in extractedAspectTypes)
        _extractedAspectTypes.Add(aspectMetadata.AspectId, aspectMetadata);
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
    /// Returns the priority of the metadata extractor.
    /// </summary>
    public MetadataExtractorPriority Priority
    {
      get { return _metadataExtractorPriority; }
    }

    /// <summary>
    /// Returns the categories of media items which are supported by the metadata extractor.
    /// </summary>
    /// <remarks>
    /// The categories can be used by the system to classify shares and metadata extractors. The system will
    /// offer all metadata extractors of category "Audio" for shares classified as "Audio", for example.
    /// It will also offer the metadata extractors for parent categories.
    /// <br/>
    /// There are default categories which can be taken from the enum <see cref="DefaultMediaCategories"/>,
    /// but also user-defined categories can be returned.
    /// </remarks>
    public ICollection<MediaCategory> MediaCategories
    {
      get { return _mediaCategories; }
    }

    /// <summary>
    /// Returns the information if the metadata extractor also wants to process resources which are not files.
    /// </summary>
    public bool ProcessesNonFiles
    {
      get { return _processesNonFiles; }
    }

    /// <summary>
    /// Returns the media item aspects which are provided by the extractor.
    /// </summary>
    /// <remarks>
    /// Every media item aspect whose attributes might be equipped by the metadata extractor
    /// should be defined here. If the ME still provides metadata in method <see cref="IMetadataExtractor.TryExtractMetadata"/>
    /// for aspects which aren't returned here, these attributes might be discarded by the system.
    /// </remarks>
    public IDictionary<Guid, MediaItemAspectMetadata> ExtractedAspectTypes
    {
      get { return _extractedAspectTypes; }
    }
  }
}
