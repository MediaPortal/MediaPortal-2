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
  /// <summary>
  /// Contains the trailers for a specific movie id.
  /// </summary>
  /// <remarks>
  ///{
  ///    "id": 550,
  ///    "quicktime": [],
  ///    "youtube": [
  ///        {
  ///             "name": "Trailer 1",
  ///             "size": "HD",
  ///             "source": "SUXWAEX2jlg"
  ///        }
  ///    ]
  ///}
  ///</remarks>
  [DataContract]
  public class MovieTrailers
  {
    [DataMember(Name = "id")]
    public int Id { get; set; }

    [DataMember(Name = "quicktime")]
    public List<Trailer> QuickTimeTrailers { get; set; }

    [DataMember(Name = "youtube")]
    public List<Trailer> YoutubeTrailers { get; set; }
  }
}
