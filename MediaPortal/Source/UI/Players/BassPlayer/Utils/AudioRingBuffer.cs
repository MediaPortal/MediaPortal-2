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
using System.Runtime.InteropServices;
using MediaPortal.Extensions.BassLibraries;

namespace MediaPortal.UI.Players.BassPlayer.Utils
{
  /// <summary>
  /// Fifo ringbuffer for audio data.
  /// </summary>
  internal class AudioRingBuffer
  {
    private readonly Object _syncObj = new Object();

    private readonly int _delay;

    private readonly float[] _buffer;
    private readonly int _bytesPerMilliSec;
    private readonly bool _is32Bit = false;
    private int _writePointer;
    private int _readPointer;
    private int _space;

    /// <summary>
    /// Returns the current delay in milliseconds (delay for the actual filled part of the buffer).
    /// </summary>
    public TimeSpan Delay
    {
      get { return TimeSpan.FromMilliseconds(_delay - ((_space / _buffer.Length) * _delay)); }
    }

    /// <summary>
    /// Returns the length of the buffer in samples.
    /// </summary>
    public int Length
    {
      get { return _buffer.Length; }
    }

    /// <summary>
    /// Returns the number of bytes per milisecond.
    /// </summary>
    public int BytesPerMilliSec
    {
      get { return _bytesPerMilliSec; }
    }

    /// <summary>
    /// Returns the amount of samples available for writing
    /// </summary>
    public int Space
    {
      get { return _space; }
    }

    /// <summary>
    /// Returns the amount of samples available for reading
    /// </summary>
    public int Count
    {
      get { return _buffer.Length - _space; }
    }

    public AudioRingBuffer(int sampleRate, int channels, TimeSpan delay)
    {
      _is32Bit = (IntPtr.Size == 4);
      _delay = (int)delay.TotalMilliseconds;
      int bufferLength = CalculateLength(sampleRate, channels, delay);
      _bytesPerMilliSec = CalculateLength(sampleRate, channels, TimeSpan.FromMilliseconds(1));
      _buffer = new float[bufferLength];

      ResetPointers();
    }

    /// <summary>
    /// Calculates the number of samples required to store audio data .
    /// </summary>
    /// <param name="sampleRate"></param>
    /// <param name="channels"></param>
    /// <param name="delay"></param>
    /// <returns></returns>
    public static int CalculateLength(int sampleRate, int channels, TimeSpan delay)
    {
      int sampleSize = channels;

      // Use the (long) cast to avoid arithmetic overflow 
      int length = (int)((long)sampleRate * sampleSize * delay.TotalMilliseconds / 1000);

      // round to whole samples
      length = length / sampleSize;
      length = length * sampleSize;

      return length;
    }

    // Write:
    // - Buffer         0123456789  0123456789  0123456789
    // - Write pointer  >>>   |>>>     |>>>     >>>>>|>>>>
    // - Read pointer      |               |         |

    // Read:
    // - Buffer         0123456789  0123456789  0123456789
    // - Write pointer        |         |          |
    // - Read pointer      |>>      >>>>|>>>>>  >>>   |>>>

    /// <summary>
    /// Writes a number of samples to the buffer.
    /// </summary>
    /// <param name="data"></param>
    /// <param name="count"></param>
    /// <returns></returns>
    public int Write(float[] data, int count)
    {
      // Validate parameters, may be left out for performance
      //if (count > data.Length)
      //   count = data.Length;

      int readPointer;
      int writePointer;

      // To minimize locking contention, reserve the space we are going to fill 
      // before we actually fill it.
      lock (_syncObj)
      {
        readPointer = _readPointer;
        writePointer = _writePointer;
        if (count > _space)
          count = _space;

        _space -= count;
        _writePointer = writePointer + count;

        if (_writePointer > _buffer.Length)
          _writePointer = _writePointer - _buffer.Length;
      }

      if (writePointer >= readPointer)
      {
        int count1 = Math.Min(count, _buffer.Length - writePointer);
        Array.Copy(data, 0, _buffer, writePointer, count1);

        int count2 = Math.Min(count - count1, readPointer);
        if (count2 > 0)
          Array.Copy(data, count1, _buffer, 0, count2);

        return count1 + count2;
      }
      else
      {
        int count1 = Math.Min(count, readPointer - writePointer);
        Array.Copy(data, 0, _buffer, writePointer, count1);

        return count1;
      }
    }

