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
using MediaPortal.UI.Players.BassPlayer.Interfaces;
using MediaPortal.UI.Players.BassPlayer.Utils;

namespace MediaPortal.UI.Players.BassPlayer.PlayerComponents
{
  /// <summary>
  /// Manages creation and initialization of the outputdevice.
  /// </summary>
  public class OutputDeviceManager : IDisposable
  {
    #region Fields

    private readonly OutputDeviceFactory _outputDeviceFactory;
    private IOutputDevice _outputDevice;
    private bool _initialized;

    #endregion

    public OutputDeviceManager(Controller controller)
    {
      _outputDeviceFactory = new OutputDeviceFactory(controller);
    }

    #region IDisposable Members

    public void Dispose()
    {
      Log.Debug("OutputDeviceManager.Dispose()");

      if (_outputDevice != null)
      {
        Log.Debug("Disposing output device");

        _outputDevice.Dispose();
        _outputDevice = null;

        Log.Debug("Disposing output device factory");
        _outputDeviceFactory.Dispose();
      }
    }

    #endregion

    #region Public members

    /// <summary>
    /// Gets the current inputstream as set with SetInputStream.
    /// </summary>
    public BassStream InputStream
    {
      get { return OutputDevice.InputStream; }
    }

    /// <summary>
    /// Returns a reference to the currently used IOutputDevice object. Can be <c>null</c> if no stream is being played.
    /// </summary>
    public IOutputDevice OutputDevice
    {
      get { return _outputDevice; }
    }

    /// <summary>
    /// Sets the Bass inputstream and initializes the outputdevice.
    /// </summary>
    /// <param name="stream">The stream delivering the input data for this output device.</param>
    /// <param name="passThrough">Sets the passthrough mode.</param>
    public void SetInputStream(BassStream stream, bool passThrough)
    {
      Log.Debug("OutputDeviceManager.SetInputStream()");

      ResetInputStream();

      Log.Debug("Instantiating output device");
      _outputDevice = _outputDeviceFactory.CreateOutputDevice();

      Log.Debug("Calling SetInputStream()");
      _outputDevice.SetInputStream(stream, passThrough);
      _initialized = true;
    }

    /// <summary>
    /// Starts playback.
    /// </summary>
    public void StartDevice()
    {
      Log.Debug("OutputDeviceManager.StartDevice()");

      if (!_initialized)
        throw new BassPlayerException("OutputDeviceManager not initialized");

      if (_outputDevice.DeviceState == DeviceState.Stopped)
      {
        Log.Debug("OutputDevice: PrepareFadeIn()");
        _outputDevice.PrepareFadeIn();

        Log.Debug("OutputDevice: Start()");
        _outputDevice.Start();

        Log.Debug("OutputDevice: FadeIn()");
        _outputDevice.FadeIn(true);
      }
    }

    /// <summary>
    /// Stops playback.
    /// </summary>
    public void StopDevice(bool waitForFadeOut)
    {
      Log.Debug("OutputDeviceManager.StopDevice()");

      if (!_initialized)
        throw new BassPlayerException("OutputDeviceManager not initialized");

      if (_outputDevice.DeviceState == DeviceState.Started)
      {
        Log.Debug("Calling FadeOut()");
        _outputDevice.FadeOut(!waitForFadeOut);

        Log.Debug("Calling Stop()");
        _outputDevice.Stop();
      }
    }

    /// <summary>
    /// Resets the devices outputbuffers and fills them with zeros.
    /// </summary>
    public void ClearDeviceBuffers()
    {
      Log.Debug("OutputDeviceManager.ClearDeviceBuffers()");

      if (!_initialized)
        throw new BassPlayerException("OutputDeviceManager not initialized");

      _outputDevice.ClearBuffers();
    }

    /// <summary>
    /// Resets the outputdevice manager to its uninitialized state.
    /// </summary>
    public void ResetInputStream()
    {
      Log.Debug("OutputDeviceManager.ResetInputStream()");

      if (_initialized)
      {
        _initialized = false;

        Log.Debug("Stopping output device");

        if (_outputDevice.DeviceState == DeviceState.Started)
          _outputDevice.Stop();

        Log.Debug("Disposing output device");

        _outputDevice.Dispose();
        _outputDevice = null;
      }
    }

    #endregion
  }
}