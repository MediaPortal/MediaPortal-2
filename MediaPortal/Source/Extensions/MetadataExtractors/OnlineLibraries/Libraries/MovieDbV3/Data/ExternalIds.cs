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
  // {
  //  "imdb_id": "nm0000093",
  //  "freebase_mid": "/m/0c6qh",
  //  "freebase_id": "/en/brad_pitt",
  //  "tvrage_id": 59436,
  //  "id": 287,
  //  "tvdb_id": 30272
  //}
  [DataContract]
  public class ExternalIds
  {
    [DataMember(Name = "id")]
    public int TmDbId { get; set; }

    [DataMember(Name = "tvrage_id")]
    public int? TvRageId { get; set; }

    [DataMember(Name = "tvdb_id")]
    public int? TvDbId { get; set; }

    [DataMember(Name = "imdb_id")]
    public string ImDbId { get; set; }

    public override string ToString()
    {
      return TmDbId.ToString();
    }
  }
}
