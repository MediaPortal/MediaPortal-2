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
#include <string.h>
#include <atlconv.h>
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
#include <evr.h>
#include <mfapi.h>
#include <mfidl.h>
#include <mferror.h>
#include <objbase.h>
#include <dxva2api.h>
#include "evrcustompresenter.h"
#include <process.h>

#define TIME_LOCK(obj, crit, name)  \
DWORD then = GetTickCount(); \
CAutoLock lock(obj); \
	DWORD diff = GetTickCount() - then; \
	if ( diff >= crit ) { \
	  Log("Critical lock time for %s was %d ms", name, diff ); \
	}
//#define TIME_LOCK(obj, crit, name) CAutoLock lock(obj);


void Log(const char *fmt, ...);
HRESULT __fastcall UnicodeToAnsi(LPCOLESTR pszW, LPSTR* ppszA);


#define LOG_TRACE //Log


void LogIID( REFIID riid ) {
	LPOLESTR str;
	LPSTR astr;
	StringFromIID(riid, &str); 
	UnicodeToAnsi(str, &astr);
	Log("riid: %s", astr);
	CoTaskMemFree(str);
}


void LogGUID( REFGUID guid ) {
	LPOLESTR str;
	LPSTR astr;
	str = (LPOLESTR)CoTaskMemAlloc(200);
	StringFromGUID2(guid, str, 200); 
	UnicodeToAnsi(str, &astr);
	Log("guid: %s", astr);
	CoTaskMemFree(str);
}


//avoid dependency into MFGetService, aparently only availabe on vista
HRESULT MyGetService(IUnknown* punkObject, REFGUID guidService,
    REFIID riid, LPVOID* ppvObject ) 
{
	if ( ppvObject == NULL ) return E_POINTER;
	HRESULT hr;
	IMFGetService* pGetService;
	hr = punkObject->QueryInterface(__uuidof(IMFGetService),
		(void**)&pGetService);
	if ( SUCCEEDED(hr) ) {
		hr = pGetService->GetService(guidService, riid, ppvObject);
		SAFE_RELEASE(pGetService);
	}
	return hr;
}


void CALLBACK TimerCallback(UINT uTimerID, UINT uMsg, DWORD_PTR dwUser, DWORD_PTR dw1, DWORD_PTR dw2)
{
	SchedulerParams *p = (SchedulerParams*)dwUser;
	Log("Callback %d", uTimerID);
	TIME_LOCK(&p->csLock, 3, "TimeCallback");
	if ( p->bDone ) Log("The end is near");
	p->eHasWork.Set();
}


#define MIN(x,y) ((x)<(y))?(x):(y)
//wait for a maximum of 500 ms
#define MAX_WAIT (500)
//if we have at least 10 ms spare time to next frame, get new sample
#define MIN_TIME_TO_PROCESS (10000*10)


UINT CALLBACK WorkerThread(void* param)
{
	SchedulerParams *p = (SchedulerParams*)param;
	while ( true ) 
	{
		p->csLock.Lock();
		if ( p->bDone ) 
		{
			Log("Worker done.");
			p->csLock.Unlock();
			//AvRevertMmThreadCharacteristics(hMmThread);
			return 0;
		}
		
		if ( !p->pPresenter->CheckForInput() ) {
		}
		p->csLock.Unlock();
		LOG_TRACE("Worker sleeping.");
		while ( !p->eHasWork.Wait() );
		LOG_TRACE( "Worker woken up" );
	}
	return -1;
}


UINT CALLBACK SchedulerThread(void* param)
{
	SchedulerParams *p = (SchedulerParams*)param;
	LONGLONG hnsSampleTime = 0;
	DWORD dwTaskIndex;
	MMRESULT lastTimerId = 0;
	DWORD delay = 0;
	/*HANDLE hMmThread;
	hMmThread = AvSetMmThreadCharacteristics("Playback", &dwTaskIndex);
	AvSetMmThreadPriority(hMmThread, AVRT_PRIORITY_HIGH);*/
	while ( true ) 
	{
		//Log("Scheduler callback");
		DWORD now=GetTickCount();
		p->csLock.Lock();
		LOG_TRACE("Scheduler got lock");
		DWORD diff=GetTickCount()-now;
		if ( diff > 10 ) Log("High lock latency in SchedulerThread: %d ms", diff);
		//if ( p->bDone ) Log("Trying to end things, waiting for timers : %d", p->iTimerSet);

		if ( p->bDone ) 
		{
			Log("Scheduler done.");
			if ( lastTimerId > 0 ) timeKillEvent(lastTimerId);
			p->csLock.Unlock();
			//AvRevertMmThreadCharacteristics(hMmThread);
			return 0;
		}
		
		p->pPresenter->CheckForScheduledSample(&hnsSampleTime, delay);
		LOG_TRACE("Got scheduling time: %I64d", hnsSampleTime);
		if ( hnsSampleTime > 0) { 
			//Sleep(hnsSampleTime/10000);
			//wait for a maximum of 500 ms!
			//we try to be 3ms early and let vsync do the rest :) --> TODO better^H^H^H^H^H^H real estimation of next vblank!
			delay = MIN(hnsSampleTime/10000, MAX_WAIT);
		}
		else
		{
			//backup check to avoid starvation (and work around unknown bugs)
			delay = MAX_WAIT;
		} 
		if ( lastTimerId > 0 ) timeKillEvent(lastTimerId);
		if ( delay > 3 ) {
			lastTimerId = timeSetEvent(delay-3,1,
				(LPTIMECALLBACK)(HANDLE)p->eHasWork, 0, TIME_ONESHOT|TIME_KILL_SYNCHRONOUS|TIME_CALLBACK_EVENT_SET);
		}
		else {
			p->eHasWork.Set();
		}
		p->csLock.Unlock();
		while ( !p->eHasWork.Wait() );
		LOG_TRACE( "Scheduler woken up" );
	}
	return -1;
}


EVRCustomPresenter::EVRCustomPresenter(int id, IEVRCallback* pCallback, IDirect3DDevice9* direct3dDevice, HMONITOR monitor)
: m_refCount(1)
{
  m_id=id;
  m_enableFrameSkipping=true;
  m_guiReinitializing=false;
    if (m_pMFCreateVideoSampleFromSurface!=NULL)
    {
        Log("----------v0.37---------------------------");
        m_hMonitor=monitor;
        m_pD3DDev=direct3dDevice;
        HRESULT hr = m_pDXVA2CreateDirect3DDeviceManager9(
            &m_iResetToken, &m_pDeviceManager);
        if ( FAILED(hr) ) {
            Log( "Could not create DXVA2 Device Manager" );
        } else {
            m_pDeviceManager->ResetDevice(direct3dDevice, m_iResetToken);
        }
        m_pCallback=pCallback;
//		m_lInputAvailable = 0;
//		m_bInputAvailable = FALSE;
		m_bendStreaming = FALSE;
		m_state = RENDER_STATE_SHUTDOWN;
        m_bSchedulerRunning = FALSE;
		m_bReallocSurfaces = FALSE;
        m_fRate = 1.0f;
		m_iFreeSamples = 0;
        //TODO: use ZeroMemory
        /*for ( int i=0; i<NUM_SURFACES; i++ ) {
            chains[i] = NULL;
            surfaces[i] = NULL;
            //samples[i] = NULL;
        }*/
    }
}


void EVRCustomPresenter::EnableFrameSkipping(bool onOff)
{
  Log("Evr Enable frame skipping:%d",onOff);
  m_enableFrameSkipping=onOff;
}


EVRCustomPresenter::~EVRCustomPresenter()
{
  Log("Evr dtor");
	StopWorkers();
	ReleaseSurfaces();
	m_pMediaType.Release();
	HRESULT hr;
	m_pDeviceManager =  NULL;
	for ( int i=0; i<NUM_SURFACES; i++ ) 
    m_vFreeSamples[i] = 0;
	Log("evr dtor Done");
}	


HRESULT STDMETHODCALLTYPE EVRCustomPresenter::GetParameters( 
    /* [out] */ __RPC__out DWORD *pdwFlags,
    /* [out] */ __RPC__out DWORD *pdwQueue)
{
	Log("GetParameters");
	return S_OK;
}


HRESULT STDMETHODCALLTYPE EVRCustomPresenter::Invoke( 
    /* [in] */ __RPC__in_opt IMFAsyncResult *pAsyncResult)
{
	Log("Invoke");
	return S_OK;
}


