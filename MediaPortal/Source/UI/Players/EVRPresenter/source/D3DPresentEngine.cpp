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

#include "EVRCustomPresenter.h"
#include "MediaType.h"

const DWORD PRESENTER_BUFFER_COUNT = 3;

HRESULT FindAdapter(IDirect3D9 *pD3D9, HMONITOR hMonitor, UINT *puAdapterID);


// Constructor
D3DPresentEngine::D3DPresentEngine(HRESULT& hr) : 
  m_hwnd(NULL),
  m_DeviceResetToken(0),
  m_pD3D9(NULL),
  m_pDevice(NULL),
  m_pDeviceManager(NULL),
  m_pSurfaceRepaint(NULL)
{
  SetRectEmpty(&m_rcDestRect);

  ZeroMemory(&m_DisplayMode, sizeof(m_DisplayMode));

  hr = InitializeD3D();

  if (SUCCEEDED(hr))
  {
     hr = CreateD3DDevice();
  }
}


// Destructor
D3DPresentEngine::~D3DPresentEngine()
{
  SAFE_RELEASE(m_pDevice);
  SAFE_RELEASE(m_pSurfaceRepaint);
  SAFE_RELEASE(m_pDeviceManager);
  SAFE_RELEASE(m_pD3D9);
}


// Returns a service interface from the presenter engine.
HRESULT D3DPresentEngine::GetService(REFGUID guidService, REFIID riid, void** ppv)
{
  assert(ppv != NULL);

  HRESULT hr = S_OK;

  if (riid == __uuidof(IDirect3DDeviceManager9))
  {
    if (m_pDeviceManager == NULL)
    {
      hr = MF_E_UNSUPPORTED_SERVICE;
    }
    else
    {
      *ppv = m_pDeviceManager;
      m_pDeviceManager->AddRef();
    }
  }
  else
  {
    hr = MF_E_UNSUPPORTED_SERVICE;
  }

  return hr;
}


// Queries whether the D3DPresentEngine can use a specified Direct3D format.
HRESULT D3DPresentEngine::CheckFormat(D3DFORMAT format)
{
  HRESULT hr = S_OK;

  UINT uAdapter = D3DADAPTER_DEFAULT;
  D3DDEVTYPE type = D3DDEVTYPE_HAL;

  D3DDISPLAYMODE mode;
  D3DDEVICE_CREATION_PARAMETERS params;

  if (m_pDevice)
  {
    hr = m_pDevice->GetCreationParameters(&params);
    CHECK_HR(hr, "D3DPresentEngine::CheckFormat IDirect3DDeviceEx::GetCreationParameters() failed");

    uAdapter = params.AdapterOrdinal;
    type = params.DeviceType;
  }
    
  hr = m_pD3D9->GetAdapterDisplayMode(uAdapter, &mode);
  CHECK_HR(hr, "D3DPresentEngine::CheckFormat IDirect3D9Ex::GetAdapterDisplayMode() failed");

  hr = m_pD3D9->CheckDeviceType(uAdapter, type, mode.Format, format, TRUE);
  CHECK_HR(hr, "D3DPresentEngine::CheckFormat IDirect3D9Ex::CheckDeviceType() failed"); 

  return hr;
}


// Sets the window where the video is drawn.
HRESULT D3DPresentEngine::SetVideoWindow(HWND hwnd)
{
  // Assertions: EVRCustomPresenter checks these cases.
  assert(IsWindow(hwnd));
  assert(hwnd != m_hwnd);     

  HRESULT hr = S_OK;

  AutoLock lock(m_ObjectLock);

  m_hwnd = hwnd;

  UpdateDestRect();

  // Recreate the device.
  hr = CreateD3DDevice();

  return hr;
}


// Sets the region within the video window where the video is drawn.
HRESULT D3DPresentEngine::SetDestinationRect(const RECT& rcDest)
{
  if (EqualRect(&rcDest, &m_rcDestRect))
  {
    return S_OK; // No change.
  }

  HRESULT hr = S_OK;

  AutoLock lock(m_ObjectLock);

  m_rcDestRect = rcDest;

  UpdateDestRect();

  return hr;
}


