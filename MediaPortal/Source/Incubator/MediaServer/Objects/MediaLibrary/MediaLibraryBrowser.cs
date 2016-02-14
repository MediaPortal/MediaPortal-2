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
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Plugins.MediaServer.Profiles;
using MediaPortal.Plugins.Transcoding.Aspects;

namespace MediaPortal.Plugins.MediaServer.Objects.MediaLibrary
{
  public class MediaLibraryBrowser : MediaLibraryContainer
  {
    private static readonly Guid[] NECSSARY_MIA_TYPE_IDS = {
      ProviderResourceAspect.ASPECT_ID,
      MediaAspect.ASPECT_ID,
    };

    private static readonly Guid[] OPTIONAL_MIA_TYPE_IDS = {
      DirectoryAspect.ASPECT_ID,
      VideoAspect.ASPECT_ID,
      AudioAspect.ASPECT_ID,
      ImageAspect.ASPECT_ID,
      TranscodeItemAudioAspect.ASPECT_ID,
      TranscodeItemImageAspect.ASPECT_ID,
      TranscodeItemVideoAspect.ASPECT_ID,
      TranscodeItemVideoAudioAspect.ASPECT_ID,
      TranscodeItemVideoEmbeddedAspect.ASPECT_ID,
    };

    public MediaLibraryBrowser(MediaItem item, EndPointSettings client)
      : base(item, NECSSARY_MIA_TYPE_IDS, OPTIONAL_MIA_TYPE_IDS, null, client)
    {
    }

    public IList<MediaItem> GetItems()
    {
      IMediaLibrary library = ServiceRegistration.Get<IMediaLibrary>();
      return library.Browse(Item.MediaItemId, NECSSARY_MIA_TYPE_IDS, OPTIONAL_MIA_TYPE_IDS);
    }
  }
}