// IUnknown
HRESULT EVRCustomPresenter::QueryInterface(REFIID riid, void** ppvObject)
{
    HRESULT hr = E_NOINTERFACE;
    if( ppvObject == NULL ) {
        hr = E_POINTER;
    } 
	else if( riid == IID_IMFVideoDeviceID) {
		*ppvObject = static_cast<IMFVideoDeviceID*>( this );
        AddRef();
        hr = S_OK;
    } 
	else if( riid == IID_IMFTopologyServiceLookupClient) {
		*ppvObject = static_cast<IMFTopologyServiceLookupClient*>( this );
        AddRef();
        hr = S_OK;
    } 
	else if( riid == IID_IMFVideoPresenter) {
		*ppvObject = static_cast<IMFVideoPresenter*>( this );
        AddRef();
        hr = S_OK;
    } 
	else if( riid == IID_IMFGetService) {
		*ppvObject = static_cast<IMFGetService*>( this );
        AddRef();
        hr = S_OK;
    } 
	else if( riid == IID_IQualProp) {
		*ppvObject = static_cast<IQualProp*>( this );
        AddRef();
        hr = S_OK;
    } 
	else if( riid == IID_IMFRateSupport) {
		*ppvObject = static_cast<IMFRateSupport*>( this );
        AddRef();
        hr = S_OK;
    }
    else if( riid == IID_IMFVideoDisplayControl  ) {
        *ppvObject = static_cast<IMFVideoDisplayControl*>( this );
        AddRef();
        Log( "QueryInterface:IID_IMFVideoDisplayControl:%x",(*ppvObject)  );
        hr = S_OK;
    } 
    else if( riid == IID_IEVRTrustedVideoPlugin  ) {
        *ppvObject = static_cast<IEVRTrustedVideoPlugin*>( this );
        AddRef();
        Log( "QueryInterface:IID_IEVRTrustedVideoPlugin:%x",(*ppvObject)  );
        hr = S_OK;
    } 
    else if( riid == IID_IMFVideoPositionMapper  ) {
        *ppvObject = static_cast<IMFVideoPositionMapper*>( this );
        AddRef();
        hr = S_OK;
    } 
    else if( riid == IID_IUnknown ) {
        *ppvObject = static_cast<IUnknown*>( static_cast<IMFVideoDeviceID*>( this ) );
        AddRef();
        hr = S_OK;    
    }
    else
    {
        LogIID( riid );
        *ppvObject=NULL;
        hr=E_NOINTERFACE;
    }
	if ( FAILED(hr) ) {
    Log( "QueryInterface failed:%x",hr );
	}
    return hr;
}


ULONG EVRCustomPresenter::AddRef()
{
    Log("EVRCustomPresenter::AddRef()");
    return InterlockedIncrement(& m_refCount);
}


ULONG EVRCustomPresenter::Release()
{
    Log("EVRCustomPresenter::Release()");
    ULONG ret = InterlockedDecrement(& m_refCount);
    if( ret == 0 )
    {
        Log("EVRCustomPresenter::Cleanup()");
        delete this;
    }

    return ret;
}


void EVRCustomPresenter::ResetStatistics()
{
  m_bfirstFrame = true;
  m_bfirstInput = true;
  m_iFramesDrawn = 0;
  m_iFramesDropped = 0;
  m_hnsLastFrameTime = 0;
  m_iJitter = 0;
}


HRESULT STDMETHODCALLTYPE EVRCustomPresenter::GetSlowestRate( 
    /* [in] */ MFRATE_DIRECTION eDirection,
    /* [in] */ BOOL fThin,
    /* [out] */ __RPC__out float *pflRate)
{
	Log("GetSlowestRate");
	return S_OK;
}


HRESULT STDMETHODCALLTYPE EVRCustomPresenter::GetFastestRate( 
    /* [in] */ MFRATE_DIRECTION eDirection,
    /* [in] */ BOOL fThin,
    /* [out] */ __RPC__out float *pflRate)
{
	Log("GetFastestRate");
	return S_OK;
}


HRESULT STDMETHODCALLTYPE EVRCustomPresenter::IsRateSupported( 
    /* [in] */ BOOL fThin,
    /* [in] */ float flRate,
    /* [unique][out][in] */ __RPC__inout_opt float *pflNearestSupportedRate)
{
	Log("IsRateSupported");
	return S_OK;
}


HRESULT EVRCustomPresenter::GetDeviceID(IID* pDeviceID)
{
  Log("GetDeviceID");
  if (pDeviceID == NULL)
  {
    return E_POINTER;
  }
  *pDeviceID = __uuidof(IDirect3DDevice9);
  return S_OK;
}


HRESULT EVRCustomPresenter::InitServicePointers(IMFTopologyServiceLookup *pLookup)
{
  Log("InitServicePointers");
  HRESULT hr = S_OK;
  DWORD   cCount = 0;

	//just to make sure....
	ReleaseServicePointers();

  // Ask for the mixer
  cCount = 1;
  hr = pLookup->LookupService(      
      MF_SERVICE_LOOKUP_GLOBAL,   // Not used
      0,                          // Reserved
      MR_VIDEO_MIXER_SERVICE,    // Service to look up
	__uuidof(IMFTransform),         // Interface to look up
      (void**)&m_pMixer,          // Receives the pointer.
      &cCount                     // Number of pointers
      );

	if ( FAILED(hr) ) {
		Log( "ERR: Could not get IMFTransform interface" );
	} else {
		// If there is no clock, cCount is zero.
		Log( "Found mixers: %d", cCount );
		ASSERT(cCount == 0 || cCount == 1);
	}

  // Ask for the clock
  cCount = 1;
  hr = pLookup->LookupService(      
      MF_SERVICE_LOOKUP_GLOBAL,   // Not used
      0,                          // Reserved
      MR_VIDEO_RENDER_SERVICE,    // Service to look up
	__uuidof(IMFClock),         // Interface to look up
      (void**)&m_pClock,          // Receives the pointer.
      &cCount                     // Number of pointers
      );

	if ( FAILED(hr) ) {
    Log( "ERR: Could not get IMFClock interface" );
	} else {
		// If there is no clock, cCount is zero.
		Log( "Found clock: %d", cCount );
		ASSERT(cCount == 0 || cCount == 1);
	}

  // Ask for the event-sink
  cCount = 1;
  hr = pLookup->LookupService(      
      MF_SERVICE_LOOKUP_GLOBAL,   // Not used
      0,                          // Reserved
      MR_VIDEO_RENDER_SERVICE,    // Service to look up
	__uuidof(IMediaEventSink),         // Interface to look up
      (void**)&m_pEventSink,          // Receives the pointer.
      &cCount                     // Number of pointers
      );

	if ( FAILED(hr) ) {
		Log( "ERR: Could not get IMediaEventSink interface" );
	} else {
		// If there is no clock, cCount is zero.
		Log( "Found event sink: %d", cCount );
		ASSERT(cCount == 0 || cCount == 1);
	}

	// TODO: Get other interfaces.
  /* ... */

  return S_OK;
}


HRESULT EVRCustomPresenter::ReleaseServicePointers() 
{
	Log("ReleaseServicePointers");
	//on some channel changes it may happen that ReleaseServicePointers is called only after InitServicePointers is called
	//to avoid this race condition, we only release when not in state begin_streamingi
	m_pMediaType.Release();
	m_pMixer.Release();
	m_pClock.Release();
	m_pEventSink.Release();
	return S_OK;
}


HRESULT EVRCustomPresenter::GetCurrentMediaType(IMFVideoMediaType** ppMediaType)
{
	Log("GetCurrentMediaType");
  HRESULT hr = S_OK;
  //AutoLock lock(m_ObjectLock);  // Hold the critical section.

  if (ppMediaType == NULL)
  {
    return E_POINTER;
  }

  //CHECK_HR(hr = CheckShutdown());

  if (m_pMediaType == NULL)
  {
    CHECK_HR(hr = MF_E_NOT_INITIALIZED, "MediaType is NULL");
  }

  CHECK_HR(hr = m_pMediaType->QueryInterface(
      __uuidof(IMFVideoMediaType), (void**) ppMediaType),
	"Query interface failed in GetCurrentMediaType");

	Log("GetCurrentMediaType done");
  return hr;
}


HRESULT EVRCustomPresenter::TrackSample(IMFSample *pSample)
{
  HRESULT hr = S_OK;
  IMFTrackedSample *pTracked = NULL;

  CHECK_HR(hr = pSample->QueryInterface(__uuidof(IMFTrackedSample), (void**) &pTracked), "Cannot get Interface IMFTrackedSample");
  CHECK_HR(hr = pTracked->SetAllocator(this, NULL), "SetAllocator failed"); 

  SAFE_RELEASE(pTracked);
  return hr;
}


