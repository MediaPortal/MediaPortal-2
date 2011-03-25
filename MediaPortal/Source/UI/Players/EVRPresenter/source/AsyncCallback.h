//////////////////////////////////////////////////////////////////////////
//
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO
// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
//
// Copyright (c) Microsoft Corporation. All rights reserved.
//
//////////////////////////////////////////////////////////////////////////

// T: Type of the parent object
template<class T>
class AsyncCallback : public IMFAsyncCallback
{
public: 
  typedef HRESULT (T::*InvokeFn)(IMFAsyncResult *pAsyncResult);

  AsyncCallback(T *pParent, InvokeFn fn) : m_pParent(pParent), m_pInvokeFn(fn)
  {
  }

  // IUnknown
  STDMETHODIMP QueryInterface(REFIID iid, void** ppv)
  {
    if (!ppv)
    {
      return E_POINTER;
    }
    if (iid == __uuidof(IUnknown))
    {
      *ppv = static_cast<IUnknown*>(static_cast<IMFAsyncCallback*>(this));
    }
    else if (iid == __uuidof(IMFAsyncCallback))
    {
      *ppv = static_cast<IMFAsyncCallback*>(this);
    }
    else
    {
      *ppv = NULL;
      return E_NOINTERFACE;
    }
    AddRef();
    return S_OK;
  }


  STDMETHODIMP_(ULONG) AddRef() { 
    // Delegate to parent class.
    return m_pParent->AddRef(); 
  }


  STDMETHODIMP_(ULONG) Release() { 
    // Delegate to parent class.
    return m_pParent->Release(); 
  }


  // IMFAsyncCallback methods
  STDMETHODIMP GetParameters(DWORD*, DWORD*)
  {
    // Implementation of this method is optional.
    return E_NOTIMPL;
  }


  STDMETHODIMP Invoke(IMFAsyncResult* pAsyncResult)
  {
    return (m_pParent->*m_pInvokeFn)(pAsyncResult);
  }

  T *m_pParent;
  InvokeFn m_pInvokeFn;
};

