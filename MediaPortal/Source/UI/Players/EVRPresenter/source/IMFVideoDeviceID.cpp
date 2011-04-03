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

#include <d3d9.h>

#include "EVRCustomPresenter.h"

// IMFVideoDeviceID Interface http://msdn.microsoft.com/en-us/library/ms703065(v=VS.85).aspx
// Ensures that the presenter and the mixer use compatible technologies.

// Returns the identifier of the video device supported by the EVR mixer or presenter.
HRESULT STDMETHODCALLTYPE EVRCustomPresenter::GetDeviceID(IID *pDeviceID)
{
  Log("IMFVideoDeviceID::GetDeviceID()");

  if (pDeviceID == NULL)
  {
    return E_POINTER;
  }

  // device GUID must be IID_IDirect3DDevice9 unless a custom mixer is used as well
  *pDeviceID = __uuidof(IDirect3DDevice9);

  // succeed even when the presenter is shut down
  return S_OK;
}

