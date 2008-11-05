/* 
 *	Copyright (C) 2005 Team MediaPortal
 *  Author: Frodo
 *	http://www.team-mediaportal.com
 *
 *  This Program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation; either version 2, or (at your option)
 *  any later version.
 *   
 *  This Program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU General Public License for more details.
 *   
 *  You should have received a copy of the GNU General Public License
 *  along with GNU Make; see the file COPYING.  If not, write to
 *  the Free Software Foundation, 675 Mass Ave, Cambridge, MA 02139, USA. 
 *  http://www.gnu.org/copyleft/gpl.html
 *
 */

// Windows Header Files:
#include <windows.h>

#include <streams.h>
#include <stdio.h>
#include <atlbase.h>

#include <mmsystem.h>
#include <d3d9.h>
#include <d3dx9.h>
#include <d3d9types.h>
#include <strsafe.h>
#include <dshow.h>
#include <vmr9.h>
#include <sbe.h>
#include <dxva.h>
#include <dvdmedia.h>

#include <vector>
#include <comutil.h>
using namespace std;

#include "IVmr9Callback.h"
#include "Vmr9CustomAllocator.h"

void Log(const char *fmt, ...) ;
extern __declspec(dllexport) void Vmr9DeInit(int id);

Vmr9CustomAllocator::Vmr9CustomAllocator(int id,IDirect3DDevice9* direct3dDevice, IVMR9Callback* callback, HMONITOR monitor)
: m_refCount(1)
{
	Log("----------v0.37a---------------------------");
	m_hMonitor=monitor;
	m_pD3DDev=direct3dDevice;
	m_pCallback=callback;
	m_surfaceCount=0;
	m_bfirstFrame=true;
  m_UseOffScreenSurface=false;
  m_id=id;

  m_pSurfaces=NULL;
}

Vmr9CustomAllocator::~Vmr9CustomAllocator()
{
  Log("Vmr9CustomAllocator dtor");
	
  DeleteSurfaces();
  m_pIVMRSurfAllocNotify=NULL;
  m_pD3DDev=NULL;
}	

int Vmr9CustomAllocator::Id()
{
  return m_id;
}

void Vmr9CustomAllocator::UseOffScreenSurface(bool yesNo)
{
  m_UseOffScreenSurface=yesNo;
}

// IUnknown
HRESULT Vmr9CustomAllocator::QueryInterface( 
        REFIID riid,
        void** ppvObject)
{
    HRESULT hr = E_NOINTERFACE;

    if( ppvObject == NULL ) {
        hr = E_POINTER;
    } 
    else if( riid == IID_IVMRSurfaceAllocator9 ) {
        *ppvObject = static_cast<IVMRSurfaceAllocator9*>( this );
        AddRef();
        hr = S_OK;
    } 
    else if( riid == IID_IVMRImagePresenter9 ) {
        *ppvObject = static_cast<IVMRImagePresenter9*>( this );
        AddRef();
        hr = S_OK;
    } 
    else if( riid == IID_IVMRWindowlessControl9  ) {
        *ppvObject = static_cast<IVMRWindowlessControl9*>( this );
        AddRef();
        hr = S_OK;
    } 
    else if( riid == IID_IUnknown ) {
        *ppvObject = 
            static_cast<IUnknown*>( 
            static_cast<IVMRSurfaceAllocator9*>( this ) );
        AddRef();
        hr = S_OK;    
    }

    return hr;
}

ULONG Vmr9CustomAllocator::AddRef()
{
  Log("Vmr9CustomAllocator::AddRef() %d->%d",m_refCount,m_refCount+1);
  return InterlockedIncrement(& m_refCount);
}

ULONG Vmr9CustomAllocator::Release()
{
  Log("Vmr9CustomAllocator::Release() %d->%d",m_refCount,m_refCount-1);
  ULONG ret = InterlockedDecrement(& m_refCount);
  if( ret == 0 )
  {
      Log("Vmr9CustomAllocator::Cleanup()");
      Vmr9DeInit(m_id);
      delete this;
  }

  return ret;
}

