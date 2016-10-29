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
#include <dvdmedia.h>


// Constructor
D3DPresentEngine::D3DPresentEngine(IEVRCallback* callback, IDirect3DDevice9Ex* d3DDevice, HWND hwnd, HRESULT& hr) :
m_hwnd(hwnd),
m_DeviceResetToken(0),
m_pD3D9(NULL),
m_pDevice(d3DDevice),
m_pDeviceManager(NULL),
m_pTextureRepaint(NULL),
m_EVRCallback(callback),
m_Width(0),
m_Height(0)
{
  SetRectEmpty(&m_rcDestRect);

  ZeroMemory(&m_DisplayMode, sizeof(m_DisplayMode));

  hr = InitializeD3D();

  m_pDeviceManager->ResetDevice(m_pDevice, m_DeviceResetToken);
}


// Destructor
D3DPresentEngine::~D3DPresentEngine()
{
  SAFE_RELEASE(m_pDevice);
  SAFE_RELEASE(m_pTextureRepaint);
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


HRESULT GetAspectRatio(IMFMediaType* pFormat, UINT32& arX, UINT32& arY)
{
  HRESULT hr;
  UINT32 u32;
  if (SUCCEEDED(pFormat->GetUINT32(MF_MT_SOURCE_CONTENT_HINT, &u32)))
  {
    switch (u32)
    {
    case MFVideoSrcContentHintFlag_None:
      Log("Aspect ratio ('MediaFoundation style') is unknown");
      break;
    case MFVideoSrcContentHintFlag_16x9:
      Log("Aspect ratio ('MediaFoundation style') is 16:9 within 4:3!");
      arX = 16;
      arY = 9;
      break;
    case MFVideoSrcContentHintFlag_235_1:
      Log("Aspect ratio ('MediaFoundation style') is 2.35:1 within 16:9 or 4:3");
      arX = 47;
      arY = 20;
      break;
    default:
      Log("Aspect ratio ('MediaFoundation style') is unknown. Flag: %d", u32);
    }
  }
  else
  {
    //Try old DirectShow-Header, if above does not work
    Log("Getting aspect ratio 'DirectShow style'");
    AM_MEDIA_TYPE* pAMMediaType;
    CHECK_HR(
      hr = pFormat->GetRepresentation(FORMAT_VideoInfo2, (void**)&pAMMediaType),
      "Getting DirectShow Video Info failed");
    if (SUCCEEDED(hr))
    {
      VIDEOINFOHEADER2* vheader = (VIDEOINFOHEADER2*)pAMMediaType->pbFormat;
      arX = vheader->dwPictAspectRatioX;
      arY = vheader->dwPictAspectRatioY;
      pFormat->FreeRepresentation(FORMAT_VideoInfo2, (void*)pAMMediaType);
      Log("Aspect ratio ('DirectShow style') is %i:%i", arX, arY);
    }
    else
    {
      Log("Could not get DirectShow representation.");
    }
  }
  return hr;
}


// Creates video samples based on a specified media type.
HRESULT D3DPresentEngine::CreateVideoSamples(IMFMediaType *pFormat, VideoSampleList& videoSampleQueue)
{
  if (pFormat == NULL)
  {
    return MF_E_UNEXPECTED;
  }

  HRESULT hr = S_OK;

  D3DFORMAT d3dFormat = D3DFMT_UNKNOWN;

  IMFSample *pVideoSample = NULL;

  AutoLock lock(m_ObjectLock);

  ReleaseResources();

  // Helper object for reading the proposed type.
  VideoType videoType(pFormat);

  // Get some information about the video format.
  hr = videoType.GetFrameDimensions(&m_Width, &m_Height);
  CHECK_HR(hr, "D3DPresentEngine::CreateVideoSamples VideoType::GetFrameDimensions() failed");
  hr = GetAspectRatio(pFormat, m_ArX, m_ArY);
  if (FAILED(hr))
  {
    m_ArX = m_Width;
    m_ArY = m_Height;
  }

  //hr = videoType.GetFourCC((DWORD*)&d3dFormat);
  //CHECK_HR(hr, "D3DPresentEngine::CreateVideoSamples VideoType::GetFourCC() failed");

  // Morpheus_xx, 2016-08-14: we force a format without alpha channel here, because rendering subtitles with MPC-HC engine expects this format. Actually I can't imagine a video format
  // that actually delivers alpha channel information.
  d3dFormat = D3DFMT_X8R8G8B8;

  for (int i = 0; i < NUM_PRESENTER_BUFFERS; i++)
  {
    CComPtr<IDirect3DTexture9> texture;
    hr = m_pDevice->CreateTexture(m_Width, m_Height, 1, D3DUSAGE_RENDERTARGET, d3dFormat, D3DPOOL_DEFAULT, &texture, NULL);
    if (FAILED(hr))
    {
      Log("D3DPresentEngine::CreateVideoSamples Could not create texture %d. Error 0x%x", i, hr);
      break;
    }
    CComPtr<IDirect3DSurface9> surface;
    hr = texture->GetSurfaceLevel(0, &surface);
    if (FAILED(hr))
    {
      Log("D3DPresentEngine::CreateVideoSamples Could not get surface from texture. Error 0x%x", hr);
      break;
    }

    hr = MFCreateVideoSampleFromSurface(surface, &pVideoSample);
    if (FAILED(hr))
    {
      Log("D3DPresentEngine::CreateVideoSamples CreateVideoSampleFromSurface failed: 0x%x", hr);
      break;
    }

    // Add it to the list.
    hr = videoSampleQueue.InsertBack(pVideoSample);
    if (FAILED(hr))
    {
      SAFE_RELEASE(pVideoSample);
      ReleaseResources();
      return hr;
    }
    SAFE_RELEASE(pVideoSample);
  }

  return hr;
}


// Released Direct3D resources used by this object. 
void D3DPresentEngine::ReleaseResources()
{
  SAFE_RELEASE(m_pTextureRepaint);
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

    // TODO Albert: Not sure what we should do here... Should we remember the device-lost-state and set the
    // pState to DeviceReset the next time the device is ok again?
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
  IDirect3DTexture9* pTexture = NULL;

  if (pSample)
  {
    // Get the buffer from the sample.
    hr = pSample->GetBufferByIndex(0, &pBuffer);
    if (SUCCEEDED(hr))
    {
      // Get the surface from the buffer.
      IDirect3DSurface9* pSurface = NULL;
      hr = MFGetService(pBuffer, MR_BUFFER_SERVICE, __uuidof(IDirect3DSurface9), (void**)&pSurface);

      if (SUCCEEDED(hr))
      {
        // Get the texture from the buffer.
        pSurface->GetContainer(IID_IDirect3DTexture9, (void**)&pTexture);
      }
    }
    if (hr == D3DERR_DEVICELOST || hr == D3DERR_DEVICENOTRESET || hr == D3DERR_DEVICEHUNG)
    {
      // We failed because the device was lost.
      // This case is ignored. The Reset(Ex) method must be called from the thread that created the device.

      // The presenter will detect the state when it calls CheckDeviceState() on the next sample.
      hr = S_OK;
    }
  }
  else if (m_pTextureRepaint)
  {
    // Redraw from the last surface.
    pTexture = m_pTextureRepaint;
    pTexture->AddRef();
  }

  hr = m_EVRCallback->PresentSurface(m_Width, m_Height, m_ArX, m_ArY, (DWORD)&pTexture); // Return reference, so C# side can modify the pointer after Dispose() to avoid duplicated releasing.

  SAFE_RELEASE(pTexture);
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

