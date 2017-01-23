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
/// VideoAspectWrapper wraps the contents of <see cref="VideoAspect"/> into properties that can be bound from xaml controls.
/// Note: this code was automatically created by the MediaItemAspectModelBuilder helper tool under Resources folder.
/// </summary>
public class VideoAspectWrapper: Control
{
#region Constants

public static readonly ICollection<string> EMPTY_STRING_COLLECTION = new List<string>().AsReadOnly();
public static readonly ICollection<VideoStreamAspectWrapper> EMPTY_VIDEOSTREAMASPECT_COLLECTION = new List<VideoStreamAspectWrapper>().AsReadOnly();
public static readonly ICollection<VideoAudioStreamAspectWrapper> EMPTY_VIDEOAUDIOSTREAMASPECT_COLLECTION = new List<VideoAudioStreamAspectWrapper>().AsReadOnly();
public static readonly ICollection<SubtitleAspectWrapper> EMPTY_SUBTITLEASPECT_COLLECTION = new List<SubtitleAspectWrapper>().AsReadOnly();

#endregion

#region Fields

protected AbstractProperty _actorsProperty;
protected AbstractProperty _directorsProperty;
protected AbstractProperty _writersProperty;
protected AbstractProperty _charactersProperty;
protected AbstractProperty _isDVDProperty;
protected AbstractProperty _storyPlotProperty;
protected AbstractProperty _videoStreamsProperty;
protected AbstractProperty _videoAudioStreamsProperty;
protected AbstractProperty _subtitlesProperty;
protected AbstractProperty _mediaItemProperty;

#endregion

#region Properties

public AbstractProperty ActorsProperty
{
  get{ return _actorsProperty; }
}

public IEnumerable<string> Actors
{
  get { return (IEnumerable<string>) _actorsProperty.GetValue(); }
  set { _actorsProperty.SetValue(value); }
}

public AbstractProperty DirectorsProperty
{
  get{ return _directorsProperty; }
}

public IEnumerable<string> Directors
{
  get { return (IEnumerable<string>) _directorsProperty.GetValue(); }
  set { _directorsProperty.SetValue(value); }
}

public AbstractProperty WritersProperty
{
  get{ return _writersProperty; }
}

public IEnumerable<string> Writers
{
  get { return (IEnumerable<string>) _writersProperty.GetValue(); }
  set { _writersProperty.SetValue(value); }
}

public AbstractProperty CharactersProperty
{
  get{ return _charactersProperty; }
}

public IEnumerable<string> Characters
{
  get { return (IEnumerable<string>) _charactersProperty.GetValue(); }
  set { _charactersProperty.SetValue(value); }
}

public AbstractProperty IsDVDProperty
{
  get{ return _isDVDProperty; }
}

public bool? IsDVD
{
  get { return (bool?) _isDVDProperty.GetValue(); }
  set { _isDVDProperty.SetValue(value); }
}

public AbstractProperty StoryPlotProperty
{
  get{ return _storyPlotProperty; }
}

public string StoryPlot
{
  get { return (string) _storyPlotProperty.GetValue(); }
  set { _storyPlotProperty.SetValue(value); }
}

public AbstractProperty VideoStreamsProperty
{
  get{ return _videoStreamsProperty; }
}

public IEnumerable<VideoStreamAspectWrapper> VideoStreams
{
  get { return (IEnumerable<VideoStreamAspectWrapper>) _videoStreamsProperty.GetValue(); }
  set { _videoStreamsProperty.SetValue(value); }
}

public AbstractProperty VideoAudioStreamsProperty
{
  get{ return _videoAudioStreamsProperty; }
}

public IEnumerable<VideoAudioStreamAspectWrapper> VideoAudioStreams
{
  get { return (IEnumerable<VideoAudioStreamAspectWrapper>) _videoAudioStreamsProperty.GetValue(); }
  set { _videoAudioStreamsProperty.SetValue(value); }
}

public AbstractProperty SubtitlesProperty
{
  get{ return _subtitlesProperty; }
}

public IEnumerable<SubtitleAspectWrapper> Subtitles
{
  get { return (IEnumerable<SubtitleAspectWrapper>) _subtitlesProperty.GetValue(); }
  set { _subtitlesProperty.SetValue(value); }
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

public VideoAspectWrapper()
{
  _actorsProperty = new SProperty(typeof(IEnumerable<string>));
  _directorsProperty = new SProperty(typeof(IEnumerable<string>));
  _writersProperty = new SProperty(typeof(IEnumerable<string>));
  _charactersProperty = new SProperty(typeof(IEnumerable<string>));
  _isDVDProperty = new SProperty(typeof(bool?));
  _storyPlotProperty = new SProperty(typeof(string));
  _videoStreamsProperty = new SProperty(typeof(IEnumerable<VideoStreamAspectWrapper>));
  _videoAudioStreamsProperty = new SProperty(typeof(IEnumerable<VideoAudioStreamAspectWrapper>));
  _subtitlesProperty = new SProperty(typeof(IEnumerable<SubtitleAspectWrapper>));
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
  if (mediaItem == null ||!MediaItemAspect.TryGetAspect(mediaItem.Aspects, VideoAspect.Metadata, out aspect))
  {
     SetEmpty();
     return;
  }

  Actors = (IEnumerable<string>) aspect[VideoAspect.ATTR_ACTORS] ?? EMPTY_STRING_COLLECTION;
  Directors = (IEnumerable<string>) aspect[VideoAspect.ATTR_DIRECTORS] ?? EMPTY_STRING_COLLECTION;
  Writers = (IEnumerable<string>) aspect[VideoAspect.ATTR_WRITERS] ?? EMPTY_STRING_COLLECTION;
  Characters = (IEnumerable<string>) aspect[VideoAspect.ATTR_CHARACTERS] ?? EMPTY_STRING_COLLECTION;
  IsDVD = (bool?) aspect[VideoAspect.ATTR_ISDVD];
  StoryPlot = (string) aspect[VideoAspect.ATTR_STORYPLOT];
  AddVideoStreamAspects(mediaItem);
  AddVideoAudioStreamAspects(mediaItem);
  AddSubtitleAspects(mediaItem);
}

public void SetEmpty()
{
  Actors = EMPTY_STRING_COLLECTION;
  Directors = EMPTY_STRING_COLLECTION;
  Writers = EMPTY_STRING_COLLECTION;
  Characters = EMPTY_STRING_COLLECTION;
  IsDVD = null;
  StoryPlot = null;
  VideoStreams = EMPTY_VIDEOSTREAMASPECT_COLLECTION;
  VideoAudioStreams = EMPTY_VIDEOAUDIOSTREAMASPECT_COLLECTION;
  Subtitles = EMPTY_SUBTITLEASPECT_COLLECTION;
}

protected void AddVideoStreamAspects(MediaItem mediaItem)
{
  IList<MultipleMediaItemAspect> multiAspect;
  if (MediaItemAspect.TryGetAspects(mediaItem.Aspects, VideoStreamAspect.Metadata, out multiAspect))
    VideoStreams = multiAspect.Select((a, i) => new VideoStreamAspectWrapper() { AspectIndex = i, MediaItem = mediaItem }).ToList();
  else
    VideoStreams = EMPTY_VIDEOSTREAMASPECT_COLLECTION;
}

protected void AddVideoAudioStreamAspects(MediaItem mediaItem)
{
  IList<MultipleMediaItemAspect> multiAspect;
  if (MediaItemAspect.TryGetAspects(mediaItem.Aspects, VideoAudioStreamAspect.Metadata, out multiAspect))
    VideoAudioStreams = multiAspect.Select((a, i) => new VideoAudioStreamAspectWrapper() { AspectIndex = i, MediaItem = mediaItem }).ToList();
  else
    VideoAudioStreams = EMPTY_VIDEOAUDIOSTREAMASPECT_COLLECTION;
}

protected void AddSubtitleAspects(MediaItem mediaItem)
{
  IList<MultipleMediaItemAspect> multiAspect;
  if (MediaItemAspect.TryGetAspects(mediaItem.Aspects, SubtitleAspect.Metadata, out multiAspect))
    Subtitles = multiAspect.Select((a, i) => new SubtitleAspectWrapper() { AspectIndex = i, MediaItem = mediaItem }).ToList();
  else
    Subtitles = EMPTY_SUBTITLEASPECT_COLLECTION;
}

#endregion

}

}
