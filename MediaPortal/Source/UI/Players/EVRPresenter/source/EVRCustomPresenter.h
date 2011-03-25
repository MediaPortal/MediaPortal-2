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

#include <assert.h>
#include <atlbase.h>
#include <dshow.h>
#include <mferror.h>

#include "EVRPresenter.h"
#include "ClassFactory.h"
#include "AsyncCallback.h"
#include "Scheduler.h"
#include "SamplePool.h"
#include "D3DPresentEngine.h"

class EVRCustomPresenter : 
  BaseObject,  
  RefCountedObject, 
  // COM interfaces:
	public IMFVideoPresenter,   // inherits IMFClockStateSink
  public IMFGetService,
  public IMFTopologyServiceLookupClient,
  public IMFVideoDeviceID,
  public IMFRateSupport,
  public IMFVideoPositionMapper,
  public IQualProp,
  public IEVRTrustedVideoPlugin,
  public IMFVideoDisplayControl,
  public IMFAsyncCallback,
  public CCritSec
{

public:
  // Defines the state of the presenter. 
  enum RENDER_STATE
  {
    RENDER_STATE_STARTED = 1,
    RENDER_STATE_STOPPED,
    RENDER_STATE_PAUSED,
    RENDER_STATE_SHUTDOWN,    // Initial state. 
  };

  // Defines the presenter's state with respect to frame-stepping.
  enum FRAMESTEP_STATE
  {
    FRAMESTEP_NONE,             // Not frame stepping.
    FRAMESTEP_WAITING_START,    // Frame stepping, but the clock is not started.
    FRAMESTEP_PENDING,          // Clock is started. Waiting for samples.
    FRAMESTEP_SCHEDULED,        // Submitted a sample for rendering.
    FRAMESTEP_COMPLETE          // Sample was rendered. 
  };


  // IMFVideoPresenter Interface http://msdn.microsoft.com/en-us/library/ms700214(v=VS.85).aspx
  virtual HRESULT STDMETHODCALLTYPE GetCurrentMediaType(IMFVideoMediaType **ppMediaType);
  virtual HRESULT STDMETHODCALLTYPE ProcessMessage(MFVP_MESSAGE_TYPE eMessage, ULONG_PTR ulParam);

  // IMFClockStateSink Interface http://msdn.microsoft.com/en-us/library/ms701593(v=VS.85).aspx
  virtual HRESULT STDMETHODCALLTYPE OnClockPause(MFTIME hnsSystemTime);
  virtual HRESULT STDMETHODCALLTYPE OnClockRestart(MFTIME hnsSystemTime);
  virtual HRESULT STDMETHODCALLTYPE OnClockSetRate(MFTIME hnsSystemTime, float fRate);
  virtual HRESULT STDMETHODCALLTYPE OnClockStart(MFTIME hnsSystemTime, LONGLONG llClockStartOffset);
  virtual HRESULT STDMETHODCALLTYPE OnClockStop(MFTIME hnssSystemTime);

  // IMFGetService Interface http://msdn.microsoft.com/en-us/library/ms694261(v=VS.85).aspx
  virtual HRESULT STDMETHODCALLTYPE GetService(REFGUID guidService, REFIID riid, LPVOID *ppv);

  // IMFTopologyServiceLookupClient Interface http://msdn.microsoft.com/en-us/library/ms703063(v=VS.85).aspx
  virtual HRESULT STDMETHODCALLTYPE InitServicePointers(IMFTopologyServiceLookup *pLookup);
  virtual HRESULT STDMETHODCALLTYPE ReleaseServicePointers();

  // IMFVideoDeviceID Interface http://msdn.microsoft.com/en-us/library/ms703065(v=VS.85).aspx
  virtual HRESULT STDMETHODCALLTYPE GetDeviceID(IID *pDeviceID);

  // IMFRateSupport Interface http://msdn.microsoft.com/en-us/library/ms701858(v=VS.85).aspx
  virtual HRESULT STDMETHODCALLTYPE GetFastestRate(MFRATE_DIRECTION eDirection, BOOL bThin, float *pfRate);
  virtual HRESULT STDMETHODCALLTYPE GetSlowestRate(MFRATE_DIRECTION eDirection, BOOL bThin, float *pfRate);
  virtual HRESULT STDMETHODCALLTYPE IsRateSupported(BOOL bThin, float fRate, float *pfNearestSupportedRate);

  // IMFVideoPositionMapper Interface http://msdn.microsoft.com/en-us/library/ms695386(v=VS.85).aspx
  virtual HRESULT STDMETHODCALLTYPE MapOutputCoordinateToInputStream(float xOut, float yOut, DWORD dwOutputStreamIndex, DWORD dwInputStreamIndex, float *pxIn, float *pyIn);

  // IQualProp Interface http://msdn.microsoft.com/en-us/library/dd376915(v=VS.85).aspx
  virtual HRESULT STDMETHODCALLTYPE get_AvgFrameRate(int *piAvgFrameRate);
  virtual HRESULT STDMETHODCALLTYPE get_AvgSyncOffset(int *piAvg);
  virtual HRESULT STDMETHODCALLTYPE get_DevSyncOffset(int *piDev);
  virtual HRESULT STDMETHODCALLTYPE get_FramesDrawn(int *pcFramesDrawn);
  virtual HRESULT STDMETHODCALLTYPE get_FramesDroppedInRenderer(int *pcFrames);
  virtual HRESULT STDMETHODCALLTYPE get_Jitter(int *piJitter);

  // IEVRTrustedVideoPlugin Interface http://msdn.microsoft.com/en-us/library/aa473784(v=VS.85).aspx
  virtual HRESULT STDMETHODCALLTYPE CanConstrict(BOOL *pYes);
  virtual HRESULT STDMETHODCALLTYPE DisableImageExport(BOOL bDisable);
  virtual HRESULT STDMETHODCALLTYPE IsInTrustedVideoMode(BOOL *pYes);
  virtual HRESULT STDMETHODCALLTYPE SetConstriction(DWORD dwKPix);

  // IMFVideoDisplayControl Interface http://msdn.microsoft.com/en-us/library/ms704002(v=VS.85).aspx
  virtual HRESULT STDMETHODCALLTYPE GetAspectRatioMode(DWORD *pdwAspectRatioMode);
  virtual HRESULT STDMETHODCALLTYPE GetBorderColor(COLORREF *pClr);
  virtual HRESULT STDMETHODCALLTYPE GetCurrentImage(BITMAPINFOHEADER *pBih, BYTE **pDib, DWORD *pcbDib,LONGLONG *pTimeStamp);
  virtual HRESULT STDMETHODCALLTYPE GetFullscreen(BOOL *pfFullscreen);
  virtual HRESULT STDMETHODCALLTYPE GetIdealVideoSize(SIZE *pszMin, SIZE *pszMax);
  virtual HRESULT STDMETHODCALLTYPE GetNativeVideoSize(SIZE *pszVideo, SIZE *pszARVideo);
  virtual HRESULT STDMETHODCALLTYPE GetRenderingPrefs(DWORD *pdwRenderFlags);
  virtual HRESULT STDMETHODCALLTYPE GetVideoPosition(MFVideoNormalizedRect *pnrcSource, LPRECT prcDest);
  virtual HRESULT STDMETHODCALLTYPE GetVideoWindow(HWND *phwndVideo);
  virtual HRESULT STDMETHODCALLTYPE RepaintVideo();
  virtual HRESULT STDMETHODCALLTYPE SetAspectRatioMode(DWORD dwAspectRatioMode);
  virtual HRESULT STDMETHODCALLTYPE SetBorderColor(COLORREF Clr);
  virtual HRESULT STDMETHODCALLTYPE SetFullscreen(BOOL fFullscreen);
  virtual HRESULT STDMETHODCALLTYPE SetRenderingPrefs(DWORD dwRenderFlags);
  virtual HRESULT STDMETHODCALLTYPE SetVideoPosition(const MFVideoNormalizedRect *pnrcSource, const LPRECT prcDest);
  virtual HRESULT STDMETHODCALLTYPE SetVideoWindow(HWND hwndVideo);

  // IMFAsyncCallback Interface http://msdn.microsoft.com/en-us/library/ms699856(v=VS.85).aspx
  virtual HRESULT STDMETHODCALLTYPE GetParameters(DWORD *pdwFlags, DWORD *pdwQueue);
  virtual HRESULT STDMETHODCALLTYPE Invoke(IMFAsyncResult *pAsyncResult);

  // IUnknown Interface http://msdn.microsoft.com/en-us/library/ms680509(v=VS.85).aspx
  virtual HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid, void **ppvObject);
  virtual ULONG STDMETHODCALLTYPE AddRef();
  virtual ULONG STDMETHODCALLTYPE Release();

  EVRCustomPresenter(HRESULT& hr);
  virtual ~EVRCustomPresenter();