// Creates video samples based on a specified media type.
HRESULT D3DPresentEngine::CreateVideoSamples(IMFMediaType *pFormat, VideoSampleList& videoSampleQueue)
{
  if (m_hwnd == NULL)
  {
    return MF_E_INVALIDREQUEST;
  }

  if (pFormat == NULL)
  {
    return MF_E_UNEXPECTED;
  }

  HRESULT hr = S_OK;
  D3DPRESENT_PARAMETERS pp;

  IDirect3DSwapChain9 *pSwapChain = NULL;    // Swap chain
  IMFSample *pVideoSample = NULL;            // Sampl
    
  AutoLock lock(m_ObjectLock);

  ReleaseResources();

  // Get the swap chain parameters from the media type.
  hr = GetSwapChainPresentParameters(pFormat, &pp);
  if (FAILED(hr))
  {
    ReleaseResources();
    return hr;
  }

  UpdateDestRect();

  // Create the video samples.
  for (int i = 0; i < PRESENTER_BUFFER_COUNT; i++)
  {
    // Create a new swap chain.
    hr = m_pDevice->CreateAdditionalSwapChain(&pp, &pSwapChain);
    if (FAILED(hr))
    {
      SAFE_RELEASE(pSwapChain);
      SAFE_RELEASE(pVideoSample);
      ReleaseResources();
      return hr;
    }
        
    // Create the video sample from the swap chain.
    hr = CreateD3DSample(pSwapChain, &pVideoSample);
    if (FAILED(hr))
    {
      SAFE_RELEASE(pSwapChain);
      SAFE_RELEASE(pVideoSample);
      ReleaseResources();
      return hr;
    }

    // Add it to the list.
    hr = videoSampleQueue.InsertBack(pVideoSample);
    if (FAILED(hr))
    {
      SAFE_RELEASE(pSwapChain);
      SAFE_RELEASE(pVideoSample);
      ReleaseResources();
      return hr;
    }

    // Set the swap chain pointer as a custom attribute on the sample. This keeps
    // a reference count on the swap chain, so that the swap chain is kept alive
    // for the duration of the sample's lifetime.
    hr = pVideoSample->SetUnknown(MFSamplePresenter_SampleSwapChain, pSwapChain);
    if (FAILED(hr))
    {
      SAFE_RELEASE(pSwapChain);
      SAFE_RELEASE(pVideoSample);
      ReleaseResources();
      return hr;
    }

    SAFE_RELEASE(pVideoSample);
    SAFE_RELEASE(pSwapChain);
  }

  // Let the derived class create any additional D3D resources that it needs.
  hr = OnCreateVideoSamples(pp);
  if (FAILED(hr))
  {
    SAFE_RELEASE(pSwapChain);
    SAFE_RELEASE(pVideoSample);
    ReleaseResources();
    return hr;
  }

  SAFE_RELEASE(pSwapChain);
  SAFE_RELEASE(pVideoSample);

  return hr;
}


// Released Direct3D resources used by this object. 
void D3DPresentEngine::ReleaseResources()
{
  // Let the derived class release any resources it created.
  OnReleaseResources();

  SAFE_RELEASE(m_pSurfaceRepaint);
}


