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
using System.Threading;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.UI.Players.BassPlayer.Settings;
using MediaPortal.UI.Players.BassPlayer.Utils;
using MediaPortal.UI.Presentation.Players;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Mix;

namespace MediaPortal.UI.Players.BassPlayer.PlayerComponents
{
  /// <summary>
  /// Buffers the output stream to ensure stable playback. Also provides a synchronized stream for visualization purposes.
  /// </summary>
  public class PlaybackBuffer : IDisposable
  {
    #region Fields

    private readonly Controller _controller;

    private int _vizReadOffsetBytes;

    private volatile bool _terminated = false;
    private volatile bool _inputStreamInitialized = false;
    private volatile bool _streamEnded = false; // Our AudioRingBuffer doesn't track our underlaying input stream's end mark, so we remember the stream end in this field

    private float[] _readData = new float[1];

    private readonly AutoResetEvent _updateThreadFinished;
    private readonly AutoResetEvent _notifyBufferUpdateThread;

    private BassStream _inputStream;
    private BassStream _outputStream;
    private BassStream _vizRawStream;
    private BassStream _vizStream;

    private readonly TimeSpan _bufferSize;
    private TimeSpan _vizReadOffset;

    private readonly STREAMPROC _streamWriteProcDelegate;
    private readonly STREAMPROC _vizRawStreamWriteProcDelegate;

    private AudioRingBuffer _buffer;
    private readonly Silence _silence;
    private Thread _bufferUpdateThread = null;

    #endregion

    public PlaybackBuffer(Controller controller)
    {
      _controller = controller;
      _silence = new Silence();

      BassPlayerSettings settings = Controller.GetSettings();

      _bufferSize = settings.PlaybackBufferSize;

      _streamWriteProcDelegate = OutputStreamWriteProc;
      _vizRawStreamWriteProcDelegate = VizRawStreamWriteProc;

      _notifyBufferUpdateThread = new AutoResetEvent(false);
      _updateThreadFinished = new AutoResetEvent(false);
    }

    #region IDisposable Members

