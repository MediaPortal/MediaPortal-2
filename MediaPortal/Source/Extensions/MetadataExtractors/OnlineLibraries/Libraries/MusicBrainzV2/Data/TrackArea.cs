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

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.MusicBrainzV2.Data
{
  //  {
  //    "sort-name": "United States",
  //    "iso-3166-1-codes": [
  //      "US"
  //    ],
  //    "name": "United States",
  //    "id": "489ce91b-6658-3307-9877-795b68554c98",
  //    "disambiguation": ""
  //  }
  [DataContract]
  public class TrackArea : TrackBaseName
  {
    [DataMember(Name = "iso-3166-1-codes")]
    public List<string> LanguageCodes { get; set; }

    public override string ToString()
    {
      return string.Format("Id: {0}, Name: {1}, SortName: {2}", Id, Name, SortName);
    }
  }
}