// Tests the Direct3D device state.
HRESULT D3DPresentEngine::CheckDeviceState(DeviceState *pState)
{
  HRESULT hr = S_OK;

  AutoLock lock(m_ObjectLock);

  // Check the device state. Not every failure code is a critical failure.
  hr = m_pDevice->CheckDeviceState(m_hwnd);

  *pState = DeviceOK;

  switch (hr)
  {
  case S_OK:
  case S_PRESENT_OCCLUDED:
  case S_PRESENT_MODE_CHANGED:
    // state is DeviceOK
    hr = S_OK;
  break;

  case D3DERR_DEVICELOST:
  case D3DERR_DEVICEHUNG:
    // Lost/hung device. Destroy the device and create a new one.
    hr = CreateD3DDevice();
    CHECK_HR(hr, "D3DPresentEngine::CheckDeviceState D3DPresentEngine::Create3DDevice() failed");
    *pState = DeviceReset;
    hr = S_OK;
  break;

  case D3DERR_DEVICEREMOVED:
    // This is a fatal error.
    *pState = DeviceRemoved;
  break;

  case E_INVALIDARG:
    // CheckDeviceState can return E_INVALIDARG if the window is not valid
    // We'll assume that the window was destroyed; we'll recreate the device 
    // if the application sets a new window.
    hr = S_OK;
  break;
  }

  return hr;
}


// Presents a video frame.
HRESULT D3DPresentEngine::PresentSample(IMFSample* pSample, LONGLONG llTarget)
{
  HRESULT hr = S_OK;

  IMFMediaBuffer* pBuffer = NULL;
  IDirect3DSurface9* pSurface = NULL;
  IDirect3DSwapChain9* pSwapChain = NULL;

  if (pSample)
  {
    // Get the buffer from the sample.
    hr = pSample->GetBufferByIndex(0, &pBuffer);
    if (FAILED(hr))
    {
      SAFE_RELEASE(pBuffer);

      if (hr == D3DERR_DEVICELOST || hr == D3DERR_DEVICENOTRESET || hr == D3DERR_DEVICEHUNG)
      {
        // We failed because the device was lost. Fill the destination rectangle.
        PaintFrameWithGDI();

        // Ignore. The Reset(Ex) method must be called from the thread that created the device.

        // The presenter will detect the state when it calls CheckDeviceState() on the next sample.
        hr = S_OK;
      }
    }

    // Get the surface from the buffer.
    hr = MFGetService(pBuffer, MR_BUFFER_SERVICE, __uuidof(IDirect3DSurface9), (void**)&pSurface);
    if (FAILED(hr))
    {
      SAFE_RELEASE(pSurface);
      SAFE_RELEASE(pBuffer);

      if (hr == D3DERR_DEVICELOST || hr == D3DERR_DEVICENOTRESET || hr == D3DERR_DEVICEHUNG)
      {
        // We failed because the device was lost. Fill the destination rectangle.
        PaintFrameWithGDI();

        // Ignore. The Reset(Ex) method must be called from the thread that created the device.

        // The presenter will detect the state when it calls CheckDeviceState() on the next sample.
        hr = S_OK;
      }
    }
  }
  else if (m_pSurfaceRepaint)
  {
    // Redraw from the last surface.
    pSurface = m_pSurfaceRepaint;
    pSurface->AddRef();
  }

  if (pSurface)
  {
    // Get the swap chain from the surface.
    hr = pSurface->GetContainer(__uuidof(IDirect3DSwapChain9), (LPVOID*)&pSwapChain);
    if (FAILED(hr))
    {
      SAFE_RELEASE(pSwapChain);
      SAFE_RELEASE(pSurface);
      SAFE_RELEASE(pBuffer);

      if (hr == D3DERR_DEVICELOST || hr == D3DERR_DEVICENOTRESET || hr == D3DERR_DEVICEHUNG)
      {
        // We failed because the device was lost. Fill the destination rectangle.
        PaintFrameWithGDI();

        // Ignore. The Reset(Ex) method must be called from the thread that created the device.

        // The presenter will detect the state when it calls CheckDeviceState() on the next sample.
        hr = S_OK;
      }
    }

    // Present the swap chain.
    hr = PresentSwapChain(pSwapChain, pSurface);
    if (FAILED(hr))
    {
      SAFE_RELEASE(pSwapChain);
      SAFE_RELEASE(pSurface);
      SAFE_RELEASE(pBuffer);

      if (hr == D3DERR_DEVICELOST || hr == D3DERR_DEVICENOTRESET || hr == D3DERR_DEVICEHUNG)
      {
        // We failed because the device was lost. Fill the destination rectangle.
        PaintFrameWithGDI();

        // Ignore. The Reset(Ex) method must be called from the thread that created the device.

        // The presenter will detect the state when it calls CheckDeviceState() on the next sample.
        hr = S_OK;
      }
    }

    // Store this pointer in case we need to repaint the surface.
    CopyComPointer(m_pSurfaceRepaint, pSurface);
  }
  else
  {
    // No surface. All we can do is paint a black rectangle.
    PaintFrameWithGDI();
  }

  SAFE_RELEASE(pSwapChain);
  SAFE_RELEASE(pSurface);
  SAFE_RELEASE(pBuffer);

  return hr;
}

 
// Initializes Direct3D and the Direct3D device manager.
HRESULT D3DPresentEngine::InitializeD3D()
{
  HRESULT hr = S_OK;

  assert(m_pD3D9 == NULL);
  assert(m_pDeviceManager == NULL);

  // Create Direct3D
  hr = Direct3DCreate9Ex(D3D_SDK_VERSION, &m_pD3D9);
  CHECK_HR(hr, "D3DPresentEngine::InitializeD3D Direct3DCreate9Ex() failed");

  // Create the device manager
  hr = DXVA2CreateDirect3DDeviceManager9(&m_DeviceResetToken, &m_pDeviceManager);
  CHECK_HR(hr, "D3DPresentEngine::InitializeD3D DXVA2CreateDirect3DDreviceManager9() failed");

  return hr;
}


