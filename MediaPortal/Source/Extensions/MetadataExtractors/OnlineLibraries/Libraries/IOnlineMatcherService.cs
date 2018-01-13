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

using MediaPortal.Common.MediaManagement.Helpers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries
{
  /// <summary>
  /// Service to download metadata from online sources.
  /// </summary>
  public interface IOnlineMatcherService
  {
    #region Audio

    List<AlbumInfo> GetLastChangedAudioAlbums();
    void ResetLastChangedAudioAlbums();
    List<TrackInfo> GetLastChangedAudio();
    void ResetLastChangedAudio();
    Task<bool> FindAndUpdateTrackAsync(TrackInfo trackInfo, bool importOnly);
    Task<bool> UpdateAlbumPersonsAsync(AlbumInfo albumInfo, string occupation, bool importOnly);
    Task<bool> UpdateTrackPersonsAsync(TrackInfo trackInfo, string occupation, bool forAlbum, bool importOnly);
    Task<bool> UpdateAlbumCompaniesAsync(AlbumInfo albumInfo, string companyType, bool importOnly);
    Task<bool> UpdateAlbumAsync(AlbumInfo albumInfo, bool updateTrackList, bool importOnly);
    bool DownloadAudioFanArt(Guid mediaItemId, BaseInfo mediaItemInfo, bool force);
    void StoreAudioPersonMatch(PersonInfo person);
    void StoreAudioCompanyMatch(CompanyInfo company);

    #endregion

    #region Movie

    List<MovieInfo> GetLastChangedMovies();
    void ResetLastChangedMovies();
    List<MovieCollectionInfo> GetLastChangedMovieCollections();
    void ResetLastChangedMovieCollections();
    Task<bool> FindAndUpdateMovieAsync(MovieInfo movieInfo, bool importOnly);
    Task<bool> UpdatePersonsAsync(MovieInfo movieInfo, string occupation, bool importOnly);
    Task<bool> UpdateCharactersAsync(MovieInfo movieInfo, bool importOnly);
    Task<bool> UpdateCollectionAsync(MovieCollectionInfo collectionInfo, bool updateMovieList, bool importOnly);
    Task<bool> UpdateCompaniesAsync(MovieInfo movieInfo, string companyType, bool importOnly);
    bool DownloadMovieFanArt(Guid mediaItemId, BaseInfo mediaItemInfo, bool force);
    void StoreMoviePersonMatch(PersonInfo person);
    void StoreMovieCharacterMatch(CharacterInfo character);
    void StoreMovieCompanyMatch(CompanyInfo company);

    #endregion

    #region Series

    List<SeriesInfo> GetLastChangedSeries();
    void ResetLastChangedSeries();
    List<EpisodeInfo> GetLastChangedEpisodes();
    void ResetLastChangedEpisodes();
    Task<bool> FindAndUpdateEpisodeAsync(EpisodeInfo episodeInfo, bool importOnly);
    Task<bool> UpdateEpisodePersonsAsync(EpisodeInfo episodeInfo, string occupation, bool importOnly);
    Task<bool> UpdateEpisodeCharactersAsync(EpisodeInfo episodeInfo, bool importOnly);
    Task<bool> UpdateSeasonAsync(SeasonInfo seasonInfo, bool importOnly);
    Task<bool> UpdateSeriesAsync(SeriesInfo seriesInfo, bool updateEpisodeList, bool importOnly);
    Task<bool> UpdateSeriesPersonsAsync(SeriesInfo seriesInfo, string occupation, bool importOnly);
    Task<bool> UpdateSeriesCharactersAsync(SeriesInfo seriesInfo, bool importOnly);
    Task<bool> UpdateSeriesCompaniesAsync(SeriesInfo seriesInfo, string companyType, bool importOnly);
    bool DownloadSeriesFanArt(Guid mediaItemId, BaseInfo mediaItemInfo, bool force);
    void StoreSeriesPersonMatch(PersonInfo person);
    void StoreSeriesCharacterMatch(CharacterInfo character);
    void StoreSeriesCompanyMatch(CompanyInfo company);

    #endregion
  }
}
