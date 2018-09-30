#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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

using MediaPortal.Common;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.Settings;
using MediaPortal.UiComponents.Media.Settings;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaPortal.UiComponents.Media.Helpers
{
  public static class VirtualMediaHelper
  {
    static VirtualMediaHelper()
    {
      ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();
      ViewSettings settings = settingsManager.Load<ViewSettings>();
      ShowVirtualSeriesMedia = settings.ShowVirtualSeriesMedia;
      ShowVirtualMovieMedia = settings.ShowVirtualMovieMedia;
      ShowVirtualAudioMedia = settings.ShowVirtualAudioMedia;
    }

    public static bool ShowVirtualSeriesMedia { get; set; }
    public static bool ShowVirtualMovieMedia { get; set; }
    public static bool ShowVirtualAudioMedia { get; set; }

    public static bool ShowVirtualMedia(IEnumerable<Guid> aspectIds)
    {
      if (aspectIds.Intersect(new[] { AudioAspect.ASPECT_ID, AudioAlbumAspect.ASPECT_ID }).Count() > 0)
        return ShowVirtualAudioMedia;
      else if (aspectIds.Intersect(new[] { EpisodeAspect.ASPECT_ID, SeasonAspect.ASPECT_ID, SeriesAspect.ASPECT_ID }).Count() > 0)
        return ShowVirtualSeriesMedia;
      else if (aspectIds.Intersect(new[] { MovieAspect.ASPECT_ID, MovieCollectionAspect.ASPECT_ID }).Count() > 0)
        return ShowVirtualMovieMedia;
      return false;
    }
  }
}
