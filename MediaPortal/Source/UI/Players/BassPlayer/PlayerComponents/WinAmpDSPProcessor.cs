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

// Todo: Implement. This is just a passthrough stub.

using System;

namespace MediaPortal.UI.Players.BassPlayer.PlayerComponents
{
  /// <summary>
  /// Performs signal processing using WinAmp DSP plugins.
  /// </summary>
  public class WinAmpDSPProcessor : IDisposable
  {
    #region Static members

    /// <summary>
    /// Creates and initializes an new instance.
    /// </summary>
    /// <param name="controller">Containing controller instance.</param>
    public WinAmpDSPProcessor(Controller controller)
    {
    }

    #endregion

    #region Fields

    private BassStream _inputStream;
    private BassStream _outputStream;
    private bool _initialized;

    #endregion

    #region IDisposable Members

    public void Dispose()
    {
    }

    #endregion

    #region Public members

    /// <summary>
    /// Gets the current inputstream as set with SetInputStream.
    /// </summary>
    public BassStream InputStream
    {
      get { return _inputStream; }
    }

    /// <summary>
    /// Gets the output Bass stream.
    /// </summary>
    public BassStream OutputStream
    {
      get { return _outputStream; }
    }

    /// <summary>
    /// Sets the Bass inputstream.
    /// </summary>
    /// <param name="stream"></param>
    public void SetInputStream(BassStream stream)
    {
      ResetInputStream();
      _inputStream = stream;
      _outputStream = stream;
      _initialized = true;
    }

    /// <summary>
    /// Resets the instance to its uninitialized state.
    /// </summary>
    public void ResetInputStream()
    {
      if (_initialized)
      {
        _initialized = false;

        //_OutputStream.Dispose();
        _outputStream = null;

        _inputStream = null;
      }
    }

    #endregion
  }
}