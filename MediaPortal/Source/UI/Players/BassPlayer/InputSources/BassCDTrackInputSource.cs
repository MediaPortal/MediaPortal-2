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
using MediaPortal.Extensions.BassLibraries;
using MediaPortal.UI.Players.BassPlayer.Interfaces;
using MediaPortal.UI.Players.BassPlayer.Utils;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Cd;

namespace MediaPortal.UI.Players.BassPlayer.InputSources
{
  /// <summary>
  /// Represents a CD track inputsource specified by drive letter and track number.
  /// </summary>
  internal class BassCDTrackInputSource : IInputSource
  {
    #region Static members

    /// <summary>
    /// Creates and initializes an new instance using drive and track number.
    /// </summary>
    /// <param name="drive">Drive letter of the drive where the audio CD is inserted.</param>
    /// <param name="trackNo">Number of the track to play. The number of the first track is 1.</param>
    /// <returns>The new instance.</returns>
    public static BassCDTrackInputSource Create(char drive, uint trackNo)
    {
      BassCDTrackInputSource inputSource = new BassCDTrackInputSource(drive, trackNo);
      inputSource.Initialize();
      return inputSource;
    }

    #endregion

    #region Fields

    private readonly char _drive;
    private uint _trackNo;
    private TimeSpan _length;
    private BassStream _bassStream = null;

    #endregion

    /// <summary>
    /// Drive letter of the CD drive where the audio CD is located.
    /// </summary>
    public char Drive
    {
      get { return _drive; }
    }

    /// <summary>
    /// Number of the audio CD track of this input source. The first track has the number 1.
    /// </summary>
    public uint TrackNo
    {
      get { return _trackNo; }
    }

    /// <summary>
    /// Number of the audio CD track of this input source. The first track has the number 0.
    /// </summary>
    public int BassTrackNo
    {
      get { return (int) _trackNo - 1; }
    }

    public bool IsInitialized
    {
      get { return _bassStream != null; }
    }

    public bool SwitchTo(BassCDTrackInputSource other)
    {
      if (other.Drive != _drive)
        return false;

      BassStream stream = _bassStream;
      if (stream != null && BassCd.BASS_CD_StreamSetTrack(stream.Handle, other.BassTrackNo))
      {
        Log.Debug("BassCDTrackInputSource: Simply switched the current track to drive '{0}', track {1}", other.Drive, other.TrackNo);
        _trackNo = other._trackNo;
        _length = other._length;
        stream.UpdateLocalFields();
        other.Dispose();
        return true;
      }
      Log.Debug("BassCDTrackInputSource: Could not simply switch the current track to drive '{0}', track {1}", other.Drive, other.TrackNo);
      return false;
    }

    #region IInputSource Members

    public MediaItemType MediaItemType
    {
      get { return MediaItemType.CDTrack; }
    }

    public BassStream OutputStream
    {
      get
      {
        if (!IsInitialized)
          Initialize();
        return _bassStream;
      }
    }

    public TimeSpan Length
    {
      get { return _length; }
    }

    #endregion

    #region IDisposable Members

    public void Dispose()
    {
      if (_bassStream == null)
        return;
      _bassStream.Dispose();
      _bassStream = null;
    }

    #endregion

    #region Private members

    private BassCDTrackInputSource(char drive, uint trackNo)
    {
      _drive = drive;
      _trackNo = trackNo;
      _length = TimeSpan.FromSeconds(BassCd.BASS_CD_GetTrackLength(drive, BassTrackNo));
    }

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    private void Initialize()
    {
      Log.Debug("BassCDTrackInputSource.Initialize()");

      Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_CD_FREEOLD, false);

      const BASSFlag flags = BASSFlag.BASS_STREAM_DECODE | BASSFlag.BASS_SAMPLE_FLOAT;

      int bassDrive = BassUtils.Drive2BassID(_drive);
      int handle = BassCd.BASS_CD_StreamCreate(bassDrive, BassTrackNo, flags);

      if (handle == 0)
      {
        if (Bass.BASS_ErrorGetCode() == BASSError.BASS_ERROR_ALREADY)
          // Drive is already playing - stream must be lazily initialized
          return;
        throw new BassLibraryException("BASS_CD_StreamCreate");
      }
      _bassStream = BassStream.Create(handle);
    }

    #endregion

    public override string ToString()
    {
      return string.Format("{0}: Track {1} of drive {2}:", GetType().Name, _trackNo, _drive);
    }
  }
}