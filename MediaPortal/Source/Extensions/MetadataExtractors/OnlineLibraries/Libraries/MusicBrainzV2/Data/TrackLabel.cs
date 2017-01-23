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

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.MusicBrainzV2.Data
{
  //{
  //    "catalog-number": "pgram002",
  //    "label": {
  //      "label-code": null,
  //      "name": "Phonometrography",
  //      "disambiguation": "",
  //      "id": "a0759efa-f583-49ea-9a8d-d5bbce55541c",
  //      "sort-name": "Phonometrography"
  //    }
  //  }
  [DataContract]
  public class TrackLabel
  {
    [DataMember(Name = "catalog-number")]
    public string CatalogNumber { get; set; }

    [DataMember(Name = "label")]
    public TrackLabelDetail Label { get; set; }
  }
}
