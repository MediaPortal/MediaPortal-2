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

using MediaPortal.Plugins.MediaServer.DLNA;

namespace MediaPortal.Plugins.MediaServer.Objects.MediaLibrary
{
  public class MediaLibraryAlbumArtResource : IDirectoryResource
  {
    public MediaLibraryAlbumArt Item { get; set; }

    public MediaLibraryAlbumArtResource(MediaLibraryAlbumArt item)
    {
      Item = item;
    }

    public void Initialise()
    {
      Uri = Item.Uri;

      var dlnaProtocolInfo = DlnaProtocolInfoFactory.GetThumbnailProfileInfo(Item.MimeType, Item.ProfileId).ToString();
      if (dlnaProtocolInfo != null)
        ProtocolInfo = dlnaProtocolInfo.ToString();
      BitRate = uint.MinValue;
      SampleFrequency = uint.MinValue;
      NumberOfAudioChannels = uint.MinValue;
      BitsPerSample = uint.MinValue;
      ColorDepth = uint.MinValue;
      Resolution = Item.Client.Profile.Settings.Thumbnails.MaxWidth + "x" + Item.Client.Profile.Settings.Thumbnails.MaxHeight;
    }

    public string Uri { get; set; }

    public ulong Size { get; set; }

    public string Duration { get; set; }

    public uint BitRate { get; set; }

    public uint SampleFrequency { get; set; }

    public uint BitsPerSample { get; set; }

    public uint NumberOfAudioChannels { get; set; }

    public string Resolution { get; set; }

    public uint ColorDepth { get; set; }

    public string ProtocolInfo { get; set; }

    public string Protection { get; set; }

    public string ImportUri { get; set; }

    public string DlnaIfoFileUrl { get; set; }

    public string PacketVideoSubtitleType { get; set; }

    public string PacketVideoSubtitleUri { get; set; }
  }
}
