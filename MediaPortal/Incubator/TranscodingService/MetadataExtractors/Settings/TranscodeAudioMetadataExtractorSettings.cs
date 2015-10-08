#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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

namespace MediaPortal.Plugins.Transcoding.MetadataExtractors.Settings
{
  public class TranscodeAudioMetadataExtractorSettings
  {
    protected readonly static List<string> DEFAULT_AUDIO_FILE_EXTENSIONS = new List<string>
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
          ".dsf",
          ".dff",
      };

    protected List<string> _audioExtensions = new List<string>(DEFAULT_AUDIO_FILE_EXTENSIONS);

    /// <summary>
    /// Audio extensions for which the <see cref="TranscodeAudioMetadataExtractor"/> should be used.
    /// </summary>
    [Setting(SettingScope.Global)]
    public List<string> AudioExtensions
    {
      get { return _audioExtensions; }
      set { _audioExtensions = value; }
    }
  }
}