// Creates the Direct3D device.
HRESULT D3DPresentEngine::CreateD3DDevice()
{
  HRESULT     hr = S_OK;
  HWND        hwnd = NULL;
  HMONITOR    hMonitor = NULL;
  UINT        uAdapterID = D3DADAPTER_DEFAULT;
  DWORD       vp = 0;

  D3DCAPS9    ddCaps;
  ZeroMemory(&ddCaps, sizeof(ddCaps));

  IDirect3DDevice9Ex* pDevice = NULL;

  // Hold the lock because we might be discarding an exisiting device.
  AutoLock lock(m_ObjectLock);    

  if (!m_pD3D9 || !m_pDeviceManager)
  {
    return MF_E_NOT_INITIALIZED;
  }

  hwnd = GetDesktopWindow();

  // Note: The presenter creates additional swap chains to present the
  // video frames. Therefore, it does not use the device's implicit 
  // swap chain, so the size of the back buffer here is 1 x 1.

  D3DPRESENT_PARAMETERS pp;
  ZeroMemory(&pp, sizeof(pp));

  pp.BackBufferWidth = 1;
  pp.BackBufferHeight = 1;
  pp.Windowed = TRUE;
  pp.SwapEffect = D3DSWAPEFFECT_COPY;
  pp.BackBufferFormat = D3DFMT_UNKNOWN;
  pp.hDeviceWindow = hwnd;
  pp.Flags = D3DPRESENTFLAG_VIDEO;
  pp.PresentationInterval = D3DPRESENT_INTERVAL_DEFAULT;

  // Find the monitor for this window.
  if (m_hwnd)
  {
    hMonitor = MonitorFromWindow(m_hwnd, MONITOR_DEFAULTTONEAREST);

    // Find the corresponding adapter.
    hr = FindAdapter(m_pD3D9, hMonitor, &uAdapterID);
    if (FAILED(hr))
    {
      SAFE_RELEASE(pDevice);
      return hr;
    }
  }

  // Get the device caps for this adapter.
  hr = m_pD3D9->GetDeviceCaps(uAdapterID, D3DDEVTYPE_HAL, &ddCaps);
  if (FAILED(hr))
  {
    SAFE_RELEASE(pDevice);
    return hr;
  }

  if(ddCaps.DevCaps & D3DDEVCAPS_HWTRANSFORMANDLIGHT)
  {
    vp = D3DCREATE_HARDWARE_VERTEXPROCESSING;
  }
  else
  {
    vp = D3DCREATE_SOFTWARE_VERTEXPROCESSING;
  }

  // Create the device.
  hr = m_pD3D9->CreateDeviceEx(
    uAdapterID,
    D3DDEVTYPE_HAL,
    pp.hDeviceWindow,
    vp | D3DCREATE_NOWINDOWCHANGES | D3DCREATE_MULTITHREADED | D3DCREATE_FPU_PRESERVE,
    &pp, 
    NULL,
    &pDevice
  );
  if (FAILED(hr))
  {
    SAFE_RELEASE(pDevice);
    return hr;
  }
 
  // Get the adapter display mode.
  hr = m_pD3D9->GetAdapterDisplayMode(uAdapterID, &m_DisplayMode);
  if (FAILED(hr))
  {
    SAFE_RELEASE(pDevice);
    return hr;
  }

  // Reset the D3DDeviceManager with the new device 
  hr = m_pDeviceManager->ResetDevice(pDevice, m_DeviceResetToken);
  if (FAILED(hr))
  {
    SAFE_RELEASE(pDevice);
    return hr;
  }

  SAFE_RELEASE(m_pDevice);

  m_pDevice = pDevice;
  m_pDevice->AddRef();

  SAFE_RELEASE(pDevice);
  return hr;
}


