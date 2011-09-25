#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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

using System.Collections.Generic;
using MediaPortal.Common.Settings;

namespace MediaPortal.Extensions.MetadataExtractors.MusicMetadataExtractor.Settings
{
  public class MusicMetadataExtractorSettings
  {
    protected readonly static List<string> DEFAULT_UNSPLITTABLE_ARTISTS = new List<string>
      {
          "AC/DC",
          "De/Vision",
      };

    protected readonly static List<string> DEFAULT_AUDIO_EXTENSIONS = new List<string>
      {
          ".ape",
          ".flac",
          ".mp3",
          ".ogg",
          ".wv",
          ".wav",
          ".wma",
          ".mp4",
          ".m4a",
          ".m4p",
          ".mpc",
          ".mp+",
          ".mpp",
      };

    protected List<string> _unsplittableValues = new List<string>(DEFAULT_UNSPLITTABLE_ARTISTS);
    protected List<string> _audioExtensions = new List<string>(DEFAULT_AUDIO_EXTENSIONS);

    /// <summary>
    /// Returns a list of strings of the form "AC/DC". Each string must contain at least one "/" character.
    /// </summary>
    /// <remarks>
    /// The ID3v2 tag specification states that the "/" character should be used to separate different artists or album artists.
    /// But some artists contain a "/" character in their name. This setting is initialized with some default artists containing
    /// a "/" in their name, e.g. "AC/DC" or "De/Vision". It can be extended by more artists of that kind by the user.
    /// Also artists with more than one "/" in their name are supported.
    /// </remarks>
    // Global or per-user?
    [Setting(SettingScope.Global)]
    public List<string> UnsplittableValues
    {
      get { return _unsplittableValues; }
      set { _unsplittableValues = value; }
    }

    /// <summary>
    /// Audio extensions for which the <see cref="MusicMetadataExtractor"/> should be used.
    /// </summary>
    // Global or per-user?
    [Setting(SettingScope.Global)]
    public List<string> AudioExtensions
    {
      get { return _audioExtensions; }
      set { _audioExtensions = value; }
    }
  }
}
