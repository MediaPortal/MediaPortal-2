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
  public interface IMusicMatcher
  {
    bool Primary { get; set; }
    bool Enabled { get; set; }
    string Id { get; }
    string PreferredLanguageCulture { get; set; }

    List<AlbumInfo> GetLastChangedAudioAlbums();
    void ResetLastChangedAudioAlbums();
    List<TrackInfo> GetLastChangedAudio();
    void ResetLastChangedAudio();

    bool FindAndUpdateTrack(TrackInfo trackInfo, bool importOnly);
    bool UpdateTrackPersons(TrackInfo trackInfo, string occupation, bool forAlbum, bool importOnly);
    bool UpdateAlbumPersons(AlbumInfo albumInfo, string occupation, bool importOnly);
    bool UpdateAlbumCompanies(AlbumInfo albumInfo, string companyType, bool importOnly);
    bool UpdateAlbum(AlbumInfo albumInfo, bool updateTrackList, bool importOnly);

    void StoreArtistMatch(PersonInfo person);
    void StoreComposerMatch(PersonInfo person);
    void StoreMusicLabelMatch(CompanyInfo company);

    bool ScheduleFanArtDownload(Guid mediaItemId, BaseInfo info, bool force);
  }
}
