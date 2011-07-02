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

using System.Collections.Generic;
using System.IO;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.MediaManagement.ResourceAccess;

namespace MediaPortal.UiComponents.Media.Views.RemovableMediaDrives
{
  public enum VideoMediaType
  {
    Unknown,
    VideoBD,
    VideoDVD,
    VideoCD,
  }

  public class VideoDriveHandler : BaseDriveHandler
  {
    #region Protected fields

    protected MediaItem _mediaItem;

    #endregion

    protected VideoDriveHandler(DriveInfo driveInfo) : base(driveInfo)
    {
        IMediaAccessor mediaAccessor = ServiceRegistration.Get<IMediaAccessor>();
        ResourcePath rp = LocalFsMediaProviderBase.ToProviderResourcePath(driveInfo.Name);
        using (IResourceAccessor ra = rp.CreateLocalResourceAccessor())
          _mediaItem = mediaAccessor.CreateMediaItem(ra, mediaAccessor.GetMetadataExtractorsForCategory(DefaultMediaCategory.Video.ToString()));
    }

    public static VideoDriveHandler TryCreateVideoDriveHandler(DriveInfo driveInfo)
    {
      VideoMediaType vmt;
      if (DetectVideoMedia(driveInfo.Name, out vmt))
        return new VideoDriveHandler(driveInfo);
      return null;
    }

    /// <summary>
    /// Detects if a video CD/DVD/BD is contained in the given <paramref name="drive"/>.
    /// </summary>
    /// <param name="drive">The drive to be examined.</param>
    /// <param name="videoMediaType">Returns the type of the media found in the given <paramref name="drive"/>. This parameter
    /// only returns a sensible value when the return value of this method is <c>true</c>.</param>
    /// <returns><c>true</c>, if a video media was identified, else <c>false</c>.</returns>
    public static bool DetectVideoMedia(string drive, out VideoMediaType videoMediaType)
    {
      videoMediaType = VideoMediaType.Unknown;
      if (string.IsNullOrEmpty(drive))
        return false;

      if (Directory.Exists(drive + "\\BDMV"))
      {
        ServiceRegistration.Get<ILogger>().Info("RemovableMediaManager: BD inserted into drive {0}", drive);
        videoMediaType = VideoMediaType.VideoBD;
        return true;
      }

      if (Directory.Exists(drive + "\\VIDEO_TS"))
      {
        ServiceRegistration.Get<ILogger>().Info("RemovableMediaManager: DVD inserted into drive {0}", drive);
        videoMediaType = VideoMediaType.VideoDVD;
        return true;
      }

      if (Directory.Exists(drive + "\\MPEGAV"))
      {
        ServiceRegistration.Get<ILogger>().Info("RemovableMediaManager: Video CD inserted into drive {0}", drive);
        videoMediaType = VideoMediaType.VideoCD;
        return true;
      }
      return false;
    }

    public MediaItem VideoItem
    {
      get { return _mediaItem; }
    }

    #region IRemovableDriveHandler implementation

    public override IList<MediaItem> MediaItems
    {
      get { return new List<MediaItem>(1) {_mediaItem}; }
    }

    public override IList<ViewSpecification> SubViewSpecifications
    {
      get { return new List<ViewSpecification>(0); }
    }

    public override IEnumerable<MediaItem> GetAllMediaItems()
    {
      yield return _mediaItem;
    }

    #endregion
  }
}