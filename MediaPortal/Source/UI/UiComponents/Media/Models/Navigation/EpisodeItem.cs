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

using System.Linq;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.UiComponents.Media.General;

namespace MediaPortal.UiComponents.Media.Models.Navigation
{
  public class EpisodeItem : VideoItem
  {
    public EpisodeItem(MediaItem mediaItem) :
      base(mediaItem)
    {
    }

    public override void Update(MediaItem mediaItem)
    {
      base.Update(mediaItem);
      EpisodeInfo episodeInfo = new EpisodeInfo();
      if (!episodeInfo.FromMetadata(mediaItem.Aspects)) 
        return;

      Series = episodeInfo.SeriesName.Text;
      EpisodeName = episodeInfo.EpisodeName.Text;
      Season = episodeInfo.SeasonNumber.ToString();
      EpisodeNumber = string.Join(", ", episodeInfo.EpisodeNumbers.OrderBy(e => e));
      if (episodeInfo.DvdEpisodeNumbers.Count > 0)
        DVDEpisodeNumber = string.Join(", ", episodeInfo.DvdEpisodeNumbers.OrderBy(e => e));
      else
        DVDEpisodeNumber = EpisodeNumber;

      // Use the short string without series name here
      SimpleTitle = episodeInfo.ToShortString();
      StoryPlot = episodeInfo.Summary.Text;
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

    /// <summary>
    /// Gets a formatted string of the episode number. If a single video contains multiple episodes, they will be 
    /// concatenated like '01, 02'.
    /// </summary>
    public string DVDEpisodeNumber
    {
      get { return this[Consts.KEY_SERIES_DVD_EPISODE_NUM]; }
      set { SetLabel(Consts.KEY_SERIES_DVD_EPISODE_NUM, value); }
    }

    public string EpisodeName
    {
      get { return this[Consts.KEY_SERIES_EPISODE_NAME]; }
      set { SetLabel(Consts.KEY_SERIES_EPISODE_NAME, value); }
    }
  }
}
