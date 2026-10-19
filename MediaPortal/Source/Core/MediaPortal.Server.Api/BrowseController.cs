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
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MediaPortal.Common;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess;

namespace MediaPortal.Server.Api
{
  /// <summary>
  /// The folders and the pictures inside a folder that somebody registered as a share.
  /// </summary>
  /// <remarks>
  /// <para>
  /// <b>Why this exists next to the titles endpoints.</b> <see cref="MediaController"/> answers what the library
  /// <i>knows</i>; this answers what the folders <i>hold</i>, and for photographs those are different questions. A
  /// library is built by an importer that decided what each file is, and a household's picture folders often have
  /// nothing for that decision to attach to - no title, no year, no episode number. MP2's own pictures screen has
  /// walked the folders for exactly this reason. Without this endpoint a front end can reach every photograph
  /// whose id it already happens to know and cannot find a single one, which looks the same as a media centre with
  /// no photographs in it.
  /// </para>
  /// <para>
  /// <b>The same boundary as the file server.</b> A resource path arrives here the way it arrives at
  /// <see cref="ResourceAccessModule"/> - serialized, in a URL - and the same owner of the data decides, through
  /// <see cref="IResourceAccessGuard"/>. It asks the weaker of the two questions,
  /// <see cref="IResourceAccessGuard.IsInsideAShare"/>, because browsing starts at a share: the share's own folder
  /// is the first thing anybody asks for and the file server rightly refuses to stream a folder. Naming a share's
  /// children is the weaker promise; handing over their bytes stays the file server's job, behind
  /// <see cref="IResourceAccessGuard.IsAllowed"/>.
  /// </para>
  /// <para>
  /// <b>Only pictures are listed.</b> The response is for a picture screen, so it says "120 files that are not
  /// pictures" rather than listing them or pretending they are not there. The extensions are the ones a front end
  /// can be relied on to draw, including the several spellings of JPEG that turn up in existing collections.
  /// </para>
  /// <para>
  /// <b>Bounded.</b> A folder is where unbounded listings go to hurt: a camera that dumped four thousand
  /// thumbnails into one directory is ordinary. Each of the two lists therefore stops at
  /// <see cref="ENTRY_LIMIT"/> and says that it did.
  /// </para>
  /// </remarks>
  [ApiController]
  [Route("api/media")]
  [MediaPortalAuthorize]
  public class BrowseController : ControllerBase
  {
    /// <summary>
    /// How many folders, and how many pictures, one answer contains at most.
    /// </summary>
    public const int ENTRY_LIMIT = 500;

    /// <summary>
    /// The file extensions a picture screen is offered.
    /// </summary>
    public static readonly ISet<string> PictureExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".jpe", ".jfi", ".jfif", ".png", ".gif", ".bmp", ".webp", ".tif", ".tiff"
    };

    /// <summary>
    /// One folder or one picture inside a folder.
    /// </summary>
    public class BrowseEntry
    {
      /// <summary>Name to show, without the extension for a picture.</summary>
      public string Name { get; set; }

      /// <summary>
      /// The serialized <see cref="ResourcePath"/> of this entry: what the caller asks about next, and what the
      /// file server wants before it will hand over the bytes.
      /// </summary>
      public string Path { get; set; }

      /// <summary>Bytes for a picture; absent for a folder, whose size means something else.</summary>
      public long? Size { get; set; }

      /// <summary>When the entry was last changed, for a screen that wants to say "these are from 2019".</summary>
      public DateTime? LastChanged { get; set; }
    }

    /// <summary>
    /// What one folder holds.
    /// </summary>
    public class BrowseResult
    {
      /// <summary>The folder this is the contents of, as it was asked for.</summary>
      public string Path { get; set; }

      /// <summary>The folder's own name, so a title bar can say where the caller is.</summary>
      public string Name { get; set; }

      public List<BrowseEntry> Folders { get; set; } = new List<BrowseEntry>();

      public List<BrowseEntry> Pictures { get; set; } = new List<BrowseEntry>();

      /// <summary>Set when the folders stopped at <see cref="ENTRY_LIMIT"/>.</summary>
      public bool FoldersTruncated { get; set; }

      /// <summary>Set when the pictures stopped at <see cref="ENTRY_LIMIT"/>.</summary>
      public bool PicturesTruncated { get; set; }

      /// <summary>
      /// Files in this folder that are not pictures. Counted whether or not the listing was cut short, because
      /// "230 files, 120 of them not pictures" is a truer answer than a silently smaller number.
      /// </summary>
      public int OtherFileCount { get; set; }
    }

