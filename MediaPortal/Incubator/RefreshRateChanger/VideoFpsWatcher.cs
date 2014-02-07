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
using System.Timers;
using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.PluginManager;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.SkinEngine.SkinManagement;

namespace MediaPortal.Plugins.RefreshRateChanger
{
  public class VideoFpsWatcher : IPluginStateTracker, IDisposable
  {
    protected RefreshRateChanger _refreshRateChanger;
    protected Timer _timer;
    protected object _syncObj = new object();

    protected uint GetScreenNum()
    {
      return (uint)Array.IndexOf(System.Windows.Forms.Screen.AllScreens, System.Windows.Forms.Screen.FromControl(SkinContext.Form));
    }

    private void SyncToPlayer(IVideoPlayer player)
    {
      if (_refreshRateChanger != null)
      {
        _refreshRateChanger.Dispose();
        _refreshRateChanger = null;
      }
      MediaItem mediaItem = GetCurrentMediaItem(player);
      if (mediaItem == null)
        return;

      int intFps;
      double fps;
      if (MediaItemAspect.TryGetAttribute(mediaItem.Aspects, VideoAspect.ATTR_FPS, out intFps))
      {
        _refreshRateChanger = new TemporaryRefreshRateChanger(GetScreenNum());
        fps = intFps;
        // TODO: mappings into settings?
        if (intFps == 23)
          fps = 23.976;
        if (intFps == 59)
          fps = 59.940;
        if (intFps == 25)
          fps = 50;
        if (!IsMultipleOf(_refreshRateChanger.GetRefreshRate(), fps))
          _refreshRateChanger.SetRefreshRate(fps);
      }
    }

    private bool IsMultipleOf(double screenRefreshRate, double videoFps)
    {
      return ((int)screenRefreshRate * 1000) % (int)(videoFps * 1000) == 0;
    }

    private MediaItem GetCurrentMediaItem(IVideoPlayer player)
    {
      IPlayerContextManager playerContextManager = ServiceRegistration.Get<IPlayerContextManager>();
      for (int index = 0; index < playerContextManager.NumActivePlayerContexts; index++)
      {
        IPlayerContext playerContext = playerContextManager.GetPlayerContext(index);
        if (playerContext == null || playerContext.CurrentPlayer != player)
          continue;
        return playerContext.CurrentMediaItem;
      }
      return null;
    }

    public void Activated(PluginRuntime pluginRuntime)
    {
      _timer = new Timer(1000);
      _timer.Elapsed += ActivateWhenReady;
      _timer.Start();
    }

    private void ActivateWhenReady(object sender, ElapsedEventArgs e)
    {
      IScreenControl screenControl = ServiceRegistration.Get<IScreenControl>(false);
      if (screenControl == null || screenControl.VideoPlayerSynchronizationStrategy == null)
        return;

      _timer.Close();
      _timer = null;

      screenControl.VideoPlayerSynchronizationStrategy.SynchronizeToVideoPlayerFramerate += SyncToPlayer;
    }

    public bool RequestEnd()
    {
      return true;
    }

    public void Stop()
    {
      IScreenControl screenControl = ServiceRegistration.Get<IScreenControl>();
      screenControl.VideoPlayerSynchronizationStrategy.SynchronizeToVideoPlayerFramerate -= SyncToPlayer;
    }

    public void Continue()
    {
    }

    public void Shutdown()
    {
      Dispose();
    }

    public void Dispose()
    {
      if (_refreshRateChanger != null)
        _refreshRateChanger.Dispose();
    }
  }
}
