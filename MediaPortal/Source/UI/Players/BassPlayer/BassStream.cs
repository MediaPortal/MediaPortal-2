#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using System.Collections.Generic;
using MediaPortal.Extensions.BassLibraries;
using MediaPortal.UI.Players.BassPlayer.Utils;
using Un4seen.Bass;

namespace MediaPortal.UI.Players.BassPlayer
{
  /// <summary>
  /// Represents a Bass stream.
  /// </summary>
  public class BassStream : IDisposable
  {
    #region Static members

    /// <summary>
    /// Creates and initializes an new instance.
    /// </summary>
    /// <param name="handle">Bass channel handle.</param>
    /// <returns>The new instance.</returns>
    public static BassStream Create(int handle)
    {
      BassStream stream = new BassStream(handle);
      stream.Initialize();
      return stream;
    }

    #endregion

    #region Fields

    private int _handle;
    private BASS_CHANNELINFO _info;
    private StreamContentType _streamContentType = StreamContentType.Unknown;
    private TimeSpan _length;
    private readonly IList<int> _syncProcHandles = new List<int>();

    #endregion

    #region Public members

    /// <summary>
    /// Gets the Bass handle for the stream.
    /// </summary>
    public int Handle
    {
      get { return _handle; }
    }

    /// <summary>
    /// Gets the number of channels for the stream.
    /// </summary>
    public int Channels
    {
      get { return _info.chans; }
    }

    /// <summary>
    /// Gets the samplerate for the stream in samples per second.
    /// </summary>
    public int SampleRate
    {
      get { return _info.freq; }
    }

    /// <summary>
    /// Gets whether the stream needs bitperfect passthrough.
    /// </summary>
    public bool IsPassThrough
    {
      get { return StreamContentType != StreamContentType.PCM && StreamContentType != StreamContentType.Unknown; }
    }

    /// <summary>
    /// Gets the underlaying BASS_CHANNELINFO object for the stream.
    /// </summary>
    public BASS_CHANNELINFO BassInfo
    {
      get { return _info; }
    }

    /// <summary>
    /// Gets the contenttype for the stream.
    /// </summary>
    public StreamContentType StreamContentType
    {
      get { return _streamContentType; }
    }

    /// <summary>
    /// Gets the total stream length.
    /// </summary>
    public TimeSpan Length
    {
      get { return _length; }
    }

    /// <summary>
    /// Gets or sets the volume (0-100).
    /// </summary>
    public float Volume
    {
      get { return GetVolume() * 100; }
      set { SetVolume(value / 100); }
    }

    /// <summary>
    /// Gets a list of SyncProcHandles. If a caller creates a sync procedure by <see cref="Bass.BASS_ChannelSetSync"/>, the returned handle must be added to this list.
    /// </summary>
    public IList<int> SyncProcHandles
    {
      get { return _syncProcHandles; }
    }

    /// <summary>
    /// Reads the specified number of samples from the stream.
    /// </summary>
    /// <param name="buffer">Buffer in which the read data is to be returned.</param>
    /// <returns></returns>
    public int Read(float[] buffer)
    {
      return Read(buffer, buffer.Length);
    }

    /// <summary>
    /// Reads the specified number of samples from the stream.
    /// </summary>
    /// <param name="buffer">Buffer to write the read data to.</param>
    /// <param name="length">Number of samples to read.</param>
    /// <returns></returns>
    public int Read(float[] buffer, int length)
    {
      int bytesRead = Bass.BASS_ChannelGetData(_handle, buffer, length * BassConstants.FloatBytes);

      if (bytesRead == -1)
      {
        CheckException("BASS_ChannelGetData");
        return -1;
      }
      return bytesRead / BassConstants.FloatBytes;
    }

    /// <summary>
    /// Reads the specified number of bytes from the stream.
    /// </summary>
    /// <param name="buffer">Buffer to write the read data to.</param>
    /// <param name="length">Number of bytes to read.</param>
    /// <returns>The number of bytes read or -1 when the end of the stream is reached.</returns>
    public int Read(IntPtr buffer, int length)
    {
      int bytesRead = Bass.BASS_ChannelGetData(_handle, buffer, length);

      if (bytesRead == -1)
      {
        CheckException("BASS_ChannelGetData");
        return -1;
      }
      return bytesRead;
    }