STDMETHODIMP Vmr9CustomAllocator::InitializeDevice(DWORD_PTR dwUserID, VMR9AllocationInfo* lpAllocInfo, DWORD* lpNumBuffers)
{
	previousEndFrame=0;
	m_bfirstFrame=true;
	if(!lpAllocInfo || !lpNumBuffers)
		return E_POINTER;

	if(!m_pIVMRSurfAllocNotify)
		return E_FAIL;

	Log("vmr9:InitializeDevice() %dx%d AR %d:%d flags:%d buffers:%d  fmt:(%x) %c%c%c%c", 
			lpAllocInfo->dwWidth,lpAllocInfo->dwHeight, 
			lpAllocInfo->szAspectRatio.cx,lpAllocInfo->szAspectRatio.cy,
			lpAllocInfo->dwFlags,
			*lpNumBuffers,
			lpAllocInfo->Format,
			((char)lpAllocInfo->Format&0xff),
			((char)(lpAllocInfo->Format>>8)&0xff),
			((char)(lpAllocInfo->Format>>16)&0xff),
			((char)(lpAllocInfo->Format>>24)&0xff));
  if (m_UseOffScreenSurface)
    lpAllocInfo->dwFlags =VMR9AllocFlag_OffscreenSurface;
	// StretchRect's yv12 -> rgb conversion looks horribly bright compared to the result of yuy2 -> rgb


	DeleteSurfaces();
	
	m_surfaceCount=*lpNumBuffers;
	
  HRESULT hr;

//  m_pSurfaces.resize(*lpNumBuffers);
  m_pSurfaces = new IDirect3DSurface9* [m_surfaceCount];
	
	//Log("vmr9:IntializeDevice() try TexureSurface|DXVA|3DRenderTarget");
	DWORD dwFlags=lpAllocInfo->dwFlags;
	Log("vmr9:flags:");
	if (dwFlags & VMR9AllocFlag_3DRenderTarget)   Log("vmr9:  3drendertarget");
	if (dwFlags & VMR9AllocFlag_DXVATarget)		  Log("vmr9:  DXVATarget");
	if (dwFlags & VMR9AllocFlag_OffscreenSurface) Log("vmr9:  OffscreenSurface");
	if (dwFlags & VMR9AllocFlag_RGBDynamicSwitch) Log("vmr9:  RGBDynamicSwitch");
	if (dwFlags & VMR9AllocFlag_TextureSurface)   Log("vmr9:  TextureSurface");
	
	//lpAllocInfo->dwFlags =dwFlags| VMR9AllocFlag_OffscreenSurface;
	hr = m_pIVMRSurfAllocNotify->AllocateSurfaceHelper(lpAllocInfo, lpNumBuffers,m_pSurfaces);// & m_pSurfaces.at(0) );
	if(FAILED(hr))
	{
		Log("vmr9:InitializeDevice()   AllocateSurfaceHelper returned:0x%x",hr);
		return hr;
	}

	m_iVideoWidth=lpAllocInfo->dwWidth;
	m_iVideoHeight=lpAllocInfo->dwHeight;
	m_iARX=lpAllocInfo->szAspectRatio.cx;
	m_iARY=lpAllocInfo->szAspectRatio.cy;
	Log("vmr9:InitializeDevice() done()");
	return hr;
}

STDMETHODIMP Vmr9CustomAllocator::TerminateDevice(DWORD_PTR dwUserID)
{
	
	Log("vmr9:TerminateDevice()");
  DeleteSurfaces();
  return S_OK;
}

