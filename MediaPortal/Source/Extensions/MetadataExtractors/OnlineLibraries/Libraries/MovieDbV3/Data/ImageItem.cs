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
  //  "aspect_ratio": 1.78,
  //  "file_path": "/mOTtuakUTb1qY6jG6lzMfjdhLwc.jpg",
  //  "height": 1080,
  //  "iso_639_1": null,
  //  "width": 1920
  //}
  [DataContract]
  public class ImageItem
  {
    // Not filled by API!
    public int Id { get; set; }

    [DataMember(Name = "aspect_ratio")]
    public float AspectRatio { get; set; }

    [DataMember(Name = "file_path")]
    public string FilePath { get; set; }

    [DataMember(Name = "height")]
    public int Height { get; set; }

    [DataMember(Name = "width")]
    public int Width { get; set; }

    [DataMember(Name = "iso_639_1")]
    public string Language { get; set; }
    
    public override string ToString()
    {
      return FilePath;
    }
  }
}