    /// <summary>
    /// Gets the current stream position measured in samples.
    /// </summary>
    /// <returns></returns>
    public long GetSamplePosition()
    {
      long bytes = Bass.BASS_ChannelGetPosition(Handle);

      if (bytes == -1)
      {
        CheckException("BASS_ChannelGetPosition");
        return -1;
      }

      return bytes / BassConstants.FloatBytes;
    }

    /// <summary>
    /// Sets the current stream position in samples.
    /// </summary>
    /// <param name="position">Number of samples to set the current position to.</param>
    public void SetSamplePosition(long position)
    {
      if (!Bass.BASS_ChannelSetPosition(Handle, position * BassConstants.FloatBytes))
        CheckException("BASS_ChannelSetPosition");
    }

    /// <summary>
    /// Gets the current stream position.
    /// </summary>
    /// <returns></returns>
    public TimeSpan GetPosition()
    {
      long bytes = Bass.BASS_ChannelGetPosition(Handle);

      if (bytes == -1)
      {
        CheckException("BASS_ChannelGetPosition");
        return TimeSpan.Zero;
      }

      double seconds = Bass.BASS_ChannelBytes2Seconds(Handle, bytes);
      return TimeSpan.FromSeconds(seconds);
    }

    /// <summary>
    /// Sets the current stream position.
    /// </summary>
    /// <param name="position"></param>
    public void SetPosition(TimeSpan position)
    {
      double seconds = position.TotalSeconds;

      if (!Bass.BASS_ChannelSetPosition(Handle, seconds))
        CheckException("BASS_ChannelSetPosition");
    }

    /// <summary>
    /// Positions the stream at the start of the next frame.
    /// </summary>
    /// <param name="stream"></param>
    /// <param name="streamContentType"></param>
    /// <returns></returns>
    public void SeekNextFrame(int stream, StreamContentType streamContentType)
    {
      SyncWord syncWord;
      switch (_streamContentType)
      {
        case StreamContentType.DD:
          syncWord = new DDSyncWord();
          break;

        case StreamContentType.DTS14Bit:
          syncWord = new DTS14bitSyncWord();
          break;

        case StreamContentType.DTS:
          syncWord = new DTSSyncWord();
          break;

        case StreamContentType.IEC61937:
          syncWord = new IECSyncWord();
          break;

        default:
          syncWord = null;
          break;
      }

      if (syncWord != null)
        SeekNextFrame(syncWord);
    }

    /// <summary>
    /// Updates locally cached fields.
    /// </summary>
    /// <remarks>
    /// Actually, this method could/should be private but in case the stream is a CD track stream and that track is switched
    /// from outside to another track, we need a means to update our internal data from outside.
    /// </remarks>
    public void UpdateLocalFields()
    {
      _length = GetLength();
      GetSampleLength();
    }

    #endregion

    #region Private members

    private BassStream(int handle)
    {
      _handle = handle;
    }

    private static void CheckException(string bassFunction)
    {
      // Ignore BASS_ERROR_ENDED and BASS_ERROR_HANDLE - can occur if stream ended or if the stream was disposed while still playing
      // Ignore BASS_ERROR_NOTAVAIL which happens for URL streams that don't offer length information
      BASSError error = Bass.BASS_ErrorGetCode();
      if (error != BASSError.BASS_ERROR_ENDED && error != BASSError.BASS_ERROR_HANDLE && error != BASSError.BASS_ERROR_NOTAVAIL)
        throw new BassLibraryException(bassFunction);
    }

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    private void Initialize()
    {
      _info = Bass.BASS_ChannelGetInfo(_handle);
      Log.Debug("Stream type: {0}", _info.ctype);

      if (_info.ctype != BASSChannelType.BASS_CTYPE_STREAM &&
          _info.ctype != BASSChannelType.BASS_CTYPE_STREAM_MIXER)
      {
        Log.Info("Stream info: {0}", _info.ToString());

        UpdateLocalFields();
        _streamContentType = GetStreamContentType();

        Log.Info("Stream content: {0}", _streamContentType);
      }
    }

