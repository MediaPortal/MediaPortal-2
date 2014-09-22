// Copyright (C) 2007-2014 Team MediaPortal
// http://www.team-mediaportal.com
// 
// This file is part of MediaPortal 2
// 
// MediaPortal 2 is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// MediaPortal 2 is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.

#include "EVRCustomPresenter.h"

// IMFGetService Interface http://msdn.microsoft.com/en-us/library/ms694261(v=VS.85).aspx
// Provides a way for the application and other components in the pipeline to get interfaces from the presenter.

// Retrieves a service interface.
HRESULT STDMETHODCALLTYPE EVRCustomPresenter::GetService(REFGUID guidService, REFIID riid, LPVOID *ppvObject)
{
  HRESULT hr = S_OK;

  CheckPointer(ppvObject, E_POINTER);

  // The only service GUIDs that we support are MR_VIDEO_RENDER_SERVICE and MR_VIDEO_ACCELERATION_SERVICE for DXVA2 support.
  if (guidService != MR_VIDEO_RENDER_SERVICE && guidService != MR_VIDEO_ACCELERATION_SERVICE)
  {
    return MF_E_UNSUPPORTED_SERVICE;
  }

  // First try to get the service interface from the D3DPresentEngine object.
  hr = m_pD3DPresentEngine->GetService(guidService, riid, ppvObject);
  if (FAILED(hr))
  {
    // Next, query interface to check if this object supports the interface.
    hr = QueryInterface(riid, ppvObject);
  }

  return hr;
}

