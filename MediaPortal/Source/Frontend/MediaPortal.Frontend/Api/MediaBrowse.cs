#region Copyright (C) 2007-2026 Team MediaPortal

/*
    Copyright (C) 2007-2026 Team MediaPortal
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
using System.Text.Json.Serialization;

namespace MediaPortal.Frontend.Api
{
  /// <summary>
  /// One folder or one picture inside a folder of the server's files.
  /// </summary>
  public class FolderEntry
  {
    /// <summary>Name to show. A picture has already lost its extension, which is the difference between a file
    /// manager and a picture screen.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>The serialized resource path, which is what the next request is about.</summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>Bytes. Only a picture has one; a folder's size would be a different question.</summary>
    public long? Size { get; set; }

    /// <summary>When it was last changed.</summary>
    public DateTime? LastChanged { get; set; }

    /// <summary>
    /// The line under a name on a tile: how big, or how many.
    /// </summary>
    [JsonIgnore]
    public string Detail
    {
      get
      {
        if (Size == null)
          return "folder";
        long bytes = Size.Value;
        if (bytes < 1024)
          return bytes + " bytes";
        if (bytes < 1024 * 1024)
          return (bytes / 1024) + " KB";
        // A photograph is spoken of with one decimal at most; "12.4 MB" is a line, "12 345 678 bytes" is a
        // complaint, and neither of them is worth reading from three metres away.
        return string.Format("{0:0.#} MB", bytes / (1024.0 * 1024.0));
      }
    }
  }

  /// <summary>
  /// What one folder of the server's files holds.
  /// </summary>
  public class FolderListing
  {
    /// <summary>The folder itself, as a serialized resource path.</summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>Its own name.</summary>
    public string Name { get; set; } = string.Empty;

    public List<FolderEntry> Folders { get; set; } = new List<FolderEntry>();

    public List<FolderEntry> Pictures { get; set; } = new List<FolderEntry>();

    /// <summary>Whether the server stopped counting folders.</summary>
    public bool FoldersTruncated { get; set; }

    /// <summary>Whether the server stopped counting pictures.</summary>
    public bool PicturesTruncated { get; set; }

    /// <summary>Files that are not pictures, counted but not listed.</summary>
    public int OtherFileCount { get; set; }

    /// <summary>
    /// The folder as a person spells it, when it is a folder on a disk; the resource path when it is not.
    /// </summary>
    /// <remarks>
    /// "{local}:///D:/Pictures/2019" is the right thing to send a server and the wrong thing to put on a
    /// television. When the path is not a local folder - a share on another machine, an entry in an archive -
    /// there is no better spelling available, so the resource path is shown rather than nothing.
    /// </remarks>
    [JsonIgnore]
    public string LocalPath
    {
      get
      {
        string? local;
        string? reason;
        return MediaLibraryPath.TryAsLocalFile(Path, out local, out reason) ? local : Path;
      }
    }

    /// <summary>
    /// The line under the folder's name: what is in here, in the words a viewer would use.
    /// </summary>
    [JsonIgnore]
    public string Summary
    {
      get
      {
        List<string> parts = new List<string>();
        if (Pictures.Count > 0)
          parts.Add(Pictures.Count + (PicturesTruncated ? "+" : "") + (Pictures.Count == 1 ? " picture" : " pictures"));
        if (Folders.Count > 0)
          parts.Add(Folders.Count + (FoldersTruncated ? "+" : "") + (Folders.Count == 1 ? " folder" : " folders"));
        if (parts.Count == 0)
          return OtherFileCount > 0
              ? "no pictures here - " + OtherFileCount + (OtherFileCount == 1 ? " file" : " files") + " of something else"
              : "empty";
        if (OtherFileCount > 0)
          parts.Add(OtherFileCount + (OtherFileCount == 1 ? " other file" : " other files"));
        return string.Join(", ", parts.ToArray());
      }
    }
  }
}
