#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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
using System.Linq;
using MediaPortal.Common.FanArt;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Plugins.WifiRemote;
using MediaPortal.Plugins.WifiRemote.Messages.MediaInfo;

namespace MediaPortal.Plugins.WifiRemote
{
  internal class ImageInfo : IAdditionalMediaInfo
  {
    public string MediaType => "image";
    public string Id => ItemId.ToString();
    public int MpMediaType => (int)MpMediaTypes.Movie; 
    public int MpProviderId => (int)MpProviders.MPVideo; 

    /// <summary>
    /// ID of the image
    /// </summary>
    public Guid ItemId { get; set; }
    /// <summary>
    /// Image title
    /// </summary>
    public string Title { get; set; }
    /// <summary>
    /// Image height
    /// </summary>
    public int Height { get; set; }
    /// <summary>
    /// Image width
    /// </summary>
    public int Width { get; set; }
    /// <summary>
    /// Image thumb
    /// </summary>
    public string ImageName { get; set; }

    /// <summary>
    /// Constructor
    /// </summary>
    public ImageInfo(MediaItem mediaItem)
    {
      var mediaAspect = MediaItemAspect.GetAspect(mediaItem.Aspects, MediaAspect.Metadata);
      var imageAspect = MediaItemAspect.GetAspect(mediaItem.Aspects, ImageAspect.Metadata);

      ItemId = mediaItem.MediaItemId;
      Title = mediaAspect.GetAttributeValue<string>(MediaAspect.ATTR_TITLE);
      Height = imageAspect.GetAttributeValue<int>(ImageAspect.ATTR_HEIGHT);
      Width = imageAspect.GetAttributeValue<int>(ImageAspect.ATTR_WIDTH);
      ImageName = Helper.GetImageBaseURL(mediaItem, FanArtMediaTypes.Image, FanArtTypes.Thumbnail);
    }
  }
}
