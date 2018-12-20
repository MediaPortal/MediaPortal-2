#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.SimApiV1.Data
{
  //[
  //      {
  //        "id": "0000288",
  //          "name": "Christian Bale",
  //          "image": "https://images-na.ssl-images-amazon.com/images/M/MV5BMTkxMzk4MjQ4MF5BMl5BanBnXkFtZTcwMzExODQxOA@@._V1_UX214_CR0,0,214,317_AL_.jpg",
  //          "type": "person"
  //    },
  //    {
  //        "id": "3577667",
  //        "name": "Christian Bales",
  //        "image": "None",
  //        "type": "person"
  //    },
  //    {
  //        "id": "7635250",
  //        "name": "Christian Balenciaga",
  //        "image": "None",
  //        "type": "person"
  //    },
  //    {
  //        "id": "1019691",
  //        "name": "Christian Borle",
  //        "image": "https://images-na.ssl-images-amazon.com/images/M/MV5BMTcxMjg1MDc4OV5BMl5BanBnXkFtZTcwMDU4Mzk0Nw@@._V1_UY317_CR122,0,214,317_AL_.jpg",
  //        "type": "person"
  //    },
  //    {
  //        "id": "2738202",
  //        "name": "Christian Barillas",
  //        "image": "https://images-na.ssl-images-amazon.com/images/M/MV5BNzUxODgzNTA0Nl5BMl5BanBnXkFtZTgwODAzNzA5NzE@._V1_UX214_CR0,0,214,317_AL_.jpg",
  //        "type": "person"
  //    }
  //]
  [DataContract]
  public class SimApiPersonSearchResult
  {
    public List<SimApiPersonSearchItem> SearchResults { get; set; }
  }
}
