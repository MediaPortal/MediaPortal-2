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

namespace MediaPortal.UPnPRenderer.MediaItems
{
  class DmapData
  {
    // DIDL-Lite
    // DC: http://www.upnp.org/schemas/av/didl-lite-v2.xsd

    public string Title { get; set; }
    public string Description { get; set; }
    public string Creator { get; set; }

    // UPnP: http://www.upnp.org/schemas/av/upnp.xsd

    // Contributor Related Properties

    public string[] Directors { get; set; }
    public string[] Artists { get; set; }
    public string[] Actors { get; set; }

    // missing: author, producer

    //Affiliation Related Properties

    public string Album { get; set; }
    public string[] Genres { get; set; }

    // missing: playlist

    // Associated Resources Properties 

    // albumArtURI is not implemnted here => see utils.cs

    // missing: artistDiscographyURI, lyricsURI

    // missing: Storage Related Properties, General Description Properties, Recorded Object Related Properties, User Channel and EPG Related Properties 
    //          Radio Broadcast Properties, Video Broadcast Properties, Physical Tuner Status-related Properties, Bookmark Related Properties
    //          Foreign Metadata Related Properties, Miscellaneous Properties, Object Tracking Properties, Content Protection Properties
    //          Base Properties, Contributor Related Properties, Affiliation Related Properties, User Channel and EPG Related Properties
    //          Video Broadcast Properties, ... to the end

    // didn't found a .xsd file
    
    public int OriginalTrackNumber { get; set; }
    public int OriginalDiscNumber { get; set; }
    public int OriginalDiscCount { get; set; }
  }
}