    public void Dispose()
    {
      Log.Debug("PlaybackBuffer.Dispose()");

      ResetInputStream();

      _updateThreadFinished.Close();
      _notifyBufferUpdateThread.Close();
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
    /// Gets the visualization Bass stream.
    /// </summary>
    public BassStream VizStream
    {
      get { return _vizStream; }
    }

    /// <summary>
    /// Sets the Bass inputstream and initializes the playbackbuffer.
    /// </summary>
    /// <param name="stream">New inputstream.</param>
    public void SetInputStream(BassStream stream)
    {
      ResetInputStream();

      _inputStream = stream;

      UpdateVizLatencyCorrection();

      _buffer = new AudioRingBuffer(stream.SampleRate, stream.Channels, _bufferSize + _vizReadOffset);
      _streamEnded = false;
      _buffer.ResetPointers();

      CreateOutputStream();
      CreateVizStream();
      _inputStreamInitialized = true;

      // Ensure prebuffering
      _updateThreadFinished.Reset();

      StartBufferUpdateThread();
      _updateThreadFinished.WaitOne();
    }

    /// <summary>
    /// Resets and clears the buffer.
    /// </summary>
    public void ClearBuffer()
    {
      _buffer.Clear();
      _buffer.ResetPointers();
    }

    /// <summary>
    /// Resets the instance to its uninitialized state.
    /// </summary>
    public void ResetInputStream()
    {
      TerminateBufferUpdateThread();
      if (_inputStreamInitialized)
      {
        _inputStreamInitialized = false;

        _outputStream.Dispose();
        _outputStream = null;

        _vizStream.Dispose();
        _vizStream = null;

        _vizRawStream.Dispose();
        _vizRawStream = null;

        _buffer = null;
        _inputStream = null;
        _vizReadOffsetBytes = 0;
      }
    }

    #endregion

    #region Private members

    /// <summary>
    /// Reclculates the visualization stream byte-offset according usersettings.
    /// </summary>
    private void UpdateVizLatencyCorrection()
    {
      BassPlayerSettings settings = Controller.GetSettings();

      _vizReadOffset = InternalSettings.VizLatencyCorrectionRange.Add(settings.VizStreamLatencyCorrection);
      _vizReadOffsetBytes = AudioRingBuffer.CalculateLength(_inputStream.SampleRate, _inputStream.Channels, _vizReadOffset);

      Log.Debug("Vizstream reading offset: {0} ms", _vizReadOffset.TotalMilliseconds);
    }

    private void CreateOutputStream()
    {
      const BASSFlag flags = BASSFlag.BASS_SAMPLE_FLOAT | BASSFlag.BASS_STREAM_DECODE;

      int handle = Bass.BASS_StreamCreate(
          _inputStream.SampleRate,
          _inputStream.Channels,
          flags,
          _streamWriteProcDelegate,
          IntPtr.Zero);

      if (handle == BassConstants.BassInvalidHandle)
        throw new BassLibraryException("BASS_StreamCreate");

      _outputStream = BassStream.Create(handle);
    }

    /// <summary>
    /// Creates the visualization Bass stream.
    /// </summary>
    private void CreateVizStream()
    {
      BASSFlag streamFlags = BASSFlag.BASS_STREAM_DECODE | BASSFlag.BASS_SAMPLE_FLOAT;

      int handle = Bass.BASS_StreamCreate(
          _inputStream.SampleRate,
          _inputStream.Channels,
          streamFlags,
          _vizRawStreamWriteProcDelegate,
          IntPtr.Zero);

      if (handle == BassConstants.BassInvalidHandle)
        throw new BassLibraryException("BASS_StreamCreate");

      _vizRawStream = BassStream.Create(handle);

      // Todo: apply AGC

      streamFlags = BASSFlag.BASS_MIXER_NONSTOP | BASSFlag.BASS_SAMPLE_FLOAT | BASSFlag.BASS_STREAM_DECODE;

      handle = BassMix.BASS_Mixer_StreamCreate(_inputStream.SampleRate, 2, streamFlags);
      if (handle == BassConstants.BassInvalidHandle)
        throw new BassLibraryException("BASS_StreamCreate");

      _vizStream = BassStream.Create(handle);

      streamFlags = BASSFlag.BASS_MIXER_NORAMPIN | BASSFlag.BASS_MIXER_DOWNMIX | BASSFlag.BASS_MIXER_MATRIX;

      if (!BassMix.BASS_Mixer_StreamAddChannel(_vizStream.Handle, _vizRawStream.Handle, streamFlags))
        throw new BassLibraryException("BASS_Mixer_StreamAddChannel");

      // TODO Albert 2010-02-27: What is this?
      if (_inputStream.Channels == 1)
      {
        float[,] mixMatrix = new float[2, 1];
        mixMatrix[0, 0] = 1;
        mixMatrix[1, 0] = 1;

        if (!BassMix.BASS_Mixer_ChannelSetMatrix(_vizRawStream.Handle, mixMatrix))
          throw new BassLibraryException("BASS_Mixer_ChannelSetMatrix");
      }
    }

    /// <summary>
    /// Callback function for the output stream.
    /// </summary>
    /// <param name="streamHandle">Bass stream handle that requests sample data.</param>
    /// <param name="buffer">Buffer to write the sampledata in.</param>
    /// <param name="requestedBytes">Requested number of bytes.</param>
    /// <param name="userData">Not used.</param>
    /// <returns>Number of bytes read.</returns>
    private int OutputStreamWriteProc(int streamHandle, IntPtr buffer, int requestedBytes, IntPtr userData)
    {
      if (buffer == IntPtr.Zero)
        return 0;

      int read = _buffer.Read(buffer, requestedBytes / BassConstants.FloatBytes, 0);
      int result = (read == 0 && _streamEnded) ? (int) BASSStreamProc.BASS_STREAMPROC_END : read * BassConstants.FloatBytes;
      _notifyBufferUpdateThread.Set();
      return result;
    }

    /// <summary>
    /// Callback function for the visualization stream.
    /// </summary>
    /// <param name="streamHandle">Bass stream handle that requests sample data.</param>
    /// <param name="buffer">Buffer to write the sampledata to.</param>
    /// <param name="requestedBytes">Requested number of bytes.</param>
    /// <param name="userData">Not used.</param>
    /// <returns>Number of bytes read.</returns>
    private int VizRawStreamWriteProc(int streamHandle, IntPtr buffer, int requestedBytes, IntPtr userData)
    {
      if (buffer == IntPtr.Zero)
        return 0;

      if (_controller.Player.ExternalState == PlayerState.Active)
      {
        int read = _buffer.Peek(buffer, requestedBytes / BassConstants.FloatBytes, _vizReadOffsetBytes);
        return (read == 0 && _streamEnded) ? (int) BASSStreamProc.BASS_STREAMPROC_END : read * BassConstants.FloatBytes;
      }
      return _silence.Write(buffer, requestedBytes);
    }

    private void StartBufferUpdateThread()
    {
      Thread thread = _bufferUpdateThread;
      if (thread != null)
        return;
      _terminated = false;
      // Don't just use a background thread and forget about it - because we run in a plugin. Using a background thread and
      // not stopping it at the end of the plugin's activation time would prevent the plugin from being unloaded and,
      // even worse, the thread would be started again each time a new BassPlayer instance is created.
      // So we store a reference to the thread and dispose the thread when we don't need it any more.
      _bufferUpdateThread = new Thread(ThreadBufferUpdate) {Name = "PlaybackBuffer update thread"};
      _bufferUpdateThread.Start();
      _notifyBufferUpdateThread.Set();
    }

    private void TerminateBufferUpdateThread()
    {
      Thread thread = _bufferUpdateThread;
      if (thread == null)
        return;
      _bufferUpdateThread = null;
      // Set the terminate event before setting the notify event to ensure that the thread executor method
      // realizes the termination
      _terminated = true;
      _notifyBufferUpdateThread.Set(); // Awake thread
      thread.Join();
    }

    /// <summary>
    /// Bufferupdate thread executor method.
    /// </summary>
    private void ThreadBufferUpdate()
    {
      try
      {
        while (true)
        {
          _notifyBufferUpdateThread.WaitOne();

          // Test for _terminated after waiting for our notify event. The TerminateBufferUpdateThread() method
          // will first set the _terminated flag before it sets the notify event.
          if (_terminated)
            return;

          if (_inputStreamInitialized)
          {
            int requestedSamples = _buffer.Space;
            if (requestedSamples > 0)
            {
              if (_readData.Length < requestedSamples)
                Array.Resize(ref _readData, requestedSamples);

              int samplesRead = _inputStream.Read(_readData, requestedSamples);
              if (samplesRead > 0)
                _buffer.Write(_readData, samplesRead);
              else if (samplesRead == -1)
                _streamEnded = true;
            }
          }
          _updateThreadFinished.Set();
        }
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("Exception in bufferupdate thread", e);
      }
    }

    #endregion
  }
}
