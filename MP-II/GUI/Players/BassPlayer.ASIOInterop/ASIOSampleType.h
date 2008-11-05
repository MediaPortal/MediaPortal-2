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
        public enum struct AsioSampleType : long
        {
          // See ASIO SDK for further info

          Int16MSB = ASIOSTInt16MSB,
          Int24MSB = ASIOSTInt24MSB,
          Int32MSB = ASIOSTInt32MSB,

          Float32MSB = ASIOSTFloat32MSB,
          Float64MSB = ASIOSTFloat64MSB,

          Int32MSB16 = ASIOSTInt32MSB16,
          Int32MSB18 = ASIOSTInt32MSB18,
          Int32MSB20 = ASIOSTInt32MSB20,
          Int32MSB24 = ASIOSTInt32MSB24,

          Int16LSB = ASIOSTInt16LSB,
          Int24LSB = ASIOSTInt24LSB,
          Int32LSB = ASIOSTInt32LSB,

          Float32LSB = ASIOSTFloat32LSB,
          Float64LSB = ASIOSTFloat64LSB,

          Int32LSB16 = ASIOSTInt32LSB16,
          Int32LSB18 = ASIOSTInt32LSB18,
          Int32LSB20 = ASIOSTInt32LSB20,
          Int32LSB24 = ASIOSTInt32LSB24,

          DSDInt8LSB1 = ASIOSTDSDInt8LSB1,
          DSDInt8MSB1 = ASIOSTDSDInt8MSB1,
          DSDInt8NER8 = ASIOSTDSDInt8NER8
        };
      };
    }
  }
}