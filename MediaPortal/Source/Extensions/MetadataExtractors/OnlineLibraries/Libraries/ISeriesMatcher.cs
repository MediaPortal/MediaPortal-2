#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
  public interface ISeriesMatcher : IMatcher
  {
    List<SeriesInfo> GetLastChangedSeries();
    void ResetLastChangedSeries();
    List<EpisodeInfo> GetLastChangedEpisodes();
    void ResetLastChangedEpisodes();

    Task<IEnumerable<EpisodeInfo>> FindMatchingEpisodesAsync(EpisodeInfo episodeInfo);
    Task<IEnumerable<SeriesInfo>> FindMatchingSeriesAsync(SeriesInfo seriesInfo);

    Task<bool> FindAndUpdateEpisodeAsync(EpisodeInfo episodeInfo);
    Task<bool> UpdateSeriesAsync(SeriesInfo seriesInfo, bool updateEpisodeList);
    Task<bool> UpdateSeasonAsync(SeasonInfo seasonInfo);
    Task<bool> UpdateSeriesPersonsAsync(SeriesInfo seriesInfo, string occupation);
    Task<bool> UpdateSeriesCharactersAsync(SeriesInfo seriesInfo);
    Task<bool> UpdateSeriesCompaniesAsync(SeriesInfo seriesInfo, string companyType);
    Task<bool> UpdateEpisodePersonsAsync(EpisodeInfo episodeInfo, string occupation);
    Task<bool> UpdateEpisodeCharactersAsync(EpisodeInfo episodeInfo);

    void StoreActorMatch(PersonInfo person);
    void StoreDirectorMatch(PersonInfo person);
    void StoreWriterMatch(PersonInfo person);
    void StoreCharacterMatch(CharacterInfo character);
    void StoreCompanyMatch(CompanyInfo company);
    void StoreTvNetworkMatch(CompanyInfo company);

    Task<bool> DownloadFanArtAsync(Guid mediaItemId, BaseInfo info);
  }
}
