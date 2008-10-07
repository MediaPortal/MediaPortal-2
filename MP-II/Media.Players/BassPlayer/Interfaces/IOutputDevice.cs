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
    /// Provides members to control and write to an outputdevice.
    /// </summary>
    interface IOutputDevice : IDisposable
    {
      /// <summary>
      /// Gets the current inputstream as set with SetInputStream.
      /// </summary>
      BassStream InputStream { get; }

      /// <summary>
      /// Gets the current state the device is in.
      /// </summary>
      DeviceState DeviceState { get; }

      /// <summary>
      /// Gets the number of available channels.
      /// </summary>
      string Name { get; }

      /// <summary>
      /// Gets the number of available channels.
      /// </summary>
      string Driver { get; }

      /// <summary>
      /// Gets the number of available channels.
      /// </summary>
      int Channels { get; }

      /// <summary>
      /// Gets the minimum supported samplingrate.
      /// </summary>
      int MinRate { get; }

      /// <summary>
      /// Gets the maximum supported samplingrate.
      /// </summary>
      int MaxRate { get; }

      /// <summary>
      /// Gets the device's latency in ms.
      /// </summary>
      TimeSpan Latency { get; }

      /// <summary>
      /// Sets the Bass inputstream and initializes the device.
      /// </summary>
      /// <param name="stream"></param>
      void SetInputStream(BassStream stream);

      /// <summary>
      /// Starts playback.
      /// </summary>
      /// <param name="fadeIn"></param>
      void Start(bool fadeIn);

      /// <summary>
      /// Stops playback.
      /// </summary>
      void Stop();

      /// <summary>
      /// Clears playbackbuffers by overwriting them with zeros.
      /// </summary>
      void ClearBuffers();
    }
  }
}