STDMETHODIMP Vmr9CustomAllocator::GetSurface(DWORD_PTR dwUserID, DWORD SurfaceIndex, DWORD SurfaceFlags, IDirect3DSurface9** lplpSurface)
{
	//Log("vmr9:GetSurface()");
    if(!lplpSurface)
	{
		Log("vmr9:GetSurface() invalid pointer");
		return E_POINTER;
	}


    if (SurfaceIndex < 0 || SurfaceIndex >= m_surfaceCount) //m_pSurfaces.size() ) 
  {
	    Log("vmr9:GetSurface() invalid SurfaceIndex:%d",SurfaceIndex);
      return E_FAIL;
  }
	//return m_pSurfaces[SurfaceIndex].CopyTo(lplpSurface) ;
  *lplpSurface=m_pSurfaces[SurfaceIndex];
  m_pSurfaces[SurfaceIndex]->AddRef();
  return S_OK;
}

STDMETHODIMP Vmr9CustomAllocator::AdviseNotify(IVMRSurfaceAllocatorNotify9* lpIVMRSurfAllocNotify)
{
    //CAutoLock cAutoLock(this);
	
	Log("vmr9:AdviseNotify()");
	m_pIVMRSurfAllocNotify = lpIVMRSurfAllocNotify;

	HRESULT hr;
  if(FAILED(hr = m_pIVMRSurfAllocNotify->SetD3DDevice(m_pD3DDev, m_hMonitor)))
	{
		Log("vmr9:AdviseNotify() failed to set d3d device:%x",hr);
		return hr;
	}
  return S_OK;
}

// IVMRImagePresenter9

STDMETHODIMP Vmr9CustomAllocator::StartPresenting(DWORD_PTR dwUserID)
{
	Log("vmr9:StartPresenting()");
	return m_pD3DDev ? S_OK : E_FAIL;
}

STDMETHODIMP Vmr9CustomAllocator::StopPresenting(DWORD_PTR dwUserID)
{
	Log("vmr9:StopPresenting()");
	return S_OK;
}


STDMETHODIMP Vmr9CustomAllocator::PresentImage(DWORD_PTR dwUserID, VMR9PresentationInfo* lpPresInfo)
{
  HRESULT hr;
	static long frameCounter=0;
  try
  {
	  frameCounter++;
	  //Log("vmr9:PresentImage(%d)",frameCounter);
	  if(!m_pIVMRSurfAllocNotify)
	  {
		  Log("vmr9:PresentImage() allocNotify not set");
		  return E_FAIL;
	  }
	  if(!lpPresInfo || !lpPresInfo->lpSurf)
	  {
		  Log("vmr9:PresentImage() no surface");
		  return E_POINTER;
	  }

		previousEndFrame=lpPresInfo->rtEnd;
		m_fps = 10000000.0 / (lpPresInfo->rtEnd - lpPresInfo->rtStart);
		m_iARX=lpPresInfo->szAspectRatio.cx;
		m_iARY=lpPresInfo->szAspectRatio.cy;
  	Paint(lpPresInfo->lpSurf, lpPresInfo->szAspectRatio);//TEST
	}
	catch(...)
	{
		Log("vmr9:PresentImage() exception");
	}
  return S_OK;
}

void Vmr9CustomAllocator::ReleaseCallBack()
{
	m_pCallback=NULL;
}
void Vmr9CustomAllocator::DeleteSurfaces()
{
	Log("vmr9:DeleteSurfaces()");

	if (m_pCallback!=NULL)
		m_pCallback->PresentImage(0,0,0,0,0);

  for( size_t i = 0; i < /*m_pSurfaces.size()*/ m_surfaceCount; ++i ) 
  {
    if (m_pSurfaces!=NULL)
    {
      while (true) 
      { 
        int hr=m_pSurfaces[i]->Release() ;
        Log(" del surf #%d->%d",i,hr);
        if  (hr<=0) break;
      };
      //m_pSurfaces[i].Release();
      m_pSurfaces[i] = NULL;
    }
  }
  //m_pSurfaces.clear();
  m_pSurfaces=NULL;
  m_surfaceCount=0;
}

