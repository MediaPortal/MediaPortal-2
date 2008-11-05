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

#include "AsioRedirect.h"
#include "BufferInt16LSB.h"

namespace Media
{
  namespace Players
  {
    namespace BassPlayer
    {
      namespace ASIOInterop
      {
        BufferInt16LSB::BufferInt16LSB(void* pTheirBuffer0, void* pTheirBuffer1)
        {
          // remember the two buffers (one plays the other updates)
          _pTheirBuffer0 = (WORD*)pTheirBuffer0;
          _pTheirBuffer1 = (WORD*)pTheirBuffer1;
        }

        void BufferInt16LSB::SetDoubleBufferIndex(long doubleBufferIndex)
        {
          // set what buffer should be affected by our indexer

          if (doubleBufferIndex == 0)
            _pTheirCurrentBuffer = _pTheirBuffer0;
          else
            _pTheirCurrentBuffer = _pTheirBuffer1;
        }

        void BufferInt16LSB::default::set(int sample, float value)
        {
          // set the value of a sample

          // convert float between -1.0 and 1.0 to signed integer
          _pTheirCurrentBuffer[sample] = (WORD)(value * 32768.0f);
        }

        float BufferInt16LSB::default::get(int sample)
        {
          // get the value of a sample

          // convert signed integer to float between -1.0 and 1.0
          return (float)(_pTheirCurrentBuffer[sample] / 32768.0f);
        }
      }
    }
  }
}