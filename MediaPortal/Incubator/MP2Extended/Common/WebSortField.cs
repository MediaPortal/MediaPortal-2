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

using System.Runtime.Serialization;

namespace MediaPortal.Plugins.MP2Extended.Common
{
  [DataContract]
  public enum WebSortField
  {
    [EnumMember] Title = 0,
    [EnumMember] DateAdded = 1,
    [EnumMember] Year = 2,
    [EnumMember] Genre = 3,
    [EnumMember] Rating = 4,
    [EnumMember] Categories = 5,
    [EnumMember] MusicTrackNumber = 6,
    [EnumMember] MusicComposer = 7,
    [EnumMember] TVEpisodeNumber = 8,
    [EnumMember] TVSeasonNumber = 9,
    [EnumMember] PictureDateTaken = 10,
    [EnumMember] TVDateAired = 11,
    [EnumMember] Type = 12,
    [EnumMember] User = 15,
    [EnumMember] Channel = 16,
    [EnumMember] StartTime = 17,
    [EnumMember] NaturalTitle = 18
  }
}
