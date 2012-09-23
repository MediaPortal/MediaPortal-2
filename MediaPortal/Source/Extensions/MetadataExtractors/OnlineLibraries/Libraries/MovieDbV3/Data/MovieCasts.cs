#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbV3.Data
{
  /// <summary>
  /// Contains the cast information for a specific movie id.
  /// </summary>
  /// <remarks>
  ///{
  ///"id": 550,
  ///"cast": [
  ///    {
  ///        "id": 819,
  ///        "name": "Edward Norton",
  ///        "character": "The Narrator",
  ///        "order": 0,
  ///        "profile_path": "/7cf2mCVI0qv2PnZVNbbEktS8Xae.jpg"
  ///        }
  ///    ],
  ///"crew": [
  ///    {
  ///        "id": 7469,
  ///        "name": "Jim Uhls",
  ///        "department": "Writing",
  ///        "job": "Author",
  ///        "profile_path": null
  ///        }
  ///    ]
  ///}
  ///</remarks>
  [DataContract]
  public class MovieCasts
  {
    #region CastItem class

    //{
    //  "id": 819,
    //  "name": "Edward Norton",
    //  "character": "The Narrator",
    //  "order": 0,
    //  "profile_path": "/7cf2mCVI0qv2PnZVNbbEktS8Xae.jpg"
    //}
    [DataContract]
    public class CastItem
    {
      // Not filled by API!
      public int MovieId { get; set; }

      [DataMember(Name = "id")]
      public int PersonId { get; set; }

      [DataMember(Name = "name")]
      public string Name { get; set; }

      [DataMember(Name = "character")]
      public string Character { get; set; }

      [DataMember(Name = "order")]
      public string Order { get; set; }

      [DataMember(Name = "profile_path")]
      public string ProfilePath { get; set; }

      public override string ToString()
      {
        return Name;
      }
    }

    #endregion

    #region CrewItem class

    //{
    //  "id": 7469,
    //  "name": "Jim Uhls",
    //  "department": "Writing",
    //  "job": "Author",
    //  "profile_path": null
    //}
    [DataContract]
    public class CrewItem
    {
      // Not filled by API!
      public int MovieId { get; set; }

      [DataMember(Name = "id")]
      public int PersonId { get; set; }

      [DataMember(Name = "name")]
      public string Name { get; set; }

      [DataMember(Name = "department")]
      public string Department { get; set; }

      [DataMember(Name = "job")]
      public string Job { get; set; }

      [DataMember(Name = "profile_path")]
      public string ProfilePath { get; set; }

      public override string ToString()
      {
        return Name;
      }
    }

    #endregion

    [DataMember(Name = "id")]
    public int Id { get; set; }

    [DataMember(Name = "cast")]
    public List<CastItem> Cast { get; set; }

    [DataMember(Name = "crew")]
    public List<CrewItem> Crew { get; set; }

    public void SetMovieIds()
    {
      if (Cast != null) Cast.ForEach(c => c.MovieId = Id);
      if (Crew != null) Crew.ForEach(c => c.MovieId = Id);
    }
  }
}
