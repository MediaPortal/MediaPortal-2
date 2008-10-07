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

namespace Media.Players.BassPlayer
{
  public partial class BassPlayer
  {
    /// <summary>
    /// Manages creation and initialization of the outputdevice.
    /// </summary>
    partial class OutputDeviceManager : IDisposable
    {
      #region Static members

      /// <summary>
      /// Creates and initializes an new instance.
      /// </summary>
      /// <param name="player">Reference to containing IPlayer object.</param>
      /// <returns>The new instance.</returns>
      public static OutputDeviceManager Create(BassPlayer player)
      {
        OutputDeviceManager outputDeviceManager = new OutputDeviceManager(player);
        outputDeviceManager.Initialize();
        return outputDeviceManager;
      }

      #endregion

      #region Fields

      private BassPlayer _Player;
      private OutputDeviceFactory _OutputDeviceFactory;
      private IOutputDevice _OutputDevice;
      private bool _Initialized;

      #endregion

      #region IDisposable Members

      public void Dispose()
      {
        if (_OutputDevice != null)
        {
          _OutputDevice.Dispose();
          _OutputDevice = null;
          _OutputDeviceFactory.Dispose();
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
      /// Returns a reference to the currently used IOutputDevice object.
      /// </summary>
      public IOutputDevice OutputDevice
      {
        get { return _OutputDevice; }
      }

      /// <summary>
      /// Sets the Bass inputstream and initializes the outputdevice.
      /// </summary>
      /// <param name="stream"></param>
      public void SetInputStream(BassStream stream)
      {
        ResetInputStream();
        _OutputDevice = _OutputDeviceFactory.CreateOutputDevice();
        _OutputDevice.SetInputStream(stream);
        _Initialized = true;
      }

      /// <summary>
      /// Starts playback.
      /// </summary>
      /// <param name="fadeIn"></param>
      public void StartDevice(bool fadeIn)
      {
        if (_OutputDevice.DeviceState == DeviceState.Stopped)
          _OutputDevice.Start(fadeIn);
      }

      /// <summary>
      /// Stops playback.
      /// </summary>
      public void StopDevice()
      {
        if (_OutputDevice.DeviceState == DeviceState.Started)
          _OutputDevice.Stop();
      }

      /// <summary>
      /// Resets the outputdevice manager to its uninitialized state.
      /// </summary>
      public void ResetInputStream()
      {
        if (_Initialized)
        {
          _Initialized = false;

          if (_OutputDevice.DeviceState == DeviceState.Started)
            _OutputDevice.Stop();

          _OutputDevice.Dispose();
          _OutputDevice = null;
        }
      }

      #endregion

      #region Private members
      
      private OutputDeviceManager(BassPlayer player)
      {
        _Player = player;
      }
      
      /// <summary>
      /// Initializes a new instance.
      /// </summary>
      private void Initialize()
      {
        _OutputDeviceFactory = new OutputDeviceFactory(this._Player);
      }

      #endregion

    }
  }
}