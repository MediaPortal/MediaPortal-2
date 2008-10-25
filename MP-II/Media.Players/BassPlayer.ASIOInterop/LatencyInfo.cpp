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

#include "LatencyInfo.h"

namespace Media
{
  namespace Players
  {
    namespace BassPlayer
    {
      namespace ASIOInterop
      {
        LatencyInfo::LatencyInfo(IAsio* pAsio)
        {
          long p1, p2;

          // ask the driver for the two bits of info
          pAsio->getLatencies(&p1, &p2);

          // and set them
          _InputLatency = p1;
          _OutputLatency = p2;
        }

        int LatencyInfo::InputLatency::get()
        {
          return _InputLatency;
        }

        int LatencyInfo::OutputLatency::get()
        {
          return _OutputLatency;
        }
      }
    }
  }
}