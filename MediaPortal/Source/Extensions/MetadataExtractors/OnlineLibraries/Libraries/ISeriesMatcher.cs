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
using MediaPortal.Common.MediaManagement.Helpers;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries
{
  public interface ISeriesMatcher
  {
    bool Primary { get; set; }
    bool Enabled { get; set; }
    string Id { get; }

    bool FindAndUpdateEpisode(EpisodeInfo episodeInfo, bool forceQuickMode);
    bool UpdateSeries(SeriesInfo seriesInfo, bool updateEpisodeList, bool forceQuickMode);
    bool UpdateSeason(SeasonInfo seasonInfo, bool forceQuickMode);
    bool UpdateSeriesPersons(SeriesInfo seriesInfo, string occupation, bool forceQuickMode);
    bool UpdateSeriesCharacters(SeriesInfo seriesInfo, bool forceQuickMode);
    bool UpdateSeriesCompanies(SeriesInfo seriesInfo, string companyType, bool forceQuickMode);
    bool UpdateEpisodePersons(EpisodeInfo episodeInfo, string occupation, bool forceQuickMode);
    bool UpdateEpisodeCharacters(EpisodeInfo episodeInfo, bool forceQuickMode);

    void StoreActorMatch(PersonInfo person);
    void StoreDirectorMatch(PersonInfo person);
    void StoreWriterMatch(PersonInfo person);
    void StoreCharacterMatch(CharacterInfo character);
    void StoreCompanyMatch(CompanyInfo company);
    void StoreTvNetworkMatch(CompanyInfo company);

    bool ScheduleFanArtDownload(Guid mediaItemId, BaseInfo info);
  }
}
