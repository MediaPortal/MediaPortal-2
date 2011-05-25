#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.MediaManagement.DefaultItemAspects;
using MediaPortal.UI.SkinEngine.ContentManagement;

namespace MediaPortal.UI.SkinEngine.Controls.ImageSources
{
  /// <summary>
  /// <see cref="MediaItemSource"/> acts as a source provider / renderer for the <see cref="Visuals.Image"/> control.
  /// It's extends the <see cref="BitmapImage"/> to support thumbnail building for MediaItems.
  /// </summary>
  public class MediaItemSource: BitmapImage
  {
    #region Variables

    protected byte[] _thumbBinary = null;
    protected string _key;
    protected int _thumbnailSize;
    #endregion

    #region Constructor

    /// <summary>
    /// Constructs a <see cref="MediaItemSource"/> for building thumbnails for MediaItems.
    /// </summary>
    /// <param name="mediaItem">MediaItem to create thumbnail.</param>
    /// <param name="thumbnailSize">Requested thumbnail size.</param>
    public MediaItemSource(MediaItem mediaItem, int thumbnailSize)
    {
      _thumbnailSize = thumbnailSize;
      Guid id = mediaItem.MediaItemId;
      // Local media items don't have an item id
      _key = (id == Guid.Empty ? Guid.NewGuid() : id).ToString();

      if (thumbnailSize <= 32)
        _thumbBinary = (byte[]) mediaItem.Aspects[MediaAspect.ASPECT_ID].GetAttributeValue(MediaAspect.ATTR_THUMB_SMALL);
      else if (thumbnailSize <= 96)
        _thumbBinary = (byte[]) mediaItem.Aspects[MediaAspect.ASPECT_ID].GetAttributeValue(MediaAspect.ATTR_THUMB_MEDIUM);
      else 
        _thumbBinary = (byte[]) mediaItem.Aspects[MediaAspect.ASPECT_ID].GetAttributeValue(MediaAspect.ATTR_THUMB_LARGE);
    }

    #endregion

    #region Overrides

    public override void Allocate()
    {
      if (_texture == null && _thumbBinary != null)
        _texture = ContentManager.Instance.GetTexture(_thumbBinary, _key);
      if (_texture != null && !_texture.IsAllocated)
      {
        _texture.Allocate();
        if (_texture.IsAllocated)
        {
          _frameData.X = _texture.Width;
          _frameData.Y = _texture.Height;
          _imageContext.Refresh();
          FireChanged();
        }
      }
    }

    #endregion
  }
}
