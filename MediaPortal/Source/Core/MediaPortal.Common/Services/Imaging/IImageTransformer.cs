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

using System.IO;
using MediaPortal.Common.Services.ThumbnailGenerator;

namespace MediaPortal.Common.Services.Imaging
{
  /// <summary>
  /// Decodes, resizes and re-encodes image data for code that must stay platform neutral.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Two places in the portable core need to look inside an image: <c>BaseInfo</c> shrinks the
  /// thumbnail it is about to store in the database, and the fan art handlers re-encode downloaded
  /// artwork to the JPEG file name they already committed to. Both used to call
  /// <c>System.Drawing</c> directly, which cannot work outside Windows: on .NET 6 and later
  /// <c>System.Drawing.Common</c> throws <c>PlatformNotSupportedException</c> for every imaging type
  /// off Windows (measured - see scripts/ci/README.md), so this interface is the seam that lets the
  /// core ask for it without naming the technology.
  /// </para>
  /// <para>
  /// Deliberately distinct from <see cref="IThumbnailGenerator"/>: that service answers "give me a
  /// thumbnail for this file", dispatches to priority-ordered providers and may return a cached or
  /// provider-chosen result (the GDI provider applies EXIF rotation and switches to PNG for images
  /// with an alpha channel). Both callers here store or name the result in a fixed way, so they need
  /// a transformation with a defined output, not a provider chain. A portable
  /// <see cref="IThumbnailProvider"/> remains the right thing to write for generating thumbnails on
  /// other platforms.
  /// </para>
  /// <para>
  /// An implementation must never throw: undecodable input is a normal outcome for a media library,
  /// and both callers treat it as "no image", the same way the GDI+ code they replaced did through
  /// its exception handler. Log the reason instead, because a library that silently loses all its
  /// thumbnails is indistinguishable from a library with no images.
  /// </para>
  /// <para>
  /// The core cannot pick an implementation - <c>MediaPortal.Common</c> must not reference a
  /// platform-specific assembly. Each host registers one during startup, next to the other platform
  /// services it installs; <c>MediaPortal.Common.Windows</c> provides <see cref="Imaging.GdiPlusImageTransformer"/>
  /// for the Windows hosts.
  /// </para>
  /// </remarks>
  public interface IImageTransformer
  {
    /// <summary>
    /// Decodes <paramref name="source"/> and encodes it to <paramref name="targetFormat"/> without
    /// changing its dimensions.
    /// </summary>
    /// <param name="source">Encoded image data. Not disposed by this method.</param>
    /// <param name="targetFormat">Format to encode to.</param>
    /// <param name="imageData">Encoded result, if the call returned <c>true</c>.</param>
    /// <returns><c>false</c> if the input could not be decoded, is empty, or
    /// <paramref name="targetFormat"/> is not supported.</returns>
    bool TryEncode(Stream source, ImageType targetFormat, out byte[] imageData);

    /// <summary>
    /// Decodes <paramref name="source"/>, scales it down to fit into
    /// <paramref name="maxWidth"/>/<paramref name="maxHeight"/> and encodes the result.
    /// </summary>
    /// <param name="source">Encoded image data. Not disposed by this method.</param>
    /// <param name="maxWidth">Upper bound for the result width.</param>
    /// <param name="maxHeight">Upper bound for the result height.</param>
    /// <param name="targetFormat">Format to encode to.</param>
    /// <param name="imageData">Encoded result, if the call returned <c>true</c>.</param>
    /// <returns><c>false</c> if the input could not be decoded, is empty, or
    /// <paramref name="targetFormat"/> is not supported.</returns>
    /// <remarks>
    /// Aspect ratio is preserved and images already smaller than the bounds keep their size - they
    /// are never upscaled, they are only re-encoded. Orientation from EXIF is not applied, because
    /// the behaviour being preserved did not apply it either.
    /// </remarks>
    bool TryResize(Stream source, int maxWidth, int maxHeight, ImageType targetFormat, out byte[] imageData);
  }
}