HRESULT EVRCustomPresenter::GetTimeToSchedule(CComPtr<IMFSample> pSample, LONGLONG *phnsDelta) 
{
	LONGLONG hnsPresentationTime = 0; // Target presentation time
	LONGLONG hnsTimeNow = 0;          // Current presentation time
	MFTIME   hnsSystemTime = 0;       // System time
	LONGLONG hnsDelta = 0;
	HRESULT  hr;

	if ( m_pClock == NULL ) {
		*phnsDelta = -1;
		return S_OK;
	}
	// Get the sample's time stamp.
	hr = pSample->GetSampleTime(&hnsPresentationTime);
	// Get the current presentation time.
	// If there is no time stamp, there is no reason to get the clock time.
	if (SUCCEEDED(hr))
	{
		if ( hnsPresentationTime == 0 )
		{
			//immediate presentation
			*phnsDelta = -1;
			return S_OK;
		}
		// This method also returns the system time, which is not used
		// in this example.
		CHECK_HR(hr=m_pClock->GetCorrelatedTime(0, &hnsTimeNow, &hnsSystemTime), "Could not get correlated time!");
	}
	else
	{
		Log("Could not get sample time from %p!", pSample);
		return hr;
	}

	// Calculate the amount of time until the sample's presentation
	// time. A negative value means the sample is late.
	hnsDelta = hnsPresentationTime - hnsTimeNow;
	//if off more than a second
	if (hnsDelta > 100000000 )
	{
		Log("dangerous and unlikely time to schedule [%p]: %I64d. scheduled time: %I64d, now: %I64d",
			pSample, hnsDelta, hnsPresentationTime, hnsTimeNow);
	}
	LOG_TRACE("Calculated delta: %I64d (rate: %f)", hnsDelta, m_fRate);
	if ( m_fRate != 1.0f && m_fRate != 0.0f )
		*phnsDelta = ((float)hnsDelta) / m_fRate;
	else
		*phnsDelta = hnsDelta;
	return hr;
}


HRESULT EVRCustomPresenter::GetAspectRatio(CComPtr<IMFMediaType> pType, int* piARX, int* piARY)
{
	HRESULT hr;
	UINT32 u32;
	if ( SUCCEEDED(pType->GetUINT32(MF_MT_SOURCE_CONTENT_HINT, &u32) ) )
	{
		Log( "Getting aspect ratio 'MediaFoundation style'");
		switch ( u32 )
		{
			case MFVideoSrcContentHintFlag_None:
				Log("Aspect ratio unknown");
				break;
			case MFVideoSrcContentHintFlag_16x9:
				Log("Source is 16:9 within 4:3!");
				*piARX = 16;
				*piARY = 9;
				break;
			case MFVideoSrcContentHintFlag_235_1:
				Log("Source is 2.35:1 within 16:9 or 4:3");
				*piARX = 47;
				*piARY = 20;
				break;
			default:
				Log("Unkown aspect ratio flag: %d", u32);
		}
	}
	else
	{
		//Try old DirectShow-Header, if above does not work
		Log( "Getting aspect ratio 'DirectShow style'");
		AM_MEDIA_TYPE* pAMMediaType;
		CHECK_HR(
			hr = pType->GetRepresentation(FORMAT_VideoInfo2, (void**)&pAMMediaType),
			"Getting DirectShow Video Info failed");
		if ( SUCCEEDED(hr) ) 
		{
			VIDEOINFOHEADER2* vheader = (VIDEOINFOHEADER2*)pAMMediaType->pbFormat;
			*piARX = vheader->dwPictAspectRatioX;
			*piARY = vheader->dwPictAspectRatioY;
			pType->FreeRepresentation(FORMAT_VideoInfo2, (void*)pAMMediaType);
		}
		else
		{
			Log( "Could not get directshow representation.");
		}
	}
	return hr;
}


HRESULT EVRCustomPresenter::SetMediaType(CComPtr<IMFMediaType> pType)
{
	if (pType == NULL) 
	{
		m_pMediaType.Release();
		return S_OK;
	}

	HRESULT hr = S_OK;

	LARGE_INTEGER u64;
//	UINT32 u32;
	
	hr = pType->GetUINT64(MF_MT_FRAME_RATE, (UINT64*)&u64);
	if ( SUCCEEDED(hr) ) {
		Log("Media frame rate: %d / %d", u64.HighPart, u64.LowPart);
	}

	CHECK_HR(pType->GetUINT64(MF_MT_FRAME_SIZE, (UINT64*)&u64), "Getting Framesize failed!");

	MFVideoArea Area;
	UINT32 rSize;
	CHECK_HR(pType->GetBlob(MF_MT_GEOMETRIC_APERTURE, (UINT8*)&Area, sizeof(Area), &rSize), "Failed to get MF_MT_GEOMETRIC_APERTURE");
	m_iVideoWidth = u64.HighPart; //Area.Area.cx; //u64.HighPart;
	m_iVideoHeight = u64.LowPart; //Area.Area.cy; //u64.LowPart;
	//use video size as default value for aspect ratios
	m_iARX = m_iVideoWidth;
	m_iARY = m_iVideoHeight;
	CHECK_HR(GetAspectRatio(pType, &m_iARX, &m_iARY), "Failed to get aspect ratio");
	Log( "New format: %dx%d, Ratio: %d:%d",
		m_iVideoWidth, m_iVideoHeight, m_iARX, m_iARY );

	GUID subtype;
	CHECK_HR(pType->GetGUID(MF_MT_SUBTYPE, &subtype), "Could not get subtype");
	LogGUID( subtype );
	m_pMediaType = pType;
	return S_OK;
}


void EVRCustomPresenter::ReAllocSurfaces()
{
	Log("ReallocSurfaces");

	//TIME_LOCK(this, 20, "ReAllocSurfaces")
	//make sure both threads are paused
	CAutoLock wLock(&m_workerParams.csLock);
	CAutoLock sLock(&m_schedulerParams.csLock);
	ReleaseSurfaces();

	// set the presentation parameters
	D3DPRESENT_PARAMETERS d3dpp;
	ZeroMemory(&d3dpp, sizeof(d3dpp));
	d3dpp.BackBufferWidth = m_iVideoWidth;
	d3dpp.BackBufferHeight = m_iVideoHeight;
	d3dpp.BackBufferCount = 1;
	//TODO check media type for correct format!
	d3dpp.BackBufferFormat = D3DFMT_X8R8G8B8;
	d3dpp.SwapEffect = D3DSWAPEFFECT_DISCARD;
	d3dpp.Windowed = true;
	d3dpp.EnableAutoDepthStencil = false;
	d3dpp.AutoDepthStencilFormat = D3DFMT_X8R8G8B8;
	d3dpp.FullScreen_RefreshRateInHz = D3DPRESENT_RATE_DEFAULT;
	d3dpp.PresentationInterval = D3DPRESENT_INTERVAL_DEFAULT;


	HANDLE hDevice;
	IDirect3DDevice9* pDevice;
	CHECK_HR(m_pDeviceManager->OpenDeviceHandle(&hDevice), "Cannot open device handle");
	CHECK_HR(m_pDeviceManager->LockDevice(hDevice, &pDevice, TRUE), "Cannot lock device");
	HRESULT hr;
	Log("Textures will be %dx%d", m_iVideoWidth, m_iVideoHeight);
  m_iSurfacesAllocated=0;
	for ( int i=0; i < NUM_SURFACES; i++ ) 
  {
		hr = pDevice->CreateTexture(m_iVideoWidth, m_iVideoHeight, 1,D3DUSAGE_RENDERTARGET, D3DFMT_X8R8G8B8, D3DPOOL_DEFAULT,&textures[i], NULL);
		if ( FAILED(hr) )
		{
			Log("Could not create offscreen surface %d. Error 0x%x",i, hr);
      break;
		}
		CHECK_HR( textures[i]->GetSurfaceLevel(0, &surfaces[i]), "Could not get surface from texture");
	
		hr = m_pMFCreateVideoSampleFromSurface(surfaces[i],&samples[i]);
		if (FAILED(hr)) 
    {
			Log("CreateVideoSampleFromSurface failed: 0x%x", hr);
			return;
		}
		Log("Adding sample: %d/%d 0x%x",i,m_iSurfacesAllocated, NUM_SURFACES, samples[i]);
		m_vFreeSamples[i] = samples[i];
		m_iSurfacesAllocated++;
	} 
  
	Log("created %d/%d surfaces",m_iSurfacesAllocated, NUM_SURFACES);
	m_iFreeSamples = m_iSurfacesAllocated;
	CHECK_HR(m_pDeviceManager->UnlockDevice(hDevice, FALSE), "failed: Unlock device");
	Log("Releasing device: %d", pDevice->Release());
	CHECK_HR(m_pDeviceManager->CloseDeviceHandle(hDevice), "failed: CloseDeviceHandle");
	Log("ReallocSurfaces done");
}


