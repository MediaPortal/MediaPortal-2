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
  public interface IMusicMatcher : IMatcher
  {
    List<AlbumInfo> GetLastChangedAudioAlbums();
    void ResetLastChangedAudioAlbums();
    List<TrackInfo> GetLastChangedAudio();
    void ResetLastChangedAudio();

    Task<IEnumerable<TrackInfo>> FindMatchingTracksAsync(TrackInfo trackInfo);
    Task<IEnumerable<AlbumInfo>> FindMatchingAlbumsAsync(AlbumInfo albumInfo);

    Task<bool> FindAndUpdateTrackAsync(TrackInfo trackInfo);
    Task<bool> UpdateTrackPersonsAsync(TrackInfo trackInfo, string occupation, bool forAlbum);
    Task<bool> UpdateAlbumPersonsAsync(AlbumInfo albumInfo, string occupation);
    Task<bool> UpdateAlbumCompaniesAsync(AlbumInfo albumInfo, string companyType);
    Task<bool> UpdateAlbumAsync(AlbumInfo albumInfo, bool updateTrackList);

    void StoreArtistMatch(PersonInfo person);
    void StoreComposerMatch(PersonInfo person);
    void StoreConductorMatch(PersonInfo person);
    void StoreMusicLabelMatch(CompanyInfo company);

    Task<bool> DownloadFanArtAsync(Guid mediaItemId, BaseInfo info);
  }
}
