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
    Task<IEnumerable<TrackInfo>> FindMatchingTracksAsync(TrackInfo trackInfo, string category);
    Task<IEnumerable<AlbumInfo>> FindMatchingAlbumsAsync(AlbumInfo albumInfo, string category);
    Task<bool> FindAndUpdateTrackAsync(TrackInfo trackInfo, string category);
    Task<bool> UpdateAlbumPersonsAsync(AlbumInfo albumInfo, string occupation, string category);
    Task<bool> UpdateTrackPersonsAsync(TrackInfo trackInfo, string occupation, bool forAlbum, string category);
    Task<bool> UpdateAlbumCompaniesAsync(AlbumInfo albumInfo, string companyType, string category);
    Task<bool> UpdateAlbumAsync(AlbumInfo albumInfo, bool updateTrackList, string category);
    Task<bool> DownloadAudioFanArtAsync(Guid mediaItemId, BaseInfo mediaItemInfo);
    void StoreAudioPersonMatch(PersonInfo person);
    void StoreAudioCompanyMatch(CompanyInfo company);

    #endregion

    #region Movie

    List<MovieInfo> GetLastChangedMovies();
    void ResetLastChangedMovies();
    List<MovieCollectionInfo> GetLastChangedMovieCollections();
    void ResetLastChangedMovieCollections();
    Task<IEnumerable<MovieInfo>> FindMatchingMoviesAsync(MovieInfo movieInfo, string category);
    Task<bool> FindAndUpdateMovieAsync(MovieInfo movieInfo, string category);
    Task<bool> UpdatePersonsAsync(MovieInfo movieInfo, string occupation, string category);
    Task<bool> UpdateCharactersAsync(MovieInfo movieInfo, string category);
    Task<bool> UpdateCollectionAsync(MovieCollectionInfo collectionInfo, bool updateMovieList, string category);
    Task<bool> UpdateCompaniesAsync(MovieInfo movieInfo, string companyType, string category);
    Task<bool> DownloadMovieFanArtAsync(Guid mediaItemId, BaseInfo mediaItemInfo);
    void StoreMoviePersonMatch(PersonInfo person);
    void StoreMovieCharacterMatch(CharacterInfo character);
    void StoreMovieCompanyMatch(CompanyInfo company);

    #endregion

    #region Series

    List<SeriesInfo> GetLastChangedSeries();
    void ResetLastChangedSeries();
    List<EpisodeInfo> GetLastChangedEpisodes();
    void ResetLastChangedEpisodes();
    Task<IEnumerable<EpisodeInfo>> FindMatchingEpisodesAsync(EpisodeInfo episodeInfo, string category);
    Task<IEnumerable<SeriesInfo>> FindMatchingSeriesAsync(SeriesInfo seriesInfo, string category);
    Task<bool> FindAndUpdateEpisodeAsync(EpisodeInfo episodeInfo, string category);
    Task<bool> UpdateEpisodePersonsAsync(EpisodeInfo episodeInfo, string occupation, string category);
    Task<bool> UpdateEpisodeCharactersAsync(EpisodeInfo episodeInfo, string category);
    Task<bool> UpdateSeasonAsync(SeasonInfo seasonInfo, string category);
    Task<bool> UpdateSeriesAsync(SeriesInfo seriesInfo, bool updateEpisodeList, string category);
    Task<bool> UpdateSeriesPersonsAsync(SeriesInfo seriesInfo, string occupation, string category);
    Task<bool> UpdateSeriesCharactersAsync(SeriesInfo seriesInfo, string category);
    Task<bool> UpdateSeriesCompaniesAsync(SeriesInfo seriesInfo, string companyType, string category);
    Task<bool> DownloadSeriesFanArtAsync(Guid mediaItemId, BaseInfo mediaItemInfo);
    void StoreSeriesPersonMatch(PersonInfo person);
    void StoreSeriesCharacterMatch(CharacterInfo character);
    void StoreSeriesCompanyMatch(CompanyInfo company);

    #endregion

    #region Subtitles

    Task<IEnumerable<SubtitleInfo>> FindMatchingEpisodeSubtitlesAsync(SubtitleInfo subtitleInfo, List<string> languages, string category);
    Task<IEnumerable<SubtitleInfo>> FindMatchingMovieSubtitlesAsync(SubtitleInfo subtitleInfo, List<string> languages, string category);
    Task<bool> DownloadSubtitleAsync(SubtitleInfo subtitleInfo, bool overwriteExisting, string category = null);

    #endregion
  }
}
