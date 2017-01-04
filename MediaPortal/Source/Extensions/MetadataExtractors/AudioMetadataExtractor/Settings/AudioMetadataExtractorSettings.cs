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

using System.Collections.Generic;
using MediaPortal.Common.Settings;

namespace MediaPortal.Extensions.MetadataExtractors.AudioMetadataExtractor.Settings
{
  public class AudioMetadataExtractorSettings
  {
    protected readonly static string[] DEFAULT_UNSPLITTABLE_ID3V23_VALUES = new string[]
      {
          "AC/DC",
          "De/Vision",
      };

    protected readonly static char DEFAULT_ADDITIONAL_SEPARATOR = ';';

    protected readonly static string[] DEFAULT_UNSPLITTABLE_ADDITIONAL_SEPARATOR_VALUES = new string[0];

    protected readonly static string[] DEFAULT_AUDIO_FILE_EXTENSIONS = new string[]
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

    protected string[] _unsplittableID3v23Values;
    protected bool _useAdditionalSeparator;
    protected char _additionalSeparator;
    protected string[] _unsplittableAddditionalSeparatorValues;
    protected string[] _audioExtensions;

    public AudioMetadataExtractorSettings()
    {
      _unsplittableID3v23Values = DEFAULT_UNSPLITTABLE_ID3V23_VALUES;
      _unsplittableAddditionalSeparatorValues = DEFAULT_UNSPLITTABLE_ADDITIONAL_SEPARATOR_VALUES;
      _audioExtensions = DEFAULT_AUDIO_FILE_EXTENSIONS;
      _additionalSeparator = DEFAULT_ADDITIONAL_SEPARATOR;
    }

    /// <summary>
    /// Returns a list of strings of the form "AC/DC". Each string must contain at least one "/" character.
    /// </summary>
    /// <remarks>
    /// The ID3v2.3 tag specification states that the "/" character should be used to separate different artists, album artists or composers.
    /// The TagLib# library additionally treats "/" as a separator for genres.
    /// But some artists, composers or maybe genres contain a "/" character in their name. This setting is initialized with some default artists containing
    /// a "/" in their name, e.g. "AC/DC" or "De/Vision". It can be extended by more artists, composers or genres of that kind by the user.
    /// Also values with more than one "/" in their name are supported.
    /// </remarks>
    [Setting(SettingScope.Global)]
    public string[] UnsplittableID3v23Values
    {
      get { return _unsplittableID3v23Values; }
      set { _unsplittableID3v23Values = value; }
    }

    /// <summary>
    /// Indicates whether an additional separator is taken into account for multiple value fields.
    /// </summary>
    /// <remarks>
    /// Some tagging formats (such as e.g. Vorbis Comments) support multi-value fields in a way that you can store multiple
    /// fields with the same name. If a track for example has two artists "Beavis" and "Butthead", you can store one field with
    /// the name "artist" and the value "Beavis" and a second field with the same name "artist" and the value "Butthead".
    /// Other tagging formats such as ID3v2.3 store multiple values in one field and use a separator character to separate
    /// multiple values. In the ID3v2.3 format e.g., the separator for the field with the name "TPE1", which is used to store the
    /// artists, is '/'. As a result, in the example above, you would store one field named "TPE1" with the value
    /// "Beavis/Butthead".
    /// Unfortunately, the separator differs from one tagging format to another. In ID3v2.4 for example, the null character is used
    /// instead of the '/'. Since there is no unique and correct separator, many tagging tools have started to use their "own" separator
    /// values. MPTagThat for example uses the semicolon (';') as a unique separator.
    /// If this value is set to true, the <see cref="AudioMetadataExtractor"/> will use <see cref="AdditionalSeparator"/> as an additional separator character for
    /// all multiple value fields and all tagging formats. Since, depending on the separator used, there may be values that contain
    /// <see cref="AdditionalSeparator"/> as a regular character, <see cref="UnsplittableAddditionalSeparatorValues"/> contains values, which are not splitted
    /// although they contain <see cref="AdditionalSeparator"/>.
    /// The standard value for this setting is true combined with ';' as <see cref="AdditionalSeparator"/> to make <see cref="AudioMetadataExtractor"/> behave
    /// like MP1 and MPTagThat.
    /// For more information see this thread in the MediaPortal forum: http://forum.team-mediaportal.com/submit-bug-reports-532/multiple-music-genres-not-handled-correctly-103169/
    /// </remarks>
    [Setting(SettingScope.Global, true)]
    public bool UseAdditionalSeparator
    {
      get { return _useAdditionalSeparator; }
      set { _useAdditionalSeparator = value; }
    }

    /// <summary>
    /// Character to use as an additional separator for multiple values stored in a single tagging field.
    /// </summary>
    [Setting(SettingScope.Global)]
    public char AdditionalSeparator
    {
      get { return _additionalSeparator; }
      set { _additionalSeparator = value; }
    }

    /// <summary>
    /// List of values, which contain one or more <see cref="AdditionalSeparator"/> values and nevertheless must not be splitted.
    /// </summary>
    [Setting(SettingScope.Global)]
    public string[] UnsplittableAddditionalSeparatorValues
    {
      get { return _unsplittableAddditionalSeparatorValues; }
      set { _unsplittableAddditionalSeparatorValues = value; }
    }

    /// <summary>
    /// Audio extensions for which the <see cref="AudioMetadataExtractor"/> should be used.
    /// </summary>
    [Setting(SettingScope.Global)]
    public string[] AudioExtensions
    {
      get { return _audioExtensions; }
      set { _audioExtensions = value; }
    }

    /// <summary>
    /// If <c>true</c>, no online searches will be done for metadata.
    /// </summary>
    [Setting(SettingScope.Global, false)]
    public bool SkipOnlineSearches { get; set; }

    /// <summary>
    /// If <c>true</c>, no FanArt is downloaded.
    /// </summary>
    [Setting(SettingScope.Global, false)]
    public bool SkipFanArtDownload { get; set; }

    /// <summary>
    /// If <c>true</c>, the AudioMetadataExtractor does not fetch any information for missing local album tracks.
    /// </summary>
    [Setting(SettingScope.Global, false)]
    public bool OnlyLocalMedia { get; set; }

    /// <summary>
    /// If <c>true</c>, a copy will be made of FanArt placed on network drives to allow browsing when they are offline.
    /// </summary>
    [Setting(SettingScope.Global, true)]
    public bool CacheOfflineFanArt { get; set; }

    /// <summary>
    /// If <c>true</c>, a copy will be made of FanArt placed on local drives to allow browsing when they are asleep.
    /// </summary>
    [Setting(SettingScope.Global, false)]
    public bool CacheLocalFanArt { get; set; }

    /// <summary>
    /// If <c>true</c>, Artists details will be fetched from online sources.
    /// </summary>
    [Setting(SettingScope.Global, true)]
    public bool IncludeArtistDetails { get; set; }

    /// <summary>
    /// If <c>true</c>, Composer details will be fetched from online sources.
    /// </summary>
    [Setting(SettingScope.Global, true)]
    public bool IncludeComposerDetails { get; set; }

    /// <summary>
    /// If <c>true</c>, Music Label details will be fetched from online sources.
    /// </summary>
    [Setting(SettingScope.Global, true)]
    public bool IncludeMusicLabelDetails { get; set; }
  }
}
