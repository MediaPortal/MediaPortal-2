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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Plugins.MediaServer.ResourceAccess;
using MediaPortal.Plugins.MediaServer.Profiles;
using MediaPortal.Plugins.MediaServer.DLNA;

namespace MediaPortal.Plugins.MediaServer.Objects.MediaLibrary
{
  public class MediaLibrarySubtitle : IDirectorySubtitle
  {
    public MediaItem Item { get; private set; }

    public EndPointSettings Client { get; private set; }

    public string MimeType { get; private set; }

    public string SubtitleType { get; private set; }

    public MediaLibrarySubtitle(MediaItem item, EndPointSettings client)
    {
      Item = item;
      Client = client;
      MimeType = null;
      SubtitleType = null;
    }

    public void Initialise()
    {
      DlnaMediaItem dlnaItem = Client.GetDlnaItem(Item, false);
      if (DlnaResourceAccessUtils.IsSoftCodedSubtitleAvailable(dlnaItem, Client) == true)
      {
        string mime = null;
        string type = null;
        Uri = DlnaResourceAccessUtils.GetSubtitleBaseURL(Item, Client, out mime, out type);
        MimeType = mime;
        SubtitleType = type;
      }
    }

    public string Uri { get; set; }
  }
}
