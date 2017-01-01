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

namespace MediaPortal.UI.Players.BassPlayer.Interfaces
{
  /// <summary>
  /// Provides members to control and read an inputsource.
  /// </summary>
  public interface IInputSource : IDisposable
  {
    /// <summary>
    /// Gets the mediaitem type for the inputsource.
    /// </summary>
    MediaItemType MediaItemType { get; }

    /// <summary>
    /// Gets the output Bass stream.
    /// </summary>
    BassStream OutputStream { get; }

    /// <summary>
    /// Gets the length of this input source. For CD track input sources, the output stream will be lazily initialized
    /// and requesting the output stream from a CD track input source which isn't currently playing via property <see cref="OutputStream"/>
    /// will interfere with the current CD playback.
    /// </summary>
    TimeSpan Length { get; }
  }
}