HRESULT EVRCustomPresenter::CreateProposedOutputType(IMFMediaType* pMixerType, IMFMediaType** pType)
{
  HRESULT hr;
  LARGE_INTEGER i64Size;

  hr = m_pMFCreateMediaType(pType);
  if (SUCCEEDED (hr))
  {
    CHECK_HR(hr=pMixerType->CopyAllItems(*pType), "failed: CopyAllItems. Could not clone media type" );
    if (SUCCEEDED(hr))
    {
    	Log("Successfully cloned media type");
    }
    (*pType)->SetUINT32 (MF_MT_PAN_SCAN_ENABLED, 0);

    i64Size.HighPart = 800;
    64Size.LowPart	 = 600;
    //(*pType)->SetUINT64 (MF_MT_FRAME_SIZE, i64Size.QuadPart);

    i64Size.HighPart = 1;
    i64Size.LowPart  = 1;
    //(*pType)->SetUINT64 (MF_MT_PIXEL_ASPECT_RATIO, i64Size.QuadPart);

    CHECK_HR((*pType)->GetUINT64(MF_MT_FRAME_SIZE, (UINT64*)&i64Size.QuadPart), "Failed to get MF_MT_FRAME_SIZE");
    Log("Frame size: %dx%d",i64Size.HighPart, i64Size.LowPart); 

    MFVideoArea Area;
    UINT32 rSize;
    /*Log("Would set aperture: %dx%d", VideoFormat->videoInfo.dwWidth, VideoFormat->videoInfo.dwHeight);*/
    ZeroMemory(&Area, sizeof(MFVideoArea));
    //TODO get the real screen size, and calculate area
    //corresponding to the given aspect ratio
    Area.Area.cx = MIN(800, i64Size.HighPart);
    Area.Area.cy = MIN(450, i64Size.LowPart);
    //for hardware scaling, use the following line:
    //(*pType)->SetBlob(MF_MT_GEOMETRIC_APERTURE, (UINT8*)&Area, sizeof(MFVideoArea));
    CHECK_HR((*pType)->GetBlob(MF_MT_GEOMETRIC_APERTURE, (UINT8*)&Area, sizeof(Area), &rSize), "Failed to get MF_MT_GEOMETRIC_APERTURE");
    Log("Aperture size: %x:%x, %dx%d", Area.OffsetX.value, Area.OffsetY.value, Area.Area.cx, Area.Area.cy); 
  }
  return hr;
}


HRESULT EVRCustomPresenter::LogOutputTypes()
{
	Log("--Dumping output types----");
	//CAutoLock lock(this);
  HRESULT hr = S_OK;
  BOOL fFoundMediaType = FALSE;

  CComPtr<IMFMediaType> pMixerType;
  CComPtr<IMFMediaType> pType;

  if (!m_pMixer)
  {
    return MF_E_INVALIDREQUEST;
  }

	//LogMediaTypes(m_pMixer);
  // Loop through all of the mixer's proposed output types.
  DWORD iTypeIndex = 0;
  while (!fFoundMediaType && (hr != MF_E_NO_MORE_TYPES))
  {
    pMixerType.Release();
    pType.Release();
    Log("Testing media type...");
    // Step 1. Get the next media type supported by mixer.
    hr = m_pMixer->GetOutputAvailableType(0, iTypeIndex++, &pMixerType);
    if (FAILED(hr))
    {
    	if ( hr != MF_E_NO_MORE_TYPES )
    		Log("stopping, hr=0x%x!", hr );
      break;
    }
    int arx, ary;
    GetAspectRatio(pMixerType, &arx, &ary);
    Log("Aspect ratio: %d:%d", arx, ary);
    UINT32 interlaceMode;
    pMixerType->GetUINT32(MF_MT_INTERLACE_MODE, &interlaceMode);
  
    Log("Interlace mode: %d", interlaceMode);
    GUID subtype;
    CHECK_HR(pMixerType->GetGUID(MF_MT_SUBTYPE, &subtype), "Could not get subtype");
    LogGUID( subtype );
  }
	Log("---- Dumping output types done ----");
  return S_OK;
}


HRESULT EVRCustomPresenter::RenegotiateMediaOutputType()
{
	CAutoLock wLock(&m_workerParams.csLock);
	CAutoLock sLock(&m_schedulerParams.csLock);
	Log("RenegotiateMediaOutputType");
	LogOutputTypes();
  HRESULT hr = S_OK;
  BOOL fFoundMediaType = FALSE;

  CComPtr<IMFMediaType> pMixerType;
  CComPtr<IMFMediaType> pType;

  if (!m_pMixer)
  {
    return MF_E_INVALIDREQUEST;
  }

	//LogMediaTypes(m_pMixer);
  // Loop through all of the mixer's proposed output types.
  DWORD iTypeIndex = 0;
  while (!fFoundMediaType && (hr != MF_E_NO_MORE_TYPES))
  {
  	pMixerType.Release();
  	pType.Release();
  	Log(  "Testing media type..." );
    // Step 1. Get the next media type supported by mixer.
    hr = m_pMixer->GetOutputAvailableType(0, iTypeIndex++, &pMixerType);
    if (FAILED(hr))
    {
      Log("ERR: Cannot find usable media type!");
      break;
    }
    // Step 2. Check if we support this media type.
    if (SUCCEEDED(hr))
    {
      hr = S_OK; //IsMediaTypeSupported(pMixerType);
    }

    // Step 3. Adjust the mixer's type to match our requirements.
    if (SUCCEEDED(hr))
    {
			//Create a clone of the suggested outputtype
      hr = CreateProposedOutputType(pMixerType, &pType);
			//pType = pMixerType;
    }

    // Step 4. Check if the mixer will accept this media type.
    if (SUCCEEDED(hr))
    {
      hr = m_pMixer->SetOutputType(0, pType, MFT_SET_TYPE_TEST_ONLY);
    }

    // Step 5. Try to set the media type on ourselves.
    if (SUCCEEDED(hr))
    {
			Log( "New media type successfully negotiated!" );
			
      hr = SetMediaType(pType);
			//m_pMediaType = pType;
			if (SUCCEEDED(hr))
			{
				ReAllocSurfaces();
			}
			else
			{
				Log("ERR: Could not set media type on self!");
			}
    }

    // Step 6. Set output media type on mixer.
    if (SUCCEEDED(hr)) 
    {
			Log("Setting media type on mixer");
      hr = m_pMixer->SetOutputType(0, pType, 0);

      // If something went wrong, clear the media type.
      if (FAILED(hr))
      {
        Log( "Could not set output type: 0x%x", hr );
        SetMediaType(NULL);
      }
    }

    if (SUCCEEDED(hr))
    {
      fFoundMediaType = TRUE;
    }
  }
  return hr;
}


HRESULT EVRCustomPresenter::GetFreeSample(CComPtr<IMFSample> &ppSample) 
{
	TIME_LOCK(&m_lockSamples,5,"GetFreeSample");
	//TODO hold lock?
	LOG_TRACE( "Trying to get free sample, size: %d", m_iFreeSamples);
	if ( m_iFreeSamples == 0 ) return E_FAIL;
	m_iFreeSamples--;
	ppSample = m_vFreeSamples[m_iFreeSamples];
	m_vFreeSamples[m_iFreeSamples] = NULL;
	
	return S_OK;
}


void EVRCustomPresenter::Flush()
{
	//CAutoLock wLock(&m_workerParams.csLock);
	//CAutoLock sLock(&m_schedulerParams.csLock);
	CAutoLock sLock(&m_lockSamples);
	CAutoLock ssLock(&m_lockScheduledSamples);
	LOG_TRACE( "Flushing: size=%d", m_vScheduledSamples.size() );
	while ( m_vScheduledSamples.size()>0 )
	{
		CComPtr<IMFSample> pSample = PeekSample();
		if ( pSample != NULL ) 
		{
			PopSample();
			ReturnSample(pSample, FALSE);
		}
	}
}


void EVRCustomPresenter::ReturnSample(CComPtr<IMFSample> pSample, BOOL tryNotify)
{
	//CAutoLock lock(this);
	TIME_LOCK(&m_lockSamples, 5, "ReturnSample")
	LOG_TRACE( "Sample returned: now having %d samples", m_iFreeSamples+1);
	m_vFreeSamples[m_iFreeSamples++] = pSample;
	//todo, if queue was empty, do something?
	if ( m_vScheduledSamples.size() == 0 ) CheckForEndOfStream();
	if ( tryNotify && m_iFreeSamples == 1 && m_bInputAvailable ) NotifyWorker();
}