protected:
  // The "active" state is started or paused.
  inline BOOL IsActive() const
  {
    return ((m_RenderState == RENDER_STATE_STARTED) || (m_RenderState == RENDER_STATE_PAUSED));
  }

  // Scrubbing occurs when the frame rate is 0.
  inline BOOL IsScrubbing() const
  { 
    return m_fRate == 0.0f;
  }

  // Returns MF_E_SHUTDOWN if the presenter is shutdown.
  inline HRESULT CheckShutdown() const 
  {
    if (m_RenderState == RENDER_STATE_SHUTDOWN)
    {
      return MF_E_SHUTDOWN;
    }
    else
    {
      return S_OK;
    }
  }

  // Mixer operations
  HRESULT EVRCustomPresenter::ConfigureMixer(IMFTransform *pMixer);
  HRESULT EVRCustomPresenter::SetMixerSourceRect(IMFTransform *pMixer, const MFVideoNormalizedRect& nrcSource);

  // Helpers
  void    EVRCustomPresenter::NotifyEvent(long EventCode, LONG_PTR Param1, LONG_PTR Param2);
  float   EVRCustomPresenter::GetMaxRate(BOOL bThin);
  BOOL    EVRCustomPresenter::AreMediaTypesEqual(IMFMediaType *pType1, IMFMediaType *pType2);
  HRESULT EVRCustomPresenter::ValidateVideoArea(const MFVideoArea& area, UINT32 width, UINT32 height);
  RECT    EVRCustomPresenter::CorrectAspectRatio(const RECT& src, const MFRatio& srcPAR, const MFRatio& destPAR);

  // Formats
  HRESULT CreateOptimalVideoType(IMFMediaType* pProposed, IMFMediaType **ppOptimal);
  HRESULT CalculateOutputRectangle(IMFMediaType *pProposed, RECT *prcOutput);
  HRESULT SetMediaType(IMFMediaType *pMediaType);
  HRESULT IsMediaTypeSupported(IMFMediaType *pMediaType);

  // Message Handlers
  HRESULT EVRCustomPresenter::Flush();
  HRESULT EVRCustomPresenter::BeginStreaming();
  HRESULT EVRCustomPresenter::EndStreaming();
  HRESULT EVRCustomPresenter::ProcessInputNotify();
  HRESULT EVRCustomPresenter::CheckEndOfStream();
  HRESULT EVRCustomPresenter::RenegotiateMediaType();

  // Frame Stepping
  HRESULT EVRCustomPresenter::PrepareFrameStep(DWORD cSteps);
  HRESULT EVRCustomPresenter::StartFrameStep();
  HRESULT EVRCustomPresenter::CompleteFrameStep(IMFSample *pSample);
  HRESULT EVRCustomPresenter::CancelFrameStep();
  HRESULT EVRCustomPresenter::DeliverFrameStepSample(IMFSample *pSample);

  // Sample Management
  void    ProcessOutputLoop();   
  HRESULT ProcessOutput();
  HRESULT DeliverSample(IMFSample *pSample, BOOL bRepaint);
  HRESULT TrackSample(IMFSample *pSample);
  void    ReleaseResources();
  HRESULT SetDesiredSampleTime(IMFSample *pSample, const LONGLONG& hnsSampleTime, const LONGLONG& hnsDuration);
  HRESULT ClearDesiredSampleTime(IMFSample *pSample);
  BOOL    EVRCustomPresenter::IsSampleTimePassed(IMFClock *pClock, IMFSample *pSample);
  HRESULT OnSampleFree(IMFAsyncResult *pResult);

  // Callback
  AsyncCallback<EVRCustomPresenter> m_SampleFreeCB;

  // Holds information related to frame-stepping in one varible for better organization
  struct FrameStep
  {
    FrameStep() : state(FRAMESTEP_NONE), steps(0), pSampleNoRef(NULL)
    {
    }

    FRAMESTEP_STATE     state;          // Current frame-step state.
    VideoSampleList     samples;        // List of pending samples for frame-stepping.
    DWORD               steps;          // Number of steps left.
    DWORD_PTR           pSampleNoRef;   // Identifies the frame-step sample.
  };

  RENDER_STATE                m_RenderState;          // render state
  FrameStep                   m_FrameStep;            // Frame-stepping information.


  // Rendering state
  BOOL                        m_bSampleNotify;        // Did the mixer signal it has an input sample?
  BOOL                        m_bRepaint;             // Do we need to repaint the last sample?
  BOOL                        m_bPrerolled;           // Have we presented at least one sample?
  BOOL                        m_bEndStreaming;        // Did we reach the end of the stream?

  // Samples and scheduling
  Scheduler                   m_scheduler;            // Manages scheduling of samples.
  SamplePool                  m_SamplePool;           // Pool of allocated samples.
  DWORD                       m_TokenCounter;         // Counter. Incremented whenever we create new samples.

  MFVideoNormalizedRect       m_nrcSource;            // Source rectangle.
  float                       m_fRate;                // Playback rate.

  // Deletable objects.
  D3DPresentEngine            *m_pD3DPresentEngine;   // Rendering engine. (Never null if the constructor succeeds.)

  // COM Interfaces
  IMFClock                    *m_pClock;              // The EVR's clock
  IMFTransform                *m_pMixer;              // The mixer 
  IMediaEventSink             *m_pMediaEventSink;     // The EVR's event-sink interface.
  IMFMediaType                *m_pMediaType;          // Output media type
};