    /// <summary>
    /// Reads a number of samples without moving the readpointer.
    /// </summary>
    /// <param name="buffer">The buffer to write the data to.</param>
    /// <param name="requested">Number of requested number of samples.</param>
    /// <param name="offset">Offset to move the ring buffer read pointer before reading data.</param>
    /// <returns>Number of bytes read.</returns>
    public int Peek(IntPtr buffer, int requested, int offset)
    {
      if (buffer == IntPtr.Zero)
        return 0;

      // Same code as method Read, only without the last update of _readPointer and _space
      int read;
      int readPointer;
      int writePointer;
      int space;

      lock (_syncObj)
      {
        readPointer = _readPointer;
        writePointer = _writePointer;
        space = _space;
      }

      offset = Math.Min(offset, _buffer.Length - space);

      readPointer += offset;
      if (readPointer > _buffer.Length)
        readPointer -= _buffer.Length;

      requested = Math.Min(requested, _buffer.Length - space - offset);

      if (writePointer > readPointer)
      {
        int count1 = Math.Min(requested, writePointer - readPointer);

        Marshal.Copy(_buffer, readPointer, buffer, count1);
        //readPointer += count1;

        read = count1;
      }
      else
      {
        int count1 = Math.Min(requested, _buffer.Length - readPointer);

        if (count1 > 0)
        {
          Marshal.Copy(_buffer, readPointer, buffer, count1);
          //readPointer += count1;
          //if (readPointer == _buffer.Length)
          //  readPointer = 0;
        }

        int count2 = Math.Min(requested - count1, writePointer);
        if (count2 > 0)
        {
          IntPtr ptr = new IntPtr((_is32Bit ? buffer.ToInt32() : buffer.ToInt64()) + (count1 * BassConstants.FloatBytes));

          Marshal.Copy(_buffer, 0, ptr, count2);
          //readPointer = count2;
        }
        else
          count2 = 0;
        read = count1 + count2;
      }
      return read;
    }

    /// <summary>
    /// Reads a number of samples.
    /// </summary>
    /// <param name="buffer">The buffer to write the data to.</param>
    /// <param name="requested">Number of requested number of samples.</param>
    /// <param name="offset">Offset to move the ring buffer read pointer before reading data.</param>
    /// <returns>Number of samples read.</returns>
    public int Read(IntPtr buffer, int requested, int offset)
    {
      int read;
      int readPointer;
      int writePointer;
      int space;

      lock (_syncObj)
      {
        readPointer = _readPointer;
        writePointer = _writePointer;
        space = _space;
      }

      offset = Math.Min(offset, _buffer.Length - space);

      readPointer += offset;
      if (readPointer > _buffer.Length)
        readPointer -= _buffer.Length;

      requested = Math.Min(requested, _buffer.Length - space - offset);

      if (writePointer > readPointer)
      {
        int count1 = Math.Min(requested, writePointer - readPointer);

        Marshal.Copy(_buffer, readPointer, buffer, count1);
        readPointer += count1;

        read = count1;
      }
      else
      {
        int count1 = Math.Min(requested, _buffer.Length - readPointer);

        if (count1 > 0)
        {
          Marshal.Copy(_buffer, readPointer, buffer, count1);
          readPointer += count1;
          if (readPointer == _buffer.Length)
            readPointer = 0;
        }

        int count2 = Math.Min(requested - count1, writePointer);
        if (count2 > 0)
        {
          IntPtr ptr = new IntPtr((_is32Bit ? buffer.ToInt32() : buffer.ToInt64()) + (count1*BassConstants.FloatBytes));
          Marshal.Copy(_buffer, 0, ptr, count2);
          readPointer = count2;
        }
        else
          count2 = 0;
        read = count1 + count2;
      }

      readPointer = readPointer - offset;
      if (readPointer < 0)
        readPointer += _buffer.Length;

      lock (_syncObj)
      {
        _readPointer = readPointer;
        _space += read;
      }
      return read;
    }

    /// <summary>
    /// Clears the buffer. Reset all pointers and fils the entire buffer with silence.
    /// </summary>
    public void Clear()
    {
      _buffer.Initialize();
      ResetPointers();
    }

    /// <summary>
    /// Resets the readpointer and sets the writepointer at the specified position.
    /// </summary>
    public void ResetPointers()
    {
      _writePointer = 0;
      _readPointer = _buffer.Length;
      _space = _buffer.Length;
    }
  }
}
