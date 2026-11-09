#region Copyright (C) 2007-2021 Team MediaPortal

/*
    Copyright (C) 2007-2021 Team MediaPortal
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
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;

namespace MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.Utilities
{
  /// <summary>
  /// Writes <see cref="TrailerAspect"/> rows for the trailers an import has found for one title.
  /// </summary>
  /// <remarks>
  /// Both halves of an import note trailers - the metadata file names them, and the film folder sometimes
  /// carries a trailer file of its own - and both go through here, so that the two cannot disagree about what
  /// a trailer row looks like.
  /// </remarks>
  public static class TrailerAspectWriter
  {
    /// <summary>
    /// Notes one trailer of the title being imported.
    /// </summary>
    /// <param name="extractedAspectData">Dictionary of MediaItemAspects to write into</param>
    /// <param name="url">The address of the trailer. A metadata file's own address for a web trailer, and the
    /// resource address of the file for a trailer standing in the film's folder.</param>
    /// <param name="kind">One of the <c>TrailerAspect.KIND_</c> values</param>
    /// <param name="label">What the trailer is called, or <c>null</c> for the one nobody named</param>
    /// <returns><c>true</c> if the trailer was new and is now noted; <c>false</c> if the title already carried
    /// it, which happens because the tagging tool often names the very file that sits in the folder. A trailer
    /// standing at a site that answers no more is not noted at all and answers <c>false</c> too.</returns>
    /// <remarks>
    /// A trailer the title already carries is not left alone: its kind and its name are read off the address,
    /// and rewriting them from today's rules is what lets a better reading of an address reach a library
    /// without importing it again. The address itself is the trailer, so it keeps what the file wrote.
    /// <para>
    /// Apple's trailer site is shut, so a link from it is refused here and never reaches the database. Tagging
    /// tools went on writing those links long after the site stopped answering, so refusing them is the only
    /// thing that keeps them out of new imports. What an older import already stored goes away when the server
    /// starts; see <c>MediaLibrary.ForgetTrailersAtDeadSites</c>.
    /// </para>
    /// </remarks>
    public static bool Add(IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData, string url, int kind,
        string label)
    {
      if (string.IsNullOrEmpty(url))
        return false;
      if (TrailerAddress.IsDeadSite(url))
      {
        ServiceRegistration.Get<ILogger>().Debug("TrailerAspectWriter: Giving up the trailer at '{0}' - that site answers no more", url);
        return false;
      }

      MultipleMediaItemAspect noted = NotedTrailer(extractedAspectData, url);
      if (noted != null)
      {
        noted.SetAttribute(TrailerAspect.ATTR_KIND, kind);
        noted.SetAttribute(TrailerAspect.ATTR_LABEL, label);
        return false;
      }

      MediaItemAspect aspect = MediaItemAspect.CreateAspect(extractedAspectData, TrailerAspect.Metadata);
      aspect.SetAttribute(TrailerAspect.ATTR_URL, url);
      aspect.SetAttribute(TrailerAspect.ATTR_KIND, kind);
      aspect.SetAttribute(TrailerAspect.ATTR_LABEL, label);
      return true;
    }

    /// <summary>
    /// Notes one trailer and reads both its kind and its name off the address, which is all there is to go by
    /// when a metadata file wrote the trailer.
    /// </summary>
    public static bool AddDetected(IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData, string url)
    {
      int kind = TrailerAddress.DetectKind(url);
      return Add(extractedAspectData, url, kind, TrailerAddress.ToLabel(url, kind));
    }

    /// <summary>
    /// The row this address already has, or <c>null</c> when the title does not carry this trailer yet.
    /// </summary>
    /// <remarks>
    /// Comparison ignores case and the trailing slash, since the same file reaches the library through
    /// addresses that differ in those two ways - the tagging tool writes what its own machine saw.
    /// </remarks>
    private static MultipleMediaItemAspect NotedTrailer(IDictionary<Guid, IList<MediaItemAspect>> extractedAspectData,
        string url)
    {
      IList<MultipleMediaItemAspect> aspects;
      if (!MediaItemAspect.TryGetAspects(extractedAspectData, TrailerAspect.Metadata, out aspects))
        return null;
      string wanted = url.TrimEnd('/').ToLowerInvariant();
      foreach (var aspect in aspects)
      {
        string existing = aspect.GetAttributeValue<string>(TrailerAspect.ATTR_URL);
        if (existing != null && existing.TrimEnd('/').ToLowerInvariant() == wanted)
          return aspect;
      }
      return null;
    }
  }
}