    /// <summary>
    /// Lists one folder inside a share.
    /// </summary>
    /// <param name="path">The serialized <see cref="ResourcePath"/>, as <c>GET api/media/shares</c> reports it.</param>
    /// <returns>
    /// The folders and pictures inside it, both sorted by name. <c>400</c> when the path cannot be read as a
    /// resource path, <c>403</c> when it is outside every share - or when the server has no guard registered at
    /// all, which is the case the file server also refuses - and <c>404</c> when nothing is there or what is there
    /// is a file.
    /// </returns>
    [HttpGet("browse")]
    [ProducesResponseType(typeof(BrowseResult), StatusCodes.Status200OK)]
    public IActionResult Browse([FromQuery] string path)
    {
      if (string.IsNullOrEmpty(path))
        return BadRequest("Say which folder to look in with ?path=[serialized resource path].");

      ResourcePath resourcePath;
      try
      {
        resourcePath = ResourcePath.Deserialize(path);
      }
      catch (Exception e) when (e is ArgumentException || e is FormatException)
      {
        // Deserializing walks one segment per '://' and turns the part in front of it into a GUID, so a made-up
        // argument fails as an ArgumentException from the segment or as a FormatException from the GUID. Both are
        // the caller's mistake about a string, and neither is a reason for a stack trace.
        return BadRequest("That is not a resource path this server understands.");
      }

      IResourceAccessGuard guard = ServiceRegistration.Get<IResourceAccessGuard>(false);
      if (guard == null)
        // Nobody said what this server may show. The file server refuses everything in the same case, and a
        // server that came up without its backend has no shares to browse anyway.
        return StatusCode(StatusCodes.Status403Forbidden);

      if (!guard.IsInsideAShare(resourcePath))
        // The same empty refusal the file server gives, for the same reason: an answer that explained the
        // difference would tell a stranger which folders on this machine exist.
        return StatusCode(StatusCodes.Status403Forbidden);

      IResourceAccessor accessor;
      if (!resourcePath.TryCreateLocalResourceAccessor(out accessor))
        return NotFound();

      using (accessor)
      {
        IFileSystemResourceAccessor folder = accessor as IFileSystemResourceAccessor;
        if (folder == null || !folder.Exists || folder.IsFile)
          // A share somebody deleted, and a file somebody tried to browse. Neither is a folder, so neither has
          // contents to name, and the difference is not worth publishing.
          return NotFound();

        BrowseResult result = new BrowseResult
        {
            Path = path,
            Name = folder.ResourceName
        };

        List<BrowseEntry> folders = Take(folder.GetChildDirectories(), ENTRY_LIMIT, out bool foldersTruncated);
        result.Folders = folders;
        result.FoldersTruncated = foldersTruncated;

        // Pictures are kept and handed to the bounded listing below, which lets them go; everything else is
        // counted and let go here, because an accessor is a handle and a folder of videos is not a short list.
        List<IFileSystemResourceAccessor> pictures = new List<IFileSystemResourceAccessor>();
        ICollection<IFileSystemResourceAccessor> files = folder.GetFiles();
        if (files != null)
          foreach (IFileSystemResourceAccessor file in files)
            if (IsPicture(file.ResourceName))
              pictures.Add(file);
            else
            {
              result.OtherFileCount++;
              file.Dispose();
            }

        List<BrowseEntry> listed = Take(pictures, ENTRY_LIMIT, out bool picturesTruncated);
        result.Pictures = listed;
        result.PicturesTruncated = picturesTruncated;
        return Ok(result);
      }
    }

    /// <summary>
    /// Whether a file is one a picture screen should offer.
    /// </summary>
    public static bool IsPicture(string fileName)
    {
      if (string.IsNullOrEmpty(fileName))
        return false;

      string extension = Path.GetExtension(fileName);
      // A file with no extension gives the empty string, which is not in the set.
      return extension.Length > 0 && PictureExtensions.Contains(extension);
    }

    /// <summary>
    /// The first <paramref name="limit"/> accessors by name, as entries. Every accessor handed in is disposed,
    /// listed or not - which is why this takes the collection rather than returning one to be iterated later.
    /// </summary>
    private static List<BrowseEntry> Take(IEnumerable<IFileSystemResourceAccessor> children, int limit,
        out bool truncated)
    {
      List<BrowseEntry> entries = new List<BrowseEntry>();
      truncated = false;
      if (children == null)
        return entries;

      List<IFileSystemResourceAccessor> all = new List<IFileSystemResourceAccessor>(children);
      try
      {
        all.Sort(delegate (IFileSystemResourceAccessor a, IFileSystemResourceAccessor b)
        {
          return string.Compare(a.ResourceName, b.ResourceName, StringComparison.OrdinalIgnoreCase);
        });

        for (int i = 0; i < all.Count; i++)
        {
          if (entries.Count >= limit)
          {
            truncated = true;
            break;
          }

          IFileSystemResourceAccessor child = all[i];
          bool isFile = child.IsFile;
          entries.Add(new BrowseEntry
          {
              Name = isFile ? Path.GetFileNameWithoutExtension(child.ResourceName) : child.ResourceName,
              Path = child.CanonicalLocalResourcePath.Serialize(),
              Size = isFile ? child.Size : (long?) null,
              LastChanged = child.LastChanged
          });
        }
      }
      finally
      {
        foreach (IFileSystemResourceAccessor child in all)
          child.Dispose();
      }

      return entries;
    }
  }
}