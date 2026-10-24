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

using MediaPortal.Common;
using MediaPortal.Common.Settings;

namespace MediaPortal.Extensions.MetadataExtractors.AudioTagMetadataExtractor.Settings
{
  /// <summary>
  /// Settings of the reader that looks into the tags of music files and at the folders they lie in.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The reference spreads these two questions over every one of its music extractors; here they stand on their
  /// own, because this reader is the one the portable server can load everywhere and the one that walks an
  /// album's folder today. It carries only what it answers: a cover read out of a tag needs no switch, because
  /// reading it costs nothing beyond the file that is being read for its length and its artist anyway.
  /// </para>
  /// <para>
  /// <see cref="CacheOfflineFanArt"/> keeps the reference's word "offline" for a network share - the share a
  /// picture is copied away against, because it might not be there later.
  /// </para>
  /// </remarks>
  public class AudioTagMetadataExtractorSettings
  {
    /// <summary>
    /// If <c>true</c>, the pictures lying in an album's folder on a drive plugged into this machine are copied
    /// into the fan art cache.
    /// </summary>
    [Setting(SettingScope.Global, false)]
    public bool CacheLocalFanArt { get; set; }

    /// <summary>
    /// If <c>true</c>, the pictures lying in an album's folder on a network share are copied into the fan art
    /// cache, which is what lets a browse answer while that share is asleep.
    /// </summary>
    [Setting(SettingScope.Global, true)]
    public bool CacheOfflineFanArt { get; set; }
  }
}
