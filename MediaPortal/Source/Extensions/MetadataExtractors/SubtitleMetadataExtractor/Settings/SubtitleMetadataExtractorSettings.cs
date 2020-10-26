#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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
using System.Text.RegularExpressions;
using MediaPortal.Common;
using MediaPortal.Common.Localization;

namespace MediaPortal.Extensions.MetadataExtractors.SubtitleMetadataExtractor.Settings
{
  public class SubtitleMetadataExtractorSettings
  {
    // Don't add any others unless support has been added for them
    protected readonly static string[] DEFAULT_SUBTITLE_FILE_EXTENSIONS = 
      {
        ".srt",
        ".smi",
        ".ass",
        ".ssa",
        ".sub",
        ".vtt",
        ".idx",
      };

    protected readonly static string[] DEFAULT_SUBTITLE_FOLDERS = 
      {
        "subtitles",
        "subs",
      };

    protected string[] _subtitleFileExtensions;
    protected string[] _subtitleFolders;
    protected string _languageCultures = ServiceRegistration.Get<ILocalization>().CurrentCulture.Name;

    public SubtitleMetadataExtractorSettings()
    {
      _subtitleFileExtensions = DEFAULT_SUBTITLE_FILE_EXTENSIONS;
      _subtitleFolders = DEFAULT_SUBTITLE_FOLDERS;
      if (string.IsNullOrEmpty(_languageCultures))
        _languageCultures = "en-US";
    }

    /// <summary>
    /// Subtitle extensions for which the <see cref="SubtitleMetadataExtractor"/> should be used.
    /// </summary>
    [Setting(SettingScope.Global)]
    public string[] SubtitleFileExtensions
    {
      get => _subtitleFileExtensions;
      set => _subtitleFileExtensions = value;
    }

    /// <summary>
    /// Subtitle folders where subtitles for media can be found
    /// </summary>
    [Setting(SettingScope.Global)]
    public string[] SubtitleFolders
    {
      get => _subtitleFolders;
      set => _subtitleFolders = value;
    }

    /// <summary>
    /// If <c>true</c>, subtitle download will be skipped during import.
    /// </summary>
    [Setting(SettingScope.Global, true)]
    public bool SkipOnlineSearches { get; set; }

    /// <summary>
    /// A comma separated list of preferred subtitle languages.
    /// </summary>
    [Setting(SettingScope.Global)]
    public string ImportLanguageCultures
    {
      get { return _languageCultures; }
      set { _languageCultures = value; }
    }
  }
}
