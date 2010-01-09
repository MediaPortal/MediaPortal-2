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
using Un4seen.Bass;

namespace Media.Players.BassPlayer
{
  public partial class BassPlayer
  {
    /// <summary>
    /// Switches between inputsources and can perform crossfading and gapless playback.
    /// </summary>
    class InputSourceSwitcher : IDisposable
    {
      #region Static members

      /// <summary>
      /// Creates and initializes an new instance.
      /// </summary>
      /// <param name="player">Reference to containing IPlayer object.</param>
      /// <returns>The new instance.</returns>
      public static InputSourceSwitcher Create(BassPlayer player)
      {
        InputSourceSwitcher inputSourceSwitcher = new InputSourceSwitcher(player);
        inputSourceSwitcher.Initialize();
        return inputSourceSwitcher;
      }

      #endregion

      #region Fields

      private BassPlayer _Player;
      private BassStream _OutputStream;
      private IInputSource _CurrentInputSource;
      private STREAMPROC _StreamWriteProcDelegate;
      private bool _Initialized;

      #endregion

      #region IDisposable Members

      public void Dispose()
      {
        Log.Debug("InputSourceSwitcher.Dispose()");
        
        Reset();
      }

      #endregion

      #region Public members

      public IInputSource CurrentInputSource
      {
        get { return _CurrentInputSource; }
      }

      /// <summary>
      /// Gets the output Bass stream.
      /// </summary>
      public BassStream OutputStream
      {
        get { return _OutputStream; }
      }

      /// <summary>
      /// Initializes the inputsource switcher for a new playback session.
      /// </summary>
      public void InitToInputSource()
      {
        Log.Debug("InputSourceSwitcher.InitToInputSource()");

        Reset();

        _CurrentInputSource = _Player._InputSourceQueue.Dequeue();

        Log.Debug("Creating output stream");

        BASSFlag flags =
            BASSFlag.BASS_SAMPLE_FLOAT |
            BASSFlag.BASS_STREAM_DECODE;

        int handle = Bass.BASS_StreamCreate(
            _CurrentInputSource.OutputStream.SampleRate,
            _CurrentInputSource.OutputStream.Channels,
            flags,
            _StreamWriteProcDelegate,
            IntPtr.Zero);

        if (handle == BassConstants.BassInvalidHandle)
          throw new BassLibraryException("BASS_StreamCreate");

        _OutputStream = BassStream.Create(handle);
        
        _Initialized = true;
      }

      /// <summary>
      /// Resets the instance to its uninitialized state.
      /// </summary>
      public void Reset()
      {
        Log.Debug("InputSourceSwitcher.Reset()");

        if (_Initialized)
        {
          _Initialized = false;

          _OutputStream.Dispose();
          _OutputStream = null;
          
          _CurrentInputSource.Dispose();
          _CurrentInputSource = null;
        }
      }

      #endregion

      #region Private members

      private InputSourceSwitcher(BassPlayer player)
      {
        _Player = player;
      }

      /// <summary>
      /// Initializes a new instance.
      /// </summary>
      private void Initialize()
      {
        _StreamWriteProcDelegate = new STREAMPROC(OutputStreamWriteProc);
      }

      /// <summary>
      /// Callback function for the outputstream.
      /// </summary>
      /// <param name="streamHandle">Bass stream handle that requests sample data.</param>
      /// <param name="buffer">Buffer to write the sampledata in.</param>
      /// <param name="requestedBytes">Requested number of bytes.</param>
      /// <param name="userData"></param>
      /// <returns>Number of bytes read.</returns>
      private int OutputStreamWriteProc(int streamHandle, IntPtr buffer, int requestedBytes, IntPtr userData)
      {
        int read = _CurrentInputSource.OutputStream.Read(buffer, requestedBytes);
        
        // Todo: this must be done from a stream syncproc
        if (read <= 0)
          _Player._Controller.HandleNextMediaItemSyncPoint();
        
        return read;
      }

      #endregion
    }
  }
}