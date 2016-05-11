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

using MediaPortal.Extensions.OnlineLibraries.Libraries.FanArtTVV3;
using MediaPortal.Extensions.OnlineLibraries.Libraries.FanArtTVV3.Data;

namespace MediaPortal.Extensions.OnlineLibraries.FanArtTV
{
  class FanArtTVWrapper
  {
    protected FanArtTVApiV3 _fanArtTvHandler;
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
      get { return _preferredLanguage ?? FanArtTVApiV3.DefaultLanguage; }
    }

    /// <summary>
    /// Initializes the library. Needs to be called at first.
    /// </summary>
    /// <returns></returns>
    public bool Init(string cachePath)
    {
      _fanArtTvHandler = new FanArtTVApiV3("53b9498b23f38abf1e1cbe11de2f8102", cachePath);
      return true;
    }

    public bool DownloadFanArt(string id, FanArtThumb thumb, string category)
    {
      return _fanArtTvHandler.DownloadImage(id, thumb, category);
    }

    public byte[] GetFanArt(string id, FanArtThumb thumb, string category)
    {
      return _fanArtTvHandler.GetImage(id, thumb, category);
    }

    public bool GetArtistFanArt(string musicBrainzId, out FanArtArtistThumbs artistThumbs)
    {
      artistThumbs = _fanArtTvHandler.GetArtistThumbs(musicBrainzId);
      return artistThumbs != null;
    }

    public bool GetAlbumFanArt(string musicBrainzId, out FanArtAlbumDetails albumThumbs)
    {
      albumThumbs = _fanArtTvHandler.GetAlbumThumbs(musicBrainzId);
      return albumThumbs != null;
    }

    public bool GetLabelFanArt(string musicBrainzId, out FanArtLabelThumbs labelThumbs)
    {
      labelThumbs = _fanArtTvHandler.GetLabelThumbs(musicBrainzId);
      return labelThumbs != null;
    }

    public bool GetMovieFanArt(string imDbIdOrtmDbId, out FanArtMovieThumbs movieThumbs)
    {
      movieThumbs = _fanArtTvHandler.GetMovieThumbs(imDbIdOrtmDbId);
      return movieThumbs != null;
    }

    public bool GetSeriesFanArt(string ttvDbId, out FanArtTVThumbs tvThumbs)
    {
      tvThumbs = _fanArtTvHandler.GetSeriesThumbs(ttvDbId);
      return tvThumbs != null;
    }
  }
}
