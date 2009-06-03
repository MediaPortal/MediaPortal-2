#pragma once

#pragma warning(push, 2)
#pragma warning(disable : 4995)

#include <vector>
#include <queue>
#include <dxva2api.h>
#include "IEVRCallback.h"

#pragma warning(pop)

using namespace std;

typedef HRESULT __stdcall TDXVA2CreateDirect3DDeviceManager9(__out UINT* pResetToken,__deref_out IDirect3DDeviceManager9** ppDeviceManager);
extern TDXVA2CreateDirect3DDeviceManager9* m_pDXVA2CreateDirect3DDeviceManager9;

typedef HRESULT __stdcall TMFCreateVideoSampleFromSurface(__in IUnknown* pUnkSurface,__deref_out IMFSample** ppSample);
extern TMFCreateVideoSampleFromSurface* m_pMFCreateVideoSampleFromSurface;

typedef HRESULT __stdcall TMFCreateMediaType(IMFMediaType** ppIMediaType);
extern TMFCreateMediaType* m_pMFCreateMediaType;


#define CHECK_HR(hr, msg) if (FAILED(hr)) Log(msg);
#define SAFE_RELEASE(p) { if(p) { (p)->Release(); (p)=NULL; } }

#define NUM_SURFACES 8

class EVRCustomPresenter;

enum RENDER_STATE
{
	RENDER_STATE_STARTED,
	RENDER_STATE_STOPPED,
	RENDER_STATE_PAUSED,
	RENDER_STATE_SHUTDOWN
};

typedef struct _SchedulerParams
{
	EVRCustomPresenter* pPresenter;
	CCritSec csLock;
	CAMEvent eHasWork;
	BOOL bDone;
} SchedulerParams;