    /// <summary>
    /// Gets the total stream length.
    /// </summary>
    /// <returns></returns>
    private TimeSpan GetLength()
    {
      long bytes = Bass.BASS_ChannelGetLength(Handle);

      if (bytes == -1)
        CheckException("BASS_ChannelGetLength");

      double seconds = Bass.BASS_ChannelBytes2Seconds(Handle, bytes);
      return TimeSpan.FromSeconds(seconds);
    }

    /// <summary>
    /// Gets the total stream length in samples.
    /// </summary>
    /// <returns></returns>
    private long GetSampleLength()
    {
      long bytes = Bass.BASS_ChannelGetLength(Handle);

      if (bytes == -1)
        CheckException("BASS_ChannelGetLength");

      return bytes / BassConstants.FloatBytes;
    }

    /// <summary>
    /// Gets the contenttype for the stream.
    /// </summary>
    /// <returns></returns>
    public StreamContentType GetStreamContentType()
    {
      StreamContentType result;
      if (Is14bitDTS())
        result = StreamContentType.DTS14Bit;
      else if (IsIEC())
        result = StreamContentType.IEC61937;
      else if (IsDD())
        result = StreamContentType.DD;
      else if (IsDTS())
        result = StreamContentType.DTS;
      else
        result = StreamContentType.PCM;

      return result;
    }

    /// <summary>
    /// Determines if the stream contains IEC 61937 data.
    /// </summary>
    /// <returns></returns>
    private bool IsIEC()
    {
      SyncWord syncWord = new IECSyncWord();
      return IsEncoded(syncWord);
    }

    /// <summary>
    /// Determines if the stream contains Dolby Digital data.
    /// </summary>
    /// <returns></returns>
    private bool IsDD()
    {
      SyncWord syncWord = new DDSyncWord();
      return IsEncoded(syncWord);
    }

    /// <summary>
    /// Determines if the stream contains DTS data in 14 bit format.
    /// </summary>
    /// <returns></returns>
    private bool Is14bitDTS()
    {
      SyncWord syncWord = new DTS14bitSyncWord();
      return IsEncoded(syncWord);
    }

    /// <summary>
    /// Determines if the stream contains DTS data in 16 bit format.
    /// </summary>
    /// <returns></returns>
    private bool IsDTS()
    {
      SyncWord syncWord = new DTSSyncWord();
      return IsEncoded(syncWord);
    }

    /// <summary>
    /// Determines if the stream contains frames with the specified syncwords. 
    /// </summary>
    /// <param name="syncWord">Syncword to search for.</param>
    /// <returns></returns>
    private bool IsEncoded(SyncWord syncWord)
    {
      const int framesToCheck = 5;
      const int bytesPerSample = 4;
      const int bytesPerWord = 2;
      const int channelCount = 2;

      long streamLength = Bass.BASS_ChannelGetLength(_handle);
      long currentPosition = 0;
      if (streamLength > 0)
      {
        currentPosition = Bass.BASS_ChannelGetPosition(_handle);
        if (currentPosition != 0)
          Bass.BASS_ChannelSetPosition(_handle, 0);
      }

      SyncFifoBuffer syncFifoBuffer = new SyncFifoBuffer(syncWord);
      float[] readBuffer = new float[channelCount];

      int lastSyncWordPosition = -1;
      int lastFrameSize = -1;
      int frameCount = 0;

      bool result = false;
      int sampleIndex = 0;
      int maxSampleIndex = (syncWord.MaxFrameSize / bytesPerWord) * framesToCheck + syncWord.WordLength;

      while (!result && sampleIndex < maxSampleIndex)
      {
        int bytesRead = Bass.BASS_ChannelGetData(_handle, readBuffer, readBuffer.Length * bytesPerSample);
        if (bytesRead <= 0)
          // End of stream
          break;
        int samplesRead = bytesRead / bytesPerSample;
        int readSample = 0;
        while (!result && readSample < samplesRead)
        {
          // Convert float value to word
          UInt16 word = (UInt16)(readBuffer[readSample] * 32768);

          // Add word to fifo buffer
          syncFifoBuffer.Write(word);

          // Check Sync word
          if (syncFifoBuffer.IsMatch())
          {
            int newSyncWordPosition = (sampleIndex - syncWord.WordLength + 1) * bytesPerWord;
            if (lastSyncWordPosition != -1)
            {
              int thisFrameSize = newSyncWordPosition - lastSyncWordPosition;
              if (lastFrameSize != -1)
              {
                if (thisFrameSize != lastFrameSize)
                  break;
              }
              lastFrameSize = thisFrameSize;
              frameCount++;
            }
            lastSyncWordPosition = newSyncWordPosition;
            result = (frameCount == framesToCheck);
          }
          sampleIndex++;
          readSample++;
        }
      }

      if (streamLength > 0)
        Bass.BASS_ChannelSetPosition(_handle, currentPosition);

      return result;
    }

