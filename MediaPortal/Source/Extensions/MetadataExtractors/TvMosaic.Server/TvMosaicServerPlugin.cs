#region Copyright (C) 2007-2021 Team MediaPortal

/*
    Copyright (C) 2007-2021 Team MediaPortal
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

using MediaPortal.Common.PluginManager;
using MediaPortal.Common.Services.Settings;
using System;

namespace TvMosaic.Server
{
  public class TvMosaicServerPlugin : IPluginStateTracker
  {
    protected TvMosaicShareWatcher _shareWatcher;
    protected SettingsChangeWatcher<TvMosaicShareSettings> _shareSettingsWatcher;

    public void Activated(PluginRuntime pluginRuntime)
    {
      _shareWatcher = new TvMosaicShareWatcher();
      _shareSettingsWatcher = new SettingsChangeWatcher<TvMosaicShareSettings>();
      StartShareWatcher(_shareSettingsWatcher.Settings);
      _shareSettingsWatcher.SettingsChanged = ShareSettingsChanged;
    }

    private void ShareSettingsChanged(object sender, EventArgs e)
    {
      StartShareWatcher(_shareSettingsWatcher.Settings);
    }

    protected void StartShareWatcher(TvMosaicShareSettings settings)
    {
      if (settings.EnableRecordedTvShareWatcher)
      {
        TimeSpan initialDelay = TimeSpan.FromSeconds(settings.InitialCheckDelaySeconds > 0 ? settings.InitialCheckDelaySeconds : 0);
        TimeSpan interval = TimeSpan.FromSeconds(settings.CheckIntervalSeconds > 0 ? settings.CheckIntervalSeconds : 0);
        // This is OK to call again even if share watcher is already started, it will just update the interval in that case
        _shareWatcher.Start(initialDelay, interval);
      }
      else
      {
        _shareWatcher.Stop().Wait();
      }
    }

    public void Continue()
    {
    }

    public bool RequestEnd()
    {
      return true;
    }

    public void Stop()
    {
    }

    public void Shutdown()
    {
      _shareWatcher?.Dispose();
    }
  }
}
