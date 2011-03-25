// Copyright (C) 2005-2010 Team MediaPortal
// http://www.team-mediaportal.com
// 
// MediaPortal is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 2 of the License, or
// (at your option) any later version.
// 
// MediaPortal is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with MediaPortal. If not, see <http://www.gnu.org/licenses/>.

#include "EVRCustomPresenter.h"

// IMFVideoPositionMapper Interface http://msdn.microsoft.com/en-us/library/ms695386(v=VS.85).aspx
// Maps coordinates on the output video frame to coordinates on the input video frame.

// Maps output image coordinates to input image coordinates.
HRESULT STDMETHODCALLTYPE EVRCustomPresenter::MapOutputCoordinateToInputStream(float xOut, float yOut, DWORD dwOutputStreamIndex, DWORD dwInputStreamIndex, float *pxIn, float *pyIn)
{
  return E_NOTIMPL;
}


