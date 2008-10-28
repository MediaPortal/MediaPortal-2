#pragma region Copyright (C) 2007-2008 Team MediaPortal

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

#pragma endregion

//-----------------------------------------------------------------------------------
// Based on code originally written by Rob Philpott and published on CodeProject.com:
//   http://www.codeproject.com/KB/audio-video/Asio_Net.aspx
//
// ASIO is a trademark and software of Steinberg Media Technologies GmbH
//-----------------------------------------------------------------------------------

#include "BufferInt24LSB.h"

namespace Media
{
  namespace Players
  {
    namespace BassPlayer
    {
      namespace ASIOInterop
      {
        BufferInt24LSB::BufferInt24LSB(void* pTheirBuffer0, void* pTheirBuffer1) : BufferInt24LSB::ChannelBuffer()
        {
          // remember the two buffers (one plays the other updates)
          _pTheirBuffer0 = (BYTE*)pTheirBuffer0;
          _pTheirBuffer1 = (BYTE*)pTheirBuffer1;

          // byte structure used while handling sample data
          _Int32Bytes = gcnew array<Byte>(4);
        }

        void BufferInt24LSB::SetDoubleBufferIndex(long doubleBufferIndex)
        {
          // set what buffer should be affected by our indexer

          if (doubleBufferIndex == 0)
            _pTheirCurrentBuffer = _pTheirBuffer0;
          else
            _pTheirCurrentBuffer = _pTheirBuffer1;
        }

        void BufferInt24LSB::default::set(int sample, float value)
        {
          // set the value of a sample

          // Calculate write position for byte buffer
          // We're using a BYTE buffer; each sample occupies 3 bytes 
          int bufferIndex = sample * 3;

          // Convert float value to 4 byte signed integer
          int intValue = (int)(value * 2147483648.0f);

          // Split the integer into its 4 bytes
          // Todo: memleak. All this stuff ends up un the heap
          array<Byte>^ bytes = BitConverter::GetBytes(intValue);

          // Write 3 MSB to little-endian buffer
          if (BitConverter::IsLittleEndian)
          {
            // Discard byte 0, write bytes 1, 2, 3
            for (int i = 0; i < 3; i++)
              _pTheirCurrentBuffer[bufferIndex + i] = bytes[i + 1];
          }
          else
          {
            // Discard byte 3, write bytes 2, 1, 0
            for (int i = 0; i < 3; i++)
              _pTheirCurrentBuffer[bufferIndex + i] = bytes[2-i];
          }
        }

        float BufferInt24LSB::default::get(int sample)
        {
          // get the value of a sample

          // Calculate write position for byte buffer
          // We're using a BYTE buffer; each sample occupies 3 bytes 
          int bufferIndex = sample * 3;

          // Create an int32 byte structure
          array<Byte>^ bytes = gcnew array<Byte>(4);
          if (BitConverter::IsLittleEndian)
          {
            _Int32Bytes[0] = 0;
            for (int i = 0; i< 3; i++)
              _Int32Bytes[i + 1] = _pTheirCurrentBuffer[bufferIndex + i];
          }
          else
          {
            _Int32Bytes[3] = 0;
            for (int i = 0; i < 3; i++)
              _Int32Bytes[2 - i] = _pTheirCurrentBuffer[bufferIndex + i];
          }

          // Create a signed integer from byte structure
          int intValue = BitConverter::ToInt32(_Int32Bytes,0);

          // Convert signed integer to float between -1.0 and 1.0
          return (float)(intValue / 2147483648.0f);
        }
      }
    }
  }
}