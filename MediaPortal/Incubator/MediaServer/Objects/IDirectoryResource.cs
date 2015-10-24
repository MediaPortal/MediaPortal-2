#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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

namespace MediaPortal.Plugins.MediaServer.Objects
{
  public interface IDirectoryResource
  {
    [DirectoryProperty("")]
    string Uri { get; set; }

    [DirectoryProperty("@size", Required = false)]
    ulong Size { get; set; }

    [DirectoryProperty("@duration", Required = false)]
    string Duration { get; set; }

    [DirectoryProperty("@bitrate", Required = false)]
    uint BitRate { get; set; }

    [DirectoryProperty("@sampleFrequency", Required = false)]
    uint SampleFrequency { get; set; }

    [DirectoryProperty("@bitsPerSample", Required = false)]
    uint BitsPerSample { get; set; }

    [DirectoryProperty("@nrAudioChannels", Required = false)]
    uint NumberOfAudioChannels { get; set; }

    [DirectoryProperty("@resolution", Required = false)]
    string Resolution { get; set; }

    [DirectoryProperty("@colorDepth", Required = false)]
    uint ColorDepth { get; set; }

    [DirectoryProperty("@protocolInfo")]
    string ProtocolInfo { get; set; }

    [DirectoryProperty("@protection", Required = false)]
    string Protection { get; set; }

    [DirectoryProperty("@importUri", Required = false)]
    string ImportUri { get; set; }

    [DirectoryProperty("@dlna:ifoFileURI", Required = false)]
    string DlnaIfoFileUrl { get; set; }

    [DirectoryProperty("@pv:subtitleFileType", Required = false)]
    string PacketVideoSubtitleType { get; set; }

    [DirectoryProperty("@pv:subtitleFileUri", Required = false)]
    string PacketVideoSubtitleUri { get; set; }
  }
}