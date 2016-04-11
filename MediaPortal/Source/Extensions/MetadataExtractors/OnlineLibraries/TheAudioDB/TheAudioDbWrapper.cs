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
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Extensions.OnlineLibraries.Libraries.AudioDbV1;
using MediaPortal.Extensions.OnlineLibraries.Libraries.AudioDbV1.Data;
using MediaPortal.Utilities;

namespace MediaPortal.Extensions.OnlineLibraries.TheAudioDB
{
  class TheAudioDbWrapper
  {
    protected AudioDbApiV1 _audioDbHandler;
    protected string _preferredLanguage;
    public const int MAX_LEVENSHTEIN_DIST = 4;

    private enum ValueToCheck
    {
      ArtistLax,
      AlbumLax,
      TrackNum,
      ArtistStrict,
      AlbumStrict,
      Image,
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
      get { return _preferredLanguage ?? AudioDbApiV1.DefaultLanguage; }
    }

    /// <summary>
    /// Initializes the library. Needs to be called at first.
    /// </summary>
    /// <returns></returns>
    public bool Init(string cachePath)
    {
      _audioDbHandler = new AudioDbApiV1("975376238723lcbzmsjwq98", cachePath);
      return true;
    }

    public bool SearchTrack(string title, List<string> artists, out List<AudioDbTrack> tracks)
    {
      tracks = new List<AudioDbTrack>();
      foreach (string artist in artists) tracks.AddRange(_audioDbHandler.SearchTrack(artist, title));
      foreach (AudioDbTrack track in tracks) track.SetLanguage(PreferredLanguage);
      return tracks.Count > 0;
    }

    public bool SearchTrackUnique(string title, List<string> artists, string album, int trackNum, out List<AudioDbTrack> tracks)
    {
      tracks = new List<AudioDbTrack>();
      foreach (string artist in artists) tracks.AddRange(_audioDbHandler.SearchTrack(artist, title));
      foreach (AudioDbTrack track in tracks) track.SetLanguage(PreferredLanguage);
      if (TestMatch(title, artists, album, trackNum, ref tracks))
        return true;
      return false;
    }

    private bool TestMatch(string title, List<string> artists, string album, int trackNum, ref List<AudioDbTrack> tracks)
    {
      if (tracks.Count == 1)
      {
        if (GetLevenshteinDistance(tracks[0].Track, title) <= MAX_LEVENSHTEIN_DIST)
        {
          ServiceRegistration.Get<ILogger>().Debug("TheAudioDbWrapper: Unique match found \"{0}\"!", title);
          return true;
        }
        // No valid match, clear list to allow further detection ways
        tracks.Clear();
        return false;
      }

      // Multiple matches
      if (tracks.Count > 1)
      {
        ServiceRegistration.Get<ILogger>().Debug("TheAudioDbWrapper: Multiple matches for \"{0}\" ({1}). Try to find exact name match.", title, tracks.Count);
        var exactMatches = tracks.FindAll(s => s.Track == title || GetLevenshteinDistance(s.Track, title) == 0);
        if (exactMatches.Count == 1)
        {
          ServiceRegistration.Get<ILogger>().Debug("TheAudioDbWrapper: Unique match found \"{0}\"!", title);
          tracks = exactMatches;
          return true;
        }

        if (exactMatches.Count > 1)
        {
          var lastGood = exactMatches;
          foreach (ValueToCheck checkValue in Enum.GetValues(typeof(ValueToCheck)))
          {
            if (checkValue == ValueToCheck.ArtistLax && artists != null && artists.Count > 0)
              exactMatches = exactMatches.FindAll(s => CompareArtists(s.Artist, artists, false));

            if (checkValue == ValueToCheck.AlbumLax && !string.IsNullOrEmpty(album))
              exactMatches = exactMatches.FindAll(s => GetLevenshteinDistance(s.Album, album) <= MAX_LEVENSHTEIN_DIST);

            if (checkValue == ValueToCheck.ArtistStrict && artists != null && artists.Count > 0)
              exactMatches = exactMatches.FindAll(s => CompareArtists(s.Artist, artists, true));

            if (checkValue == ValueToCheck.AlbumStrict && !string.IsNullOrEmpty(album))
              exactMatches = exactMatches.FindAll(s => s.Album == album || GetLevenshteinDistance(s.Album, album) == 0);

            if (checkValue == ValueToCheck.TrackNum && trackNum > 0)
              exactMatches = exactMatches.FindAll(s => s.TrackNumber > 0 && s.TrackNumber == trackNum);

            if (checkValue == ValueToCheck.Image)
              exactMatches = exactMatches.FindAll(s => !string.IsNullOrEmpty(s.TrackThumb));

            if (exactMatches.Count == 0) //Too many were removed restore last good
              exactMatches = lastGood;
            else
              lastGood = exactMatches;

            if (exactMatches.Count == 1)
            {
              ServiceRegistration.Get<ILogger>().Debug("TheAudioDbWrapper: Unique match found \"{0}\" [{1}]!", title, checkValue.ToString());
              tracks = exactMatches;
              return true;
            }
          }

          tracks = lastGood;
        }

        if (tracks.Count > 1)
          ServiceRegistration.Get<ILogger>().Debug("TheAudioDbWrapper: Multiple matches found for \"{0}\" (count: {1})", string.Join(",", artists), tracks.Count);

        return tracks.Count == 1;
      }
      return false;
    }

    private bool CompareArtists(string trackArtist, List<string> searchArtists, bool strict)
    {
      if (strict)
      {
        foreach (string artist in searchArtists)
          if (trackArtist == artist || GetLevenshteinDistance(trackArtist, artist) == 0)
            return true;
      }
      else
      {
        foreach (string artist in searchArtists)
          if (GetLevenshteinDistance(trackArtist, artist) <= MAX_LEVENSHTEIN_DIST)
            return true;
      }
      return false;
    }

    public bool GetTrackFromId(long id, out AudioDbTrack trackDetail)
    {
      trackDetail = _audioDbHandler.GetTrackByTadb(id);
      if (trackDetail != null) trackDetail.SetLanguage(PreferredLanguage);
      return trackDetail != null;
    }

    public bool GetTrackFromMBId(string mbId, out AudioDbTrack trackDetail)
    {
      trackDetail = _audioDbHandler.GetTrackByMbid(mbId);
      if (trackDetail != null) trackDetail.SetLanguage(PreferredLanguage);
      return trackDetail != null;
    }

    public bool GetAlbumFromId(long id, out AudioDbAlbum albumDetail)
    {
      albumDetail = _audioDbHandler.GetAlbumByTadb(id);
      if (albumDetail != null) albumDetail.SetLanguage(PreferredLanguage);
      return albumDetail != null;
    }

    public bool GetArtistFromId(long id, out AudioDbArtist artistDetail)
    {
      artistDetail = _audioDbHandler.GetArtistByTadb(id);
      if (artistDetail != null) artistDetail.SetLanguage(PreferredLanguage);
      return artistDetail != null;
    }

    public bool DownloadImage(long id, string url, string category)
    {
      if (id <= 0) return false;
      if (string.IsNullOrEmpty(url)) return false;
      if (string.IsNullOrEmpty(category)) return false;
      return _audioDbHandler.DownloadImage(id, url, category);
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
  }
}
