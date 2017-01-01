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

using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.UiComponents.Media.General;
using System.Collections.Generic;

namespace MediaPortal.UiComponents.Media.Models.Navigation
{
  public class ImageItem : PlayableMediaItem
  {
    public ImageItem(MediaItem mediaItem)
      : base(mediaItem)
    {
    }

    public override void Update(MediaItem mediaItem)
    {
      base.Update(mediaItem);
      SingleMediaItemAspect imageAspect;
      if (MediaItemAspect.TryGetAspect(mediaItem.Aspects, ImageAspect.Metadata, out imageAspect))
      {
        SimpleTitle = Title;
        int? width = (int?)imageAspect[ImageAspect.ATTR_WIDTH];
        int? height = (int?)imageAspect[ImageAspect.ATTR_HEIGHT];
        if (width.HasValue && width.Value > 0 && height.HasValue && height.Value > 0)
        {
          Width = width;
          Height = height;
          Size = width + " x " + height;
        }
      }
      IList<MultipleMediaItemAspect> resourceAspects;
      if (MediaItemAspect.TryGetAspects(mediaItem.Aspects, ProviderResourceAspect.Metadata, out resourceAspects))
      {
        ResourcePath rp = ResourcePath.Deserialize((string)resourceAspects[0][ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH]);
        string ext = ProviderPathHelper.GetExtension(rp.FileName);
        if (ext.Length > 1)
          // remove leading '.'
          ext = ext.Substring(1);
        Extension = ext;
        MimeType = (string)resourceAspects[0][ProviderResourceAspect.ATTR_MIME_TYPE];
      }
      FireChange();
    }

    public int? Width
    {
      get { return (int?)_additionalProperties[Consts.KEY_WIDTH]; }
      set { _additionalProperties[Consts.KEY_WIDTH] = value; }
    }

    public int? Height
    {
      get { return (int?)_additionalProperties[Consts.KEY_HEIGHT]; }
      set { _additionalProperties[Consts.KEY_HEIGHT] = value; }
    }

    public string Size
    {
      get { return this[Consts.KEY_SIZE]; }
      set { SetLabel(Consts.KEY_SIZE, value); }
    }

    public string Extension
    {
      get { return this[Consts.KEY_EXTENSION]; }
      set { SetLabel(Consts.KEY_EXTENSION, value); }
    }

    public string MimeType
    {
      get { return this[Consts.KEY_MIMETYPE]; }
      set { SetLabel(Consts.KEY_MIMETYPE, value); }
    }
  }
}
