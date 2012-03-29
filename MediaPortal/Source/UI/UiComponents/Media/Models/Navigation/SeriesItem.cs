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

using System.Collections.Generic;
using System.Linq;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.UiComponents.Media.General;

namespace MediaPortal.UiComponents.Media.Models.Navigation
{
  public class SeriesItem : VideoItem
  {
    public SeriesItem(MediaItem mediaItem) : base(mediaItem)
    {
      MediaItemAspect seriesAspect;
      if (mediaItem.Aspects.TryGetValue(SeriesAspect.ASPECT_ID, out seriesAspect))
      {
        Series = (string) seriesAspect[SeriesAspect.ATTR_SERIESNAME] ?? string.Empty;
        EpisodeName = (string) seriesAspect[SeriesAspect.ATTR_EPISODENAME] ?? string.Empty;
        Season = (seriesAspect[SeriesAspect.ATTR_SEASONNUMBER] ?? 0).ToString();
        if (seriesAspect[SeriesAspect.ATTR_EPISODENUMBER] != null)
          EpisodeNumber = string.Join(", ", (from num in (IList<int>) seriesAspect[SeriesAspect.ATTR_EPISODENUMBER] select num.ToString()).ToArray());

        SimpleTitle = string.Format("S{0} E{1} - {2}", Season.PadLeft(2, '0'), EpisodeNumber.PadLeft(2, '0'), EpisodeName);
      }
    }

    public string Series
    {
      get { return this[Consts.KEY_SERIES_NAME]; }
      set { SetLabel(Consts.KEY_SERIES_NAME, value); }
    }

    public string Season
    {
      get { return this[Consts.KEY_SERIES_SEASON]; }
      set { SetLabel(Consts.KEY_SERIES_SEASON, value); }
    }

    public string EpisodeNumber
    {
      get { return this[Consts.KEY_SERIES_EPISODE_NUM]; }
      set { SetLabel(Consts.KEY_SERIES_EPISODE_NUM, value); }
    }

    public string EpisodeName
    {
      get { return this[Consts.KEY_SERIES_EPISODE_NAME]; }
      set { SetLabel(Consts.KEY_SERIES_EPISODE_NAME, value); }
    }
  }
}