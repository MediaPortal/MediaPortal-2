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
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.UiComponents.Media.Settings;

namespace MediaPortal.UiComponents.Media.Models.Navigation
{
  /// <summary>
  /// Holds a GUI item which represents a series filter choice.
  /// </summary>
  public class SeriesFilterItem : PlayableContainerMediaItem
  {
    public override void Update(MediaItem mediaItem)
    {
      base.Update(mediaItem);

      SeriesInfo seriesInfo = new SeriesInfo();
      if (!seriesInfo.FromMetadata(mediaItem.Aspects))
        return;

      Series = seriesInfo.SeriesName.Text ?? "";
      StoryPlot = seriesInfo.Description.Text ?? "";

      AvailableSeasons = "";
      TotalSeasons = "";
      AvailableEpisodes = "";
      TotalEpisodes = "";

      int? count;
      if(mediaItem.Aspects.ContainsKey(SeriesAspect.ASPECT_ID))
      {
        if (MediaItemAspect.TryGetAttribute(mediaItem.Aspects, SeriesAspect.ATTR_AVAILABLE_SEASONS, out count))
          AvailableSeasons = count.Value.ToString();

        if (MediaItemAspect.TryGetAttribute(mediaItem.Aspects, SeriesAspect.ATTR_NUM_SEASONS, out count))
          TotalSeasons = count.Value.ToString();

        if (ShowVirtualSetting.ShowVirtualSeriesMedia)
          Seasons = TotalSeasons;
        else
          Seasons = AvailableSeasons;

        if (MediaItemAspect.TryGetAttribute(mediaItem.Aspects, SeriesAspect.ATTR_AVAILABLE_EPISODES, out count))
          AvailableEpisodes = count.Value.ToString();         

        if (MediaItemAspect.TryGetAttribute(mediaItem.Aspects, SeriesAspect.ATTR_NUM_EPISODES, out count))
          TotalEpisodes = count.Value.ToString();

        if (ShowVirtualSetting.ShowVirtualSeriesMedia)
          Episodes = TotalEpisodes;
        else
          Episodes = AvailableEpisodes;

        string text;
        if (MediaItemAspect.TryGetAttribute(mediaItem.Aspects, SeriesAspect.ATTR_SERIES_NAME, out text))
        {
          SimpleTitle = text;
          Series = text;
        }
        if (MediaItemAspect.TryGetAttribute(mediaItem.Aspects, SeriesAspect.ATTR_DESCRIPTION, out text))
        {
          StoryPlot = text;
        }
      }

      FireChange();
    }

    public string Series
    {
      get { return this[Consts.KEY_SERIES_NAME]; }
      set { SetLabel(Consts.KEY_SERIES_NAME, value); }
    }

    public string StoryPlot
    {
      get { return this[Consts.KEY_STORY_PLOT]; }
      set { SetLabel(Consts.KEY_STORY_PLOT, value); }
    }

    public string AvailableEpisodes
    {
      get { return this[Consts.KEY_AVAIL_EPISODES]; }
      set { SetLabel(Consts.KEY_AVAIL_EPISODES, value); }
    }

    public string TotalEpisodes
    {
      get { return this[Consts.KEY_TOTAL_EPISODES]; }
      set { SetLabel(Consts.KEY_TOTAL_EPISODES, value); }
    }

    public string Episodes
    {
      get { return this[Consts.KEY_NUM_EPISODES]; }
      set { SetLabel(Consts.KEY_NUM_EPISODES, value); }
    }

    public string AvailableSeasons
    {
      get { return this[Consts.KEY_AVAIL_SEASONS]; }
      set { SetLabel(Consts.KEY_AVAIL_SEASONS, value); }
    }

    public string TotalSeasons
    {
      get { return this[Consts.KEY_TOTAL_SEASONS]; }
      set { SetLabel(Consts.KEY_TOTAL_SEASONS, value); }
    }

    public string Seasons
    {
      get { return this[Consts.KEY_NUM_SEASONS]; }
      set { SetLabel(Consts.KEY_NUM_SEASONS, value); }
    }
  }
}
