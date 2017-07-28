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

using System;
using MediaPortal.Common.MediaManagement.Helpers;
using System.Collections.Generic;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries
{
  public interface IMovieMatcher : IMatcher
  {
    List<MovieInfo> GetLastChangedMovies();
    void ResetLastChangedMovies();
    List<MovieCollectionInfo> GetLastChangedMovieCollections();
    void ResetLastChangedMovieCollections();

    bool FindAndUpdateMovie(MovieInfo movieInfo, bool importOnly);
    bool UpdatePersons(MovieInfo movieInfo, string occupation, bool importOnly);
    bool UpdateCharacters(MovieInfo movieInfo, bool importOnly);
    bool UpdateCompanies(MovieInfo movieInfo, string companyType, bool importOnly);
    bool UpdateCollection(MovieCollectionInfo movieCollectionInfo, bool updateMovieList, bool importOnly);

    void StoreActorMatch(PersonInfo person);
    void StoreDirectorMatch(PersonInfo person);
    void StoreWriterMatch(PersonInfo person);
    void StoreCharacterMatch(CharacterInfo character);
    void StoreCompanyMatch(CompanyInfo company);

    bool ScheduleFanArtDownload(Guid mediaItemId, BaseInfo info, bool force);
  }
}