class EVRCustomPresenter : public IMFVideoDeviceID,
	public IMFTopologyServiceLookupClient,
	public IMFVideoPresenter,
	public IMFGetService,
	public IMFAsyncCallback,
	public IQualProp,
	public IMFRateSupport,
	public IMFVideoDisplayControl,
	public IEVRTrustedVideoPlugin,
	public IMFVideoPositionMapper,
	public CCritSec
{

public:
	EVRCustomPresenter(int id, IEVRCallback* callback, IDirect3DDevice9* direct3dDevice, HMONITOR monitor);
  virtual ~EVRCustomPresenter();

	//IQualProp (stub)
  virtual HRESULT STDMETHODCALLTYPE get_FramesDroppedInRenderer(int *pcFrames);
  virtual HRESULT STDMETHODCALLTYPE get_FramesDrawn(int *pcFramesDrawn);     
  virtual HRESULT STDMETHODCALLTYPE get_AvgFrameRate(int *piAvgFrameRate);    
  virtual HRESULT STDMETHODCALLTYPE get_Jitter(int *iJitter);     
  virtual HRESULT STDMETHODCALLTYPE get_AvgSyncOffset(int *piAvg);
  virtual HRESULT STDMETHODCALLTYPE get_DevSyncOffset(int *piDev);

	//IMFAsyncCallback
  virtual HRESULT STDMETHODCALLTYPE GetParameters( 
      /* [out] */ DWORD *pdwFlags,
      /* [out] */ DWORD *pdwQueue);
    
  virtual HRESULT STDMETHODCALLTYPE Invoke( 
      /* [in] */ IMFAsyncResult *pAsyncResult);

	//IMFGetService
	virtual HRESULT STDMETHODCALLTYPE GetService( 
      /* [in] */  REFGUID guidService,
      /* [in] */  REFIID riid,
      /* [iid_is][out] */ LPVOID *ppvObject);

	//IMFVideoDeviceID
	virtual HRESULT STDMETHODCALLTYPE GetDeviceID(IID* pDeviceID);

	//IMFTopologyServiceLookupClient
	virtual HRESULT STDMETHODCALLTYPE InitServicePointers(IMFTopologyServiceLookup *pLookup);
	virtual HRESULT STDMETHODCALLTYPE ReleaseServicePointers();

	//IMFVideoPresenter
	virtual HRESULT STDMETHODCALLTYPE GetCurrentMediaType(IMFVideoMediaType** ppMediaType);
	virtual HRESULT STDMETHODCALLTYPE ProcessMessage(MFVP_MESSAGE_TYPE eMessage, ULONG_PTR ulParam);

	//IMFClockState
  virtual HRESULT STDMETHODCALLTYPE OnClockStart( 
      /* [in] */ MFTIME hnsSystemTime,
      /* [in] */ LONGLONG llClockStartOffset);
  
  virtual HRESULT STDMETHODCALLTYPE OnClockStop( 
      /* [in] */ MFTIME hnsSystemTime);
  
  virtual HRESULT STDMETHODCALLTYPE OnClockPause( 
      /* [in] */ MFTIME hnsSystemTime);
  
  virtual HRESULT STDMETHODCALLTYPE OnClockRestart( 
      /* [in] */ MFTIME hnsSystemTime);
  
  virtual HRESULT STDMETHODCALLTYPE OnClockSetRate( 
      /* [in] */ MFTIME hnsSystemTime,
      /* [in] */ float flRate);


	//IMFRateSupport
  virtual HRESULT STDMETHODCALLTYPE GetSlowestRate( 
      /* [in] */ MFRATE_DIRECTION eDirection,
      /* [in] */ BOOL fThin,
      /* [out] */ float *pflRate);
    
  virtual HRESULT STDMETHODCALLTYPE GetFastestRate( 
      /* [in] */ MFRATE_DIRECTION eDirection,
      /* [in] */ BOOL fThin,
      /* [out] */ float *pflRate);
    
  virtual HRESULT STDMETHODCALLTYPE IsRateSupported( 
      /* [in] */ BOOL fThin,
      /* [in] */ float flRate,
      /* [unique][out][in] */ float *pflNearestSupportedRate);

  virtual HRESULT STDMETHODCALLTYPE GetNativeVideoSize( 
      /* [unique][out][in] */ SIZE *pszVideo,
      /* [unique][out][in] */ SIZE *pszARVideo);
        
  virtual HRESULT STDMETHODCALLTYPE GetIdealVideoSize( 
      /* [unique][out][in] */ SIZE *pszMin,
      /* [unique][out][in] */ SIZE *pszMax);
  
  virtual HRESULT STDMETHODCALLTYPE SetVideoPosition( 
      /* [unique][in] */ const MFVideoNormalizedRect *pnrcSource,
      /* [unique][in] */ const LPRECT prcDest);
  
  virtual HRESULT STDMETHODCALLTYPE GetVideoPosition( 
      /* [out] */ MFVideoNormalizedRect *pnrcSource,
      /* [out] */ LPRECT prcDest);
  
  virtual HRESULT STDMETHODCALLTYPE SetAspectRatioMode( 
      /* [in] */ DWORD dwAspectRatioMode);
  
  virtual HRESULT STDMETHODCALLTYPE GetAspectRatioMode( 
      /* [out] */ DWORD *pdwAspectRatioMode);
  
  virtual HRESULT STDMETHODCALLTYPE SetVideoWindow( 
      /* [in] */ HWND hwndVideo);
  
  virtual HRESULT STDMETHODCALLTYPE GetVideoWindow( 
      /* [out] */ HWND *phwndVideo);
  
  virtual HRESULT STDMETHODCALLTYPE RepaintVideo(void);
  
  virtual HRESULT STDMETHODCALLTYPE GetCurrentImage( 
      /* [out][in] */ BITMAPINFOHEADER *pBih,
      /* [size_is][size_is][out] */ BYTE **pDib,
      /* [out] */  DWORD *pcbDib,
      /* [unique][out][in] */ LONGLONG *pTimeStamp);
  
  virtual HRESULT STDMETHODCALLTYPE SetBorderColor( 
      /* [in] */ COLORREF Clr);
  
  virtual HRESULT STDMETHODCALLTYPE GetBorderColor( 
      /* [out] */ COLORREF *pClr);
  
  virtual HRESULT STDMETHODCALLTYPE SetRenderingPrefs( 
      /* [in] */ DWORD dwRenderFlags);
  
  virtual HRESULT STDMETHODCALLTYPE GetRenderingPrefs( 
      /* [out] */ DWORD *pdwRenderFlags);
  
  virtual HRESULT STDMETHODCALLTYPE SetFullscreen( 
      /* [in] */ BOOL fFullscreen);
  
  virtual HRESULT STDMETHODCALLTYPE GetFullscreen( 
      /* [out] */ BOOL *pfFullscreen);
  
  virtual HRESULT STDMETHODCALLTYPE IsInTrustedVideoMode (BOOL *pYes);
  virtual HRESULT STDMETHODCALLTYPE CanConstrict (BOOL *pYes);
  virtual HRESULT STDMETHODCALLTYPE SetConstriction(DWORD dwKPix);
  virtual HRESULT STDMETHODCALLTYPE DisableImageExport(BOOL bDisable);

  // IUnknown
  virtual HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid, void** ppvObject);

  virtual ULONG STDMETHODCALLTYPE AddRef();
  virtual ULONG STDMETHODCALLTYPE Release();

  // IMFVideoPositionMapper
  virtual HRESULT STDMETHODCALLTYPE MapOutputCoordinateToInputStream(float xOut, float yOut,
      DWORD dwOutputStreamIndex, DWORD dwInputStreamIndex, float* pxIn,float* pyIn);

	//Local
	void ReleaseCallBack();

	HRESULT CheckForScheduledSample(LONGLONG *pNextSampleTime, DWORD msLastSleepTime);
	//returns true if there was some input to be processed
	BOOL CheckForInput();
	HRESULT ProcessInputNotify(int* samplesProcessed);
  void EnableFrameSkipping(bool onOff);
  int Id(){return m_id;}
  void FreeDirectxResources();
  void ReAllocDirectxResources();
  bool m_guiReinitializing;