HRESULT EVRCustomPresenter::PresentSample(CComPtr<IMFSample> pSample)
{
  HRESULT hr = S_OK;
  IMFMediaBuffer* pBuffer = NULL;
  IDirect3DSurface9* pSurface = NULL;
  //IDirect3DSwapChain9* pSwapChain = NULL;
	LOG_TRACE("Presenting sample");
  // Get the buffer from the sample.
	CHECK_HR(hr = pSample->GetBufferByIndex(0, &pBuffer), "failed: GetBufferByIndex");

  CHECK_HR(hr = MyGetService(pBuffer, MR_BUFFER_SERVICE, __uuidof(IDirect3DSurface9), 
      (void**)&pSurface), "failed: MyGetService");
	
  if (pSurface)
  {
    // Get the swap chain from the surface.
    /*CHECK_HR(hr = pSurface->GetContainer(
        __uuidof(IDirect3DSwapChain9),
        (void**)&pSwapChain), "failed: GetContainer");*/

    // Present the swap surface
		DWORD then = GetTickCount();
		CHECK_HR(hr = Paint(pSurface), "failed: Paint");
		DWORD diff = GetTickCount() - then;
		LOG_TRACE("Paint() latency: %d ms", diff);
		// Calculate offset to scheduled time
		if ( m_pClock != NULL ) {
			LONGLONG hnsTimeNow, hnsSystemTime, hnsTimeScheduled;
			m_pClock->GetCorrelatedTime(0, &hnsTimeNow, &hnsSystemTime);

			pSample->GetSampleTime(&hnsTimeScheduled);
			if ( hnsTimeScheduled > 0 )
			{
				LONGLONG deviation = hnsTimeNow - hnsTimeScheduled;
				if ( deviation < 0 ) deviation = -deviation;
				m_hnsTotalDiff += deviation;
			}
			if ( m_hnsLastFrameTime != 0 )
			{
				LONGLONG hnsDiff = hnsTimeNow - m_hnsLastFrameTime;
				//todo: expected: standard deviation!
				m_iJitter = hnsDiff / 10000;
			}
			m_hnsLastFrameTime = hnsTimeNow;
		}
		m_iFramesDrawn++;
    /*CHECK_HR(hr = pSwapChain->Present(NULL, NULL, NULL, NULL, 0), "failed: Present");*/
  }

  SAFE_RELEASE(pBuffer);
  SAFE_RELEASE(pSurface);
  //SAFE_RELEASE(pSwapChain);
  if (hr == D3DERR_DEVICELOST || hr == D3DERR_DEVICENOTRESET)
  {
    // Failed because the device was lost.
		Log("D3DDevice was lost!");
    //hr = S_OK;
    /*HRESULT hrTmp = TestCooperativeLevel();
    if (hrTmp == D3DERR_DEVICENOTRESET)
    {
			Log("Lost device!");
      //HandleLostDevice();
    }*/
  }

	//Log ( "Presented sample, returning %d\n", hr );
  return hr;
}


BOOL EVRCustomPresenter::CheckForInput()
{
	if ( m_guiReinitializing ) return FALSE;
	int counter;
	ProcessInputNotify(&counter);
	//if ( counter == 0 ) Log("Unneccessary call to ProcessInputNotify");
	return counter != 0;
}


HRESULT EVRCustomPresenter::CheckForScheduledSample(LONGLONG *pNextSampleTime, DWORD msLastSleepTime)
{
	HRESULT hr = S_OK;
	int samplesProcessed=0;
	LOG_TRACE("Checking for scheduled sample (size: %d)", m_vScheduledSamples.size());
	if ( m_guiReinitializing ) {
		*pNextSampleTime = 0;
		return S_OK;
	}
	*pNextSampleTime = 0;
	while ( m_vScheduledSamples.size() > 0 ) {
		CComPtr<IMFSample> pSample = PeekSample();
		if ( pSample == NULL ) break;
		if ( m_state == RENDER_STATE_STARTED ) 
		{
			CHECK_HR(hr=GetTimeToSchedule(pSample, pNextSampleTime), "Couldn't get time to schedule!");
			if ( FAILED(hr) ) *pNextSampleTime = 1;
		}
		else if ( m_bfirstFrame )
		{
			*pNextSampleTime = -1; //immediate
		}
		else
		{
			*pNextSampleTime = 0; //not now!
			break;
		}
		LOG_TRACE( "Time to schedule: %I64d", *pNextSampleTime );
		//if we are ahead only 3 ms, present this sample anyway, as the vsync will be waited for anyway
		//else sleep for some time
		if ( *pNextSampleTime > 30000 ) {
			break;
		}
		PopSample();
		samplesProcessed++;
		//skip only if we have a newer sample available
		if ( *pNextSampleTime < -600000  ) {
			if (  m_vScheduledSamples.size() > 0 ) //BREAKS DVD NAVIGATION: || *pNextSampleTime < -1500000 ) 
			{
				//skip!
				m_iFramesDropped++;
			
        if (!m_enableFrameSkipping)
        {
          CHECK_HR(PresentSample(pSample), "PresentSample failed");
        }
        else
        {
				 Log( "skipping frame, behind %I64d ms, last sleep time %d ms.", -*pNextSampleTime/10000, msLastSleepTime );
        }
			}
			else
			{
				//too late, but present anyway
				Log("frame is too late for %I64d ms, last sleep time %d ms.", -*pNextSampleTime/10000, msLastSleepTime );
				CHECK_HR(PresentSample(pSample), "PresentSample failed");
			}
		} 
    else 
    {
			CHECK_HR(PresentSample(pSample), "PresentSample failed");
		}
		*pNextSampleTime = 0;
		ReturnSample(pSample, TRUE);
	}
	//if ( samplesProcessed == 0 ) Log("Useless call to CheckForScheduledSamples");
	//*pNextSampleTime = 0;
	return hr;
}


void EVRCustomPresenter::StartWorkers()
{
	CAutoLock lock(this);
	if ( m_bSchedulerRunning ) return;
	StartThread(&m_hScheduler, &m_schedulerParams, SchedulerThread, &m_uSchedulerThreadId, THREAD_PRIORITY_ABOVE_NORMAL);
	StartThread(&m_hWorker, &m_workerParams, WorkerThread, &m_uWorkerThreadId, THREAD_PRIORITY_ABOVE_NORMAL);
	m_bSchedulerRunning = TRUE;
}


void EVRCustomPresenter::StopWorkers()
{
	Log("Stopping workers...");
	CAutoLock lock(this);
	Log("Threads running : %s", m_bSchedulerRunning?"TRUE":"FALSE");
	if ( !m_bSchedulerRunning ) return;
	EndThread(m_hScheduler, &m_schedulerParams);
	EndThread(m_hWorker, &m_workerParams);
	m_bSchedulerRunning = FALSE;
}


void EVRCustomPresenter::StartThread(PHANDLE handle, SchedulerParams* pParams,
					UINT  (CALLBACK *ThreadProc)(void*), UINT* threadId, int priority)
{
	Log("Starting thread!");
	pParams->pPresenter = this;
	pParams->bDone = FALSE;
	
	*handle = (HANDLE)_beginthreadex(NULL, 0, ThreadProc,
		pParams, 0, threadId);
	Log("Started thread. id: 0x%x, handle: 0x%x", *threadId, *handle);
	SetThreadPriority(*handle, priority);
}


void EVRCustomPresenter::EndThread(HANDLE hThread, SchedulerParams* params)
{
	Log("Ending thread 0x%x, 0x%x", hThread, params);
	params->csLock.Lock();
	Log("Got lock.");
	params->bDone = TRUE;
	Log("Notifying thread...");
	params->eHasWork.Set();
	Log("Set done.");
	params->csLock.Unlock();
	Log("Waiting for thread to end...");
	WaitForSingleObject(hThread, INFINITE);
	Log("Waiting done");
	CloseHandle(hThread);
}


void EVRCustomPresenter::NotifyThread(SchedulerParams* params)
{
	if ( m_bSchedulerRunning ){
		params->eHasWork.Set();
	} else {
		Log("Scheduler is already shut down");
	}
	/*if ( !m_bSchedulerRunning ) {
		Log("ERROR: Scheduler not running!");
		return;
	} 
	m_schedulerParams->eHasWork.Set();*/
}


void EVRCustomPresenter::NotifyScheduler()
{
	LOG_TRACE( "NotifyScheduler()" );
	NotifyThread(&m_schedulerParams);
}


