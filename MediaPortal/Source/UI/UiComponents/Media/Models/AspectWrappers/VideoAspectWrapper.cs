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

using System.Collections.Generic;
using MediaPortal.Common.General;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.UI.SkinEngine.Controls.Visuals;

namespace MediaPortal.UiComponents.Media.Models.AspectWrappers
{
  /// <summary>
  /// VideoAspectWrapper wraps the contents of <see cref="VideoAspect"/> into properties that can be bound from xaml controls.
  /// Note: this code was automatically created by the MediaItemAspectModelBuilder helper tool under Resources folder.
  /// </summary>
  public class VideoAspectWrapper : Control
  {
    #region Constants

    public static readonly ICollection<string> EMPTY_STRING_COLLECTION = new List<string>().AsReadOnly();

    #endregion

    #region Fields

    protected AbstractProperty _genresProperty;
    protected AbstractProperty _durationProperty;
    protected AbstractProperty _audioStreamCountProperty;
    protected AbstractProperty _audioEncodingProperty;
    protected AbstractProperty _audioBitRateProperty;
    protected AbstractProperty _audioLanguagesProperty;
    protected AbstractProperty _videoEncodingProperty;
    protected AbstractProperty _videoBitRateProperty;
    protected AbstractProperty _aspectWidthProperty;
    protected AbstractProperty _aspectHeightProperty;
    protected AbstractProperty _aspectRatioProperty;
    protected AbstractProperty _fPSProperty;
    protected AbstractProperty _actorsProperty;
    protected AbstractProperty _directorsProperty;
    protected AbstractProperty _writersProperty;
    protected AbstractProperty _isDVDProperty;
    protected AbstractProperty _storyPlotProperty;
    protected AbstractProperty _mediaItemProperty;

    #endregion

    #region Properties

    public AbstractProperty GenresProperty
    {
      get { return _genresProperty; }
    }

    public IEnumerable<string> Genres
    {
      get { return (IEnumerable<string>)_genresProperty.GetValue(); }
      set { _genresProperty.SetValue(value); }
    }

    public AbstractProperty DurationProperty
    {
      get { return _durationProperty; }
    }

    public long? Duration
    {
      get { return (long?)_durationProperty.GetValue(); }
      set { _durationProperty.SetValue(value); }
    }

    public AbstractProperty AudioStreamCountProperty
    {
      get { return _audioStreamCountProperty; }
    }

    public int? AudioStreamCount
    {
      get { return (int?)_audioStreamCountProperty.GetValue(); }
      set { _audioStreamCountProperty.SetValue(value); }
    }

    public AbstractProperty AudioEncodingProperty
    {
      get { return _audioEncodingProperty; }
    }

    public string AudioEncoding
    {
      get { return (string)_audioEncodingProperty.GetValue(); }
      set { _audioEncodingProperty.SetValue(value); }
    }

    public AbstractProperty AudioBitRateProperty
    {
      get { return _audioBitRateProperty; }
    }

    public long? AudioBitRate
    {
      get { return (long?)_audioBitRateProperty.GetValue(); }
      set { _audioBitRateProperty.SetValue(value); }
    }

    public AbstractProperty AudioLanguagesProperty
    {
      get { return _audioLanguagesProperty; }
    }

    public IEnumerable<string> AudioLanguages
    {
      get { return (IEnumerable<string>)_audioLanguagesProperty.GetValue(); }
      set { _audioLanguagesProperty.SetValue(value); }
    }

    public AbstractProperty VideoEncodingProperty
    {
      get { return _videoEncodingProperty; }
    }

    public string VideoEncoding
    {
      get { return (string)_videoEncodingProperty.GetValue(); }
      set { _videoEncodingProperty.SetValue(value); }
    }

    public AbstractProperty VideoBitRateProperty
    {
      get { return _videoBitRateProperty; }
    }

    public long? VideoBitRate
    {
      get { return (long?)_videoBitRateProperty.GetValue(); }
      set { _videoBitRateProperty.SetValue(value); }
    }

    public AbstractProperty AspectWidthProperty
    {
      get { return _aspectWidthProperty; }
    }

    public int? AspectWidth
    {
      get { return (int?)_aspectWidthProperty.GetValue(); }
      set { _aspectWidthProperty.SetValue(value); }
    }

    public AbstractProperty AspectHeightProperty
    {
      get { return _aspectHeightProperty; }
    }

    public int? AspectHeight
    {
      get { return (int?)_aspectHeightProperty.GetValue(); }
      set { _aspectHeightProperty.SetValue(value); }
    }

    public AbstractProperty AspectRatioProperty
    {
      get { return _aspectRatioProperty; }
    }

    public float? AspectRatio
    {
      get { return (float?)_aspectRatioProperty.GetValue(); }
      set { _aspectRatioProperty.SetValue(value); }
    }

    public AbstractProperty FPSProperty
    {
      get { return _fPSProperty; }
    }

    public int? FPS
    {
      get { return (int?)_fPSProperty.GetValue(); }
      set { _fPSProperty.SetValue(value); }
    }

    public AbstractProperty ActorsProperty
    {
      get { return _actorsProperty; }
    }

    public IEnumerable<string> Actors
    {
      get { return (IEnumerable<string>)_actorsProperty.GetValue(); }
      set { _actorsProperty.SetValue(value); }
    }

