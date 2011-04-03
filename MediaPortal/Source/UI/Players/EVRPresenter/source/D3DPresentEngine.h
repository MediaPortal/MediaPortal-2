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

#include <d3d9.h>
#include <dxva2api.h>

const DWORD NUM_PRESENTER_BUFFERS = 3;

class D3DPresentEngine : public SchedulerCallback
{
public:

  // State of the Direct3D device.
  enum DeviceState
  {
    DeviceOK,
    DeviceReset,    // The device was reset OR re-created.
    DeviceRemoved,  // The device was removed.
  };

  D3DPresentEngine(IEVRCallback* callback, IDirect3DDevice9Ex* d3DDevice, HWND hwnd, HRESULT& hr);
  ~D3DPresentEngine();

  // GetService: Returns the IDirect3DDeviceManager9 interface.
  HRESULT GetService(REFGUID guidService, REFIID riid, void** ppv);
  HRESULT CheckFormat(D3DFORMAT format);

  HRESULT CreateVideoSamples(IMFMediaType *pFormat, VideoSampleList& videoSampleQueue);
  void    ReleaseResources();

  HRESULT CheckDeviceState(DeviceState *pState);
  HRESULT PresentSample(IMFSample* pSample, LONGLONG llTarget); 

  UINT    RefreshRate() const { return m_DisplayMode.RefreshRate; }

protected:
  HRESULT InitializeD3D();

  UINT                        m_DeviceResetToken;     // Reset token for the D3D device manager.

  HWND                        m_hwnd;                 // Application-provided destination window.
  RECT                        m_rcDestRect;           // Destination rectangle.
  UINT32                      m_Width;
  UINT32                      m_Height;
  UINT32                      m_ArX;
  UINT32                      m_ArY;
  D3DDISPLAYMODE              m_DisplayMode;          // Adapter's display mode.

  CritSec                     m_ObjectLock;           // Thread lock for the D3D device.

  IEVRCallback                *m_EVRCallback;         // Callback interface to MP2

  // COM interfaces
  IDirect3D9Ex                *m_pD3D9;
  IDirect3DDevice9Ex          *m_pDevice;
  IDirect3DDeviceManager9     *m_pDeviceManager;      // Direct3D device manager.
  IDirect3DSurface9           *m_pSurfaceRepaint;     // Surface for repaint requests.
};