void EVRCustomPresenter::NotifyWorker()
{
	//Log( "NotifyWorker()" );
	NotifyThread(&m_workerParams);
}


BOOL EVRCustomPresenter::PopSample()
{
	CAutoLock lock(&m_lockScheduledSamples);
	LOG_TRACE("Removing scheduled sample, size: %d", m_vScheduledSamples.size());
	if ( m_vScheduledSamples.size() > 0 )
	{
		m_vScheduledSamples.pop();
		return TRUE;
	}
	return FALSE;
}


CComPtr<IMFSample> EVRCustomPresenter::PeekSample()
{
	CAutoLock lock(&m_lockScheduledSamples);
	if ( m_vScheduledSamples.size() == 0 )
	{
		Log("ERR: PeekSample: empty queue!");
		return NULL;
	}
	return m_vScheduledSamples.front();
}


void EVRCustomPresenter::ScheduleSample(CComPtr<IMFSample> pSample)
{
	CAutoLock lock(&m_lockScheduledSamples);
	LOG_TRACE( "Scheduling Sample, size: %d", m_vScheduledSamples.size() );
	m_vScheduledSamples.push(pSample);
	if (m_vScheduledSamples.size() == 1) NotifyScheduler();
}


BOOL EVRCustomPresenter::CheckForEndOfStream()
{
	//CAutoLock lock(this);
	LOG_TRACE("CheckForEndOfStream");
	if ( !m_bendStreaming )
	{
		LOG_TRACE("No message from mixer yet");
		return FALSE;
	}
	//samples pending
	if ( m_vScheduledSamples.size() > 0 )
	{
		LOG_TRACE("Still having scheduled samples");
		return FALSE;
	}
	if ( m_pEventSink ) 
	{
		Log("Sending completion message");
		m_pEventSink->Notify(EC_COMPLETE, (LONG_PTR)S_OK,
		0);
	}
	m_bendStreaming = FALSE;
	return TRUE;
}


HRESULT EVRCustomPresenter::ProcessInputNotify(int* samplesProcessed)
{
	//TIME_LOCK(this, 1, "ProcessInputNotify");
	//TIME_LOCK(&m_lockSamples, 5, "ProcessInputNotify")
	LOG_TRACE("ProcessInputNotify");
	HRESULT hr=S_OK;
	*samplesProcessed = 0;
	if ( m_pClock != NULL ) {
		MFCLOCK_STATE state;
		m_pClock->GetState(0, &state);
		if (state == MFCLOCK_STATE_PAUSED && !m_bfirstInput) 
		{
			Log( "Should not be processing data in pause mode");
			m_bInputAvailable = FALSE;
			return S_OK;
		}
	}
	//try to process as many samples as possible:
	BOOL bhasMoreSamples = true;
	m_bInputAvailable = FALSE;
	do {
		CComPtr<IMFSample> sample;
		hr = GetFreeSample(sample);
		if ( FAILED(hr) ) {
			//Log( "No free sample available" );
			m_bInputAvailable = TRUE;
			//double-checked locking, in case someone freed a sample between the above 2 steps and we would miss notification
			hr = GetFreeSample(sample);
			if ( FAILED(hr) ) {
				LOG_TRACE("Still more input available");
				return S_OK;
			}
			m_bInputAvailable = FALSE;
		}
		if ( m_pMixer == NULL ) return E_POINTER;
		DWORD dwStatus;
		MFT_OUTPUT_DATA_BUFFER outputSamples[1];
		outputSamples[0].dwStreamID = 0; 
		outputSamples[0].dwStatus = 0; 
		outputSamples[0].pSample = sample; 
		outputSamples[0].pEvents = NULL;
		hr = m_pMixer->ProcessOutput(0, 1, outputSamples,
			&dwStatus);
		SAFE_RELEASE(outputSamples[0].pEvents);
//		LONGLONG latency;
		if ( SUCCEEDED( hr ) ) {
			//if ( m_lInputAvailable > 0 ) InterlockedDecrement(&m_lInputAvailable);
			LOG_TRACE("Processoutput succeeded, status: %d", dwStatus);
			//Log("Scheduling sample");
			m_bfirstInput = false;
			*samplesProcessed++;
			ScheduleSample(sample);
		} else {
			ReturnSample(sample, FALSE);
			switch ( hr ) {
				case MF_E_TRANSFORM_NEED_MORE_INPUT:
					//we are done for now
					hr = S_OK;
					bhasMoreSamples = false;
					LOG_TRACE("Need more input...");
					//m_bInputAvailable = FALSE;
					CheckForEndOfStream();
					break;
				case MF_E_TRANSFORM_STREAM_CHANGE:
					Log( "Unhandled: transform_stream_change");
					break;
				case MF_E_TRANSFORM_TYPE_NOT_SET:
					//no errors, just infos why it didn't succeed
					Log( "ProcessOutput: change of type" );
					bhasMoreSamples = FALSE;
					//hr = S_OK;
					hr = RenegotiateMediaOutputType();
					break;
				default:
					Log( "ProcessOutput failed: 0x%x", hr );
			}
			return hr;
		}
	} while ( bhasMoreSamples );
	return hr;
}


HRESULT STDMETHODCALLTYPE EVRCustomPresenter::ProcessMessage( 
            MFVP_MESSAGE_TYPE eMessage,
            ULONG_PTR ulParam)
{
	HRESULT hr = S_OK;
	LOG_TRACE( "Processmessage: %d, %p", eMessage, ulParam );
	switch ( eMessage ) {
		case MFVP_MESSAGE_INVALIDATEMEDIATYPE:
			Log( "Negotiate Media type" );
			//The mixer's output media type is invalid. The presenter should negotiate a new media type with the mixer. See Negotiating Formats.
			hr = RenegotiateMediaOutputType();
			break;

		case MFVP_MESSAGE_BEGINSTREAMING:
			//Streaming has started. No particular action is required by this message, but you can use it to allocate resources.
			Log("ProcessMessage %x", eMessage);
			m_bendStreaming = FALSE;
			m_state = RENDER_STATE_STARTED;
			ResetStatistics();
			StartWorkers();
			break;

		case MFVP_MESSAGE_ENDSTREAMING:
			//Streaming has ended. Release any resources that you allocated in response to the MFVP_MESSAGE_BEGINSTREAMING message.
			Log("ProcessMessage %x", eMessage);
			//m_bendStreaming = TRUE;
			m_state = RENDER_STATE_STOPPED;
			break;

		case MFVP_MESSAGE_PROCESSINPUTNOTIFY:
			//The mixer has received a new input sample and might be able to generate a new output frame. The presenter should call IMFTransform::ProcessOutput on the mixer. See Processing Output.
			//Log("Message 2: %d", m_lInputAvailable);
			//InterlockedIncrement(&m_lInputAvailable);
			NotifyWorker();
			break;

		case MFVP_MESSAGE_ENDOFSTREAM:
			//m_pEventSink->Notify(EC_COMPLETE, (LONG_PTR)S_OK,
			//0);
			//The presentation has ended. See End of Stream.
			Log("ProcessMessage %x", eMessage);
			m_bendStreaming = TRUE;
			CheckForEndOfStream();
			break;

		case MFVP_MESSAGE_FLUSH:
			//The EVR is flushing the data in its rendering pipeline. The presenter should discard any video frames that are scheduled for presentation.
			LOG_TRACE("ProcessMessage %x", eMessage);
			Flush();
			break;

		case MFVP_MESSAGE_STEP:
			//Requests the presenter to step forward N frames. The presenter should discard the next N-1 frames and display the Nth frame. See Frame Stepping.
	Log("ProcessMessage %x", eMessage);
			break;

		case MFVP_MESSAGE_CANCELSTEP:
			//Cancels frame stepping.
	Log("ProcessMessage %x", eMessage);
			break;
		default:
			Log( "ProcessMessage: Unknown: %d", eMessage );
			break;
	}
	if ( FAILED(hr) ) {
		Log( "ProcessMessage failed with 0x%x", hr );
	}
	LOG_TRACE("ProcessMessage done");
	return hr;
}


HRESULT STDMETHODCALLTYPE EVRCustomPresenter::OnClockStart( 
    /* [in] */ MFTIME hnsSystemTime,
    /* [in] */ LONGLONG llClockStartOffset)
{
	Log("OnClockStart");
	m_state = RENDER_STATE_STARTED;
	Flush();
//	m_bInputAvailable = TRUE;
	NotifyWorker();
	NotifyScheduler();
	return S_OK;
}