    public AbstractProperty DirectorsProperty
    {
      get { return _directorsProperty; }
    }

    public IEnumerable<string> Directors
    {
      get { return (IEnumerable<string>)_directorsProperty.GetValue(); }
      set { _directorsProperty.SetValue(value); }
    }

    public AbstractProperty WritersProperty
    {
      get { return _writersProperty; }
    }

    public IEnumerable<string> Writers
    {
      get { return (IEnumerable<string>)_writersProperty.GetValue(); }
      set { _writersProperty.SetValue(value); }
    }

    public AbstractProperty IsDVDProperty
    {
      get { return _isDVDProperty; }
    }

    public bool? IsDVD
    {
      get { return (bool?)_isDVDProperty.GetValue(); }
      set { _isDVDProperty.SetValue(value); }
    }

    public AbstractProperty StoryPlotProperty
    {
      get { return _storyPlotProperty; }
    }

    public string StoryPlot
    {
      get { return (string)_storyPlotProperty.GetValue(); }
      set { _storyPlotProperty.SetValue(value); }
    }

    public AbstractProperty MediaItemProperty
    {
      get { return _mediaItemProperty; }
    }

    public MediaItem MediaItem
    {
      get { return (MediaItem)_mediaItemProperty.GetValue(); }
      set { _mediaItemProperty.SetValue(value); }
    }

    #endregion

    #region Constructor

    public VideoAspectWrapper()
    {
      _genresProperty = new SProperty(typeof(IEnumerable<string>));
      _durationProperty = new SProperty(typeof(long?));
      _audioStreamCountProperty = new SProperty(typeof(int?));
      _audioEncodingProperty = new SProperty(typeof(string));
      _audioBitRateProperty = new SProperty(typeof(long?));
      _audioLanguagesProperty = new SProperty(typeof(IEnumerable<string>));
      _videoEncodingProperty = new SProperty(typeof(string));
      _videoBitRateProperty = new SProperty(typeof(long?));
      _aspectWidthProperty = new SProperty(typeof(int?));
      _aspectHeightProperty = new SProperty(typeof(int?));
      _aspectRatioProperty = new SProperty(typeof(float?));
      _fPSProperty = new SProperty(typeof(int?));
      _actorsProperty = new SProperty(typeof(IEnumerable<string>));
      _directorsProperty = new SProperty(typeof(IEnumerable<string>));
      _writersProperty = new SProperty(typeof(IEnumerable<string>));
      _isDVDProperty = new SProperty(typeof(bool?));
      _storyPlotProperty = new SProperty(typeof(string));
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
      MediaItemAspect aspect;
      if (mediaItem == null || !mediaItem.Aspects.TryGetValue(VideoAspect.ASPECT_ID, out aspect))
      {
        SetEmpty();
        return;
      }

      Genres = (IEnumerable<string>)aspect[VideoAspect.ATTR_GENRES] ?? EMPTY_STRING_COLLECTION;
      Duration = (long?)aspect[VideoAspect.ATTR_DURATION];
      AudioStreamCount = (int?)aspect[VideoAspect.ATTR_AUDIOSTREAMCOUNT];
      AudioEncoding = (string)aspect[VideoAspect.ATTR_AUDIOENCODING];
      AudioBitRate = (long?)aspect[VideoAspect.ATTR_AUDIOBITRATE];
      AudioLanguages = (IEnumerable<string>)aspect[VideoAspect.ATTR_AUDIOLANGUAGES] ?? EMPTY_STRING_COLLECTION;
      VideoEncoding = (string)aspect[VideoAspect.ATTR_VIDEOENCODING];
      VideoBitRate = (long?)aspect[VideoAspect.ATTR_VIDEOBITRATE];
      AspectWidth = (int?)aspect[VideoAspect.ATTR_WIDTH];
      AspectHeight = (int?)aspect[VideoAspect.ATTR_HEIGHT];
      AspectRatio = (float?)aspect[VideoAspect.ATTR_ASPECTRATIO];
      FPS = (int?)aspect[VideoAspect.ATTR_FPS];
      Actors = (IEnumerable<string>)aspect[VideoAspect.ATTR_ACTORS] ?? EMPTY_STRING_COLLECTION;
      Directors = (IEnumerable<string>)aspect[VideoAspect.ATTR_DIRECTORS] ?? EMPTY_STRING_COLLECTION;
      Writers = (IEnumerable<string>)aspect[VideoAspect.ATTR_WRITERS] ?? EMPTY_STRING_COLLECTION;
      IsDVD = (bool?)aspect[VideoAspect.ATTR_ISDVD];
      StoryPlot = (string)aspect[VideoAspect.ATTR_STORYPLOT];
    }

    public void SetEmpty()
    {
      Genres = EMPTY_STRING_COLLECTION;
      Duration = null;
      AudioStreamCount = null;
      AudioEncoding = null;
      AudioBitRate = null;
      AudioLanguages = EMPTY_STRING_COLLECTION;
      VideoEncoding = null;
      VideoBitRate = null;
      AspectWidth = null;
      AspectHeight = null;
      AspectRatio = null;
      FPS = null;
      Actors = EMPTY_STRING_COLLECTION;
      Directors = EMPTY_STRING_COLLECTION;
      Writers = EMPTY_STRING_COLLECTION;
      IsDVD = null;
      StoryPlot = null;
    }


    #endregion

  }

}
