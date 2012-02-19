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

// Constructor
EVRCustomPresenter::EVRCustomPresenter(IEVRCallback* callback, IDirect3DDevice9Ex* d3DDevice, HWND hwnd, HRESULT& hr) :
  m_RenderState(RENDER_STATE_SHUTDOWN),
  m_pD3DPresentEngine(NULL),
  m_pClock(NULL),
  m_pMixer(NULL),
  m_pMediaEventSink(NULL),
  m_pMediaType(NULL),
  m_bSampleNotify(FALSE),
  m_bRepaint(FALSE),
  m_bEndStreaming(FALSE),
  m_bPrerolled(FALSE),
  m_fRate(1.0f),
  m_TokenCounter(0),
  m_SampleFreeCB(this, &EVRCustomPresenter::OnSampleFree)
{
  hr = S_OK;

  // Initial source rectangle = (0,0,1,1)
  m_nrcSource.top = 0;
  m_nrcSource.left = 0;
  m_nrcSource.bottom = 1;
  m_nrcSource.right = 1;

  m_pD3DPresentEngine = new D3DPresentEngine(callback, d3DDevice, hwnd, hr);
  if (m_pD3DPresentEngine == NULL)
  {
    hr = E_OUTOFMEMORY;
    Log("EVRCustomPresenter::EVRCustomPresenter out of memory");
    SAFE_DELETE(m_pD3DPresentEngine);
  }
  else
  {
    m_scheduler.SetCallback(m_pD3DPresentEngine);
  }
}

  
// Destructor
EVRCustomPresenter::~EVRCustomPresenter()
{
  Log("EVRCustomPresenter::~EVRCustomPresenter");
}


// Init EVR Presenter (called by VideoPlayer.cs)
__declspec(dllexport) int EvrInit(IEVRCallback* callback, DWORD dwD3DDevice, IBaseFilter* evrFilter, HWND hwnd, EVRCustomPresenter** ppPresenterInstance)
{
	HRESULT hr;
  *ppPresenterInstance = NULL;

  // Set IMFVideoRenderer Interface
	CComQIPtr<IMFVideoRenderer> pVideoRenderer = evrFilter;
	if (!pVideoRenderer) 
  {
		Log("EvrInit could not set IMFVideoRenderer interface");
	  hr = E_FAIL;
    return hr;
	}

  EVRCustomPresenter* presenter = new EVRCustomPresenter(callback, (IDirect3DDevice9Ex*)dwD3DDevice, hwnd, hr);
  if (FAILED(hr))
  {
    Log("EvrInit EVRCustomPresenter() failed");
    presenter->Release();
    pVideoRenderer.Release();
	  return hr;
  }

  hr = pVideoRenderer->InitializeRenderer(NULL, presenter);
  if (FAILED(hr))
  {
    Log("EvrInit IMFVIdeoRenderer::InitializeRenderer failed");
    presenter->Release();
    pVideoRenderer.Release();
	  return hr;
  }

  *ppPresenterInstance = presenter;
  Log("EvrInit: successfully created presenter 0x%x", *ppPresenterInstance);
  pVideoRenderer.Release();
	return hr;
}


__declspec(dllexport) void EvrDeinit(EVRCustomPresenter* pPresenterInstance)
{
  if (pPresenterInstance != NULL)
  {
    Log("EvrDeinit: Releasing presenter 0x%x", pPresenterInstance);
    pPresenterInstance->Release();
  }
}

