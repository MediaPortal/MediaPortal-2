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
    partial class OutputDeviceManager
    {
      partial class OutputDeviceFactory
      {
        /// <summary>
        /// Represents an ASIO outputdevice.
        /// </summary>
        class ASIOOutputDevice : IOutputDevice
        {
          #region Static members

          /// <summary>
          /// Creates and initializes an new instance.
          /// </summary>
          /// <param name="player">Reference to containing IPlayer object.</param>
          /// <returns>The new instance.</returns>
          public static ASIOOutputDevice Create(BassPlayer player)
          {
            ASIOOutputDevice outputDevice = new ASIOOutputDevice(player);
            outputDevice.Initialize();
            return outputDevice;
          }

          #endregion

          #region Fields

          BassPlayer _Player;

          #endregion

          #region IOutputDevice Members

          public BassStream InputStream
          {
            get { throw new NotImplementedException(); }
          }

          public DeviceState DeviceState
          {
            get { throw new NotImplementedException(); }
          }

          public string Name
          {
            get { throw new NotImplementedException(); }
          }

          public string Driver
          {
            get { throw new NotImplementedException(); }
          }

          public int Channels
          {
            get { throw new NotImplementedException(); }
          }

          public int MinRate
          {
            get { throw new NotImplementedException(); }
          }

          public int MaxRate
          {
            get { throw new NotImplementedException(); }
          }

          public TimeSpan Latency
          {
            get { throw new NotImplementedException(); }
          }

          public void SetInputStream(BassStream stream)
          {
            throw new NotImplementedException();
          }

          public void PrepareFadeIn()
          {
            throw new NotImplementedException();
          }

          public void FadeIn()
          {
            throw new NotImplementedException();
          }

          public void FadeOut()
          {
            throw new NotImplementedException();
          }

          public void Start()
          {
            throw new NotImplementedException();
          }

          public void Stop()
          {
            throw new NotImplementedException();
          }

          public void ClearBuffers()
          {
            throw new NotImplementedException();
          }
          
          #endregion

          #region IDisposable Members

          public void Dispose()
          {
          }

          #endregion

          #region Public members

          #endregion

          #region Private members

          private ASIOOutputDevice(BassPlayer player)
          {
            _Player = player;
          }

          /// <summary>
          /// Initializes a new instance.
          /// </summary>
          private void Initialize()
          {
          }

          #endregion

        }
      }
    }
  }
}