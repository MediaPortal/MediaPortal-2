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

using System.Text.RegularExpressions;
using MediaPortal.Common.Settings;
using MediaPortal.Extensions.OnlineLibraries;

namespace MediaPortal.Extensions.MetadataExtractors.SeriesMetadataExtractor.Settings
{
  #region Replacement class

  public abstract class RegexBase
  {
    protected Regex _regex;

    /// <summary>
    /// Indicates if the replacement is enabled. If set to <c>false</c> it will not be evaluated.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Simple string or Regex, depending on <see cref="Replacement.IsRegex"/>.
    /// </summary>
    public string Pattern { get; set; }

    /// <summary>
    /// RegexOptions.
    /// </summary>
    public RegexOptions? RegexOptions { get; set; }

    public override string ToString()
    {
      return string.Format("{0}: Enabled: {1}, Pattern: {2}, Option: {3}", GetType().Name, Enabled, Pattern, RegexOptions);
    }
  }

  /// <summary>
  /// Definition of a RegEx based replacement which is executed before the pattern matching.
  /// </summary>
  public class Replacement : RegexBase
  {
    /// <summary>
    /// Indicates if the replacement should happen before matching (<c>true</c>) or after (<c>false</c>).
    /// </summary>
    public bool BeforeMatch { get; set; }

    /// <summary>
    /// Indicates if the <see cref="RegexBase.Pattern"/> will be evaluated as <see cref="Regex"/>. Requires also a value for <see cref="RegexOptions"/>.
    /// </summary>
    public bool IsRegex { get; set; }

    /// <summary>
    /// Target replacement string.
    /// </summary>
    public string ReplaceBy { get; set; }

