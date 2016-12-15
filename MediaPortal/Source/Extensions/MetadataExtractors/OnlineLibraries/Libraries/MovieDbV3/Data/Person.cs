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
using System.Globalization;
using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbV3.Data
{
  //  {
  //  "adult": false,
  //  "also_known_as": [],
  //  "biography": "From Wikipedia, the free encyclopedia.\n\nWilliam Bradley \"Brad\" Pitt (born December 18, 1963) is an American actor and film producer.
  //  "birthday": "1963-12-18",
  //  "deathday": "",
  //  "homepage": "http://simplybrad.com/",
  //  "id": 287,
  //  "name": "Brad Pitt",
  //  "place_of_birth": "Shawnee, Oklahoma, United States",
  //  "profile_path": "/w8zJQuN7tzlm6FY9mfGKihxp3Cb.jpg"
  //}
  [DataContract]
  public class Person
  {
    [DataMember(Name = "id")]
    public int PersonId { get; set; }

    [DataMember(Name = "name")]
    public string Name { get; set; }

    [DataMember(Name = "place_of_birth")]
    public string PlaceOfBirth { get; set; }

    [DataMember(Name = "biography")]
    public string Biography { get; set; }

    [DataMember(Name = "birthday")]
    private string _birth
    {
      set
      {
        if(value != null)
        {
          DateTime date;
          if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            DateOfBirth = date;
          else if (DateTime.TryParse("01-" + value, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            DateOfBirth = date;
          else if (DateTime.TryParse("01-01-" + value, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            DateOfBirth = date;
        }
      }
    }

    public DateTime? DateOfBirth { get; set; }

    [DataMember(Name = "deathday")]
    private string _death
    {
      set
      {
        if (value != null)
        {
          DateTime date;
          if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            DateOfDeath = date;
          else if (DateTime.TryParse("01-" + value, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            DateOfDeath = date;
          else if (DateTime.TryParse("01-01-" + value, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            DateOfDeath = date;
        }
      }
    }

    public DateTime? DateOfDeath { get; set; }

    [DataMember(Name = "profile_path")]
    public string ProfilePath { get; set; }

    [DataMember(Name = "external_ids")]
    public ExternalIds ExternalId { get; set; }

    public override string ToString()
    {
      return Name;
    }
  }
}
