#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion


using System;
using System.Collections.Generic;
using System.Threading;
using System.Timers;
using MediaPortal.Core;
using MediaPortal.Presentation.DataObjects;
using MediaPortal.Presentation.Screen;
using Timer = System.Timers.Timer;

namespace Models.Pictures
{
  public class SlideShow
  {

    private Property _currentPicture;
    private Property _currentPictureUri;
    private Property _previousPicture;
    private Property _currentPictureVisible;
    private Property _previousPictureVisible;
    private Property _currentTitle;
    private Property _pausedProperty;
    private int _pictureIndex;
    private readonly Timer _slideShowTimer;
    private Property _info;
    ItemsCollection _pictures;

    /// <summary>
    /// starts a slideshow.
    /// </summary>
    public SlideShow(ref ItemsCollection pictures)
    {
      _pausedProperty = new Property(typeof(bool), false);
      _pictures = pictures;
      _slideShowTimer = new Timer();
      _slideShowTimer.Interval = 3000;
      _slideShowTimer.Elapsed += _slideShowTimer_Elapsed;
      _currentPicture = new Property(typeof(string), "");
      _previousPicture = new Property(typeof(string), "");
      _currentPictureUri = new Property(typeof(string), "");
      _currentTitle = new Property(typeof(string), "");
      _currentPictureVisible = new Property(typeof(bool), true);
      _previousPictureVisible = new Property(typeof(bool), true);
      _info = new Property(typeof(PictureInfo), new PictureInfo());
      PictureIndex = 0;
      IsPaused = false;
    }


    /// <summary>
    /// Starts the slideshow.
    /// </summary>
    public void Start()
    {
      PictureIndex = -1;
      ProgressSlideShow();
      _slideShowTimer.Enabled = true;
    }
    /// <summary>
    /// Gets or sets the index of the current picture shown.
    /// </summary>
    /// <value>The index of the picture.</value>
    public int PictureIndex
    {
      get
      {
        return _pictureIndex;
      }
      set
      {
        _pictureIndex = value;
        UpdatePictureInfo();
      }
    }
    /// <summary>
    /// Gets or sets a value indicating whether slideshow is paused.
    /// </summary>
    /// <value><c>true</c> if this slideshow is paused; otherwise, <c>false</c>.</value>
    public bool IsPaused
    {
      get { return (bool)_pausedProperty.GetValue(); }
      set { _pausedProperty.SetValue(value); }
    }

    /// <summary>
    /// Gets or sets the is paused property.
    /// </summary>
    /// <value>The is paused property.</value>
    public Property IsPausedProperty
    {
      get { return _pausedProperty; }
      set { _pausedProperty = value; }
    }

    /// <summary>
    /// Pauses /continues the slideshow
    /// </summary>
    public void Pause()
    {
      _slideShowTimer.Enabled = !_slideShowTimer.Enabled;
      IsPaused = !_slideShowTimer.Enabled;
    }

    private void _slideShowTimer_Elapsed(object sender, ElapsedEventArgs e)
    {
      IScreenManager manager = ServiceScope.Get<IScreenManager>();
      if (manager.CurrentScreenName != "pictureviewer")
      {
        PictureIndex = 0;
        _slideShowTimer.Enabled = false;
        return;
      }
      ProgressSlideShow();
    }

    /// <summary>
    /// Shows the next picture from the slideshow
    /// </summary>
    private void ProgressSlideShow()
    {
      while (true)
      {
        PictureIndex++;
        if (PictureIndex >= _pictures.Count)
        {
          _slideShowTimer.Enabled = false;
          PictureIndex = 0;
          IScreenManager manager = ServiceScope.Get<IScreenManager>();
          manager.ShowPreviousScreen();
          return;
        }
        if ((_pictures[PictureIndex] as FolderItem) == null)
        {
          PictureItem picture = (PictureItem)_pictures[PictureIndex];
          Uri uri = picture.MediaItem.ContentUri;
          CurrentPictureUri = uri;
          CurrentPicture = uri.AbsoluteUri;
          CurrentTitle = picture.MediaItem.Title;
          return;
        }
      }
    }