    /// <summary>
    /// Processes the given <see cref="textToReplace"/> if the instance is <see cref="RegexBase.Enabled"/>.
    /// </summary>
    /// <param name="textToReplace">Reference to text</param>
    /// <returns><c>false</c> if not <see cref="RegexBase.Enabled"/>, otherwise <c>true</c></returns>
    public bool Replace(ref string textToReplace)
    {
      if (!Enabled)
        return false;

      if (IsRegex)
      {
        if (!string.IsNullOrEmpty(Pattern) && RegexOptions.HasValue)
          _regex = new Regex(Pattern, RegexOptions.Value);
        else if(!string.IsNullOrEmpty(Pattern))
          _regex = new Regex(Pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        var regex = _regex;
        if (regex != null)
        {
          textToReplace = regex.Replace(textToReplace, ReplaceBy);
        }
      }
      else
      {
        if(RegexOptions.HasValue)
          textToReplace = Regex.Replace(textToReplace, Pattern, ReplaceBy, RegexOptions.Value);
        else
          textToReplace = Regex.Replace(textToReplace, Pattern, ReplaceBy, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
      }
      return true;
    }
  }

  #endregion

  #region MatchPattern class

  /// <summary>
  /// Definition of a RegEx pattern for series matching used for serialization in settings.
  /// </summary>
  public class MatchPattern : RegexBase
  {
    /// <summary>
    /// Returns a <see cref="Regex"/> instance if the <see cref="RegexBase.Enabled"/> value is true and the <see cref="RegexBase.Pattern"/> and <see cref="RegexBase.RegexOptions"/> are set correctly.
    /// </summary>
    /// <param name="regex">Returns the regex instance or <c>null</c> if not enabled or not valid.</param>
    /// <returns></returns>
    public bool GetRegex(out Regex regex)
    {
      if (!Enabled)
      {
        regex = null;
        return false;
      }

      if (!string.IsNullOrEmpty(Pattern) && RegexOptions.HasValue)
        _regex = new Regex(Pattern, RegexOptions.Value);

      regex = _regex;
      return regex != null;
    }
  }

  #endregion

  /// <summary>
  /// Settings class for the SeriesMetadataExtractor
  /// </summary>
  public class SeriesMetadataExtractorSettings
  {
    public SeriesMetadataExtractorSettings()
    {
      // Init default replacements
      Replacements = new Replacement[]
      {
        new Replacement { Enabled = true, BeforeMatch = true, Pattern = "720p", ReplaceBy = "", IsRegex = false },
        new Replacement { Enabled = true, BeforeMatch = true, Pattern = "1080i", ReplaceBy = "", IsRegex = false },
        new Replacement { Enabled = true, BeforeMatch = true, Pattern = "1080p", ReplaceBy = "", IsRegex = false },
        new Replacement { Enabled = true, BeforeMatch = true, Pattern = "x264", ReplaceBy = "", IsRegex = false },
        new Replacement { Enabled = true, BeforeMatch = true, Pattern = @"(?<!(?:S\d+.?E\\d+\-E\d+.*|S\d+.?E\d+.*|\s\d+x\d+.*))P[ar]*t[\s|\.|\-|_]?(\d+)(\s?of\s\d{1,2})?", ReplaceBy = "S01E${1}", IsRegex = true },
      };

      // Init default patterns.
      SeriesPatterns = new MatchPattern[]
      {
        // Multi-episodes pattern
        // "Series S1E01-E02 - Episodes"
        new MatchPattern { Enabled = true, Pattern = @"(?<series>[^\\]+)\WS(?<seasonnum>\d+)[\s|\.|\-|_]{0,1}E((?<episodenum>\d+)[\-_]?)+E(?<endepisodenum>\d+)+ - (?<episode>.*)\.", RegexOptions = RegexOptions.IgnoreCase },
        // "Series.Name.S01E01-E02.Episode.Or.Release.Info"
        new MatchPattern { Enabled = true, Pattern = @"(?<series>[^\\]+).S(?<seasonnum>\d+)[\s|\.|\-|_]{0,1}E((?<episodenum>\d+)[\-|_]?)+E(?<endepisodenum>\d+)+(?<episode>.*)\.", RegexOptions = RegexOptions.IgnoreCase },
        // Series\Season...\S01E01-E02* or Series\Season...\1x01-02*
        new MatchPattern { Enabled = true, Pattern = @"(?<series>[^\\]*)\\[^\\]*(?<seasonnum>\d+)[^\\]*\\S*(?<seasonnum>\d+)[EX]((?<episodenum>\d+)[\-|_]?)+[EX](?<endepisodenum>\d+)*(?<episode>.*)\.", RegexOptions = RegexOptions.IgnoreCase },

        // Series\Season...\S01E01* or Series\Season...\1x01*
        new MatchPattern { Enabled = true, Pattern = @"(?<series>[^\\]*)\\[^\\]*(?<seasonnum>\d+)[^\\]*\\S*(?<seasonnum>\d+)[EX](?<episodenum>\d+)*(?<episode>.*)\.", RegexOptions = RegexOptions.IgnoreCase },
        // MP1 EpisodeScanner recommendations for recordings: Series - (Episode) S1E1, also "S1 E1", "S1-E1", "S1.E1", "S1_E1"
        new MatchPattern { Enabled = true, Pattern = @"(?<series>[^\\]+) - \((?<episode>.*)\) S(?<seasonnum>[0-9]+?)[\s|\.|\-|_]{0,1}E(?<episodenum>[0-9]+?)", RegexOptions = RegexOptions.IgnoreCase },
        // "Series 1x1 - Episode" and multi-episodes "Series 1x1_2 - Episodes"
        new MatchPattern { Enabled = true, Pattern = @"(?<series>[^\\]+)\W(?<seasonnum>\d+)x((?<episodenum>\d+)_?)+ - (?<episode>.*)\.", RegexOptions = RegexOptions.IgnoreCase },
        // "Series S1E01 - Episode" and multi-episodes "Series S1E01_02 - Episodes", also "S1 E1", "S1-E1", "S1.E1", "S1_E1"
        new MatchPattern { Enabled = true, Pattern = @"(?<series>[^\\]+)\WS(?<seasonnum>\d+)[\s|\.|\-|_]{0,1}E((?<episodenum>\d+)_?)+ - (?<episode>.*)\.", RegexOptions = RegexOptions.IgnoreCase },
        // "Series.Name.1x01.Episode.Or.Release.Info"
        new MatchPattern { Enabled = true, Pattern = @"(?<series>[^\\]+).(?<seasonnum>\d+)x((?<episodenum>\d+)_?)+(?<episode>.*)\.", RegexOptions = RegexOptions.IgnoreCase },
        // "Series.Name.S01E01.Episode.Or.Release.Info", also "S1 E1", "S1-E1", "S1.E1", "S1_E1"
        new MatchPattern { Enabled = true, Pattern = @"(?<series>[^\\]+).S(?<seasonnum>\d+)[\s|\.|\-|_]{0,1}E((?<episodenum>\d+)_?)+(?<episode>.*)\.", RegexOptions = RegexOptions.IgnoreCase },

        // Folder + filename pattern
        // "Series\1\11 - Episode" "Series\Staffel 2\11 - Episode" "Series\Season 3\12 Episode" "Series\3. Season\13-Episode"
        new MatchPattern { Enabled = true, Pattern = @"(?<series>[^\\]*)\\[^\\|\d]*(?<seasonnum>\d+)\D*\\(?<episodenum>\d+)\s*-\s*(?<episode>[^\\]+)\.", RegexOptions = RegexOptions.IgnoreCase },
        // "Series.Name.101.Episode.Or.Release.Info", attention: this expression can lead to false matches for every filename with nnn included
        new MatchPattern { Enabled = true, Pattern = @"(?<series>[^\\]+)\D(?<seasonnum>\d{1})(?<episodenum>\d{2})\D(?<episode>.*)\.", RegexOptions = RegexOptions.IgnoreCase },
      };

      SeriesYearPattern = new SerializableRegex(@"(?<series>.*)[( .-_]+(?<year>\d{4})", RegexOptions.IgnoreCase);
    }

    #region Public properties

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
    /// Gets a list of matching replacements which can be extended by users.
    /// </summary>
    [Setting(SettingScope.Global)]
    public Replacement[] Replacements { get; set; }

    /// <summary>
    /// Gets a list of series matching patterns which can be extended by users.
    /// </summary>
    [Setting(SettingScope.Global)]
    public MatchPattern[] SeriesPatterns { get; set; }

    /// <summary>
    /// Regular expression used to find a year in the series name
    /// </summary>
    [Setting(SettingScope.Global)]
    public SerializableRegex SeriesYearPattern { get; set; }

    /// <summary>
    /// If <c>true</c>, the SeriesMetadataExtractor does not fetch any information for missing local episodes.
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
    /// If <c>true</c>, Actor details will be fetched from online sources.
    /// </summary>
    [Setting(SettingScope.Global, true)]
    public bool IncludeActorDetails { get; set; }

    /// <summary>
    /// If <c>true</c>, Character details will be fetched from online sources.
    /// </summary>
    [Setting(SettingScope.Global, true)]
    public bool IncludeCharacterDetails { get; set; }

    /// <summary>
    /// If <c>true</c>, Director details will be fetched from online sources.
    /// </summary>
    [Setting(SettingScope.Global, true)]
    public bool IncludeDirectorDetails { get; set; }

    /// <summary>
    /// If <c>true</c>, Writer details will be fetched from online sources.
    /// </summary>
    [Setting(SettingScope.Global, true)]
    public bool IncludeWriterDetails { get; set; }

    /// <summary>
    /// If <c>true</c>, TV Network details will be fetched from online sources.
    /// </summary>
    [Setting(SettingScope.Global, true)]
    public bool IncludeTVNetworkDetails { get; set; }

    /// <summary>
    /// If <c>true</c>, Production company details will be fetched from online sources.
    /// </summary>
    [Setting(SettingScope.Global, true)]
    public bool IncludeProductionCompanyDetails { get; set; }

    #endregion
  }
}