// Creates an sample object (IMFSample) to hold a Direct3D swap chain.
HRESULT D3DPresentEngine::CreateD3DSample(IDirect3DSwapChain9 *pSwapChain, IMFSample **ppVideoSample)
{
  // Caller holds the object lock.

  HRESULT hr = S_OK;
  D3DCOLOR clrBlack = D3DCOLOR_ARGB(0xFF, 0x00, 0x00, 0x00);

  IDirect3DSurface9* pSurface = NULL;
  IMFSample* pSample = NULL;

  // Get the back buffer surface.
  hr = pSwapChain->GetBackBuffer(0, D3DBACKBUFFER_TYPE_MONO, &pSurface);
  if (FAILED(hr))
  {
    SAFE_RELEASE(pSurface);
    SAFE_RELEASE(pSample);
    return hr;
  }

  // Fill it with black.
  hr = m_pDevice->ColorFill(pSurface, NULL, clrBlack);
  if (FAILED(hr))
  {
    SAFE_RELEASE(pSurface);
    SAFE_RELEASE(pSample);
    return hr;
  }

  // Create the sample.
  hr = MFCreateVideoSampleFromSurface(pSurface, &pSample);
  if (FAILED(hr))
  {
    SAFE_RELEASE(pSurface);
    SAFE_RELEASE(pSample);
    return hr;
  }

  // Return the pointer to the caller.
  *ppVideoSample = pSample;
  (*ppVideoSample)->AddRef();

  SAFE_RELEASE(pSurface);
  SAFE_RELEASE(pSample);
  return hr;
}


// Presents a swap chain that contains a video frame.
HRESULT D3DPresentEngine::PresentSwapChain(IDirect3DSwapChain9* pSwapChain, IDirect3DSurface9* pSurface)
{
  HRESULT hr = S_OK;

  if (m_hwnd == NULL)
  {
    return MF_E_INVALIDREQUEST;
  }

  hr = pSwapChain->Present(NULL, &m_rcDestRect, m_hwnd, NULL, 0);
  CHECK_HR(hr, "D3DPresentEngine::PresentSwapChain IDirect3DSwapChain9::Present() failed.");

  return hr;
}


// Fills the destination rectangle with black.
void D3DPresentEngine::PaintFrameWithGDI()
{
  HDC hdc = GetDC(m_hwnd);

  if (hdc)
  {
    HBRUSH hBrush = CreateSolidBrush(RGB(0, 0, 0));

    if (hBrush)
    {
      FillRect(hdc, &m_rcDestRect, hBrush);
      DeleteObject(hBrush);
    }

    ReleaseDC(m_hwnd, hdc);
  }
}


