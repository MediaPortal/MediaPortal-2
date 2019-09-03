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

namespace MediaPortal.Plugins.MP2Extended.WSS.StreamInfo
{
  public class WebVideoStream
  {
    public string Codec { get; set; }
    public decimal DisplayAspectRatio { get; set; }
    public string DisplayAspectRatioString { get; set; }
    public bool Interlaced { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int ID { get; set; }
    public int Index { get; set; }
  }

  public class WebAudioStream
  {
    public string Language { get; set; }
    public string LanguageFull { get; set; }
    public int Channels { get; set; }
    public string Codec { get; set; }
    public string Title { get; set; }
    public int ID { get; set; }
    public int Index { get; set; }
  }

  public class WebSubtitleStream
  {
    public string Language { get; set; }
    public string LanguageFull { get; set; }
    public int ID { get; set; }
    public int Index { get; set; }
    public string Filename { get; set; }
  }

  public class WebMediaInfo
  {
    // general properties
    public long Duration { get; set; } // in milliseconds
    public string Container { get; set; }

    // codecs
    public List<WebVideoStream> VideoStreams { get; set; }
    public List<WebAudioStream> AudioStreams { get; set; }
    public List<WebSubtitleStream> SubtitleStreams { get; set; }
  }
}