HRESULT STDMETHODCALLTYPE EVRCustomPresenter::OnClockStop( 
    /* [in] */ MFTIME hnsSystemTime)
{
	Log("OnClockStop");
	m_state = RENDER_STATE_STOPPED;
	return S_OK;
}


HRESULT STDMETHODCALLTYPE EVRCustomPresenter::OnClockPause( 
    /* [in] */ MFTIME hnsSystemTime)
{
	Log("OnClockPause");
	m_bfirstFrame = TRUE;
	m_state = RENDER_STATE_PAUSED;
	return S_OK;
}


HRESULT STDMETHODCALLTYPE EVRCustomPresenter::OnClockRestart( 
    /* [in] */ MFTIME hnsSystemTime)
{
	Log("OnClockRestart");
	m_state = RENDER_STATE_STARTED;
//	m_bInputAvailable = TRUE;
	NotifyScheduler();
	NotifyWorker();
	return S_OK;
}


HRESULT STDMETHODCALLTYPE EVRCustomPresenter::OnClockSetRate( 
    /* [in] */ MFTIME hnsSystemTime,
    /* [in] */ float flRate)
{
	Log("OnClockSetRate: %f", flRate);
	//m_fRate = flRate;
	return S_OK;
}


HRESULT STDMETHODCALLTYPE EVRCustomPresenter::GetService( 
    /* [in] */ REFGUID guidService,
    /* [in] */  REFIID riid,
    /* [iid_is][out] */ LPVOID *ppvObject)
{
	HRESULT hr = MF_E_UNSUPPORTED_SERVICE;
    if( ppvObject == NULL ) {
        hr = E_POINTER;
    } 
	
	else if( riid == __uuidof(IDirect3DDeviceManager9) ) {
		hr = m_pDeviceManager->QueryInterface(riid, (void**)ppvObject);
	}
	else if( riid == IID_IMFVideoDeviceID) {
		*ppvObject = static_cast<IMFVideoDeviceID*>( this );
        AddRef();
        hr = S_OK;
    } 
	else if( riid == IID_IMFClockStateSink) {
		*ppvObject = static_cast<IMFClockStateSink*>( this );
        AddRef();
        hr = S_OK;
    } 
	else if( riid == IID_IMFTopologyServiceLookupClient) {
		*ppvObject = static_cast<IMFTopologyServiceLookupClient*>( this );
        AddRef();
        hr = S_OK;
    } 
	else if( riid == IID_IMFVideoPresenter) {
		*ppvObject = static_cast<IMFVideoPresenter*>( this );
        AddRef();
        hr = S_OK;
    } 
	else if( riid == IID_IMFGetService) {
		*ppvObject = static_cast<IMFGetService*>( this );
        AddRef();
        hr = S_OK;
    } 
	else if( riid == IID_IMFRateSupport) {
		*ppvObject = static_cast<IMFRateSupport*>( this );
        AddRef();
        hr = S_OK;
    } 
	else if( riid == IID_IMFVideoDisplayControl) {
		*ppvObject = static_cast<IMFVideoDisplayControl*>( this );
        AddRef();
        hr = S_OK;
    } 
    else if( riid == IID_IEVRTrustedVideoPlugin  ) {
        *ppvObject = static_cast<IEVRTrustedVideoPlugin*>( this );
        AddRef();
        hr = S_OK;
    } 
    else if( riid == IID_IMFVideoPositionMapper  ) {
        *ppvObject = static_cast<IMFVideoPositionMapper*>( this );
        AddRef();
        hr = S_OK;
    } 
  else
  {
	  LogGUID(guidService);
	  LogIID(riid);
      *ppvObject=NULL;
      hr=E_NOINTERFACE;
  }
	if ( FAILED(hr) || (*ppvObject)==NULL) {
		Log("GetService failed" );
	}
	return hr;
}


void EVRCustomPresenter::ReleaseCallBack()
{
	m_pCallback=NULL;
}


void EVRCustomPresenter::ReleaseSurfaces()
{
	Log("ReleaseSurfaces()");
	CAutoLock lock(this);
	HANDLE hDevice;
	CHECK_HR(m_pDeviceManager->OpenDeviceHandle(&hDevice), "failed opendevicehandle");
	IDirect3DDevice9* pDevice;
	CHECK_HR(m_pDeviceManager->LockDevice(hDevice, &pDevice, TRUE), "failed: lockdevice");
	/*if ( m_pCallback != NULL )
		m_pCallback->PresentImage(0,0,0,0,0);*/
	Flush();
	m_iFreeSamples = 0;
	for ( int i=0; i<NUM_SURFACES; i++ ) 
  {
		//Log("Delete: %d, 0x%x", i, chains[i]);
		samples[i] = NULL;
		surfaces[i] = NULL;
		chains[i] = NULL;
		textures[i] = NULL;
		m_vFreeSamples[i] = NULL;
	}

	/*if (m_pCallback!=NULL)
		m_pCallback->PresentImage(0,0,0,0,0);*/
	m_pDeviceManager->UnlockDevice(hDevice, FALSE);
	Log("Releasing device");
	pDevice->Release();
	m_pDeviceManager->CloseDeviceHandle(hDevice);
	Log("ReleaseSurfaces() done");
}


HRESULT EVRCustomPresenter::Paint(CComPtr<IDirect3DSurface9> pSurface)
{
	try
	{
		if (m_pCallback!=NULL)
		{
			if (pSurface!=NULL)
			{
				HRESULT hr=S_OK;
				DWORD dwPtr;
				void *pContainer = NULL;
				/*pSurface->GetContainer(IID_IDirect3DTexture9,&pContainer);
				if (pContainer!=NULL)
				{
					LPDIRECT3DTEXTURE9 pTexture=(LPDIRECT3DTEXTURE9)pContainer;

					dwPtr=(DWORD)(pTexture);
					if ( dwPtr == 0 ) Log("WARNING: null-texture-pointer!");
					m_pCallback->PresentImage(m_iVideoWidth, m_iVideoHeight, m_iARX,m_iARY,dwPtr);
					if (m_bfirstFrame)
					{
						m_bfirstFrame=false;
						D3DSURFACE_DESC desc;
						pTexture->GetLevelDesc(0,&desc);
						
					}
					pTexture->Release();
					
					return;
				}*/

        /*if (_videoSurface==NULL)
        {  
          D3DDISPLAYMODE mode;
          m_pD3DDev->GetDisplayMode(0,&mode);
          m_pD3DDev->CreateTexture(m_iVideoWidth, m_iVideoHeight,1,D3DUSAGE_RENDERTARGET,mode.Format,D3DPOOL_DEFAULT,&_videoTexture,NULL);
          _videoTexture->GetSurfaceLevel(0,&_videoSurface);
        }
        m_pD3DDev->StretchRect(pSurface,NULL,_videoSurface,NULL,D3DTEXF_NONE);
        
        m_pCallback->PresentImage(m_iVideoWidth, m_iVideoHeight, m_iARX,m_iARY, (DWORD)_videoTexture);
        */
        m_pCallback->PresentSurface(m_iVideoWidth, m_iVideoHeight, m_iARX,m_iARY, (DWORD)(IDirect3DSurface9*) pSurface);
				//dwPtr=(DWORD)(IDirect3DSurface9*)pSurface;
				//m_pCallback->PresentSurface(m_iVideoWidth, m_iVideoHeight, m_iARX,m_iARY,dwPtr);
				if (m_bfirstFrame)
				{
					D3DSURFACE_DESC desc;
					pSurface->GetDesc(&desc);
					m_bfirstFrame=false;
				}
				return hr;
			}
		}
	}
	catch(...)
	{
		Log("vmr9:Paint() invalid exception");
	}
	return E_FAIL;
}


HRESULT STDMETHODCALLTYPE EVRCustomPresenter::get_FramesDroppedInRenderer(int *pcFrames)
{
	if ( pcFrames == NULL ) return E_POINTER;
//	Log("evr:get_FramesDropped: %d", m_iFramesDropped);
	*pcFrames = m_iFramesDropped;
	return S_OK;
}


HRESULT STDMETHODCALLTYPE EVRCustomPresenter::get_FramesDrawn(int *pcFramesDrawn)
{
	if ( pcFramesDrawn == NULL ) return E_POINTER;
//	Log("evr:get_FramesDrawn: %d", m_iFramesDrawn);
	*pcFramesDrawn = m_iFramesDrawn;
	return S_OK;
}


HRESULT STDMETHODCALLTYPE EVRCustomPresenter::get_AvgFrameRate(int *piAvgFrameRate)
{
	//Log("evr:get_AvgFrameRate");
	return S_OK;
}