    /// <summary>
    /// Gets or sets a value indicating whether previous picture is visible.
    /// </summary>
    /// <value>
    /// 	<c>true</c> if previous picture is visible; otherwise, <c>false</c>.
    /// </value>
    public bool PreviousPictureVisible
    {
      get { return (bool)_previousPictureVisible.GetValue(); }
      set { _previousPictureVisible.SetValue(value); }
    }

    /// <summary>
    /// Gets or sets the previous picture visible property.
    /// </summary>
    /// <value>The previous picture visible property.</value>
    public Property PreviousPictureVisibleProperty
    {
      get { return _previousPictureVisible; }
      set { _previousPictureVisible = value; }
    }

    /// <summary>
    /// Gets or sets the previous picture.
    /// </summary>
    /// <value>The previous picture.</value>
    public string PreviousPicture
    {
      get { return (string)_previousPicture.GetValue(); }
      set { _previousPicture.SetValue(value); }
    }

    /// <summary>
    /// Gets or sets the previous picture property.
    /// </summary>
    /// <value>The previous picture property.</value>
    public Property PreviousPictureProperty
    {
      get { return _previousPicture; }
      set { _previousPicture = value; }
    }

    /// <summary>
    /// Gets or sets the current picture URI property.
    /// </summary>
    /// <value>The current picture URI property.</value>
    public Property CurrentPictureUriProperty
    {
      get { return _currentPictureUri; }
      set { _currentPictureUri = value; }
    }

