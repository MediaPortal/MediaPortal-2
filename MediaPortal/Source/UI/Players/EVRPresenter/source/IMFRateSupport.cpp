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

#include <math.h>

#include "EVRCustomPresenter.h"

// IMFRateSupport Interface http://msdn.microsoft.com/en-us/library/ms701858(v=VS.85).aspx
// Reports the range of playback rates that the presenter supports.

// Gets the fastest playback rate supported by the object.
HRESULT STDMETHODCALLTYPE EVRCustomPresenter::GetFastestRate(MFRATE_DIRECTION eDirection, BOOL bThin, float *pfRate)
{
  HRESULT hr = S_OK;
  float   fMaxRate = 0.0f;

  CAutoLock lock(this);

  // We cannot get the fastest rate after shutdown.
  hr = CheckShutdown();
  CHECK_HR(hr, "EVRCustomPresenter::GetFastestRate cannot get fastest rate after shutdown");

  CheckPointer(pfRate, E_POINTER);

  // Get the maximum forward rate.
  fMaxRate = GetMaxRate(bThin);

  // For reverse playback, it's the negative of fMaxRate.
  if (eDirection == MFRATE_REVERSE)
  {
    fMaxRate = -fMaxRate;
  }

  *pfRate = fMaxRate;

  return hr;
}


// Gets the slowest playback rate supported by the object.
HRESULT STDMETHODCALLTYPE EVRCustomPresenter::GetSlowestRate(MFRATE_DIRECTION eDirection, BOOL bThin, float *pfRate)
{
  HRESULT hr = S_OK;

  CAutoLock lock(this);

  // We cannot get the slowest rate after shutdown.
  hr = CheckShutdown();
  CHECK_HR(hr, "EVRCustomPresenter::GetSlowestRate cannot get slowest rate after shutdown");

  CheckPointer(pfRate, E_POINTER);

  // There is no minimum playback rate, so the minimum is zero.
  *pfRate = 0; 

  return S_OK;
}


// Queries whether the object supports a specified playback rate.
HRESULT STDMETHODCALLTYPE EVRCustomPresenter::IsRateSupported(BOOL bThin, float fRate, float *pfNearestSupportedRate)
{
  HRESULT hr = S_OK;
  float   fMaxRate = 0.0f;
  float   fNearestRate = fRate;   // If we support fRate, then fRate *is* the nearest.

  CAutoLock lock(this);

  // We cannot check rate support after shutdown.
  hr = CheckShutdown();
  CHECK_HR(hr, "EVRCustomPresenter::IsRateSupported cannot check rate support after shutdown");

  // Find the maximum forward rate. We have no minimum rate, anything down to 0 is supported.
  fMaxRate = GetMaxRate(bThin);

  if (fabsf(fRate) > fMaxRate)
  {
    // The (absolute) requested rate exceeds the maximum rate.
    hr = MF_E_UNSUPPORTED_RATE;

    // The nearest supported rate is fMaxRate.
    fNearestRate = fMaxRate;
    if (fRate < 0)
    {
      // Negative for reverse playback.
      fNearestRate = -fNearestRate;
    }
  }

  // Return the nearest supported rate.
  if (pfNearestSupportedRate != NULL)
  {
    *pfNearestSupportedRate = fNearestRate;
  }

  return hr;
}