HRESULT STDMETHODCALLTYPE EVRCustomPresenter::get_Jitter(int *iJitter)
{
	/*Log("evr:get_Jitter: %d, deviation: %d", m_iJitter,
		(int)(m_hnsTotalDiff / m_iFramesDrawn) );*/
	*iJitter = m_iJitter;
	return S_OK;
}


HRESULT STDMETHODCALLTYPE EVRCustomPresenter::get_AvgSyncOffset(int *piAvg)
{
	//Log("evr:get_AvgSyncOffset");
	return S_OK;
}


HRESULT STDMETHODCALLTYPE EVRCustomPresenter::get_DevSyncOffset(int *piDev)
{
	//Log("evr:get_DevSyncOffset");
	return S_OK;
}


STDMETHODIMP EVRCustomPresenter::GetNativeVideoSize( 
    /* [unique][out][in] */  SIZE *pszVideo,
    /* [unique][out][in] */  SIZE *pszARVideo) 
{
  Log("IMFVideoDisplayControl.GetNativeVideoSize()");
  pszVideo->cx=m_iVideoWidth;
  pszVideo->cy=m_iVideoHeight;
  pszARVideo->cx=m_iARX;
  pszARVideo->cy=m_iARY;

  return S_OK;
}


HRESULT STDMETHODCALLTYPE EVRCustomPresenter::GetIdealVideoSize( 
    /* [unique][out][in] */  SIZE *pszMin,
    /* [unique][out][in] */  SIZE *pszMax) 
{
  Log("IMFVideoDisplayControl.GetIdealVideoSize()");
  pszMin->cx=m_iVideoWidth;
  pszMin->cy=m_iVideoHeight;
  pszMax->cx=m_iVideoWidth;
  pszMax->cy=m_iVideoHeight;
  return E_NOTIMPL;
}


HRESULT STDMETHODCALLTYPE EVRCustomPresenter::SetVideoPosition( 
    /* [unique][in] */  const MFVideoNormalizedRect *pnrcSource,
    /* [unique][in] */  const LPRECT prcDest) 
{
  Log("IMFVideoDisplayControl.SetVideoPosition()");
  return E_NOTIMPL;
}


HRESULT STDMETHODCALLTYPE EVRCustomPresenter::GetVideoPosition( 
    /* [out] */  MFVideoNormalizedRect *pnrcSource,
    /* [out] */  LPRECT prcDest) 
{
  
  //Log("IMFVideoDisplayControl.GetVideoPosition()");
  pnrcSource->left=0;
  pnrcSource->top=0;
  pnrcSource->right=m_iVideoWidth;
  pnrcSource->bottom=m_iVideoHeight;
  
  prcDest->left=0;
  prcDest->top=0;
  prcDest->right=m_iVideoWidth;
  prcDest->bottom=m_iVideoHeight;
  return S_OK;
}


HRESULT STDMETHODCALLTYPE EVRCustomPresenter::SetAspectRatioMode( 
    /* [in] */ DWORD dwAspectRatioMode) 
{
  Log("IMFVideoDisplayControl.SetAspectRatioMode()");
  return S_OK;
}


HRESULT STDMETHODCALLTYPE EVRCustomPresenter::GetAspectRatioMode( 
    /* [out] */  DWORD *pdwAspectRatioMode) 
{
  Log("IMFVideoDisplayControl.GetAspectRatioMode()");
  *pdwAspectRatioMode=VMR_ARMODE_NONE;
  return S_OK;
}


HRESULT STDMETHODCALLTYPE EVRCustomPresenter::SetVideoWindow( 
    /* [in] */  HWND hwndVideo) 
{
  Log("IMFVideoDisplayControl.SetVideoWindow()");
  return E_NOTIMPL;
}


HRESULT STDMETHODCALLTYPE EVRCustomPresenter::GetVideoWindow( 
    /* [out] */  HWND *phwndVideo) 
{
  Log("IMFVideoDisplayControl.GetVideoWindow()");
  return S_OK;
}


HRESULT STDMETHODCALLTYPE EVRCustomPresenter::RepaintVideo( void) 
{
  Log("IMFVideoDisplayControl.RepaintVideo()");
  return S_OK;
}


HRESULT STDMETHODCALLTYPE EVRCustomPresenter::GetCurrentImage( 
    /* [out][in] */  BITMAPINFOHEADER *pBih,
    /* [size_is][size_is][out] */ BYTE **pDib,
    /* [out] */  DWORD *pcbDib,
    /* [unique][out][in] */  LONGLONG *pTimeStamp) 
{
  Log("IMFVideoDisplayControl.GetCurrentImage()");
  return E_NOTIMPL;
}


HRESULT STDMETHODCALLTYPE EVRCustomPresenter::SetBorderColor( 
    /* [in] */ COLORREF Clr)
{
  Log("IMFVideoDisplayControl.SetBorderColor()");
  return E_NOTIMPL;
}


HRESULT STDMETHODCALLTYPE EVRCustomPresenter::GetBorderColor( 
    /* [out] */  COLORREF *pClr) 
{
  Log("IMFVideoDisplayControl.GetBorderColor()");
  if(pClr) *pClr = 0;
  return S_OK;
}


HRESULT STDMETHODCALLTYPE EVRCustomPresenter::SetRenderingPrefs( 
    /* [in] */ DWORD dwRenderFlags) 
{
  Log("IMFVideoDisplayControl.SetRenderingPrefs()");
  return S_OK;
}


HRESULT STDMETHODCALLTYPE EVRCustomPresenter::GetRenderingPrefs( 
    /* [out] */  DWORD *pdwRenderFlags) 
{
  Log("IMFVideoDisplayControl.GetRenderingPrefs()");
  return S_OK;
}


HRESULT STDMETHODCALLTYPE EVRCustomPresenter::SetFullscreen( 
    /* [in] */ BOOL fFullscreen) 
{
  Log("IMFVideoDisplayControl.SetFullscreen()");
  return S_OK;
}


HRESULT STDMETHODCALLTYPE EVRCustomPresenter::GetFullscreen( 
    /* [out] */  BOOL *pfFullscreen) 
{
  Log("GetFullscreen()");
  *pfFullscreen=NULL;
  return S_OK;
}


HRESULT STDMETHODCALLTYPE EVRCustomPresenter::IsInTrustedVideoMode (BOOL *pYes)
{
  Log("IEVRTrustedVideoPlugin.IsInTrustedVideoMode()");
  *pYes=TRUE;
  return S_OK;
}


HRESULT STDMETHODCALLTYPE EVRCustomPresenter::CanConstrict (BOOL *pYes)
{
  *pYes=TRUE;
  Log("IEVRTrustedVideoPlugin.CanConstrict()");
  return S_OK;
}


HRESULT STDMETHODCALLTYPE EVRCustomPresenter::SetConstriction(DWORD dwKPix)
{
  Log("IEVRTrustedVideoPlugin.SetConstriction(%d)",dwKPix);
  return S_OK;
}


HRESULT STDMETHODCALLTYPE EVRCustomPresenter::DisableImageExport(BOOL bDisable)
{
  Log("IEVRTrustedVideoPlugin.DisableImageExport(%d)",bDisable);
  return S_OK;
}


HRESULT STDMETHODCALLTYPE EVRCustomPresenter::MapOutputCoordinateToInputStream(float xOut,float yOut,DWORD dwOutputStreamIndex,DWORD dwInputStreamIndex,float* pxIn,float* pyIn)
{
  //Log("IMFVideoPositionMapper.MapOutputCoordinateToInputStream(%f,%f)",xOut,yOut);
  *pxIn=xOut;
  *pyIn=yOut;
  return S_OK;
}


void EVRCustomPresenter::FreeDirectxResources()
{
	Log("FreeDirectxResources");
  if (false==m_guiReinitializing)
  {
	  ReleaseSurfaces();
    m_pEventSink->Notify(EC_DISPLAY_CHANGED,NULL,NULL);
	  CAutoLock wLock(&m_workerParams.csLock);
	  CAutoLock sLock(&m_schedulerParams.csLock);
	  Log("Freeing device");
	//m_pDeviceManager->ResetDevice(NULL, m_iResetToken);
    m_guiReinitializing=true;
  }
}


void EVRCustomPresenter::ReAllocDirectxResources()
{
  if (true==m_guiReinitializing)
  {
 	CAutoLock wLock(&m_workerParams.csLock);
	CAutoLock sLock(&m_schedulerParams.csLock);
	Log("ReAllocDirectxResources");
    m_guiReinitializing=false;
	m_pDeviceManager->ResetDevice(m_pD3DDev, m_iResetToken);
	ReAllocSurfaces();
	NotifyWorker();
	NotifyScheduler();
  }
}
