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

DEFINE_CLASSFACTORY_SERVER_LOCK; // Defines the static member variable for the class factory lock.

// IUnknown Interface http://msdn.microsoft.com/en-us/library/ms680509(v=VS.85).aspx
// Enables clients to get pointers to other interfaces and manage the existence of the object.

// Retrieves pointers to the supported interfaces on an object.
HRESULT STDMETHODCALLTYPE EVRCustomPresenter::QueryInterface(REFIID riid, void **ppv)
{
  CheckPointer(ppv, E_POINTER);

  if (riid == __uuidof(IUnknown))
  {
    *ppv = static_cast<IUnknown*>(static_cast<IMFVideoPresenter*>(this));
  }
  else if (riid == __uuidof(IMFVideoDeviceID))
  {
    *ppv = static_cast<IMFVideoDeviceID*>(this);
  }
  else if (riid == __uuidof(IMFVideoPresenter))
  {
    *ppv = static_cast<IMFVideoPresenter*>(this);
  }
  else if (riid == __uuidof(IMFClockStateSink))    // Inherited from IMFVideoPresenter
  {
    *ppv = static_cast<IMFClockStateSink*>(this);
  }
  else if (riid == __uuidof(IMFRateSupport))
  {
    *ppv = static_cast<IMFRateSupport*>(this);
  }
  else if (riid == __uuidof(IMFGetService))
  {
    *ppv = static_cast<IMFGetService*>(this);
  }
  else if (riid == __uuidof(IMFTopologyServiceLookupClient))
  {
    *ppv = static_cast<IMFTopologyServiceLookupClient*>(this);
  }
  else if (riid == __uuidof(IEVRTrustedVideoPlugin))
  {
    *ppv = static_cast<IEVRTrustedVideoPlugin*>(this);
  }
  else if (riid == __uuidof(IMFAsyncCallback))
  {
    *ppv = static_cast<IMFAsyncCallback*>(this);
  }
  else if (riid == IID_IQualProp)
  {
    *ppv = static_cast<IQualProp*>(this);
  }
  else
  {
    *ppv = NULL;
    return E_NOINTERFACE;
  }

  AddRef();
  return S_OK;
}


// Increments the reference count for an interface on an object.
ULONG STDMETHODCALLTYPE EVRCustomPresenter::AddRef()
{
  return RefCountedObject::AddRef();
}


// Decrements the reference count for an interface on an object.
ULONG STDMETHODCALLTYPE EVRCustomPresenter::Release()
{
  return RefCountedObject::Release();
}


