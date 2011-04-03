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

#pragma once

#include <streams.h>
#include <evr.h>

#include "ComPtrList.h"

#ifdef EVRPRESENTER_EXPORTS
#define EVRPRESENTER_API __declspec(dllexport)
#else
#define EVRPRESENTER_API __declspec(dllimport)
#endif

// Writes a message to the log if HRESULT is < 0 and returns.
#ifndef CHECK_HR
#define CHECK_HR(hr, msg) if (FAILED(hr)) { Log(msg); return hr; }
#endif

// Releases a COM pointer if the pointer is not NULL, and sets the pointer.
#ifndef SAFE_RELEASE
#define SAFE_RELEASE(p) if (p) { (p)->Release(); (p) = NULL; }
#endif

// Deletes a pointer allocated with new.
#ifndef SAFE_DELETE
#define SAFE_DELETE(x) if (x) { delete x; x = NULL; }
#endif

// Returns the size of an array (on the stack only)
#ifndef ARRAY_SIZE
#define ARRAY_SIZE(x) (sizeof(x) / sizeof(x[0]) )
#endif

typedef ComPtrList<IMFSample> VideoSampleList;

// Default frame rate.
const MFRatio g_DefaultFrameRate = { 50000, 1000 };

// Assigns a COM pointer to another COM pointer.
template <class T>
void CopyComPointer(T* &dest, T *src)
{
  if (dest)
  {
    dest->Release();
  }
  dest = src;
  if (dest)
  {
    dest->AddRef();
  }
}


// Tests two COM pointers for equality.
template <class T1, class T2>
bool AreComObjectsEqual(T1 *p1, T2 *p2)
{
  bool bResult = false;
  if (p1 == NULL && p2 == NULL)
  {
    // Both are NULL
    bResult = true;
  }
  else if (p1 == NULL || p2 == NULL)
  {
    // One is NULL and one is not
    bResult = false;
  }
  else 
  {
    // Both are not NULL. Compare IUnknowns.
    IUnknown *pUnk1 = NULL;
    IUnknown *pUnk2 = NULL;
    if (SUCCEEDED(p1->QueryInterface(IID_IUnknown, (void**)&pUnk1)))
    {
      if (SUCCEEDED(p2->QueryInterface(IID_IUnknown, (void**)&pUnk2)))
      {
        bResult = (pUnk1 == pUnk2);
        pUnk2->Release();
      }
      pUnk1->Release();
    }
  }
 
  return bResult;
}

// Convert a fixed-point to a float.
inline float MFOffsetToFloat(const MFOffset& offset)
{
  return offset.value + (float(offset.fract) / 65536);
}

// MFSamplePresenter_SampleCounter
// Data type: UINT32
//
// Version number for the video samples. When the presenter increments the version number, 
// all samples with the previous version number are stale and should be discarded.
static const GUID MFSamplePresenter_SampleCounter = {0xb0bb83cc, 0xf10f, 0x4e2e, {0xaa, 0x2b, 0x29, 0xea, 0x5e, 0x92, 0xef, 0x85}};

// MFSamplePresenter_SampleSwapChain
// Data type: IUNKNOWN
// 
// Pointer to a Direct3D swap chain.
static const GUID MFSamplePresenter_SampleSwapChain = {0xad885bd1, 0x7def, 0x414a, {0xb5, 0xb0, 0xd3, 0xd2, 0x63, 0xd6, 0xe9, 0x6d}};

#pragma warning(disable: 4995)
// write message to EVR Log.
void Log(const char *fmt, ...);


