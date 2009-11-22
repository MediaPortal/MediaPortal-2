#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
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
        Log.Debug("OutputDeviceManager.Dispose()");
        
        if (_OutputDevice != null)
        {
          Log.Debug("Disposing output device");
          
          _OutputDevice.Dispose();
          _OutputDevice = null;

          Log.Debug("Disposing output device factory");
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
        Log.Debug("OutputDeviceManager.SetInputStream()");
        
        ResetInputStream();
        
        Log.Debug("Instantiating output device");
        _OutputDevice = _OutputDeviceFactory.CreateOutputDevice();

        Log.Debug("Calling SetInputStream()");
        _OutputDevice.SetInputStream(stream);
        _Initialized = true;
      }

      /// <summary>
      /// Starts playback.
      /// </summary>
      /// <param name="fadeIn"></param>
      public void StartDevice(bool fadeIn)
      {
        Log.Debug("OutputDeviceManager.StartDevice()");

        if (!_Initialized)
          throw new BassPlayerException("OutputDeviceManager not initialized");
        
        if (_OutputDevice.DeviceState == DeviceState.Stopped)
        {
          if (fadeIn)
          {
            Log.Debug("Calling PrepareFadeIn()");
            _OutputDevice.PrepareFadeIn();
          }

          Log.Debug("Calling Start()");
          _OutputDevice.Start();

          if (fadeIn)
          {
            Log.Debug("Calling FadeIn()");
            _OutputDevice.FadeIn();
          }
        }
      }

      /// <summary>
      /// Stops playback.
      /// </summary>
      public void StopDevice()
      {
        Log.Debug("OutputDeviceManager.StopDevice()");

        if (!_Initialized)
          throw new BassPlayerException("OutputDeviceManager not initialized");

        if (_OutputDevice.DeviceState == DeviceState.Started)
        {
          Log.Debug("Calling FadeOut()");
          _OutputDevice.FadeOut();
          
          Log.Debug("Calling Stop()");
          _OutputDevice.Stop();
        }
      }

      /// <summary>
      /// Resets the devices outputbuffers and fills them with zeros.
      /// </summary>
      public void ClearDeviceBuffers()
      {
        Log.Debug("OutputDeviceManager.ClearDeviceBuffers()");
        
        if (!_Initialized)
          throw new BassPlayerException("OutputDeviceManager not initialized");
        
        _OutputDevice.ClearBuffers();
      }

      /// <summary>
      /// Resets the outputdevice manager to its uninitialized state.
      /// </summary>
      public void ResetInputStream()
      {
        Log.Debug("OutputDeviceManager.ResetInputStream()");

        if (_Initialized)
        {
          _Initialized = false;

          Log.Debug("Stopping output device");

          if (_OutputDevice.DeviceState == DeviceState.Started)
            _OutputDevice.Stop();

          Log.Debug("Disposing output device");

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