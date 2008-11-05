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

#pragma once
#pragma managed
using namespace System;

namespace Media
{
  namespace Players
  {
    namespace BassPlayer
    {
      namespace ASIOInterop
      {
        // represents buffer size info specified by the driver
        public ref class BufferInfo
        {
        internal:

          // internal construction only
          BufferInfo(IAsio* pAsio);

          // these four things constitute a buffer size
          long m_nMinSize;
          long m_nMaxSize;
          long m_nPreferredSize;
          long m_nGranularity;

        public:

          // and this is where you can retrieve them
          property int MinSize { int get(); }
          property int MaxSize { int get(); }
          property int PreferredSize { int get(); }
          property int Granularity { int get(); }
        };
      }
    }
  }
}