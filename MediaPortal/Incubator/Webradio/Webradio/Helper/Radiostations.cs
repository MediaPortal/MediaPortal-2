#region Copyright (C) 2007-2023 Team MediaPortal

/*
    Copyright (C) 2007-2023 Team MediaPortal
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
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Webradio.Helper
{
  public partial class Radiostations
  {
    public List<RadioStation> Stations;

    public int Version { get; set; } = 1;
  }

  public class RadioStation
  {
    [DataMember(Name = "city")]
    public string City { get; set; } = "";

    [DataMember(Name = "country")]
    public string Country { get; set; } = "";

    [DataMember(Name = "region")]
    public string Region { get; set; } = "";

    [DataMember(Name = "language")]
    public string Language { get; set; } = "";

    [DataMember(Name = "genres")]
    public List<string> Genres { get; set; } = new List<string>();

    [DataMember(Name = "id")]
    public string Id { get; set; } = "";

    [DataMember(Name = "logo")]
    public string Logo { get; set; } = "";

    [DataMember(Name = "name")]
    public string Name { get; set; } = "";

    [DataMember(Name = "streams")] 
    public List<Stream> Streams { get; set; } = new List<Stream>();

    [DataMember(Name = "type")]
    public string Type { get; set; } = "";
  }

  public class Stream : IDisposable
  {
    [DataMember(Name = "url")]
    public string Url { get; set; } = "";

    [DataMember(Name = "contentFormat")]
    public string ContentFormat { get; set; } = "";

    [DataMember(Name = "status")] 
    public string Status { get; set; } = "";

    public void Dispose()
    {
      throw new NotImplementedException();
    }
  }
}
