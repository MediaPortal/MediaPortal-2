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

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbV3.Data
{
  //  "credits": {
  //    "cast": [
  //      {
  //        "character": "Walter White",
  //        "credit_id": "52542282760ee313280017f9",
  //        "id": 17419,
  //        "name": "Bryan Cranston",
  //        "profile_path": "/fnglrIFnI5cK4OAh66AZN86mkFq.jpg",
  //        "order": 0
  //      }
  //    ],
  //    "crew": [
  //      {
  //        "credit_id": "52542287760ee31328001af1",
  //        "department": "Production",
  //        "id": 66633,
  //        "name": "Vince Gilligan",
  //        "job": "Executive Producer",
  //        "profile_path": "/rLSUjr725ez1cK7SKVxC9udO03Y.jpg"
  //      }
  //    ]
  //  }
  [DataContract]
  public class Credits
  {
    [DataMember(Name = "cast")]
    public List<CastItem> Cast { get; set; }

    [DataMember(Name = "crew")]
    public List<CrewItem> Crew { get; set; }
  }
}
