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
using MediaPortal.Common.General;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.UI.SkinEngine.Controls.Visuals;

namespace MediaPortal.UiComponents.Media.Models.AspectWrappers
{
/// <summary>
/// SeasonAspectWrapper wraps the contents of <see cref="SeasonAspect"/> into properties that can be bound from xaml controls.
/// Note: this code was automatically created by the MediaItemAspectModelBuilder helper tool under Resources folder.
/// </summary>
public class SeasonAspectWrapper: Control
{
#region Constants

public static readonly ICollection<string> EMPTY_STRING_COLLECTION = new List<string>().AsReadOnly();

#endregion

#region Fields

protected AbstractProperty _seriesNameProperty;
protected AbstractProperty _seasonProperty;
protected AbstractProperty _descriptionProperty;
protected AbstractProperty _availEpisodesProperty;
protected AbstractProperty _numEpisodesProperty;
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

public AbstractProperty DescriptionProperty
{
  get{ return _descriptionProperty; }
}

public string Description
{
  get { return (string) _descriptionProperty.GetValue(); }
  set { _descriptionProperty.SetValue(value); }
}

public AbstractProperty AvailEpisodesProperty
{
  get{ return _availEpisodesProperty; }
}

public int? AvailEpisodes
{
  get { return (int?) _availEpisodesProperty.GetValue(); }
  set { _availEpisodesProperty.SetValue(value); }
}

public AbstractProperty NumEpisodesProperty
{
  get{ return _numEpisodesProperty; }
}

public int? NumEpisodes
{
  get { return (int?) _numEpisodesProperty.GetValue(); }
  set { _numEpisodesProperty.SetValue(value); }
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

public SeasonAspectWrapper()
{
  _seriesNameProperty = new SProperty(typeof(string));
  _seasonProperty = new SProperty(typeof(int?));
  _descriptionProperty = new SProperty(typeof(string));
  _availEpisodesProperty = new SProperty(typeof(int?));
  _numEpisodesProperty = new SProperty(typeof(int?));
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
  if (mediaItem == null ||!MediaItemAspect.TryGetAspect(mediaItem.Aspects, SeasonAspect.Metadata, out aspect))
  {
     SetEmpty();
     return;
  }

  SeriesName = (string) aspect[SeasonAspect.ATTR_SERIES_NAME];
  Season = (int?) aspect[SeasonAspect.ATTR_SEASON];
  Description = (string) aspect[SeasonAspect.ATTR_DESCRIPTION];
  AvailEpisodes = (int?) aspect[SeasonAspect.ATTR_AVAILABLE_EPISODES];
  NumEpisodes = (int?) aspect[SeasonAspect.ATTR_NUM_EPISODES];
}

public void SetEmpty()
{
  SeriesName = null;
  Season = null;
  Description = null;
  AvailEpisodes = null;
  NumEpisodes = null;
}

#endregion

}

}
