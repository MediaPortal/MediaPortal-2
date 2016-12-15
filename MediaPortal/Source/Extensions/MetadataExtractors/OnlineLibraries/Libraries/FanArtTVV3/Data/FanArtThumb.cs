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

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.FanArtTVV3.Data
{
  //    {
  //      "id": "19374",
  //      "url": "http://assets.fanart.tv/fanart/tv/75682/showbackground/bones-500994f33356b.jpg",
  //      "likes": "2",
  //    }
  [DataContract]
  public class FanArtThumb
  {
    [DataMember(Name = "id")]
    public int Id { get; set; }

    [DataMember(Name = "url")]
    public string Url { get; set; }

    [DataMember(Name = "likes")]
    public int? Likes { get; set; }
  }
}
