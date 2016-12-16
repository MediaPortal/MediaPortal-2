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

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.MusicBrainzV2.Data
{
  //  {
  //    "end": "1994-04-05",
  //    "begin": "1988-01",
  //    "ended": true
  //  }
  [DataContract]
  public class TrackLifeSpan
  {
    [DataMember(Name = "begin")]
    public string _begin
    {
      set
      {
        if (value != null)
        {
          DateTime date;
          if (DateTime.TryParse(value, out date))
            Begin = date;
          if (DateTime.TryParse(value + "-01", out date))
            Begin = date;
          if (DateTime.TryParse(value + "-01-01", out date))
            Begin = date;
        }
      }
    }

    public DateTime? Begin { get; set; }

    [DataMember(Name = "end")]
    public string _end
    {
      set
      {
        if (value != null)
        {
          DateTime date;
          if (DateTime.TryParse(value, out date))
            End = date;
          if (DateTime.TryParse(value + "-01", out date))
            End = date;
          if (DateTime.TryParse(value + "-01-01", out date))
            End = date;
        }
      }
    }

    public DateTime? End { get; set; }

    [DataMember(Name = "ended")]
    public bool? Ended { get; set; }
  }
}
