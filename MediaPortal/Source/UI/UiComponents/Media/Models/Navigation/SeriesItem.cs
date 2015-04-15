#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.UiComponents.Media.General;

namespace MediaPortal.UiComponents.Media.Models.Navigation
{
  public class SeriesItem : VideoItem
  {
    public SeriesItem(MediaItem mediaItem) :
      base(mediaItem)
    {
    }

    public override void Update(MediaItem mediaItem)
    {
      base.Update(mediaItem);
      EpisodeInfo episodeInfo = new EpisodeInfo();
      SingleMediaItemAspect episodeAspect;
      if (!MediaItemAspect.TryGetAspect(mediaItem.Aspects, EpisodeAspect.Metadata, out episodeAspect)) 
        return;

      Series = episodeInfo.Series = (string)episodeAspect[EpisodeAspect.ATTR_SERIESNAME] ?? string.Empty;
      EpisodeName = episodeInfo.Episode = (string)episodeAspect[EpisodeAspect.ATTR_EPISODENAME] ?? string.Empty;
      episodeInfo.SeasonNumber = (int)(episodeAspect[EpisodeAspect.ATTR_SEASON] ?? 0);
      Season = episodeInfo.SeasonNumber.ToString();

      IList<int> episodes = episodeAspect[EpisodeAspect.ATTR_EPISODE] as IList<int>;
      if (episodes != null)
      {
        foreach (int episode in episodes.OrderBy(e => e))
          episodeInfo.EpisodeNumbers.Add(episode);
        EpisodeNumber = episodeInfo.FormatString(string.Format("{{{0}}}", EpisodeInfo.EPISODENUM_INDEX));
      }
      // Use the short string without series name here
      SimpleTitle = episodeInfo.ToShortString();
      FireChange();
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
