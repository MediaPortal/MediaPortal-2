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

using System;
using MediaPortal.Common.FanArt;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Extensions.MediaServer.DLNA;
using MediaPortal.Extensions.MediaServer.Profiles;
using MediaPortal.Extensions.MediaServer.ResourceAccess;
using MediaPortal.Extensions.TranscodingService.Interfaces;

namespace MediaPortal.Extensions.MediaServer.Objects.MediaLibrary
{
  public class MediaLibraryAlbumArt : IDirectoryAlbumArt
  {
    private readonly string _defaultFanArtMediaType;

    public EndPointSettings Client { get; private set; }
    public Guid MediaItemId { get; private set; }

    public MediaLibraryAlbumArt(MediaItem item, EndPointSettings client)
    {
      MediaItemId = item.MediaItemId;
      Client = client;
      _defaultFanArtMediaType = DlnaResourceAccessUtils.GetFanArtMediaType(item);
    }

    public MediaLibraryAlbumArt(Guid mediaItemId, EndPointSettings client)
    {
      MediaItemId = mediaItemId;
      Client = client;
      _defaultFanArtMediaType = FanArtMediaTypes.Undefined;
    }

    public void Initialise(string channelName, bool isTv)
    {
      Uri = DlnaResourceAccessUtils.GetChannelLogoBaseURL(channelName, Client, isTv);

      string profileId = "JPEG_TN";
      string mimeType = "image/jpeg";
      DlnaProfiles.TryFindCompatibleProfile(Client, DlnaProfiles.ResolveImageProfile(ImageContainer.Jpeg, Client.Profile.Settings.Thumbnails.MaxWidth, Client.Profile.Settings.Thumbnails.MaxHeight),
        ref profileId, ref mimeType);

      ProfileId = profileId;
      MimeType = mimeType;
    }

    public void Initialise(string fanartType = null)
    {
      Uri = DlnaResourceAccessUtils.GetThumbnailBaseURL(MediaItemId, Client, fanartType ?? _defaultFanArtMediaType);

      string profileId = "JPEG_TN";
      string mimeType = "image/jpeg";
      DlnaProfiles.TryFindCompatibleProfile(Client, DlnaProfiles.ResolveImageProfile(ImageContainer.Jpeg, Client.Profile.Settings.Thumbnails.MaxWidth, Client.Profile.Settings.Thumbnails.MaxHeight),
        ref profileId, ref mimeType);

      ProfileId = profileId;
      MimeType = mimeType;
    }

    public string Uri { get; set; }

    public string ProfileId { get; set; }

    public string MimeType { get; set; }

  }
}
