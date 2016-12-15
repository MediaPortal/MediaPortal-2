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

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktSyncResponse
  {
    [DataMember(Name = "added")]
    public Items Added { get; set; }

    [DataMember(Name = "deleted")]
    public Items Deleted { get; set; }

    [DataMember(Name = "existing")]
    public Items Existing { get; set; }

    [DataContract]
    public class Items
    {
      [DataMember(Name = "movies")]
      public int Movies { get; set; }

      [DataMember(Name = "shows")]
      public int Shows { get; set; }

      [DataMember(Name = "seasons")]
      public int Seasons { get; set; }

      [DataMember(Name = "episodes")]
      public int Episodes { get; set; }

      [DataMember(Name = "people")]
      public int People { get; set; }
    }

    [DataMember(Name = "not_found")]
    public NotFoundObjects NotFound { get; set; }

    [DataContract]
    public class NotFoundObjects
    {
      [DataMember(Name = "movies")]
      public List<TraktMovie> Movies { get; set; }

      [DataMember(Name = "shows")]
      public List<TraktShow> Shows { get; set; }

      [DataMember(Name = "episodes")]
      public List<TraktEpisode> Episodes { get; set; }

      [DataMember(Name = "seasons")]
      public List<TraktSeason> Seasons { get; set; }

      [DataMember(Name = "people")]
      public List<TraktPerson> People { get; set; }
    }
  }
}