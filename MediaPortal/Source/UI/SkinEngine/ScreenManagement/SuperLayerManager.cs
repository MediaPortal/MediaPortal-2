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
using System.Threading;
using MediaPortal.Common;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.Utilities.Exceptions;

namespace MediaPortal.UI.SkinEngine.ScreenManagement
{
  public class SuperLayerManager : ISuperLayerManager, IDisposable
  {
    #region Consts

    public const string BUSY_CURSOR_SCREEN_NAME = "BusyCursor";

    protected static TimeSpan INFINITE_PERIOD = TimeSpan.FromMilliseconds(-1);

    #endregion

    #region Protected fields

    protected string _currentSuperLayerName = null;
    protected DateTime? _superLayerEndTime = null;
    protected Timer _superLayerHideTimer;
    protected int _busyScreenRequests = 0;

    protected static object _syncObj = new object();
    protected static SuperLayerManager _instance = null;

    #endregion

    private SuperLayerManager()
    {
      _superLayerHideTimer = new Timer(DoHideSuperLayer);
    }

    public void Dispose()
    {
      WaitHandle notifyObject = new ManualResetEvent(false);
      _superLayerHideTimer.Dispose(notifyObject);
      notifyObject.WaitOne();
      notifyObject.Close();
    }

    protected void DoHideSuperLayer(object state)
    {
      _currentSuperLayerName = null;
      _superLayerEndTime = null;
      IScreenManager screenManager = ServiceRegistration.Get<IScreenManager>();
      screenManager.SetSuperLayer(null);
    }

    public static SuperLayerManager Instance
    {
      get
      {
        lock (_syncObj)
          if (_instance == null)
            _instance = new SuperLayerManager();
        return _instance;
      }
    }

    public void ShowBusyScreen()
    {
      lock (_syncObj)
      {
        _busyScreenRequests++;
        if (_busyScreenRequests != 1)
          return;
      }
      IScreenManager screenManager = ServiceRegistration.Get<IScreenManager>();
      screenManager.SetSuperLayer(BUSY_CURSOR_SCREEN_NAME);
    }

    public void HideBusyScreen()
    {
      lock (_syncObj)
      {
        _busyScreenRequests--;
        if (_busyScreenRequests < 0)
        {
          _busyScreenRequests = 0;
          throw new IllegalCallException("Busy screen should be hidden but is not visible");
        }
        if (_busyScreenRequests != 0)
          return;
      }
      IScreenManager screenManager = ServiceRegistration.Get<IScreenManager>();
      screenManager.SetSuperLayer(_currentSuperLayerName);
    }

    public void ShowSuperLayer(string superLayerScreenName, TimeSpan duration)
    {
      bool changeLayer = false;
      lock (_syncObj)
      {
        if (superLayerScreenName != _currentSuperLayerName)
        {
          changeLayer = true;
          _currentSuperLayerName = superLayerScreenName;
        }
        _superLayerEndTime = DateTime.Now + duration;
        _superLayerHideTimer.Change(duration, INFINITE_PERIOD);
      }

      if (changeLayer)
      {
        IScreenManager screenManager = ServiceRegistration.Get<IScreenManager>();
        screenManager.SetSuperLayer(superLayerScreenName);
      }
    }
  }
}