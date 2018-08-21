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

using System.Runtime.Serialization;

namespace MediaPortal.Plugins.MP2Extended.Common
{
  [DataContract]
  public enum WebMediaType
  {
    [EnumMember] Movie = 0,
    [EnumMember] MusicTrack = 1,
    [EnumMember] Picture = 2,
    [EnumMember] TVEpisode = 3,
    [EnumMember] File = 4,
    [EnumMember] TVShow = 5,
    [EnumMember] TVSeason = 6,
    [EnumMember] MusicAlbum = 7,
    [EnumMember] MusicArtist = 8,
    [EnumMember] Folder = 9,
    [EnumMember] Drive = 10,
    [EnumMember] Playlist = 11,
    [EnumMember] TV = 12,
    [EnumMember] Recording = 13,
    [EnumMember] Radio = 14,
    [EnumMember] Url = 15
  }
}
