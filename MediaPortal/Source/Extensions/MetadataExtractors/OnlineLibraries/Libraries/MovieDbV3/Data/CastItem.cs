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

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbV3.Data
{
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
    [DataMember(Name = "id")]
    public int PersonId { get; set; }

    [DataMember(Name = "name")]
    public string Name { get; set; }

    [DataMember(Name = "character")]
    public string Character { get; set; }

    [DataMember(Name = "order")]
    public int Order { get; set; }

    [DataMember(Name = "profile_path")]
    public string ProfilePath { get; set; }

    public override string ToString()
    {
      return Name;
    }
  }
}