    /// <summary>
    /// Positions the stream at the start of the next frame.
    /// </summary>
    /// <param name="syncWord"></param>
    /// <returns></returns>
    private void SeekNextFrame(SyncWord syncWord)
    {
      const int bytesPerSample = 4;
      const int bytesPerWord = 2;
      const int channelCount = 2;

      long streamLength = Bass.BASS_ChannelGetLength(_handle);
      long currentPosition = 0;
      if (streamLength > 0)
        currentPosition = Bass.BASS_ChannelGetPosition(_handle);

      SyncFifoBuffer syncFifoBuffer = new SyncFifoBuffer(syncWord);
      float[] readBuffer = new float[channelCount];

      bool success = false;
      int sampleIndex = 0;
      int maxSampleIndex = (syncWord.MaxFrameSize / bytesPerWord) + syncWord.WordLength;

      while (!success && sampleIndex < maxSampleIndex)
      {
        // For float streams we get one float value for each 16bit word
        int bytesRead = Bass.BASS_ChannelGetData(_handle, readBuffer, readBuffer.Length * bytesPerSample);
        if (bytesRead <= 0)
          // End of stream
          break;
        int samplesRead = bytesRead / bytesPerSample;
        int readSample = 0;
        while (!success && readSample < samplesRead)
        {
          // Convert float value to word
          UInt16 word = (UInt16)(readBuffer[readSample] * 32768);

          // Add word to fifo buffer
          syncFifoBuffer.Write(word);

          // Check Sync word
          if (syncFifoBuffer.IsMatch())
          {
            long pos = currentPosition + (sampleIndex - syncWord.WordLength + 1) * bytesPerWord;
            Bass.BASS_ChannelSetPosition(_handle, pos);
            success = true;
          }
          sampleIndex++;
          readSample++;
        }
      }

      if (!success && streamLength > 0)
        Bass.BASS_ChannelSetPosition(_handle, currentPosition);
    }

    /// <summary>
    /// Sets the volume attribute of the channel of this stream to the given value.
    /// </summary>
    /// <param name="volume">Volume to set. 0 = silent, 1 = full.</param>
    private void SetVolume(float volume)
    {
      if (!Bass.BASS_ChannelSetAttribute(_handle, BASSAttribute.BASS_ATTRIB_VOL, volume))
        CheckException("BASS_ChannelSetAttribute");

    }

    /// <summary>
    /// Gets the current stream volume.
    /// </summary>
    /// <returns>Volume of the channel of this stream. 0 = silent, 1 = full.</returns>
    private float GetVolume()
    {
      float value = 0.0f;
      if (!Bass.BASS_ChannelGetAttribute(_handle, BASSAttribute.BASS_ATTRIB_VOL, ref value))
        CheckException("BASS_ChannelGetAttribute");

      return value;
    }

    #endregion

    #region IDisposable Members

    public void Dispose()
    {
      if (_handle == 0)
        return;

      try
      {
        // Make sure handle never points to a non-existing handle (multithreading)
        int h = _handle;
        _handle = 0;

        // Free sync procs first
        foreach (int syncProcHandle in _syncProcHandles)
          Bass.BASS_ChannelRemoveSync(h, syncProcHandle);

        _syncProcHandles.Clear();

        // Ignore error
        Bass.BASS_StreamFree(h);
      }
      catch (Exception ex)
      {
        Log.Error("Exception disposing stream", ex);
      }
    }

    #endregion
  }
}
