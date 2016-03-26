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

using System;
using System.Collections.Generic;
using MediaPortal.Extensions.OnlineLibraries.Libraries.MusicBrainzV2;
using MediaPortal.Extensions.OnlineLibraries.Libraries.MusicBrainzV2.Data;
using MediaPortal.Utilities;
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.Logging;

namespace MediaPortal.Extensions.OnlineLibraries.MusicBrainz
{
  class MusicBrainzWrapper
  {
    protected MusicBrainzApiV2 _musicBrainzHandler;
    protected string _preferredLanguage;
    public const int MAX_LEVENSHTEIN_DIST = 4;

    private enum ValueToCheck
    {
      ArtistLax,
      AlbumLax,
      Year,
      TrackNum,
      ArtistStrict,
      AlbumStrict,
      Compilation,
      Image,
      Barcode,
      Discs,
      Country,
    }

    /// <summary>
    /// Sets the preferred language in short format like: en, de, ...
    /// </summary>
    /// <param name="langShort">Short language</param>
    public void SetPreferredLanguage(string langShort)
    {
      _preferredLanguage = langShort;
    }

    /// <summary>
    /// Returns the language that matches the value set by <see cref="SetPreferredLanguage"/> or the default language (en).
    /// </summary>
    public string PreferredLanguage
    {
      get { return _preferredLanguage ?? MusicBrainzApiV2.DefaultLanguage; }
    }

    /// <summary>
    /// Initializes the library. Needs to be called at first.
    /// </summary>
    /// <returns></returns>
    public bool Init()
    {
      _musicBrainzHandler = new MusicBrainzApiV2(MusicBrainzMatcher.CACHE_PATH);
      return true;
    }

    public bool SearchTrack(string title, string[] artists, string album, int year, int trackNum, out List<TrackResult> tracks)
    {
      tracks = _musicBrainzHandler.SearchTrack(title, artists, album, year, trackNum);
      return tracks.Count > 0;
    }

    public bool SearchTrackUnique(string title, string[] artists, string album, int year, int trackNum, out List<TrackResult> tracks)
    {
      tracks = _musicBrainzHandler.SearchTrack(title, artists, album, year, trackNum);
      if (TestMatch(title, artists, album, year, trackNum, ref tracks))
        return true;

      return false;
    }

    private bool TestMatch(string title, string[] artists, string album, int year, int trackNum, ref List<TrackResult> tracks)
    {
      if (tracks.Count == 1)
      {
        if (GetLevenshteinDistance(tracks[0].Title, title) <= MAX_LEVENSHTEIN_DIST)
        {
          ServiceRegistration.Get<ILogger>().Debug("MusicBrainzWrapper: Unique match found \"{0}\"!", title);
          return true;
        }
        // No valid match, clear list to allow further detection ways
        tracks.Clear();
        return false;
      }

      // Multiple matches
      if (tracks.Count > 1)
      {
        ServiceRegistration.Get<ILogger>().Debug("MusicBrainzWrapper: Multiple matches for \"{0}\" ({1}). Try to find exact name match.", title, tracks.Count);
        var exactMatches = tracks.FindAll(s => s.Title == title || GetLevenshteinDistance(s.Title, title) == 0);
        if (exactMatches.Count == 1)
        {
          ServiceRegistration.Get<ILogger>().Debug("MusicBrainzWrapper: Unique match found \"{0}\"!", title);
          tracks = exactMatches;
          return true;
        }

        if (exactMatches.Count > 1)
        {
          var lastGood = exactMatches;
          foreach (ValueToCheck checkValue in Enum.GetValues(typeof(ValueToCheck)))
          {
            if (checkValue == ValueToCheck.ArtistLax && artists != null && artists.Length > 0)
              exactMatches = exactMatches.FindAll(s => CompareArtists(s.Artists.ToArray(), artists, false));

            if (checkValue == ValueToCheck.AlbumLax && !string.IsNullOrEmpty(album))
              exactMatches = exactMatches.FindAll(s => GetLevenshteinDistance(s.Album, album) <= MAX_LEVENSHTEIN_DIST);

            if (checkValue == ValueToCheck.ArtistStrict && artists != null && artists.Length > 0)
              exactMatches = exactMatches.FindAll(s => CompareArtists(s.Artists.ToArray(), artists, true));

            if (checkValue == ValueToCheck.AlbumStrict && !string.IsNullOrEmpty(album))
              exactMatches = exactMatches.FindAll(s => s.Album == album || GetLevenshteinDistance(s.Album, album) == 0);

            if (checkValue == ValueToCheck.Year && year > 0)
              exactMatches = exactMatches.FindAll(s => s.ReleaseDate.HasValue && s.ReleaseDate.Value.Year == year);

            if (checkValue == ValueToCheck.TrackNum && trackNum > 0)
              exactMatches = exactMatches.FindAll(s => s.TrackNum > 0 && s.TrackNum == trackNum);

            if (checkValue == ValueToCheck.Barcode)
              exactMatches = exactMatches.FindAll(s => !string.IsNullOrEmpty(s.AlbumBarcode));

            if (checkValue == ValueToCheck.Discs && trackNum > 0)
              exactMatches = exactMatches.FindAll(s => s.DiscCount > 0);

            if (checkValue == ValueToCheck.Country)
            {
              exactMatches = exactMatches.FindAll(s => !string.IsNullOrEmpty(s.Country) && s.Country.Equals(PreferredLanguage, StringComparison.InvariantCultureIgnoreCase));
              if (exactMatches.Count == 0 && MusicBrainzApiV2.DefaultLanguage != PreferredLanguage)
                exactMatches = lastGood.FindAll(s => !string.IsNullOrEmpty(s.Country) && s.Country.Equals(MusicBrainzApiV2.DefaultLanguage, StringComparison.InvariantCultureIgnoreCase));
              if (exactMatches.Count == 0) //Try european releases
                exactMatches = lastGood.FindAll(s => !string.IsNullOrEmpty(s.Country) && s.Country.Equals("XE", StringComparison.InvariantCultureIgnoreCase));
            }

            if (checkValue == ValueToCheck.Compilation)
              exactMatches = exactMatches.FindAll(s => s.FromCompilation == false);

            if (checkValue == ValueToCheck.Image)
              exactMatches = exactMatches.FindAll(s => _musicBrainzHandler.HasImages(s.AlbumId));

            if (exactMatches.Count == 0) //Too many were removed restore last good
              exactMatches = lastGood;
            else
              lastGood = exactMatches;

            if (exactMatches.Count == 1)
            {
              ServiceRegistration.Get<ILogger>().Debug("MusicBrainzWrapper: Unique match found \"{0}\" [{1}]!", title, checkValue.ToString());
              tracks = exactMatches;
              return true;
            }
          }

          tracks = lastGood;
        }

        if (tracks.Count > 1)
          ServiceRegistration.Get<ILogger>().Debug("MusicBrainzWrapper: Multiple matches found for \"{0}\" (count: {1})", string.Join(",", artists), tracks.Count);

        return tracks.Count == 1;
      }
      return false;
    }

