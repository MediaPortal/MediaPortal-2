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

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Extensions.OnlineLibraries;

namespace MediaPortal.Extensions.MetadataExtractors.AudioMetadataExtractor.Matchers
{
  /// <summary>
  /// <see cref="MusicNameMatcher"/> tries to match track title, track number, album and other information from filenames, and cleans up titles for online lookup 
  /// using regular expressions.
  /// </summary>
  public class MusicNameMatcher
  {
    public const string GROUP_ARTIST = "artist";
    public const string GROUP_ALBUM = "album";
    public const string GROUP_TRACK_NUM = "trackNum";
    public const string GROUP_DISC_NUM = "discNum";
    public const string GROUP_TRACK = "track";
    public static readonly IList<Regex> REGEXP_TRACK = new List<Regex>
      {
        // For LocalFileSystemPath & CanonicalLocalResourcePath
        new Regex(@"\\(?<artist>[^\/|^\\]*)\\(?<album>[^\/|^\\]*)\\(?<discNum>[1-9]+)\\(?<trackNum>[\d{1}|\d{2}]*)\s*(?<track>[^\/|^\\]*)\.", RegexOptions.IgnoreCase),
        new Regex(@"\\(?<artist>[^\/|^\\]*)\\(?<album>[^\/|^\\]*)\\CD.*(?<discNum>[1-9]+)\\(?<trackNum>[\d{1}|\d{2}]*)\s*(?<track>[^\/|^\\]*)\.", RegexOptions.IgnoreCase),
        new Regex(@"\\(?<artist>[^\/|^\\]*)\\(?<album>[^\/|^\\]*)\\(?<trackNum>[\d{1}|\d{2}]*)\s*(?<track>[^\/|^\\]*)\.", RegexOptions.IgnoreCase), 
        // Can be extended
      };

    public static readonly IList<Regex> REGEXP_CLEANUPS = new List<Regex>
      {
        // Removing "disc n" from name, this can be used in future to detect multipart titles!
        new Regex(@"(\s|-|_)*(Disc|CD|DVD)\s*\d{1,2}", RegexOptions.IgnoreCase), 
        new Regex(@"\s*(Blu-ray|BD|3D|®|™)", RegexOptions.IgnoreCase), 
        // If source is an ISO or ZIP medium, remove the extensions for lookup
        new Regex(@".(iso|zip)$", RegexOptions.IgnoreCase), 
        new Regex(@"(\s|-)*$", RegexOptions.IgnoreCase), 
        // Can be extended
      };

    protected static Regex _cleanUpWhiteSpaces = new Regex(@"[\.|_](\S|$)");
    protected static Regex _trimWhiteSpaces = new Regex(@"\s{2,}");

    public static bool MatchTrack(string filename, TrackInfo trackInfo)
    {
      foreach (Regex regex in REGEXP_TRACK)
      {
        Match match = regex.Match(trackInfo.TrackName);
        if (match.Groups[GROUP_ARTIST].Length > 0 && match.Groups[GROUP_ALBUM].Length > 0 && match.Groups[GROUP_TRACK].Length > 0)
        {
          trackInfo.HasChanged |= MetadataUpdater.SetOrUpdateString(ref trackInfo.TrackName, match.Groups[GROUP_TRACK].Value.Trim(new[] { ' ', '-' }));
          trackInfo.HasChanged |= MetadataUpdater.SetOrUpdateString(ref trackInfo.Album, match.Groups[GROUP_ALBUM].Value.Trim(new[] { ' ', '-' }));
          List<PersonInfo> artists = new List<PersonInfo>()
          {
            new PersonInfo()
            {
              Name = match.Groups[GROUP_ARTIST].Value.Trim(new[] { ' ', '-' }),
              Occupation = PersonAspect.OCCUPATION_ARTIST,
              ParentMediaName = trackInfo.Album,
              MediaName = trackInfo.TrackName
            }
          };
          trackInfo.HasChanged |= MetadataUpdater.SetOrUpdateList(trackInfo.Artists, artists, true);

          List<PersonInfo> albumArtists = new List<PersonInfo>()
          {
            new PersonInfo()
            {
              Name = match.Groups[GROUP_ARTIST].Value.Trim(new[] { ' ', '-' }),
              Occupation = PersonAspect.OCCUPATION_ARTIST,
              ParentMediaName = trackInfo.Album,
              MediaName = trackInfo.TrackName
            }
          };
          trackInfo.HasChanged |= MetadataUpdater.SetOrUpdateList(trackInfo.AlbumArtists, albumArtists, true);

          if (match.Groups[GROUP_TRACK_NUM].Length > 0)
            trackInfo.HasChanged |= MetadataUpdater.SetOrUpdateValue(ref trackInfo.TrackNum, Convert.ToInt32(match.Groups[GROUP_TRACK_NUM].Value));

          if (match.Groups[GROUP_DISC_NUM].Length > 0)
            trackInfo.HasChanged |= MetadataUpdater.SetOrUpdateValue(ref trackInfo.DiscNum, Convert.ToInt32(match.Groups[GROUP_DISC_NUM].Value));
          return true;
        }
      }
      return false;
    }

    public static bool CleanupTrack(TrackInfo trackInfo)
    {
      string originalTrack = trackInfo.TrackName;
      string originalAlbum = trackInfo.Album;
      foreach (Regex regex in REGEXP_CLEANUPS)
        trackInfo.TrackName = regex.Replace(trackInfo.TrackName, "");
      trackInfo.TrackName = CleanupWhiteSpaces(trackInfo.TrackName);
      foreach (Regex regex in REGEXP_CLEANUPS)
        trackInfo.Album = regex.Replace(trackInfo.Album, "");
      trackInfo.Album = CleanupWhiteSpaces(trackInfo.Album);
      return originalTrack != trackInfo.TrackName || originalAlbum != trackInfo.Album;
    }

    /// <summary>
    /// Cleans up strings by replacing unwanted characters (<c>'.'</c>, <c>'_'</c>) by spaces.
    /// </summary>
    public static string CleanupWhiteSpaces(string str)
    {
      if (string.IsNullOrEmpty(str))
        return str;
      str = _cleanUpWhiteSpaces.Replace(str, " $1");
      //replace multiple spaces with single space
      return _trimWhiteSpaces.Replace(str, " ").Trim(' ', '-');
    }
  }
}
