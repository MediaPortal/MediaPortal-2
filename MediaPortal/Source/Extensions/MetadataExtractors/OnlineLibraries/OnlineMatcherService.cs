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

using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Extensions.OnlineLibraries.Matchers;
using System;
using System.Collections.Generic;
using System.Threading;

namespace MediaPortal.Extensions.OnlineLibraries
{
  /// <summary>
  /// <see cref="OnlineMatcherService"/> searches for metadata from online sources.
  /// </summary>
  public class OnlineMatcherService
  {
    #region Audio

    public static bool FindAndUpdateTrack(TrackInfo trackInfo, bool forceQuickMode)
    {
      bool success = false;
      if (!string.IsNullOrEmpty(trackInfo.AlbumCdDdId))
      {
        success |= CDFreeDbMatcher.Instance.FindAndUpdateTrack(trackInfo, false);
      }
      success |= MusicTheAudioDbMatcher.Instance.FindAndUpdateTrack(trackInfo, false);
      success |= MusicBrainzMatcher.Instance.FindAndUpdateTrack(trackInfo, true); //Always force quick mode because online queries mostly timeout
      success |= MusicFanArtTvMatcher.Instance.FindAndUpdateTrack(trackInfo, false);
      return success;
    }

    public static bool UpdateAlbumPersons(AlbumInfo albumInfo, string occupation, bool forceQuickMode)
    {
      bool success = false;
      success |= MusicTheAudioDbMatcher.Instance.UpdateAlbumPersons(albumInfo, occupation, forceQuickMode);
      success |= MusicBrainzMatcher.Instance.UpdateAlbumPersons(albumInfo, occupation, true); //Always force quick mode because online queries mostly timeout
      success |= MusicFanArtTvMatcher.Instance.UpdateAlbumPersons(albumInfo, occupation, forceQuickMode);
      return success;
    }

    public static bool UpdateTrackPersons(TrackInfo trackInfo, string occupation, bool forceQuickMode)
    {
      bool success = false;
      success |= MusicTheAudioDbMatcher.Instance.UpdateTrackPersons(trackInfo, occupation, forceQuickMode);
      success |= MusicBrainzMatcher.Instance.UpdateTrackPersons(trackInfo, occupation, true); //Always force quick mode because online queries mostly timeout
      success |= MusicFanArtTvMatcher.Instance.UpdateTrackPersons(trackInfo, occupation, forceQuickMode);
      return success;
    }

    public static bool UpdateAlbumCompanies(AlbumInfo albumInfo, string companyType, bool forceQuickMode)
    {
      bool success = false;
      success |= MusicTheAudioDbMatcher.Instance.UpdateAlbumCompanies(albumInfo, companyType, false);
      success |= MusicBrainzMatcher.Instance.UpdateAlbumCompanies(albumInfo, companyType, true); //Always force quick mode because online queries mostly timeout
      success |= MusicFanArtTvMatcher.Instance.UpdateAlbumCompanies(albumInfo, companyType, false);
      return success;
    }

    public static bool UpdateAlbum(AlbumInfo albumInfo, bool updateTrackList, bool forceQuickMode)
    {
      bool success = false;
      success |= MusicTheAudioDbMatcher.Instance.UpdateAlbum(albumInfo, updateTrackList, false);
      success |= MusicBrainzMatcher.Instance.UpdateAlbum(albumInfo, updateTrackList, true); //Always force quick mode because online queries mostly timeout
      success |= MusicFanArtTvMatcher.Instance.UpdateAlbum(albumInfo, updateTrackList, false);

      if (updateTrackList)
      {
        if (albumInfo.Tracks.Count == 0)
          return false;

        for (int i = 0; i < albumInfo.Tracks.Count; i++)
        {
          TrackInfo trackInfo = albumInfo.Tracks[i];
          //MusicTheAudioDbMatcher.Instance.FindAndUpdateTrack(trackInfo, forceQuickMode);
          //MusicBrainzMatcher.Instance.FindAndUpdateTrack(trackInfo, forceQuickMode);
          //MusicFanArtTvMatcher.Instance.FindAndUpdateTrack(trackInfo, forceQuickMode);
        }
      }
      return success;
    }

