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
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.PluginManager;
using MediaPortal.UI.SkinEngine.Controls.ImageSources;
using MediaPortal.UI.SkinEngine.Utils;
using MediaPortal.Common.FanArt;

namespace MediaPortal.Extensions.UserServices.FanArtService.Client
{
  public class FanArtPlugin : IPluginStateTracker
  {
    private const int MAX_SIZE_THUMBS = 512;

    public static ImageSource CreateFanArtImageSource(object source, int width, int height)
    {
      MediaItem mediaItem = source as MediaItem;
      if (mediaItem == null)
        return null;
      // Use the ThumbnailAspect as fallback for non-ML imported MediaItems
      if (mediaItem.MediaItemId == Guid.Empty)
        return ImageSourceFactory.CreateMediaItemThumbnailAspectSource(source, width, height);

      string mediaType = FanArtMediaTypes.Undefined;
      // Special handling for ImageThumbs that might require rotation
      if (mediaItem.Aspects.ContainsKey(ImageAspect.ASPECT_ID))
        mediaType = FanArtMediaTypes.Image;

      FanArtImageSource fanArtImageSource = new FanArtImageSource
      {
        FanArtMediaType = mediaType,
        FanArtType = FanArtTypes.Thumbnail,
        MaxWidth = MAX_SIZE_THUMBS,
        MaxHeight = MAX_SIZE_THUMBS,
        // Order matters here: if all arguments are complete, download will start. We control the start time by setting FanArtName after all required properties are set
        FanArtName = mediaItem.MediaItemId.ToString()
      };
      return fanArtImageSource;
    }

    #region IPluginStateTracker implementation

    public void Activated(PluginRuntime pluginRuntime)
    {
      ImageSourceFactory.ReplaceCustomImageSource(ImageSourceFactory.CreateMediaItemThumbnailAspectSource, CreateFanArtImageSource);
    }

    public bool RequestEnd()
    {
      return true;
    }

    public void Stop() { }

    public void Continue() { }

    public void Shutdown() { }

    #endregion
  }
}
