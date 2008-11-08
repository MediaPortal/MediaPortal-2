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

namespace MediaPortal.Media.MediaManagement
{
  /// <summary>
  /// A metadata extractor is responsible for extracting a physical media's metadata from a
  /// physical media file.
  /// </summary>
  public interface IMetadataExtractor
  {
    /// <summary>
    /// Returns a name for this metadata extractor. This name should be unique among all metadata extractors.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// GUID which uniquely identifies this metadata extractor.
    /// </summary>
    Guid GUID { get; }

    /// <summary>
    /// Marks this metadata extractor as a default ME for media files.
    /// </summary>
    /// <remarks>
    /// If set to <c>true</c>, this metadata extractor will be considered to be used as a default
    /// metadata extractor for media files. The exact meaning of this isn't specified here - the system
    /// may decide to automatically assign all "default" metadata extractors to all shares, or it
    /// might offer a list of them to the user to be explicitly assigned to shares.
    /// If set to <c>false</c>, this metadata extractor should not be considered to be automatically
    /// assigned to shares or to be offered to the user to be assigned to shares.
    /// <br/>
    /// Metadata extractors with this property set to <c>false</c> can be managed by the system also
    /// but need to be applied explicitly by program code.
    /// </remarks>
    bool HandlesMedia { get; }

    /// <summary>
    /// Returns the format of the metadata which is provided by this extractor.
    /// </summary>
    IMetadataFormat MetadataFormat { get; }

    /// <summary>
    /// Worker method to actually try a metadata extraction from the <paramref name="provider"/> and
    /// <paramref name="filePath"/>.
    /// If this method returns <c>true</c>, the extracted metadata will have to be written into the specified
    /// <paramref name="writer"/>.
    /// </summary>
    /// <param name="provider">The provider instance to query the physical media with the specified
    /// <paramref name="filePath"/>.</param>
    /// <param name="filePath">The path of the physical media file in the specified <paramref "provider"/>
    /// to process.</param>
    /// <param name="writer">Metadata writer instance to write the metadata to. The metadata must be
    /// written exactly as the format specification returned by <see cref="MetadataFormat"/> defines.</param>
    /// <returns><c>true</c> if the metadata could be extracted from the specified file, else <c>false</c>.
    /// If the return value is <c>true</c>, the metadata were written to the <paramref name="writer"/>
    /// instance.</returns>
    bool TryExtractMetadata(IMediaProvider provider, string filePath, IMetadataWriter writer);
  }
}
