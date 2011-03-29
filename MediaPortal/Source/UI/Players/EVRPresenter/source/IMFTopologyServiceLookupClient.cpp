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

#include <mfidl.h>
#include <mferror.h>
#include <d3d9.h>

#include "EVRCustomPresenter.h"

// IMFTopologyServiceLookupClient Interface http://msdn.microsoft.com/en-us/library/ms703063(v=VS.85).aspx
// Enables the presenter to get interfaces from the EVR or the mixer

// Signals the mixer or presenter to query the EVR for interface pointers.
HRESULT STDMETHODCALLTYPE EVRCustomPresenter::InitServicePointers(IMFTopologyServiceLookup *pLookup)
{
  Log("EVRCustomPresenter::InitServicePointers");

  HRESULT   hr = S_OK;
  DWORD     dwObjectCount = 0;

  CAutoLock lock(this);

  CheckPointer(pLookup, E_POINTER);

  // Do not allow initializing when playing or paused.
  if (IsActive())
  {
    hr = MF_E_INVALIDREQUEST;
    CHECK_HR(hr, "EVRCustomPresenter::InitServicePointers initializing when playing or paused");
  }

  SAFE_RELEASE(m_pClock);
  SAFE_RELEASE(m_pMixer);
  SAFE_RELEASE(m_pMediaEventSink);

  // Ask for the clock. Optional, because the EVR might not have a clock.
  dwObjectCount = 1;
  hr = pLookup->LookupService(      
    MF_SERVICE_LOOKUP_GLOBAL,   // Not used.
    0,                          // Reserved.
    MR_VIDEO_RENDER_SERVICE,    // Service to look up.
    __uuidof(IMFClock),         // Interface to look up.
    (void**)&m_pClock,          // Interface to retrieve.
    &dwObjectCount              // Number of elements retrieved.
  );

  // Ask for the mixer. (Required.)
  dwObjectCount = 1; 
  hr = pLookup->LookupService(
    MF_SERVICE_LOOKUP_GLOBAL,   // Not used.
    0,                          // Reserved.
    MR_VIDEO_MIXER_SERVICE,     // Service to look up
    __uuidof(IMFTransform),     // Interface to look up.
    (void**)&m_pMixer,          // Interface to retrieve.
    &dwObjectCount              // Number of elements retrieved.
  );
  CHECK_HR(hr, "EVRCustomPresenter::InitServicePointers could not get mixer.");

  // Make sure that we can work with this mixer.
  hr = ConfigureMixer(m_pMixer);
  CHECK_HR(hr, "EVRCustomPresenter::InitServicePointers incompatible mixer.");

  // Ask for the EVR's event-sink interface. (Required.)
  dwObjectCount = 1;
  hr = pLookup->LookupService(
    MF_SERVICE_LOOKUP_GLOBAL,     // Not used.
    0,                            // Reserved.
    MR_VIDEO_RENDER_SERVICE,      // Service to look up.
    __uuidof(IMediaEventSink),    // Interface to look up.
    (void**)&m_pMediaEventSink,   // Interface to retrieve.
    &dwObjectCount                // Number of elements retrieved.
  );
  CHECK_HR(hr, "EVRCustomPresenter::InitServicePointers could not get event sink.");

  // Successfully initialized. Set the state to "stopped."
  m_RenderState = RENDER_STATE_STOPPED;
  return hr;
}


// Signals the mixer or presenter to release the interface pointers obtained from the EVR.
HRESULT STDMETHODCALLTYPE EVRCustomPresenter::ReleaseServicePointers()
{
  Log("EVRCustomPresenter::ReleaseServicePointers");

  HRESULT hr = S_OK;

  // Enter shut-down state
  {
    CAutoLock lock(this);
    m_RenderState = RENDER_STATE_SHUTDOWN;
  }

  // Flush any samples that were scheduled.
  Flush();

  // Clear the media type and release related resources (surfaces, etc).
  SetMediaType(NULL);

  // Release all services that were acquired from InitServicePointers.
  SAFE_RELEASE(m_pClock);
  SAFE_RELEASE(m_pMixer);
  SAFE_RELEASE(m_pMediaEventSink);

  return hr;
}

