#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;

namespace Ui.Players.BassPlayer.Interfaces
{
  /// <summary>
  /// Provides members to control and write to an outputdevice.
  /// </summary>
  public interface IOutputDevice : IDisposable
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
    /// Gets the minimum supported samplerate.
    /// </summary>
    int MinRate { get; }

    /// <summary>
    /// Gets the maximum supported samplerate.
    /// </summary>
    int MaxRate { get; }

    /// <summary>
    /// Gets the device's latency in ms.
    /// </summary>
    TimeSpan Latency { get; }

    /// <summary>
    /// Sets the Bass inputstream and initializes the device.
    /// </summary>
    /// <param name="stream">The stream to set as input stream.</param>
    /// <param name="passThrough">Sets the passthrough mode.</param>
    void SetInputStream(BassStream stream, bool passThrough);

    /// <summary>
    /// Prepares for a fadein; sets the volume to zero.
    /// </summary>
    void PrepareFadeIn();
      
    /// <summary>
    /// Performs a fadein.
    /// </summary>
    /// <param name="async">If set to <c>true</c>, the fading process is performed asynchronously and the execution thread
    /// returns at once. If set to <c>false</c>, the execution thread will be blocked until the fading has finished.</param>
    void FadeIn(bool async);

    /// <summary>
    /// Performs a fadeout.
    /// </summary>
    /// <param name="async">If set to <c>true</c>, the fading process is performed asynchronously and the execution thread
    /// returns at once. If set to <c>false</c>, the execution thread will be blocked until the fading has finished.</param>
    void FadeOut(bool async);

    /// <summary>
    /// Starts playback.
    /// </summary>
    void Start();

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