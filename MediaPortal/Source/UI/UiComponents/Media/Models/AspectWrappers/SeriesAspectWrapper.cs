#region Copyright (C) 2007-2013 Team MediaPortal

/*
    Copyright (C) 2007-2013 Team MediaPortal
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
  /// SeriesAspectWrapper wraps the contents of <see cref="SeriesAspect"/> into properties that can be bound from xaml controls.
  /// Note: this code was automatically created by the MediaItemAspectModelBuilder helper tool under Resources folder.
  /// </summary>
  public class SeriesAspectWrapper : Control
  {
    #region Constants

    public static readonly ICollection<string> EMPTY_STRING_COLLECTION = new List<string>().AsReadOnly();

    #endregion Constants

    #region Fields

    protected AbstractProperty _seriesNameProperty;
    protected AbstractProperty _seasonProperty;
    protected AbstractProperty _episodeProperty;
    protected AbstractProperty _episodeNameProperty;
    protected AbstractProperty _mediaItemProperty;

    #endregion Fields

    #region Properties

    public AbstractProperty SeriesNameProperty
    {
      get { return _seriesNameProperty; }
    }

    public string SeriesName
    {
      get { return (string) _seriesNameProperty.GetValue(); }
      set { _seriesNameProperty.SetValue(value); }
    }

    public AbstractProperty SeasonProperty
    {
      get { return _seasonProperty; }
    }

    public int? Season
    {
      get { return (int?) _seasonProperty.GetValue(); }
      set { _seasonProperty.SetValue(value); }
    }

    public AbstractProperty EpisodeProperty
    {
      get { return _episodeProperty; }
    }

    public IEnumerable<int> Episode
    {
      get { return (IEnumerable<int>) _episodeProperty.GetValue(); }
      set { _episodeProperty.SetValue(value); }
    }

    public AbstractProperty EpisodeNameProperty
    {
      get { return _episodeNameProperty; }
    }

    public string EpisodeName
    {
      get { return (string) _episodeNameProperty.GetValue(); }
      set { _episodeNameProperty.SetValue(value); }
    }

    public AbstractProperty MediaItemProperty
    {
      get { return _mediaItemProperty; }
    }

    public MediaItem MediaItem
    {
      get { return (MediaItem) _mediaItemProperty.GetValue(); }
      set { _mediaItemProperty.SetValue(value); }
    }

    #endregion Properties

    #region Constructor

    public SeriesAspectWrapper()
    {
      _seriesNameProperty = new SProperty(typeof(string));
      _seasonProperty = new SProperty(typeof(int?));
      _episodeProperty = new SProperty(typeof(IEnumerable<int>));
      _episodeNameProperty = new SProperty(typeof(string));
      _mediaItemProperty = new SProperty(typeof(MediaItem));
      _mediaItemProperty.Attach(MediaItemChanged);
    }

    #endregion Constructor

    #region Members

    private void MediaItemChanged(AbstractProperty property, object oldvalue)
    {
      Init(MediaItem);
    }

    public void Init(MediaItem mediaItem)
    {
      MediaItemAspect aspect;
      if (mediaItem == null || !mediaItem.Aspects.TryGetValue(SeriesAspect.ASPECT_ID, out aspect))
      {
        SetEmpty();
        return;
      }

      SeriesName = (string) aspect[SeriesAspect.ATTR_SERIESNAME];
      Season = (int?) aspect[SeriesAspect.ATTR_SEASON];
      Episode = (IEnumerable<int>) aspect[SeriesAspect.ATTR_EPISODE];
      EpisodeName = (string) aspect[SeriesAspect.ATTR_EPISODENAME];
    }

    public void SetEmpty()
    {
      SeriesName = null;
      Season = null;
      Episode = new List<Int32>();
      EpisodeName = null;
    }


    #endregion Members
  }
}