    /// <summary>
    /// Gets or sets the current picture URI.
    /// </summary>
    /// <value>The current picture URI.</value>
    public Uri CurrentPictureUri
    {
      get { return (Uri)_currentPictureUri.GetValue(); }
      set { _currentPictureUri.SetValue(value); }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the current picture is visibile.
    /// </summary>
    /// <value>
    /// 	<c>true</c> if current picture is visibile; otherwise, <c>false</c>.
    /// </value>
    public bool CurrentPictureVisibile
    {
      get { return (bool)_currentPictureVisible.GetValue(); }
      set { _currentPictureVisible.SetValue(value); }
    }

    /// <summary>
    /// Gets or sets the current picture visibile property.
    /// </summary>
    /// <value>The current picture visibile property.</value>
    public Property CurrentPictureVisibileProperty
    {
      get { return _currentPictureVisible; }
      set { _currentPictureVisible = value; }
    }

    /// <summary>
    /// Gets or sets the current picture.
    /// </summary>
    /// <value>The current picture.</value>
    public string CurrentPicture
    {
      get { return (string)_currentPicture.GetValue(); }
      set
      {
        if (value != CurrentPicture)
        {
          PreviousPicture = CurrentPicture;
          CurrentPictureVisibile = false;
          PreviousPictureVisible = true;
          _currentPicture.SetValue(value);
          Thread.Sleep(500);
          CurrentPictureVisibile = true;
          PreviousPictureVisible = false;
        }
      }
    }
    public void UpdateCurrentPicture()
    {

      if (PictureIndex >= 0 && PictureIndex < _pictures.Count)
      {
        if ((_pictures[PictureIndex] as FolderItem) == null)
        {
          PictureItem picture = (PictureItem)_pictures[PictureIndex];
          Uri uri = picture.MediaItem.ContentUri;
          CurrentPictureUri = uri;
          CurrentPicture = uri.AbsoluteUri;
          CurrentTitle = picture.MediaItem.Title;
        }
      }
    }

    /// <summary>
    /// Gets or sets the current picture property.
    /// </summary>
    /// <value>The current picture property.</value>
    public Property CurrentPictureProperty
    {
      get { return _currentPicture; }
      set { _currentPicture = value; }
    }

    /// <summary>
    /// Gets or sets the current title.
    /// </summary>
    /// <value>The current title.</value>
    public string CurrentTitle
    {
      get { return (string)_currentTitle.GetValue(); }
      set { _currentTitle.SetValue(value); }
    }

    /// <summary>
    /// Gets or sets the current title property.
    /// </summary>
    /// <value>The current title property.</value>
    public Property CurrentTitleProperty
    {
      get { return _currentTitle; }
      set { _currentTitle = value; }
    }

    /// <summary>
    /// shows the previous picture.
    /// </summary>
    public void ShowPreviousPicture()
    {
      if (PictureIndex > 0)
      {
        PictureIndex--;
      }
      if (PictureIndex >= 0 && PictureIndex < _pictures.Count)
      {
        if ((_pictures[PictureIndex] as FolderItem) == null)
        {
          PictureItem picture = (PictureItem)_pictures[PictureIndex];
          Uri uri = picture.MediaItem.ContentUri;
          CurrentPictureUri = uri;
          CurrentPicture = uri.AbsoluteUri;
          CurrentTitle = picture.MediaItem.Title;
        }
      }
    }

    /// <summary>
    /// shows the next picture
    /// </summary>
    public void ShowNextPicture()
    {
      if (PictureIndex + 1 < _pictures.Count)
      {
        PictureIndex++;
      }
      if (PictureIndex >= 0 && PictureIndex < _pictures.Count)
      {
        if ((_pictures[PictureIndex] as FolderItem) == null)
        {
          PictureItem picture = (PictureItem)_pictures[PictureIndex];
          Uri uri = picture.MediaItem.ContentUri;
          CurrentPictureUri = uri;
          CurrentPicture = uri.AbsoluteUri;
          CurrentTitle = picture.MediaItem.Title;
        }
      }
    }


    public PictureInfo Info
    {
      get { return (PictureInfo)_info.GetValue(); }
      set { _info.SetValue(value); }
    }

    public Property InfoProperty
    {
      get { return _info; }
      set { _info = value; }
    }

    void UpdatePictureInfo()
    {
      PictureInfo info = new PictureInfo();
      if (PictureIndex < 0 || PictureIndex >= _pictures.Count)
      {
        Info = info;
        return;
      }
      PictureItem item = _pictures[PictureIndex] as PictureItem;
      if (item == null)
      {
        Info = info;
        return;
      }
      if (item.MediaItem == null)
      {
        Info = info;
        return;
      }
      if (item.MediaItem.MetaData == null)
      {
        Info = info;
        return;
      }
      info.CameraModel = AddMetaData("CameraModel", item.MediaItem.MetaData);
      info.EquipamentMake = AddMetaData("EquipmentMake", item.MediaItem.MetaData);
      info.Date = AddMetaData("Date", item.MediaItem.MetaData);
      info.ExposureCompensation = AddMetaData("ExposureCompensation", item.MediaItem.MetaData);
      info.ExposureTime = AddMetaData("ExposureTime", item.MediaItem.MetaData);
      info.Flash = AddMetaData("Flash", item.MediaItem.MetaData);
      info.MeteringMod = AddMetaData("MeteringMod", item.MediaItem.MetaData);
      info.Fstop = AddMetaData("Fstop", item.MediaItem.MetaData);
      info.ImgDimensions = AddMetaData("ImgDimensions", item.MediaItem.MetaData);
      info.ShutterSpeed = AddMetaData("ShutterSpeed", item.MediaItem.MetaData);
      info.Resolutions = AddMetaData("Resolutions", item.MediaItem.MetaData);
      info.ViewComment = AddMetaData("ViewComment", item.MediaItem.MetaData);
      info.ImgTitle = item.MediaItem.Title;
      info.AbsolutePath = item.MediaItem.ContentUri.AbsolutePath; 
      Info = info;
    }

    string AddMetaData(string field, IDictionary<string, object> metadata)
    {
      if (metadata.ContainsKey(field))
      {
        if (metadata[field] == null) return "";
        return metadata[field].ToString();
      }
      return "";
    }
  }
}