protected:
  int m_id;
	void ReleaseSurfaces();
	HRESULT Paint(CComPtr<IDirect3DSurface9> pSurface);
	HRESULT SetMediaType(CComPtr<IMFMediaType> pType);
	void ReAllocSurfaces();
	HRESULT LogOutputTypes();
	HRESULT GetAspectRatio(CComPtr<IMFMediaType> pType, int* piARX, int* piARY);
	HRESULT CreateProposedOutputType(IMFMediaType* pMixerType, IMFMediaType** pType);
	HRESULT RenegotiateMediaOutputType();
	BOOL CheckForEndOfStream();
	void StartWorkers();
	void StopWorkers();
	void StartThread(PHANDLE handle, SchedulerParams* pParams,
	    UINT (CALLBACK *ThreadProc)(void*), UINT* threadId, int threadPriority);
	void EndThread(HANDLE hThread, SchedulerParams* params);
	void NotifyThread(SchedulerParams* params);
	void NotifyScheduler();
	void NotifyWorker();
	HRESULT GetTimeToSchedule(CComPtr<IMFSample> pSample, LONGLONG* pDelta);
	void Flush();
	void ScheduleSample(CComPtr<IMFSample> pSample);
	CComPtr<IMFSample> PeekSample();
	BOOL PopSample();
	HRESULT TrackSample(IMFSample *pSample);
	HRESULT GetFreeSample(CComPtr<IMFSample>& ppSample);
	void ReturnSample(CComPtr<IMFSample> pSample, BOOL tryNotify);
	HRESULT PresentSample(CComPtr<IMFSample> pSample);
	void ResetStatistics();

  CComPtr<IDirect3DDevice9> m_pD3DDev;
	IEVRCallback* m_pCallback;
	CComPtr<IDirect3DDeviceManager9> m_pDeviceManager;
	CComPtr<IMediaEventSink> m_pEventSink;
	CComPtr<IMFClock> m_pClock;
	CComPtr<IMFTransform> m_pMixer;
	CComPtr<IMFMediaType> m_pMediaType;
	CComPtr<IDirect3DSwapChain9> chains[NUM_SURFACES];
	CComPtr<IDirect3DTexture9> textures[NUM_SURFACES];
	CComPtr<IDirect3DSurface9> surfaces[NUM_SURFACES];
	CComPtr<IMFSample> samples[NUM_SURFACES];
	CCritSec m_lockSamples;
	CCritSec m_lockScheduledSamples;
	int m_iFreeSamples;
	CComPtr<IMFSample> m_vFreeSamples[NUM_SURFACES];
	queue<CComPtr<IMFSample>> m_vScheduledSamples;
	SchedulerParams m_schedulerParams;
	SchedulerParams m_workerParams;
	BOOL m_bSchedulerRunning;
	HANDLE m_hScheduler;
	HANDLE m_hWorker;
	UINT m_uSchedulerThreadId;
	UINT m_uWorkerThreadId;
	UINT m_iResetToken;
	float m_fRate;
	long m_refCount;
	//int m_surfaceCount;
	HMONITOR m_hMonitor;
	int m_iVideoWidth, m_iVideoHeight;
	int m_iARX, m_iARY;
	double m_fps;
	BOOL m_bfirstFrame;
	BOOL m_bfirstInput;
	BOOL m_bInputAvailable;
	//LONG m_lInputAvailable;
	BOOL m_bendStreaming;
	BOOL m_bReallocSurfaces;
	int m_iFramesDrawn, m_iFramesDropped, m_iJitter;
	LONGLONG m_hnsLastFrameTime, m_hnsTotalDiff;
	RENDER_STATE m_state;
  bool m_enableFrameSkipping;
  int m_iSurfacesAllocated;
};