    public static IList<string> GetAudioFanArtFiles(object infoObject, string mediaType, string fanArtType)
    {
      List<string> fanArtFiles = new List<string>();
      fanArtFiles.AddRange(MusicTheAudioDbMatcher.Instance.GetFanArtFiles(infoObject, mediaType, fanArtType));
      fanArtFiles.AddRange(MusicBrainzMatcher.Instance.GetFanArtFiles(infoObject, mediaType, fanArtType));
      fanArtFiles.AddRange(MusicFanArtTvMatcher.Instance.GetFanArtFiles(infoObject, mediaType, fanArtType));
      return fanArtFiles;
    }

    public static void StoreAudioPersonMatch(PersonInfo person)
    {
      if (person.Occupation == PersonAspect.OCCUPATION_ARTIST)
      {
        MusicTheAudioDbMatcher.Instance.StoreArtistMatch(person);
        MusicBrainzMatcher.Instance.StoreArtistMatch(person);
      }
      else if (person.Occupation == PersonAspect.OCCUPATION_COMPOSER)
      {
        MusicTheAudioDbMatcher.Instance.StoreComposerMatch(person);
        MusicBrainzMatcher.Instance.StoreComposerMatch(person);
      }
    }

    public static void StoreAudioCompanyMatch(CompanyInfo company)
    {
      if (company.Type == CompanyAspect.COMPANY_MUSIC_LABEL)
      {
        MusicTheAudioDbMatcher.Instance.StoreMusicLabelMatch(company);
        MusicBrainzMatcher.Instance.StoreMusicLabelMatch(company);
      }
    }

    #endregion

    #region Movie

    public static bool FindAndUpdateMovie(MovieInfo movieInfo, bool forceQuickMode)
    {
      bool success = false;
      success |= MovieTheMovieDbMatcher.Instance.FindAndUpdateMovie(movieInfo, false);
      success |= MovieOmDbMatcher.Instance.FindAndUpdateMovie(movieInfo, forceQuickMode);
      success |= MovieFanArtTvMatcher.Instance.FindAndUpdateMovie(movieInfo, false);
      return success;
    }

    public static bool UpdatePersons(MovieInfo movieInfo, string occupation, bool forceQuickMode)
    {
      bool success = false;
      success |= MovieTheMovieDbMatcher.Instance.UpdatePersons(movieInfo, occupation, forceQuickMode);
      success |= MovieOmDbMatcher.Instance.UpdatePersons(movieInfo, occupation, forceQuickMode);
      return success;
    }


    public static bool UpdateCharacters(MovieInfo movieInfo, bool forceQuickMode)
    {
      bool success = false;
      success |= MovieTheMovieDbMatcher.Instance.UpdateCharacters(movieInfo, forceQuickMode);
      return success;
    }

    public static bool UpdateCollection(MovieCollectionInfo collectionInfo, bool updateMovieList, bool forceQuickMode)
    {
      bool success = false;
      success |= MovieTheMovieDbMatcher.Instance.UpdateCollection(collectionInfo, updateMovieList, forceQuickMode);

      if (updateMovieList)
      {
        if (collectionInfo.Movies.Count == 0)
          return false;

        for (int i = 0; i < collectionInfo.Movies.Count; i++)
        {
          MovieInfo movieInfo = collectionInfo.Movies[i];
          //MovieTheMovieDbMatcher.Instance.FindAndUpdateMovie(movieInfo, forceQuickMode);
          //MovieOmDbMatcher.Instance.FindAndUpdateMovie(movieInfo, forceQuickMode);
          //MovieFanArtTvMatcher.Instance.FindAndUpdateMovie(movieInfo, forceQuickMode);
        }
      }
      return success;
    }

