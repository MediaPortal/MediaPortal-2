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

using Ui.Players.BassPlayer.Interfaces;
using Ui.Players.BassPlayer.Utils;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Cd;

namespace Ui.Players.BassPlayer.InputSources
{
  /// <summary>
  /// Represents a CD track inputsource implemented by the Bass library.
  /// </summary>
  internal class BassCDTrackInputSource : IInputSource
  {
    #region Static members

    /// <summary>
    /// Creates and initializes an new instance.
    /// </summary>
    /// <param name="cdTrackFilePath">The file path of the CD track to be handled by the instance.</param>
    /// <returns>The new instance.</returns>
    public static BassCDTrackInputSource Create(string cdTrackFilePath)
    {
      BassCDTrackInputSource inputSource = new BassCDTrackInputSource(cdTrackFilePath);
      inputSource.Initialize();
      return inputSource;
    }

    #endregion

    #region Fields

    private readonly string _cdTrackFilePath;
    private BassStream _BassStream;

    #endregion

    public string CDTrackFilePath
    {
      get { return _cdTrackFilePath; }
    }

    #region IInputSource Members

    public MediaItemType MediaItemType
    {
      get { return MediaItemType.CDTrack; }
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

    #region Private members

    private BassCDTrackInputSource(string cdTrackFilePath)
    {
      _cdTrackFilePath = cdTrackFilePath;
    }

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    private void Initialize()
    {
      Log.Debug("BassCDTrackInputSource.Initialize()");

      const BASSFlag flags = BASSFlag.BASS_STREAM_DECODE | BASSFlag.BASS_SAMPLE_FLOAT;

      int handle = BassCd.BASS_CD_StreamCreateFile(_cdTrackFilePath, flags);

      if (handle == BassConstants.BassInvalidHandle)
        throw new BassLibraryException("BASS_CD_StreamCreateFile");

      _BassStream = BassStream.Create(handle);
    }

    #endregion

    public override string ToString()
    {
      return GetType().Name + ": " + _cdTrackFilePath;
    }
  }
}