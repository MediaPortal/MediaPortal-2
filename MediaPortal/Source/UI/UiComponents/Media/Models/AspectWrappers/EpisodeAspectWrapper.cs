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

using System;
using System.Collections.Generic;
using System.Linq;
using MediaPortal.Common.General;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.UI.SkinEngine.Controls.Visuals;

namespace MediaPortal.UiComponents.Media.Models.AspectWrappers
{
/// <summary>
/// EpisodeAspectWrapper wraps the contents of <see cref="EpisodeAspect"/> into properties that can be bound from xaml controls.
/// Note: this code was automatically created by the MediaItemAspectModelBuilder helper tool under Resources folder.
/// </summary>
public class EpisodeAspectWrapper: Control
{
#region Constants

public static readonly ICollection<string> EMPTY_STRING_COLLECTION = new List<string>().AsReadOnly();

#endregion

#region Fields

protected AbstractProperty _seriesNameProperty;
protected AbstractProperty _seasonProperty;
protected AbstractProperty _seasonNameProperty;
protected AbstractProperty _episodeProperty;
protected AbstractProperty _dvdEpisodeProperty;
protected AbstractProperty _episodeNameProperty;
protected AbstractProperty _totalRatingProperty;
protected AbstractProperty _ratingCountProperty;
protected AbstractProperty _mediaItemProperty;

#endregion

#region Properties

public AbstractProperty SeriesNameProperty
{
  get{ return _seriesNameProperty; }
}

public string SeriesName
{
  get { return (string) _seriesNameProperty.GetValue(); }
  set { _seriesNameProperty.SetValue(value); }
}

public AbstractProperty SeasonProperty
{
  get{ return _seasonProperty; }
}

public int? Season
{
  get { return (int?) _seasonProperty.GetValue(); }
  set { _seasonProperty.SetValue(value); }
}

public AbstractProperty SeasonNameProperty
{
  get{ return _seasonNameProperty; }
}

public string SeasonName
{
  get { return (string) _seasonNameProperty.GetValue(); }
  set { _seasonNameProperty.SetValue(value); }
}

public AbstractProperty EpisodeProperty
{
  get{ return _episodeProperty; }
}

public IEnumerable<int> Episode
{
  get { return (IEnumerable<int>) _episodeProperty.GetValue(); }
  set { _episodeProperty.SetValue(value); }
}

public AbstractProperty DvdEpisodeProperty
{
  get{ return _dvdEpisodeProperty; }
}

public IEnumerable<double> DvdEpisode
{
  get { return (IEnumerable<double>) _dvdEpisodeProperty.GetValue(); }
  set { _dvdEpisodeProperty.SetValue(value); }
}

public AbstractProperty EpisodeNameProperty
{
  get{ return _episodeNameProperty; }
}

public string EpisodeName
{
  get { return (string) _episodeNameProperty.GetValue(); }
  set { _episodeNameProperty.SetValue(value); }
}

public AbstractProperty TotalRatingProperty
{
  get{ return _totalRatingProperty; }
}

public double? TotalRating
{
  get { return (double?) _totalRatingProperty.GetValue(); }
  set { _totalRatingProperty.SetValue(value); }
}

public AbstractProperty RatingCountProperty
{
  get{ return _ratingCountProperty; }
}

public int? RatingCount
{
  get { return (int?) _ratingCountProperty.GetValue(); }
  set { _ratingCountProperty.SetValue(value); }
}

public AbstractProperty MediaItemProperty
{
  get{ return _mediaItemProperty; }
}

public MediaItem MediaItem
{
  get { return (MediaItem) _mediaItemProperty.GetValue(); }
  set { _mediaItemProperty.SetValue(value); }
}

#endregion

#region Constructor

public EpisodeAspectWrapper()
{
  _seriesNameProperty = new SProperty(typeof(string));
  _seasonProperty = new SProperty(typeof(int?));
  _seasonNameProperty = new SProperty(typeof(string));
  _episodeProperty = new SProperty(typeof(IEnumerable<int>));
  _dvdEpisodeProperty = new SProperty(typeof(IEnumerable<double>));
  _episodeNameProperty = new SProperty(typeof(string));
  _totalRatingProperty = new SProperty(typeof(double?));
  _ratingCountProperty = new SProperty(typeof(int?));
  _mediaItemProperty = new SProperty(typeof(MediaItem));
  _mediaItemProperty.Attach(MediaItemChanged);
}

#endregion

#region Members

private void MediaItemChanged(AbstractProperty property, object oldvalue)
{
  Init(MediaItem);
}

public void Init(MediaItem mediaItem)
{
  SingleMediaItemAspect aspect;
  if (mediaItem == null ||!MediaItemAspect.TryGetAspect(mediaItem.Aspects, EpisodeAspect.Metadata, out aspect))
  {
     SetEmpty();
     return;
  }

  SeriesName = (string) aspect[EpisodeAspect.ATTR_SERIES_NAME];
  Season = (int?) aspect[EpisodeAspect.ATTR_SEASON];
  SeasonName = (string) aspect[EpisodeAspect.ATTR_SERIES_SEASON];
  Episode = (IEnumerable<int>) aspect[EpisodeAspect.ATTR_EPISODE];
  DvdEpisode = (IEnumerable<double>) aspect[EpisodeAspect.ATTR_DVDEPISODE];
  EpisodeName = (string) aspect[EpisodeAspect.ATTR_EPISODE_NAME];
  TotalRating = (double?) aspect[EpisodeAspect.ATTR_TOTAL_RATING];
  RatingCount = (int?) aspect[EpisodeAspect.ATTR_RATING_COUNT];
  // Sorting
  Episode = Episode?.OrderBy(e => e);
  DvdEpisode = DvdEpisode?.OrderBy(e => e);
}

public void SetEmpty()
{
  SeriesName = null;
  Season = null;
  SeasonName = null;
  Episode = new List<Int32>();
  DvdEpisode = new List<Double>();
  EpisodeName = null;
  TotalRating = null;
  RatingCount = null;
}

#endregion

}

}
