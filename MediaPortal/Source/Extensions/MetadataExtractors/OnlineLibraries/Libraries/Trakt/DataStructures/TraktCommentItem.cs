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
using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktCommentItem : IEquatable<TraktCommentItem>
  {
    [DataMember(Name = "type")]
    public string Type { get; set; }

    [DataMember(Name = "movie")]
    public TraktMovieSummary Movie { get; set; }

    [DataMember(Name = "show")]
    public TraktShowSummary Show { get; set; }

    [DataMember(Name = "season")]
    public TraktSeasonSummary Season { get; set; }

    [DataMember(Name = "episode")]
    public TraktEpisodeSummary Episode { get; set; }

    [DataMember(Name = "list")]
    public TraktListDetail List { get; set; }

    [DataMember(Name = "comment")]
    public TraktComment Comment { get; set; }

    #region IEquatable

    public bool Equals(TraktCommentItem other)
    {
      if (other == null || other.Comment == null)
        return false;

      return (this.Comment.Id == other.Comment.Id && this.Type == other.Type);
    }

    public override int GetHashCode()
    {
      return (this.Comment.Id.ToString() + "_" + this.Type).GetHashCode();
    }

    #endregion
  }
}