void Vmr9CustomAllocator::Paint(IDirect3DSurface9* pSurface, SIZE szAspectRatio)
{
	try
	{
		if (m_pCallback!=NULL)
		{
      m_pCallback->PresentSurface(m_iVideoWidth, m_iVideoHeight, m_iARX,m_iARY, (DWORD)pSurface);
		}
	}
	catch(...)
	{
		Log("vmr9:Paint() invalid exception");
	}
}

STDMETHODIMP Vmr9CustomAllocator::GetNativeVideoSize( 
/* [out] */ LONG *lpWidth,
/* [out] */ LONG *lpHeight,
/* [out] */ LONG *lpARWidth,
/* [out] */ LONG *lpARHeight) 
{
  *lpWidth=m_iVideoWidth;
  *lpHeight=m_iVideoHeight;
  *lpARWidth=m_iARX;
  *lpARHeight=m_iARY;
  return S_OK;
}

STDMETHODIMP Vmr9CustomAllocator::GetMinIdealVideoSize( 
/* [out] */ LONG *lpWidth,
/* [out] */ LONG *lpHeight)
{
  return E_NOTIMPL;
}

STDMETHODIMP Vmr9CustomAllocator::GetMaxIdealVideoSize( 
/* [out] */ LONG *lpWidth,
/* [out] */ LONG *lpHeight)
{
  return E_NOTIMPL;
}

STDMETHODIMP Vmr9CustomAllocator::SetVideoPosition( 
/* [in] */ const LPRECT lpSRCRect,
/* [in] */ const LPRECT lpDSTRect) 
{
  return E_NOTIMPL;
}

STDMETHODIMP Vmr9CustomAllocator::GetVideoPosition( 
/* [out] */ LPRECT lpSRCRect,
/* [out] */ LPRECT lpDSTRect) 
{
  lpSRCRect->left=0;
  lpSRCRect->top=0;
  lpSRCRect->right=m_iVideoWidth;
  lpSRCRect->bottom=m_iVideoHeight;

  
  lpDSTRect->left=0;
  lpDSTRect->top=0;
  lpDSTRect->right=m_iVideoWidth;
  lpDSTRect->bottom=m_iVideoHeight;
  return S_OK;
}

STDMETHODIMP Vmr9CustomAllocator::GetAspectRatioMode( 
/* [out] */ DWORD *lpAspectRatioMode) 
{
  *lpAspectRatioMode=VMR_ARMODE_NONE;
  return S_OK;
}

STDMETHODIMP Vmr9CustomAllocator::SetAspectRatioMode( 
/* [in] */ DWORD AspectRatioMode) 
{
  return S_OK;
}

STDMETHODIMP Vmr9CustomAllocator::SetVideoClippingWindow( 
/* [in] */ HWND hwnd) 
{
  return E_NOTIMPL;
}

STDMETHODIMP Vmr9CustomAllocator::RepaintVideo( 
/* [in] */ HWND hwnd,
/* [in] */ HDC hdc) 
{
  return E_NOTIMPL;
}

STDMETHODIMP Vmr9CustomAllocator::DisplayModeChanged( void) 
{
  return E_NOTIMPL;
}

STDMETHODIMP Vmr9CustomAllocator::GetCurrentImage( 
/* [out] */ BYTE **lpDib) 
{
  return E_NOTIMPL;
}

STDMETHODIMP Vmr9CustomAllocator::SetBorderColor( 
/* [in] */ COLORREF Clr) 
{
  return E_NOTIMPL;
}

STDMETHODIMP Vmr9CustomAllocator::GetBorderColor( 
/* [out] */ COLORREF *lpClr) 
{
  return E_NOTIMPL;
}

STDMETHODIMP Vmr9CustomAllocator::SetColorKey( 
/* [in] */ COLORREF Clr) 
{
  return E_NOTIMPL;
}

STDMETHODIMP Vmr9CustomAllocator::GetColorKey( 
/* [out] */ COLORREF *lpClr) 
{
  if(lpClr) *lpClr = 0;
	return S_OK;
}

void Vmr9CustomAllocator::FreeDirectxResources()
{
}
void Vmr9CustomAllocator::ReAllocDirectxResources()
{
}