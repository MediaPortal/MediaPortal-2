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

using MediaPortal.Common;
using MediaPortal.Extensions.OnlineLibraries.Libraries.FanArtTVV3;
using MediaPortal.Extensions.OnlineLibraries.Libraries.FanArtTVV3.Data;
using MediaPortal.Common.PathManager;

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
    public bool Init()
    {
      string cache = ServiceRegistration.Get<IPathManager>().GetPath(@"<DATA>\FanArtTV\");
      _fanArtTvHandler = new FanArtTVApiV3("53b9498b23f38abf1e1cbe11de2f8102", cache);
      return true;
    }

    public bool DownloadArtistBanners(string musicBrainzId)
    {
      ArtistThumbs thumbs = _fanArtTvHandler.GetArtistThumbs(musicBrainzId);
      if (thumbs == null) return false;
      return _fanArtTvHandler.DownloadArtistBanners(thumbs);
    }

    public bool DownloadArtistFanArt(string musicBrainzId)
    {
      ArtistThumbs thumbs = _fanArtTvHandler.GetArtistThumbs(musicBrainzId);
      if (thumbs == null) return false;
      return _fanArtTvHandler.DownloadArtistFanArt(thumbs);
    }

    public bool DownloadArtistLogos(string musicBrainzId)
    {
      ArtistThumbs thumbs = _fanArtTvHandler.GetArtistThumbs(musicBrainzId);
      if (thumbs == null) return false;
      return _fanArtTvHandler.DownloadArtistLogos(thumbs);
    }

    public bool DownloadArtistThumbs(string musicBrainzId)
    {
      ArtistThumbs thumbs = _fanArtTvHandler.GetArtistThumbs(musicBrainzId);
      if (thumbs == null) return false;
      return _fanArtTvHandler.DownloadArtistThumbs(thumbs);
    }

    public bool DownloadAlbumCovers(string musicBrainzId)
    {
      AlbumDetails thumbs = _fanArtTvHandler.GetAlbumThumbs(musicBrainzId);
      if (thumbs == null) return false;
      return _fanArtTvHandler.DownloadAlbumCovers(thumbs);
    }

    public bool DownloadAlbumCDArt(string musicBrainzId)
    {
      AlbumDetails thumbs = _fanArtTvHandler.GetAlbumThumbs(musicBrainzId);
      if (thumbs == null) return false;
      return _fanArtTvHandler.DownloadAlbumCDArt(thumbs);
    }

    public bool DownloadLabelLogos(string musicBrainzId)
    {
      LabelThumbs thumbs = _fanArtTvHandler.GetLabelThumbs(musicBrainzId);
      if (thumbs == null) return false;
      return _fanArtTvHandler.DownloadLabelLogos(thumbs);
    }

    public bool DownloadMovieBanners(string imDbIdOrtmDbId)
    {
      MovieThumbs thumbs = _fanArtTvHandler.GetMovieThumbs(imDbIdOrtmDbId);
      if (thumbs == null) return false;
      return _fanArtTvHandler.DownloadMovieBanners(thumbs, PreferredLanguage);
    }

    public bool DownloadMovieCDArt(string imDbIdOrtmDbId)
    {
      MovieThumbs thumbs = _fanArtTvHandler.GetMovieThumbs(imDbIdOrtmDbId);
      if (thumbs == null) return false;
      return _fanArtTvHandler.DownloadMovieCDArt(thumbs, PreferredLanguage);
    }

    public bool DownloadMovieClearArt(string imDbIdOrtmDbId)
    {
      MovieThumbs thumbs = _fanArtTvHandler.GetMovieThumbs(imDbIdOrtmDbId);
      if (thumbs == null) return false;
      return _fanArtTvHandler.DownloadMovieClearArt(thumbs, PreferredLanguage);
    }

    public bool DownloadMovieFanArt(string imDbIdOrtmDbId)
    {
      MovieThumbs thumbs = _fanArtTvHandler.GetMovieThumbs(imDbIdOrtmDbId);
      if (thumbs == null) return false;
      return _fanArtTvHandler.DownloadMovieFanArt(thumbs, PreferredLanguage);
    }

    public bool DownloadMovieLogos(string imDbIdOrtmDbId)
    {
      MovieThumbs thumbs = _fanArtTvHandler.GetMovieThumbs(imDbIdOrtmDbId);
      if (thumbs == null) return false;
      return _fanArtTvHandler.DownloadMovieLogos(thumbs, PreferredLanguage);
    }

    public bool DownloadMoviePosters(string imDbIdOrtmDbId)
    {
      MovieThumbs thumbs = _fanArtTvHandler.GetMovieThumbs(imDbIdOrtmDbId);
      if (thumbs == null) return false;
      return _fanArtTvHandler.DownloadMoviePosters(thumbs, PreferredLanguage);
    }

    public bool DownloadMovieThumbs(string imDbIdOrtmDbId)
    {
      MovieThumbs thumbs = _fanArtTvHandler.GetMovieThumbs(imDbIdOrtmDbId);
      if (thumbs == null) return false;
      return _fanArtTvHandler.DownloadMovieThumbs(thumbs, PreferredLanguage);
    }

    public bool DownloadShowBanners(string ttvDbId, bool includeSeasons)
    {
      TVThumbs thumbs = _fanArtTvHandler.GetShowThumbs(ttvDbId);
      if (thumbs == null) return false;
      return _fanArtTvHandler.DownloadShowBanners(thumbs, PreferredLanguage, includeSeasons);
    }

    public bool DownloadShowPosters(string ttvDbId, bool includeSeasons)
    {
      TVThumbs thumbs = _fanArtTvHandler.GetShowThumbs(ttvDbId);
      if (thumbs == null) return false;
      return _fanArtTvHandler.DownloadShowPosters(thumbs, PreferredLanguage, includeSeasons);
    }

    public bool DownloadShowThumbs(string ttvDbId, bool includeSeasons)
    {
      TVThumbs thumbs = _fanArtTvHandler.GetShowThumbs(ttvDbId);
      if (thumbs == null) return false;
      return _fanArtTvHandler.DownloadShowThumbs(thumbs, PreferredLanguage, includeSeasons);
    }

    public bool DownloadShowClearArt(string ttvDbId)
    {
      TVThumbs thumbs = _fanArtTvHandler.GetShowThumbs(ttvDbId);
      if (thumbs == null) return false;
      return _fanArtTvHandler.DownloadShowClearArt(thumbs, PreferredLanguage);
    }

    public bool DownloadShowFanArt(string ttvDbId)
    {
      TVThumbs thumbs = _fanArtTvHandler.GetShowThumbs(ttvDbId);
      if (thumbs == null) return false;
      return _fanArtTvHandler.DownloadShowFanArt(thumbs, PreferredLanguage);
    }

    public bool DownloadShowLogos(string ttvDbId)
    {
      TVThumbs thumbs = _fanArtTvHandler.GetShowThumbs(ttvDbId);
      if (thumbs == null) return false;
      return _fanArtTvHandler.DownloadShowLogos(thumbs, PreferredLanguage);
    }
  }
}
