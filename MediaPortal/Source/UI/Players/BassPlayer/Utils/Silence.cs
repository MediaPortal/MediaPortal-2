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
using System.Runtime.InteropServices;

namespace MediaPortal.UI.Players.BassPlayer.Utils
{
  /// <summary>
  /// Writes silence into a stream.
  /// </summary>
  public class Silence
  {
    #region Fields

    private byte[] _silence = new byte[1];

    #endregion

    #region Public members

    /// <summary>
    /// Writes the requested number of bytes to a buffer.
    /// </summary>
    /// <param name="buffer">Buffer to write to</param>
    /// <param name="requestedBytes">Number of bytes to write.</param>
    /// <returns>Number of bytes written. Always equals requestedBytes.</returns>
    public int Write(IntPtr buffer, int requestedBytes)
    {
      if (_silence.Length < requestedBytes)
        Array.Resize(ref _silence, requestedBytes);

      Marshal.Copy(_silence, 0, buffer, requestedBytes);
      return requestedBytes;
    }

    #endregion
  }
}