#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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
using Ui.Players.BassPlayer.Interfaces;
using Ui.Players.BassPlayer.Utils;
using Un4seen.Bass;

namespace Ui.Players.BassPlayer.InputSources
{
  /// <summary>
  /// Represents a file inputsource implemented by the Bass library.
  /// </summary>
  internal class BassWebStreamInputSource : IInputSource
  {
    #region Static members

    /// <summary>
    /// Creates and initializes an new instance.
    /// </summary>
    /// <param name="url">The URL to be handled by the instance.</param>
    /// <returns>The new instance.</returns>
    public static BassWebStreamInputSource Create(string url)
    {
      BassWebStreamInputSource inputSource = new BassWebStreamInputSource(url);
      inputSource.Initialize();
      return inputSource;
    }

    #endregion

    #region Fields

    private readonly string _url;
    private BassStream _BassStream;

    #endregion

    public string URL
    {
      get { return _url; }
    }

    #region IInputSource Members

    public MediaItemType MediaItemType
    {
      get { return MediaItemType.WebStream; }
    }

    public BassStream OutputStream
    {
      get { return _BassStream; }
    }

    #endregion

    #region IDisposable Members

    public void Dispose()
    {
      if (_BassStream != null)
        _BassStream.Dispose();
    }

    #endregion

    #region Public members

    #endregion

    #region Private Members

    private BassWebStreamInputSource(string url)
    {
      _url = url;
    }

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    private void Initialize()
    {
      Log.Debug("BassWebStreamInputSource.Initialize()");

      const BASSFlag flags = BASSFlag.BASS_STREAM_DECODE | BASSFlag.BASS_SAMPLE_FLOAT;

      int handle = Bass.BASS_StreamCreateURL(_url, 0, flags, null, new IntPtr());

      if (handle == BassConstants.BassInvalidHandle)
        throw new BassLibraryException("BASS_MusicLoad");

      _BassStream = BassStream.Create(handle);
    }

    #endregion
  
    public override string ToString()
    {
      return GetType().Name + ": " + _url;
    }
  }
}