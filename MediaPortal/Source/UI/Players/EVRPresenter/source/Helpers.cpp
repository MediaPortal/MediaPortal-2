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

#include <float.h>

#include "EVRCustomPresenter.h"
#include "MediaType.h"

// Send an event to the EVR through its IMediaEventSink interface.
void EVRCustomPresenter::NotifyEvent(long EventCode, LONG_PTR Param1, LONG_PTR Param2)
{
  if (m_pMediaEventSink)
  {
    m_pMediaEventSink->Notify(EventCode, Param1, Param2);
  }
}


// Returns the maximum forward playback rate. 
float EVRCustomPresenter::GetMaxRate(BOOL bThin)
{
  // Thinned: The maximum rate is unbounded.
  float   fMaxRate = FLT_MAX;
  MFRatio fps = { 0, 0 };
  UINT    MonitorRateHz = 0; 

  // Non-Thinned: maximum playback rate is equal to the refresh rate
  if (!bThin && (m_pMediaType != NULL))
  {
    VideoType videoType;
    videoType.GetFrameRate(m_pMediaType, &fps);
    // TODO: Replace by CCD method to get refresh rate from driver
    MonitorRateHz = m_pD3DPresentEngine->RefreshRate();

    if (fps.Denominator && fps.Numerator && MonitorRateHz)
    {
      // Max Rate = Refresh Rate / Frame Rate
      fMaxRate = (float)MulDiv(MonitorRateHz, fps.Denominator, fps.Numerator);
    }
  }

  return fMaxRate;
}


// Tests whether two IMFMediaType's are equal. Either pointer can be NULL.
BOOL EVRCustomPresenter::AreMediaTypesEqual(IMFMediaType *pType1, IMFMediaType *pType2)
{
  if ((pType1 == NULL) && (pType2 == NULL))
  {
    return TRUE; // Both are NULL.
  }
  else if ((pType1 == NULL) || (pType2 == NULL))
  {
    return FALSE; // One is NULL.
  }

  DWORD dwFlags = 0;
  HRESULT hr = pType1->IsEqual(pType2, &dwFlags);

  return (hr == S_OK);
}


// Returns S_OK if an area is smaller than width x height. Otherwise, returns MF_E_INVALIDMEDIATYPE.
HRESULT EVRCustomPresenter::ValidateVideoArea(const MFVideoArea& area, UINT32 width, UINT32 height)
{
  float fOffsetX = MFOffsetToFloat(area.OffsetX);
  float fOffsetY = MFOffsetToFloat(area.OffsetY);

  if (((LONG)fOffsetX + area.Area.cx > (LONG)width) || ((LONG)fOffsetY + area.Area.cy > (LONG)height))
  {
    return MF_E_INVALIDMEDIATYPE;
  }
  else
  {
    return S_OK;
  }
}

// Converts a rectangle from one pixel aspect ratio (PAR) to another PAR.
RECT EVRCustomPresenter::CorrectAspectRatio(const RECT& src, const MFRatio& srcPAR, const MFRatio& destPAR)
{
  // Start with a rectangle the same size as src, but offset to the origin (0,0).
  RECT rc = {0, 0, src.right - src.left, src.bottom - src.top};

  // If the source and destination have the same PAR, there is nothing to do.
  // Otherwise, adjust the image size, in two steps:
  //  1. Transform from source PAR to 1:1
  //  2. Transform from 1:1 to destination PAR.

  if ((srcPAR.Numerator != destPAR.Numerator) || (srcPAR.Denominator != destPAR.Denominator))
  {
    // Correct for the source's PAR.

    if (srcPAR.Numerator > srcPAR.Denominator)
    {
      // The source has "wide" pixels, so stretch the width.
      rc.right = MulDiv(rc.right, srcPAR.Numerator, srcPAR.Denominator);
    }
    else if (srcPAR.Numerator < srcPAR.Denominator)
    {
      // The source has "tall" pixels, so stretch the height.
      rc.bottom = MulDiv(rc.bottom, srcPAR.Denominator, srcPAR.Numerator);
    }
    // else: PAR is 1:1, which is a no-op.


    // Next, correct for the target's PAR. This is the inverse operation of the previous.

    if (destPAR.Numerator > destPAR.Denominator)
    {
      // The destination has "wide" pixels, so stretch the height.
      rc.bottom = MulDiv(rc.bottom, destPAR.Numerator, destPAR.Denominator);
    }
    else if (destPAR.Numerator < destPAR.Denominator)
    {
      // The destination has "tall" pixels, so stretch the width.
      rc.right = MulDiv(rc.right, destPAR.Denominator, destPAR.Numerator);
    }
    // else: PAR is 1:1, which is a no-op.
  }

  return rc;
}