    public static bool UpdateCompanies(MovieInfo movieInfo, string companyType, bool forceQuickMode)
    {
      bool success = false;
      success |= MovieTheMovieDbMatcher.Instance.UpdateCompanies(movieInfo, companyType, forceQuickMode);
      return success;
    }

    public static IList<string> GetMovieFanArtFiles(object infoObject, string mediaType, string fanArtType)
    {
      List<string> fanArtFiles = new List<string>();
      fanArtFiles.AddRange(MovieTheMovieDbMatcher.Instance.GetFanArtFiles(infoObject, mediaType, fanArtType));
      fanArtFiles.AddRange(MovieFanArtTvMatcher.Instance.GetFanArtFiles(infoObject, mediaType, fanArtType));
      return fanArtFiles;
    }

    public static void StoreMoviePersonMatch(PersonInfo person)
    {
      if (person.Occupation == PersonAspect.OCCUPATION_ACTOR)
      {
        MovieTheMovieDbMatcher.Instance.StoreActorMatch(person);
        MovieOmDbMatcher.Instance.StoreActorMatch(person);
      }
      else if (person.Occupation == PersonAspect.OCCUPATION_DIRECTOR)
      {
        MovieTheMovieDbMatcher.Instance.StoreDirectorMatch(person);
        MovieOmDbMatcher.Instance.StoreDirectorMatch(person);
      }
      else if (person.Occupation == PersonAspect.OCCUPATION_WRITER)
      {
        MovieTheMovieDbMatcher.Instance.StoreWriterMatch(person);
        MovieOmDbMatcher.Instance.StoreWriterMatch(person);
      }
    }

    public static void StoreMovieCharacterMatch(CharacterInfo character)
    {
      MovieTheMovieDbMatcher.Instance.StoreCharacterMatch(character);
      MovieOmDbMatcher.Instance.StoreCharacterMatch(character);
    }

    public static void StoreMovieCompanyMatch(CompanyInfo company)
    {
      MovieTheMovieDbMatcher.Instance.StoreCompanyMatch(company);
      MovieOmDbMatcher.Instance.StoreCompanyMatch(company);
    }

    #endregion

    #region Series

    public static bool FindAndUpdateEpisode(EpisodeInfo episodeInfo, bool forceQuickMode)
    {
      bool success = false;
      success |= SeriesTvDbMatcher.Instance.FindAndUpdateEpisode(episodeInfo, false); //Provides IMDBID and TVDBID
      success |= SeriesTheMovieDbMatcher.Instance.FindAndUpdateEpisode(episodeInfo, forceQuickMode); //Provides IMDBID, TMDBID and TVDBID
      success |= SeriesTvMazeMatcher.Instance.FindAndUpdateEpisode(episodeInfo, forceQuickMode); //Provides TvMazeID, IMDBID and TVDBID
      success |= SeriesOmDbMatcher.Instance.FindAndUpdateEpisode(episodeInfo, forceQuickMode); //Provides IMDBID
      success |= SeriesFanArtTvMatcher.Instance.FindAndUpdateEpisode(episodeInfo, false);
      return success;
    }

    public static bool UpdateEpisodePersons(EpisodeInfo episodeInfo, string occupation, bool forceQuickMode)
    {
      bool success = false;
      success |= SeriesTvDbMatcher.Instance.UpdateEpisodePersons(episodeInfo, occupation, forceQuickMode);
      success |= SeriesTheMovieDbMatcher.Instance.UpdateEpisodePersons(episodeInfo, occupation, forceQuickMode);
      success |= SeriesTvMazeMatcher.Instance.UpdateEpisodePersons(episodeInfo, occupation, forceQuickMode);
      return success;
    }

    public static bool UpdateEpisodeCharacters(EpisodeInfo episodeInfo, bool forceQuickMode)
    {
      bool success = false;
      success |= SeriesTvDbMatcher.Instance.UpdateEpisodeCharacters(episodeInfo, forceQuickMode);
      success |= SeriesTheMovieDbMatcher.Instance.UpdateEpisodeCharacters(episodeInfo, forceQuickMode);
      success |= SeriesTvMazeMatcher.Instance.UpdateEpisodeCharacters(episodeInfo, forceQuickMode);
      return success;
    }

