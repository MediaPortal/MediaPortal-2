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

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktComment
  {
    [DataMember(Name = "id")]
    public int Id { get; set; }

    [DataMember(Name = "parent_id")]
    public int? ParentId { get; set; }

    [DataMember(Name = "created_at")]
    public string CreatedAt { get; set; }

    [DataMember(Name = "comment")]
    public string Text { get; set; }

    [DataMember(Name = "spoiler")]
    public bool IsSpoiler { get; set; }

    [DataMember(Name = "review")]
    public bool IsReview { get; set; }

    [DataMember(Name = "replies")]
    public int Replies { get; set; }

    [DataMember(Name = "likes")]
    public int Likes { get; set; }

    [DataMember(Name = "user_rating")]
    public int? UserRating { get; set; }

    [DataMember(Name = "user")]
    public TraktUserSummary User { get; set; }
  }
}