// Given a media type that describes the video format, fills in the D3DPRESENT_PARAMETERS for creating a swap chain.
HRESULT D3DPresentEngine::GetSwapChainPresentParameters(IMFMediaType *pType, D3DPRESENT_PARAMETERS* pPP)
{
  // Caller holds the object lock.

  HRESULT hr = S_OK; 

  UINT32 width = 0, height = 0;
  DWORD d3dFormat = 0;

  // Helper object for reading the proposed type.
  VideoType videoType(pType);

  if (m_hwnd == NULL)
  {
    return MF_E_INVALIDREQUEST;
  }

  ZeroMemory(pPP, sizeof(D3DPRESENT_PARAMETERS));

  // Get some information about the video format.
  hr = videoType.GetFrameDimensions(&width, &height);
  CHECK_HR(hr, "D3DPresentEngine::GetSwapChainPresentParameters VideoType::GetFrameDimensions() failed");

  hr = videoType.GetFourCC(&d3dFormat);
  CHECK_HR(hr, "D3DPresentEngine::GetSwapChainPresentParameters VideoType::GetFourCC() failed");

  // http://msdn.microsoft.com/en-us/library/bb172588(VS.85).aspx
  ZeroMemory(pPP, sizeof(D3DPRESENT_PARAMETERS));
  pPP->BackBufferWidth = width;
  pPP->BackBufferHeight = height;
  pPP->Windowed = TRUE;
  pPP->SwapEffect = D3DSWAPEFFECT_FLIPEX;
  pPP->BackBufferFormat = (D3DFORMAT)d3dFormat;
  pPP->hDeviceWindow = m_hwnd;
  pPP->Flags = D3DPRESENTFLAG_VIDEO;
  pPP->PresentationInterval = D3DPRESENT_INTERVAL_ONE;

  D3DDEVICE_CREATION_PARAMETERS params;
  hr = m_pDevice->GetCreationParameters(&params);
  CHECK_HR(hr, "D3DPresentEngine::GetSwapChainParameters IDirect3DDevice9Ex::GetCreationParameters() failed");
    
  if (params.DeviceType != D3DDEVTYPE_HAL)
  {
    pPP->Flags |= D3DPRESENTFLAG_LOCKABLE_BACKBUFFER;
  }

  return hr;
}


// Updates the target rectangle by clipping it to the video window's client area.
HRESULT D3DPresentEngine::UpdateDestRect()
{
  if (m_hwnd == NULL)
  {
    return S_FALSE;
  }

  RECT rcView;
  GetClientRect(m_hwnd, &rcView);

  // Clip the destination rectangle to the window's client area.
  if (m_rcDestRect.right > rcView.right)
  {
    m_rcDestRect.right = rcView.right;
  }

  if (m_rcDestRect.bottom > rcView.bottom)
  {
    m_rcDestRect.bottom = rcView.bottom;
  }

  return S_OK;
}


// Given a handle to a monitor, returns the ordinal number that D3D uses to identify the adapter.
HRESULT FindAdapter(IDirect3D9 *pD3D9, HMONITOR hMonitor, UINT *puAdapterID)
{
  HRESULT hr = E_FAIL;
  UINT cAdapters = 0;
  UINT uAdapterID = (UINT)-1;

  cAdapters = pD3D9->GetAdapterCount();
  for (UINT i = 0; i < cAdapters; i++)
  {
    HMONITOR hMonitorTmp = pD3D9->GetAdapterMonitor(i);

    if (hMonitorTmp == NULL)
    {
      break;
    }
    if (hMonitorTmp == hMonitor)
    {
      uAdapterID = i;
      break;
    }
  }

  if (uAdapterID != (UINT)-1)
  {
    *puAdapterID = uAdapterID;
    hr = S_OK;
  }

  return hr;
}

