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
  //{
  //  "aspect_ratio": 1.78,
  //  "file_path": "/mOTtuakUTb1qY6jG6lzMfjdhLwc.jpg",
  //  "height": 1080,
  //  "iso_639_1": null,
  //  "width": 1920
  //}
  [DataContract]
  public class ImageCollection
  {
    [DataMember(Name = "id")]
    public int Id { get; set; }

    [DataMember(Name = "backdrops")]
    public List<ImageItem> Backdrops { get; set; }

    [DataMember(Name = "covers")]
    public List<ImageItem> Covers { get; set; }

    [DataMember(Name = "posters")]
    public List<ImageItem> Posters { get; set; }

    [DataMember(Name = "profiles")]
    public List<ImageItem> Profiles { get; set; }

    [DataMember(Name = "stills")]
    public List<ImageItem> Stills { get; set; }

    public void SetMovieIds()
    {
      if (Covers != null) Covers.ForEach(c => c.Id = Id);
      if (Backdrops != null) Backdrops.ForEach(c => c.Id = Id);
      if (Posters != null) Posters.ForEach(c => c.Id = Id);
      if (Profiles != null) Profiles.ForEach(c => c.Id = Id);
      if (Stills != null) Stills.ForEach(c => c.Id = Id);
    }
  }
}
