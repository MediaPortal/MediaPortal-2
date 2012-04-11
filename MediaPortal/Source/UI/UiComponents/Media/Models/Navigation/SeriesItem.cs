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
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.UiComponents.Media.General;

namespace MediaPortal.UiComponents.Media.Models.Navigation
{
  public class SeriesItem : VideoItem
  {
    public SeriesItem(MediaItem mediaItem) : base(mediaItem)
    {
      SeriesInfo seriesInfo = new SeriesInfo();
      MediaItemAspect seriesAspect;
      if (!mediaItem.Aspects.TryGetValue(SeriesAspect.ASPECT_ID, out seriesAspect)) 
        return;

      Series = seriesInfo.Series = (string) seriesAspect[SeriesAspect.ATTR_SERIESNAME] ?? string.Empty;
      EpisodeName = seriesInfo.Episode = (string) seriesAspect[SeriesAspect.ATTR_EPISODENAME] ?? string.Empty;
      seriesInfo.SeasonNumber = (int) (seriesAspect[SeriesAspect.ATTR_SEASON] ?? 0);
      Season = seriesInfo.SeasonNumber.ToString();

      IList<int> episodes = seriesAspect[SeriesAspect.ATTR_EPISODE] as IList<int>;
      if (episodes != null)
      {
        foreach (int episode in episodes)
          seriesInfo.EpisodeNumbers.Add(episode);
        EpisodeNumber = seriesInfo.FormatString(string.Format("{{{0}}}", SeriesInfo.EPISODENUM_INDEX));
      }
      // Use the short string without series name here
      SimpleTitle = seriesInfo.ToShortString();
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

    /// <summary>
    /// Gets a formatted string of the episode number. If a single video contains multiple episodes, they will be 
    /// concatenated like '01, 02'.
    /// </summary>
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