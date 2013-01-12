#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess;
using MediaPortal.UI.Players.Video;
using MediaPortal.UI.SkinEngine.Players;

namespace MediaPortal.UiComponents.BackgroundManager.Models
{
  public class VideoBackgroundModel: IDisposable
  {
    public const string MODEL_ID_STR = "441288AC-F88D-4186-8993-6E259F7C75D8";

    protected string _videoFilename = @"{E88E64A8-0233-4fdf-BA27-0B44C6A39AE9}://S:/Redwave.wmv";
    protected VideoPlayer _videoPlayer;
    protected AbstractProperty _videoPlayerProperty;

    #region Protected fields

    #endregion

    public AbstractProperty VideoPlayerProperty
    {
      get { return _videoPlayerProperty; }
    }

    public ISlimDXVideoPlayer VideoPlayer
    {
      get { return (ISlimDXVideoPlayer) _videoPlayerProperty.GetValue(); }
      set { _videoPlayerProperty.SetValue(value); }
    }

    public VideoBackgroundModel()
    {
      _videoPlayerProperty = new SProperty(typeof(ISlimDXVideoPlayer), null);
    }

    public void Dispose()
    {
      EndBackgroundPlayback();
    }

    public void EndBackgroundPlayback()
    {
      ISlimDXVideoPlayer player = VideoPlayer;
      IDisposable disp = player as IDisposable;
      if (player != null)
      {
        player.Stop();
        if (disp != null)
          disp.Dispose();
      }
      VideoPlayer = null;
    }

    public void StartBackgroundPlayback()
    {
      try
      {
        ResourceLocator resourceLocator = new ResourceLocator(ResourcePath.Deserialize(_videoFilename));
        _videoPlayer = new VideoPlayer { AutoRepeat = true };
        _videoPlayer.SetMediaItem(resourceLocator, "VideoBackground");
        VideoPlayer = _videoPlayer;
      }
      catch (Exception)
      {
        if (_videoPlayer != null)
          _videoPlayer.Dispose();
      }
    }
  }
}