    public static bool UpdateSeason(SeasonInfo seasonInfo, bool forceQuickMode)
    {
      bool success = false;
      success |= SeriesTvDbMatcher.Instance.UpdateSeason(seasonInfo, forceQuickMode);
      success |= SeriesTheMovieDbMatcher.Instance.UpdateSeason(seasonInfo, forceQuickMode);
      success |= SeriesOmDbMatcher.Instance.UpdateSeason(seasonInfo, forceQuickMode);
      success |= SeriesFanArtTvMatcher.Instance.UpdateSeason(seasonInfo, forceQuickMode);
      return success;
    }

    public static bool UpdateSeries(SeriesInfo seriesInfo, bool updateEpisodeList, bool forceQuickMode)
    {
      bool success = false;
      success |= SeriesTvDbMatcher.Instance.UpdateSeries(seriesInfo, updateEpisodeList, false);
      success |= SeriesTheMovieDbMatcher.Instance.UpdateSeries(seriesInfo, updateEpisodeList, forceQuickMode);
      success |= SeriesTvMazeMatcher.Instance.UpdateSeries(seriesInfo, updateEpisodeList, forceQuickMode);
      success |= SeriesOmDbMatcher.Instance.UpdateSeries(seriesInfo, updateEpisodeList, forceQuickMode);
      success |= SeriesFanArtTvMatcher.Instance.UpdateSeries(seriesInfo, updateEpisodeList, false);

      if (updateEpisodeList)
      {
        if (seriesInfo.Episodes.Count == 0)
          return false;

        for (int i = 0; i < seriesInfo.Episodes.Count; i++)
        {
          EpisodeInfo episodeInfo = seriesInfo.Episodes[i];
          //Gives more detail to the missing episodes but will be very slow
          //SeriesTvDbMatcher.Instance.FindAndUpdateEpisode(episodeInfo, forceQuickMode);
          //SeriesTheMovieDbMatcher.Instance.FindAndUpdateEpisode(episodeInfo, forceQuickMode);
          //SeriesTvMazeMatcher.Instance.FindAndUpdateEpisode(episodeInfo, forceQuickMode);
          //SeriesOmDbMatcher.Instance.FindAndUpdateEpisode(episodeInfo, forceQuickMode);
          //SeriesFanArtTvMatcher.Instance.FindAndUpdateEpisode(episodeInfo, false);
        }
      }
        return success;
    }

    public static bool UpdateSeriesPersons(SeriesInfo seriesInfo, string occupation, bool forceQuickMode)
    {
      bool success = false;
      success |= SeriesTvDbMatcher.Instance.UpdateSeriesPersons(seriesInfo, occupation, forceQuickMode);
      success |= SeriesTheMovieDbMatcher.Instance.UpdateSeriesPersons(seriesInfo, occupation, forceQuickMode);
      success |= SeriesTvMazeMatcher.Instance.UpdateSeriesPersons(seriesInfo, occupation, forceQuickMode);
      return success;
    }

    public static bool UpdateSeriesCharacters(SeriesInfo seriesInfo, bool forceQuickMode)
    {
      bool success = false;
      success |= SeriesTvDbMatcher.Instance.UpdateSeriesCharacters(seriesInfo, forceQuickMode);
      success |= SeriesTheMovieDbMatcher.Instance.UpdateSeriesCharacters(seriesInfo, forceQuickMode);
      success |= SeriesTvMazeMatcher.Instance.UpdateSeriesCharacters(seriesInfo, forceQuickMode);
      return success;
    }

