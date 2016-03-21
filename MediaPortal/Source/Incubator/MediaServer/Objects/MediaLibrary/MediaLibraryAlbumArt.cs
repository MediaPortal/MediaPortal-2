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

using MediaPortal.Common.MediaManagement;
using MediaPortal.Plugins.MediaServer.DLNA;
using MediaPortal.Plugins.MediaServer.Profiles;
using MediaPortal.Plugins.MediaServer.ResourceAccess;
using MediaPortal.Plugins.Transcoding.Interfaces;

namespace MediaPortal.Plugins.MediaServer.Objects.MediaLibrary
{
  public class MediaLibraryAlbumArt : IDirectoryAlbumArt
  {
    public MediaItem Item { get; private set; }
    public EndPointSettings Client { get; private set; }

    public MediaLibraryAlbumArt(MediaItem item, EndPointSettings client)
    {
      Item = item;
      Client = client;
    }

    public void Initialise()
    {
      Uri = DlnaResourceAccessUtils.GetThumbnailBaseURL(Item, Client);

      string profileId = "JPEG_TN";
      string mimeType = "image/jpeg";
      DlnaProfiles.FindCompatibleProfile(Client, DlnaProfiles.ResolveImageProfile(ImageContainer.Jpeg, Client.Profile.Settings.Thumbnails.MaxWidth, Client.Profile.Settings.Thumbnails.MaxHeight),
        ref profileId, ref mimeType);

      ProfileId = profileId;
      MimeType = mimeType;
    }

    public string Uri { get; set; }

    public string ProfileId { get; set; }

    public string MimeType { get; set; }

  }
}
