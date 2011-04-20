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

using MediaPortal.Core;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.MediaManagement.ResourceAccess;

namespace MediaPortal.UI.SkinEngine.Controls.ImageSources
{
  /// <summary>
  /// <see cref="MediaItemSource"/> acts as a source provider / renderer for the <see cref="Visuals.Image"/> control.
  /// It's extends the <see cref="BitmapImage"/> to support thumbnail building for MediaItems.
  /// </summary>
  public class MediaItemSource: BitmapImage
  {
    #region Variables

    private ILocalFsResourceAccessor _localFsResourceAccessor;
    private IResourceLocator _locator;

    #endregion

    #region Constructor

    /// <summary>
    /// Constructs a <see cref="MediaItemSource"/> for building thumbnails for MediaItems.
    /// </summary>
    /// <param name="mediaItem">MediaItem to create thumbnail.</param>
    /// <param name="thumbnailDimension">Requested thumbnail dimension.</param>
    public MediaItemSource(MediaItem mediaItem, int thumbnailDimension)
    {
      IMediaAccessor mediaAccessor = ServiceRegistration.Get<IMediaAccessor>();
      _locator = mediaAccessor.GetResourceLocator(mediaItem);
      Thumbnail = true; // Only Thumnbnail layouts are allowed for MediaItems (audio, video).
      ThumbnailDimension = thumbnailDimension;
    }

    #endregion

    #region Overrides

    public override void Allocate()
    {
      _localFsResourceAccessor = _locator.CreateLocalFsAccessor();
      if (_localFsResourceAccessor != null)
        UriSource = _localFsResourceAccessor.LocalFileSystemPath;
      base.Allocate();
    }

    public override void Deallocate()
    {
      if (_localFsResourceAccessor != null)
      {
        _localFsResourceAccessor.Dispose();
        _localFsResourceAccessor = null;
      }
      _locator = null;
      base.Deallocate();
    }

    #endregion
  }
}
