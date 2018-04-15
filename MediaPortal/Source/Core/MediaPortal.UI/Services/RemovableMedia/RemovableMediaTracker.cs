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
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Settings;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.RemovableMedia;
using MediaPortal.UI.Services.RemovableMedia.Settings;

namespace MediaPortal.UI.Services.RemovableMedia
{
  public class RemovableMediaTracker : IRemovableMediaTracker
  {
    #region Protected fields

    protected DeviceVolumeMonitor _deviceMonitor;
    protected IntPtr _windowHandle = IntPtr.Zero;
    protected readonly object _syncObj = new object();

    #endregion

    #region Ctor & maintainance

    ~RemovableMediaTracker()
    {
      Dispose();
    }

    public void Dispose()
    {
      StopListening();
    }

    /// <summary>
    /// Event that gets triggered whenever a new volume is inserted.
    /// </summary>	
    static void OnMediaInserted(string driveLetter)
    {
      ServiceRegistration.Get<ILogger>().Info("RemovableMediaTracker: Media inserted into drive {0}", driveLetter);

      RemovableMediaMessaging.SendMediaChangedMessage(RemovableMediaMessaging.MessageType.MediaInserted, driveLetter);
    }

    /// <summary>
    /// Event that gets triggered whenever a volume is removed.
    /// </summary>	
    static void OnMediaRemoved(string driveLetter)
    {
      ServiceRegistration.Get<ILogger>().Info("RemovableMediaTracker: Media removed from drive {0}", driveLetter);

      RemovableMediaMessaging.SendMediaChangedMessage(RemovableMediaMessaging.MessageType.MediaRemoved, driveLetter);
    }

    #endregion

    public bool StartListening()
    {
      IScreenControl screenControl = ServiceRegistration.Get<IScreenControl>(false);
      _windowHandle = screenControl == null ? IntPtr.Zero : screenControl.MainWindowHandle;
      if (_windowHandle == IntPtr.Zero)
      {
        ServiceRegistration.Get<ILogger>().Warn("RemovableMediaTracker: No main window handle available, cannot start listening for removable media messages");
        return false;
      }
      RemovableMediaTrackerSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<RemovableMediaTrackerSettings>();
      try
      {
        _deviceMonitor = new DeviceVolumeMonitor(_windowHandle);
        _deviceMonitor.MediaInserted += OnMediaInserted;
        _deviceMonitor.MediaRemoved += OnMediaRemoved;
        _deviceMonitor.Enabled = settings.TrackRemovableMedia;

        ServiceRegistration.Get<ILogger>().Info("RemovableMediaTracker: Monitoring system for removable media changes");
        return true;
      }
      catch (DeviceVolumeMonitorException ex)
      {
        ServiceRegistration.Get<ILogger>().Error("RemovableMediaTracker: Error enabling RemovableMediaTracker service", ex);
      }
      return false;
    }

    public void StopListening()
    {
      if (_deviceMonitor != null)
        _deviceMonitor.Dispose();

      _deviceMonitor = null;
    }

    #region IRemovableMediaTracker implementation

    public bool TrackRemovableMedia
    {
      get { return _deviceMonitor.Enabled; }
      set
      {
        RemovableMediaTrackerSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<RemovableMediaTrackerSettings>();
        settings.TrackRemovableMedia = value;
        ServiceRegistration.Get<ISettingsManager>().Save(settings);
        _deviceMonitor.Enabled = value;
      }
    }

    public void Startup()
    {
      StartListening();
    }

    public void Shutdown()
    {
      StopListening();
    }

    #endregion
  }
}