    public static bool UpdateSeriesCompanies(SeriesInfo seriesInfo, string companyType, bool forceQuickMode)
    {
      bool success = false;
      success |= SeriesTvDbMatcher.Instance.UpdateSeriesCompanies(seriesInfo, companyType, forceQuickMode);
      success |= SeriesTheMovieDbMatcher.Instance.UpdateSeriesCompanies(seriesInfo, companyType, forceQuickMode);
      success |= SeriesTvMazeMatcher.Instance.UpdateSeriesCompanies(seriesInfo, companyType, forceQuickMode);
      return success;
    }

    public static IList<string> GetSeriesFanArtFiles(object infoObject, string mediaType, string fanArtType)
    {
      List<string> fanArtFiles = new List<string>();
      fanArtFiles.AddRange(SeriesTvDbMatcher.Instance.GetFanArtFiles(infoObject, mediaType, fanArtType));
      fanArtFiles.AddRange(SeriesTheMovieDbMatcher.Instance.GetFanArtFiles(infoObject, mediaType, fanArtType));
      fanArtFiles.AddRange(SeriesTvMazeMatcher.Instance.GetFanArtFiles(infoObject, mediaType, fanArtType));
      fanArtFiles.AddRange(SeriesFanArtTvMatcher.Instance.GetFanArtFiles(infoObject, mediaType, fanArtType));
      return fanArtFiles;
    }

    public static void StoreSeriesPersonMatch(PersonInfo person)
    {
      if (person.Occupation == PersonAspect.OCCUPATION_ACTOR)
      {
        SeriesTvDbMatcher.Instance.StoreActorMatch(person);
        SeriesTheMovieDbMatcher.Instance.StoreActorMatch(person);
        SeriesTvMazeMatcher.Instance.StoreActorMatch(person);
        SeriesOmDbMatcher.Instance.StoreActorMatch(person);
      }
      else if (person.Occupation == PersonAspect.OCCUPATION_DIRECTOR)
      {
        SeriesTvDbMatcher.Instance.StoreDirectorMatch(person);
        SeriesTheMovieDbMatcher.Instance.StoreDirectorMatch(person);
        SeriesTvMazeMatcher.Instance.StoreDirectorMatch(person);
        SeriesOmDbMatcher.Instance.StoreDirectorMatch(person);
      }
      else if (person.Occupation == PersonAspect.OCCUPATION_WRITER)
      {
        SeriesTvDbMatcher.Instance.StoreWriterMatch(person);
        SeriesTheMovieDbMatcher.Instance.StoreWriterMatch(person);
        SeriesTvMazeMatcher.Instance.StoreWriterMatch(person);
        SeriesOmDbMatcher.Instance.StoreWriterMatch(person);
      }
    }

    public static void StoreSeriesCharacterMatch(CharacterInfo character)
    {
      SeriesTvDbMatcher.Instance.StoreCharacterMatch(character);
      SeriesTheMovieDbMatcher.Instance.StoreCharacterMatch(character);
      SeriesTvMazeMatcher.Instance.StoreCharacterMatch(character);
      SeriesOmDbMatcher.Instance.StoreCharacterMatch(character);
    }

    public static void StoreSeriesCompanyMatch(CompanyInfo company)
    {
      if (company.Type == CompanyAspect.COMPANY_PRODUCTION)
      {
        SeriesTvDbMatcher.Instance.StoreCompanyMatch(company);
        SeriesTheMovieDbMatcher.Instance.StoreCompanyMatch(company);
        SeriesTvMazeMatcher.Instance.StoreCompanyMatch(company);
        SeriesOmDbMatcher.Instance.StoreCompanyMatch(company);
      }
      else if (company.Type == CompanyAspect.COMPANY_TV_NETWORK)
      {
        SeriesTvDbMatcher.Instance.StoreTvNetworkMatch(company);
        SeriesTheMovieDbMatcher.Instance.StoreTvNetworkMatch(company);
        SeriesTvMazeMatcher.Instance.StoreTvNetworkMatch(company);
        SeriesOmDbMatcher.Instance.StoreTvNetworkMatch(company);
      }
    }

    #endregion
  }
}
