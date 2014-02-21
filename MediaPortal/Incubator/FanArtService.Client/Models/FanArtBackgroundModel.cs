#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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
using MediaPortal.Common.General;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Extensions.UserServices.FanArtService.Interfaces;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.SkinEngine.Controls.ImageSources;
using MediaPortal.UiComponents.Media.Models.Navigation;

namespace MediaPortal.Extensions.UserServices.FanArtService.Client.Models
{
  public class FanArtBackgroundModel
  {
    #region Consts

    public const string FANART_MODEL_ID_STR = "8F42C8E9-E8A3-475C-A50A-99E1E644DC91";
    public static Guid FANART_MODEL_ID = new Guid(FANART_MODEL_ID_STR);

    #endregion

    protected AbstractProperty _selectedItemProperty;
    protected AbstractProperty _fanArtMediaTypeProperty;
    protected AbstractProperty _fanArtNameProperty;
    protected AbstractProperty _itemDescriptionProperty;
    protected AbstractProperty _mediaItemProperty;
    protected AbstractProperty _imageSourceProperty;

    public FanArtBackgroundModel()
    {
      _selectedItemProperty = new WProperty(typeof(ListItem), null);
      _selectedItemProperty.Attach(SetFanArtType);
      _fanArtMediaTypeProperty = new WProperty(typeof(FanArtConstants.FanArtMediaType), FanArtConstants.FanArtMediaType.Undefined);
      _fanArtNameProperty = new WProperty(typeof(string), string.Empty);
      _itemDescriptionProperty = new WProperty(typeof(string), string.Empty);
      _mediaItemProperty = new WProperty(typeof(MediaItem), null);
      _imageSourceProperty = new WProperty(typeof(ImageSource), null);
      SetFanArtType();
      SetImageSource();
    }

    public Guid ModelId
    {
      get { return FANART_MODEL_ID; }
    }

    #region Members to be accessed from the GUI

    public AbstractProperty SelectedItemProperty
    {
      get { return _selectedItemProperty; }
    }

    public ListItem SelectedItem
    {
      get { return (ListItem) _selectedItemProperty.GetValue(); }
      set { _selectedItemProperty.SetValue(value); }
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

    public AbstractProperty FanArtMediaTypeProperty
    {
      get { return _fanArtMediaTypeProperty; }
    }

    public FanArtConstants.FanArtMediaType FanArtMediaType
    {
      get { return (FanArtConstants.FanArtMediaType) _fanArtMediaTypeProperty.GetValue(); }
      internal set { _fanArtMediaTypeProperty.SetValue(value); }
    }

    public AbstractProperty FanArtNameProperty
    {
      get { return _fanArtNameProperty; }
    }

    public string FanArtName
    {
      get { return (string) _fanArtNameProperty.GetValue(); }
      internal set { _fanArtNameProperty.SetValue(value); }
    }

    public AbstractProperty ItemDescriptionProperty
    {
      get { return _itemDescriptionProperty; }
    }

    public string ItemDescription
    {
      get { return (string) _itemDescriptionProperty.GetValue(); }
      internal set { _itemDescriptionProperty.SetValue(value); }
    }

    public AbstractProperty ImageSourceProperty
    {
      get { return _imageSourceProperty; }
    }

    public ImageSource ImageSource
    {
      get { return (ImageSource) _imageSourceProperty.GetValue(); }
      set { _imageSourceProperty.SetValue(value); }
    }

    public void SetSelectedItem(ListItem item)
    {
      SelectedItem = item;
    }

    #endregion

    private void SetFanArtType(AbstractProperty property, object value)
    {
      SetFanArtType();
      SetImageSource();
    }

    /// <summary>
    /// Creates a new FanArtImageSource instance for exposing it using <see cref="ImageSource"/>.
    /// </summary>
    private void SetImageSource()
    {
      ImageSource = new FanArtImageSource
        {
          FanArtMediaType = FanArtMediaType,
          FanArtName = FanArtName,
        };
    }

    private void SetFanArtType()
    {
      PlayableMediaItem playableMediaItem = SelectedItem as PlayableMediaItem;
      if (playableMediaItem != null)
        MediaItem = playableMediaItem.MediaItem;

      SeriesFilterItem series = SelectedItem as SeriesFilterItem;
      if (series != null)
      {
        FanArtMediaType = FanArtConstants.FanArtMediaType.Series;
        FanArtName = series.SimpleTitle;
        ItemDescription = null;
        return;
      }
      SeriesItem episode = SelectedItem as SeriesItem;
      if (episode != null)
      {
        FanArtMediaType = FanArtConstants.FanArtMediaType.Series;
        FanArtName = episode.Series;
        ItemDescription = episode.StoryPlot;
        return;
      }
      MovieFilterItem movieCollection = SelectedItem as MovieFilterItem;
      if (movieCollection != null)
      {
        FanArtMediaType = FanArtConstants.FanArtMediaType.MovieCollection;
        FanArtName = movieCollection.SimpleTitle;
        ItemDescription = null;
        return;
      }
      MovieItem movie = SelectedItem as MovieItem;
      if (movie != null)
      {
        FanArtMediaType = FanArtConstants.FanArtMediaType.Movie;
        FanArtName = movie.MovieName;
        ItemDescription = movie.StoryPlot;
        return;
      }
      VideoItem video = SelectedItem as VideoItem;
      if (video != null)
      {
        FanArtMediaType = FanArtConstants.FanArtMediaType.Undefined;
        FanArtName = string.Empty;
        ItemDescription = video.StoryPlot;
        return;
      }
      FanArtMediaType = FanArtConstants.FanArtMediaType.Undefined;
      FanArtName = string.Empty;
      ItemDescription = string.Empty;
    }
  }
}
