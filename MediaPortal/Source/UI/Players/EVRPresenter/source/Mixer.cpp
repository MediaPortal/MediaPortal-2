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

// Initializes the mixer. Called from InitServicePointers.
HRESULT EVRCustomPresenter::ConfigureMixer(IMFTransform *pMixer)
{
  Log("EVRCustomPresenter::ConfigureMixer");

  HRESULT hr = S_OK;
  IID deviceID = GUID_NULL;
  IMFVideoDeviceID *pDeviceID = NULL;

  // Make sure that the mixer has the same device ID as ourselves.
  hr = pMixer->QueryInterface(__uuidof(IMFVideoDeviceID), (void**)&pDeviceID);
  CHECK_HR(hr, "EVRCustomPresenter::ConfigureMixer IMFTransform::QueryInterface() failed.");

  hr = pDeviceID->GetDeviceID(&deviceID);
  CHECK_HR(hr, "EVRCustomPresenter::ConfigureMixer IMFVIdeoDeviceID::GetDeviceID() failed.");

  if (!IsEqualGUID(deviceID, __uuidof(IDirect3DDevice9)))
  {
    hr = MF_E_INVALIDREQUEST;
    CHECK_HR(hr, "EVRCustomPresenter::ConfigureMixer GUIDs are not equal.");
  }

  // Set the zoom rectangle (ie, the source clipping rectangle).
  hr = SetMixerSourceRect(pMixer, m_nrcSource);
 
  SAFE_RELEASE(pDeviceID);
  return hr;
}


// Sets the zoom rectangle on the mixer.
HRESULT EVRCustomPresenter::SetMixerSourceRect(IMFTransform *pMixer, const MFVideoNormalizedRect& nrcSource)
{
  Log("EVRCustomPresenter::SetMixerSourceRect");

  CheckPointer(pMixer, E_POINTER);

  HRESULT hr = S_OK;
  IMFAttributes *pAttributes = NULL;

  hr = pMixer->GetAttributes(&pAttributes);
  CHECK_HR(hr, "EVRCustomPresenter::SetMixerSourceRect could not get mixer attributes");

  hr = pAttributes->SetBlob(VIDEO_ZOOM_RECT, (const UINT8*)&nrcSource, sizeof(nrcSource));
  if (FAILED(hr))
  {
    Log("EVRCustomPresenter::SetMixerSourceRect could not set zoom rectangle");
    SAFE_RELEASE(pAttributes);
    return hr;
  }
  
  SAFE_RELEASE(pAttributes);
  return hr;
}

