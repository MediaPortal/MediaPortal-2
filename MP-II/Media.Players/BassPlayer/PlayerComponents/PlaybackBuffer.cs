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
using System.Threading;
using Un4seen.Bass;

namespace Media.Players.BassPlayer
{
  public partial class BassPlayer
  {
    /// <summary>
    /// Buffers the outputstream to ensure stable playback. Also provides a synchronized stream for visualization purposes.
    /// </summary>
    class PlaybackBuffer : IDisposable
    {
      #region Static members

      /// <summary>
      /// Creates and initializes an new instance.
      /// </summary>
      /// <param name="player">Reference to containing IPlayer object.</param>
      /// <returns>The new instance.</returns>
      public static PlaybackBuffer Create(BassPlayer player)
      {
        PlaybackBuffer playbackBuffer = new PlaybackBuffer(player);
        playbackBuffer.Initialize();
        return playbackBuffer;
      }

      #endregion

      #region Fields

      private BassPlayer _Player;
      private BassStream _InputStream;
      private BassStream _OutputStream;
      private AudioRingBuffer _Buffer;
      private STREAMPROC _StreamWriteProcDelegate;
      private Thread _BufferUpdateThread;
      private ManualResetEvent _EnableBufferUpdateThread;
      private AutoResetEvent _NotifyBufferUpdateThread;
      private AutoResetEvent _BufferUpdated;
      private bool _BufferUpdateThreadAbortFlag = false;
      private TimeSpan _BufferSize;
      private int _BufferUpdateIntervalMS;
      private TimeSpan _ReadOffset;
      private int _ReadOffsetBytes;
      private float[] _ReadData = new float[1];
      private bool _Initialized;

      #endregion

      #region IDisposable Members

      public void Dispose()
      {
        ResetInputStream();
      }

      #endregion

      #region Public members

      /// <summary>
      /// Gets the current inputstream as set with SetInputStream.
      /// </summary>
      public BassStream InputStream
      {
        get { return _InputStream; }
      }

      /// <summary>
      /// Gets the output Bass stream.
      /// </summary>
      public BassStream OutputStream
      {
        get { return _OutputStream; }
      }

      /// <summary>
      /// Sets the Bass inputstream and initializes the playbackbuffer.
      /// </summary>
      /// <param name="stream"></param>
      public void SetInputStream(BassStream stream)
      {
        ResetInputStream();

        _InputStream = stream;
        _Buffer = new AudioRingBuffer(stream.SamplingRate, stream.Channels, _BufferSize + _ReadOffset);
        
        _ReadOffsetBytes = AudioRingBuffer.CalculateLength(stream.SamplingRate, stream.Channels, _ReadOffset);
        _Buffer.ResetPointers(_ReadOffsetBytes);

        BASSFlag flags =
          BASSFlag.BASS_SAMPLE_FLOAT |
          BASSFlag.BASS_STREAM_DECODE;

        int handle = Bass.BASS_StreamCreate(
            _InputStream.SamplingRate,
            _InputStream.Channels,
            flags,
            _StreamWriteProcDelegate,
            IntPtr.Zero);

        if (handle == Constants.BassInvalidHandle)
          throw new BassLibraryException("BASS_StreamCreate");

        _OutputStream = BassStream.Create(handle);

        _Initialized = true;

        // Unblock buffeer update thread.
        _EnableBufferUpdateThread.Set();

        // Ensure prebuffering
        _BufferUpdated.Reset();
        _NotifyBufferUpdateThread.Set();
        _BufferUpdated.WaitOne();
      }

      /// <summary>
      /// Resets and clears the buffer.
      /// </summary>
      public void ClearBuffer()
      {
        _Buffer.Clear();
        _Buffer.ResetPointers(_ReadOffsetBytes);
      }

      /// <summary>
      /// Resets the instance to its uninitialized state.
      /// </summary>
      public void ResetInputStream()
      {
        if (_Initialized)
        {
          // Stall the bufferupdate thread.
          // Set _Initialized = false first so ThreadBufferUpdate() can test for it.
          _Initialized = false;
          _EnableBufferUpdateThread.Reset();

          _OutputStream.Dispose();
          _OutputStream = null;
          
          _Buffer = null;
          
          _InputStream = null;
        }
      }

      #endregion

      #region Private members

      private PlaybackBuffer(BassPlayer player)
      {
        _Player = player;
      }

      /// <summary>
      /// Initializes a new instance.
      /// </summary>
      private void Initialize()
      {
        _BufferSize = _Player.Settings.PlaybackBufferSize;
        _BufferUpdateIntervalMS = (int)_BufferSize.TotalMilliseconds / 5;

        _ReadOffset = StaticSettings.VizLatencyCorrectionRange;

        _StreamWriteProcDelegate = new STREAMPROC(OutputStreamWriteProc);
        _NotifyBufferUpdateThread = new AutoResetEvent(false);
        _EnableBufferUpdateThread = new ManualResetEvent(false);
        _BufferUpdated = new AutoResetEvent(false);

        _BufferUpdateThread = new Thread(new ThreadStart(ThreadBufferUpdate));
        _BufferUpdateThread.IsBackground = true;
        _BufferUpdateThread.Start();
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
        int read = _Buffer.Read(buffer, requestedBytes / Constants.FloatBytes, _ReadOffsetBytes);
        return read * Constants.FloatBytes;
      }

      /// <summary>
      /// Bufferupdate thread loop.
      /// </summary>
      private void ThreadBufferUpdate()
      {
        try
        {
          while (!_BufferUpdateThreadAbortFlag)
          {
            _EnableBufferUpdateThread.WaitOne();
            _NotifyBufferUpdateThread.WaitOne(_BufferUpdateIntervalMS, false);

            // Test for _Initialized because _InputStream and _Buffer may get released.
            if (_Initialized)
            {
              int requestedSamples = _Buffer.Space;
              if (requestedSamples > 0)
              {
                if (_ReadData.Length < requestedSamples)
                  Array.Resize<float>(ref _ReadData, requestedSamples);

                int samplesRead = _InputStream.Read(_ReadData, requestedSamples);
                _Buffer.Write(_ReadData, samplesRead);
              }
            }
            _BufferUpdated.Set();
          }
        }
        catch (Exception e)
        {
          throw new BassPlayerException("Exception in bufferupdate thread.", e);
        }
      }

      #endregion
    }
  }
}