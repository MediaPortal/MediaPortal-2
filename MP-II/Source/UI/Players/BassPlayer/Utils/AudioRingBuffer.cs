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

using System;
using System.Runtime.InteropServices;

namespace Ui.Players.BassPlayer.Utils
{
  /// <summary>
  /// Fifo ringbuffer for audio data.
  /// </summary>
  internal class AudioRingBuffer
  {
    private readonly Object _syncObj = new Object();

    private readonly int _delay;

    private readonly float[] _buffer;
    private readonly int _bufferLength;
    private readonly int _bytesPerMilliSec;
    private readonly bool _Is32bit = false;
    private int _writePointer;
    private int _readPointer;
    private int _space;

    /// <summary>
    /// Returns the current delay in milliseconds (delay for the actual filled part of the buffer).
    /// </summary>
    public TimeSpan Delay
    {
      get { return TimeSpan.FromMilliseconds(_delay - ((_space / _bufferLength) * _delay)); }
    }

    /// <summary>
    /// Returns the length of the buffer in samples.
    /// </summary>
    public int Length
    {
      get { return _bufferLength; }
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
      get { return _bufferLength - _space; }
    }

    public AudioRingBuffer(int sampleRate, int channels, TimeSpan delay)
    {
      _Is32bit = (IntPtr.Size == 4);
      _delay = (int)delay.TotalMilliseconds;
      _bufferLength = CalculateLength(sampleRate, channels, delay);
      _bytesPerMilliSec = CalculateLength(sampleRate, channels, TimeSpan.FromMilliseconds(1));
      _buffer = new float[_bufferLength];

      ResetPointers(0);
    }

    /// <summary>
    /// Calculates the number of samples required to store audiodata 
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
        int space = _space;

        if (count > space)
          count = space;

        _space = _space - count;
        _writePointer = writePointer + count;

        if (_writePointer > _bufferLength)
          _writePointer = _writePointer - _bufferLength;
      }

      if (writePointer >= readPointer)
      {
        int count1 = Math.Min(count, _bufferLength - writePointer);
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
    /// <param name="buffer"></param>
    /// <param name="requested"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    public int Peek(IntPtr buffer, int requested, int offset)
    {
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

      offset = Math.Min(offset, _bufferLength - space);

      readPointer += offset;
      if (readPointer > _bufferLength)
        readPointer -= _bufferLength;

      requested = Math.Min(requested, _bufferLength - space - offset);

      if (writePointer > readPointer)
      {
        int count1 = Math.Min(requested, writePointer - readPointer);

        Marshal.Copy(_buffer, readPointer, buffer, count1);
        readPointer += count1;

        read = count1;
      }
      else
      {
        int count1 = Math.Min(requested, _bufferLength - readPointer);

        Marshal.Copy(_buffer, readPointer, buffer, count1);
        readPointer += count1;

        if (readPointer == _buffer.Length)
          readPointer = 0;

        int count2 = Math.Min(requested - count1, writePointer);
        if (count2 > 0)
        {
          IntPtr ptr = new IntPtr(_Is32bit ? buffer.ToInt32() : buffer.ToInt64() + (count1 * BassConstants.FloatBytes));

          Marshal.Copy(_buffer, 0, ptr, count2);
          readPointer = count2;
        }
        read = count1 + count2;
      }
      return read;
    }

    /// <summary>
    /// Reads a number of samples.
    /// </summary>
    /// <param name="buffer"></param>
    /// <param name="requested"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
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

      offset = Math.Min(offset, _bufferLength - space);

      readPointer += offset;
      if (readPointer > _bufferLength)
        readPointer -= _bufferLength;

      requested = Math.Min(requested, _bufferLength - space - offset);

      if (writePointer > readPointer)
      {
        int count1 = Math.Min(requested, writePointer - readPointer);

        if (buffer != IntPtr.Zero)
          Marshal.Copy(_buffer, readPointer, buffer, count1);
        readPointer += count1;

        read = count1;
      }
      else
      {
        int count1 = Math.Min(requested, _bufferLength - readPointer);

        if (buffer != IntPtr.Zero)
          Marshal.Copy(_buffer, readPointer, buffer, count1);
        readPointer += count1;

        if (readPointer == _buffer.Length)
          readPointer = 0;

        int count2 = Math.Min(requested - count1, writePointer);
        if (count2 > 0)
        {
          if (buffer != IntPtr.Zero)
          {
            IntPtr ptr = new IntPtr(_Is32bit ? buffer.ToInt32() : buffer.ToInt64() + (count1 * BassConstants.FloatBytes));
            Marshal.Copy(_buffer, 0, ptr, count2);
          }
          readPointer = count2;
        }
        read = count1 + count2;
      }

      readPointer = readPointer - offset;
      if (readPointer < 0)
        readPointer += _bufferLength;

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
      ResetPointers(0);
    }

    /// <summary>
    /// Resets the readpointer and sets the writepointer at the specified position.
    /// </summary>
    /// <param name="initialPosition">The position to set toe writepointer to.</param>
    public void ResetPointers(int initialPosition)
    {
      _writePointer = initialPosition;
      _readPointer = 0;
      _space = _bufferLength - initialPosition;
    }
  }
}