    private bool CompareArtists(string[] trackArtits, string[] searchArtists, bool strict)
    {
      if(strict)
      {
        int matchCount = 0;
        foreach (string artist in searchArtists)
          foreach (string trackArtist in trackArtits)
            if (trackArtist == artist || GetLevenshteinDistance(trackArtist, artist) == 0)
            {
              matchCount++;
              break;
            }
        return matchCount >= searchArtists.Length;
      }
      else
      {
        foreach (string artist in searchArtists)
          foreach (string trackArtist in trackArtits)
            if (GetLevenshteinDistance(trackArtist, artist) <= MAX_LEVENSHTEIN_DIST)
              return true;
      }
      return false;
    }

    /// <summary>
    /// Returns the Levenshtein distance for a <paramref name="trackName"/> and a given <paramref name="searchName"/>.
    /// </summary>
    /// <param name="trackName">Track name to check</param>
    /// <param name="searchName">Track name to find</param>
    /// <returns>Levenshtein distance</returns>
    protected int GetLevenshteinDistance(string trackName, string searchName)
    {
      string cleanedName = RemoveCharacters(searchName);
      return StringUtils.GetLevenshteinDistance(RemoveCharacters(trackName), cleanedName);
    }

    /// <summary>
    /// Replaces characters that are not necessary for comparing (like whitespaces) and diacritics. The result is returned as <see cref="string.ToLowerInvariant"/>.
    /// </summary>
    /// <param name="name">Name to clean up</param>
    /// <returns>Cleaned string</returns>
    protected string RemoveCharacters(string name)
    {
      name = name.ToLowerInvariant();
      string result = new[] { "-", ",", "/", ":", " ", " ", ".", "'", "(", ")", "[", "]" }.Aggregate(name, (current, s) => current.Replace(s, ""));
      result = result.Replace("&", "and");
      return StringUtils.RemoveDiacritics(result);
    }

    public bool GetTrack(string musicBrainzId, out Track trackDetail)
    {
      trackDetail = _musicBrainzHandler.GetTrack(musicBrainzId, PreferredLanguage);
      return trackDetail != null;
    }

    /// <summary>
    /// Gets images for the requested movie.
    /// </summary>
    /// <param name="albumId">MusicBrainz ID of album</param>
    /// <param name="imageCollection">Returns the ImageCollection</param>
    /// <returns><c>true</c> if successful</returns>
    public bool GetTrackFanArt(string albumId, out TrackImageCollection imageCollection)
    {
      imageCollection = _musicBrainzHandler.GetImages(albumId, PreferredLanguage); // Download all image information, filter later!
      return imageCollection != null;
    }

    public bool DownloadImages(string Id, TrackImageCollection imageCollection)
    {
      return _musicBrainzHandler.DownloadImages(Id, imageCollection, "Front");
    }
  }
}
