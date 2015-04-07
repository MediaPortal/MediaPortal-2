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

namespace MediaPortal.Extensions.OnlineLibraries.MusicBrainz
{
  class MusicBrainzWrapper
  {
    protected MusicBrainzApiV2 _musicBrainzHandler;
    protected string _preferredLanguage;
    public const int MAX_LEVENSHTEIN_DIST = 4;

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

    public bool SearchTrack(string title, string artist, string album, string genre, int year, int trackNum, string albumArtist, string language, out IList<TrackSearchResult> tracks)
    {
      tracks = _musicBrainzHandler.SearchTrack(title, artist, album, genre, year, trackNum, language);
      return tracks.Count > 0;
    }

    public bool SearchTrackUnique(string title, string artist, string album, string genre, int year, int trackNum, string albumArtist, string language, out IList<TrackSearchResult> tracks)
    {
      language = language ?? PreferredLanguage;
      tracks = _musicBrainzHandler.SearchTrack(title, artist, album, genre, year, trackNum, language);
      if (TestMatch(title, artist, album, genre, year, trackNum, language, ref tracks))
        return true;

      if (tracks.Count == 0 && language != MusicBrainzApiV2.DefaultLanguage)
      {
        tracks = _musicBrainzHandler.SearchTrack(title, artist, album, genre, year, trackNum, MusicBrainzApiV2.DefaultLanguage);
        return tracks.Count == 1;
      }
      return false;
    }

    private bool TestMatch(string title, string artist, string album, string genre, int year, int trackNum, string language, ref IList<TrackSearchResult> tracks)
    {
      foreach (TrackSearchResult track in tracks)
      {
        if (trackNum > 0 && track.TrackNum == trackNum)
        {
          return true;
        }
      }
      return true;
    }

    public bool GetTrack(string musicBrainzId, out Track trackDetail)
    {
      trackDetail = _musicBrainzHandler.GetTrack(musicBrainzId, PreferredLanguage);
      return trackDetail != null;
    }

    /// <summary>
    /// Gets images for the requested movie.
    /// </summary>
    /// <param name="id">MusicBrainz ID of track</param>
    /// <param name="imageCollection">Returns the ImageCollection</param>
    /// <returns><c>true</c> if successful</returns>
    public bool GetTrackFanArt(string id, out ImageCollection imageCollection)
    {
      imageCollection = _musicBrainzHandler.GetImages(id, null); // Download all image information, filter later!
      return imageCollection != null;
    }

    public bool DownloadImage(TrackImage image, string category)
    {
      return _musicBrainzHandler.DownloadImage(image, category);
    }

    public bool DownloadImages(TrackCollection trackCollection)
    {
      return _musicBrainzHandler.DownloadImages(trackCollection);
    }
  }
}
