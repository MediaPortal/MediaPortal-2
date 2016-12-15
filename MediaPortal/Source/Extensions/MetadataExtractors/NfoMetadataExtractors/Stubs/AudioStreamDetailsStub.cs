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

namespace MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.Stubs
{
  /// <summary>
  /// This stub class is used to store inforation about an audio stream
  /// </summary>
  public class AudioStreamDetailsStub
  {
    /// <summary>
    /// Audiocodec
    /// </summary>
    /// <example>"ac3"</example>
    public string Codec { get; set; }

    /// <summary>
    /// Language of the audio stream
    /// </summary>
    /// <example>"deutsch"</example>
    public string Language { get; set; }

    /// <summary>
    /// Number of channels in the audio stream
    /// </summary>
    public int? Channels { get; set; }
  }
}
