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
  public class TraktLike : IEquatable<TraktLike>
  {
    [DataMember(Name = "liked_at")]
    public string LikedAt { get; set; }

    [DataMember(Name = "type")]
    public string Type { get; set; }

    [DataMember(Name = "list")]
    public TraktListDetail List { get; set; }

    [DataMember(Name = "comment")]
    public TraktComment Comment { get; set; }

    #region IEquatable

    public bool Equals(TraktLike other)
    {
      if (other == null || (other.Comment == null && other.Type == "comment") || (other.List == null && other.Type == "list"))
        return false;

      if (this.Type == "list")
      {
        if (this.List.Ids == null || other.List.Ids == null)
          return false;

        return (this.Type == other.Type && this.List.Ids.Trakt == other.List.Ids.Trakt);
      }
      else
      {
        return (this.Type == other.Type && this.Comment.Id == other.Comment.Id);
      }
    }

    public override int GetHashCode()
    {
      if (this.Type == "list")
      {
        return (this.List.Ids.Trakt.ToString() + "_" + this.Type).GetHashCode();
      }
      else
      {
        return (this.Comment.Id.ToString() + "_" + this.Type).GetHashCode();
      }
    }

    #endregion
  }
}