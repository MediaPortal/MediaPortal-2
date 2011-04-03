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

struct SchedulerCallback;

#include "ThreadSafeQueue.h"
#include "EVRPresenter.h"

const MFTIME ONE_SECOND = 10000000; // One second in hns
const LONG   ONE_MSEC = 1000;       // One msec in hns 

class Scheduler
{
public:
  Scheduler();
  virtual ~Scheduler();

  void SetCallback(SchedulerCallback *pCB)
  {
    m_pCB = pCB;
  }

  void SetFrameRate(const MFRatio& fps);
  void SetClockRate(float fRate) { m_fRate = fRate; }

  const LONGLONG& LastSampleTime() const { return m_LastSampleTime; }
  const LONGLONG& FrameDuration() const { return m_PerFrameInterval; }

  HRESULT StartScheduler(IMFClock *pClock);
  HRESULT StopScheduler();

  HRESULT ScheduleSample(IMFSample *pSample, BOOL bPresentNow);
  HRESULT ProcessSamplesInQueue(LONG *plNextSleep);
  bool ProcessSample(LONG *plNextSleep);
  HRESULT Flush();

  // ThreadProc for the scheduler thread.
  static DWORD WINAPI SchedulerThreadProc(LPVOID lpParameter);

  // MFTimeToMsec: Convert 100-ns time to seconds.

  inline LONG MFTimeToMsec(const LONGLONG& time)
  {
    return (LONG)(time / (ONE_SECOND / ONE_MSEC));
  }

private: 
  // non-static version of SchedulerThreadProc.
  DWORD SchedulerThreadProcPrivate();


private:
  ThreadSafeQueue<IMFSample>  m_ScheduledSamples;   // Samples waiting to be presented.

  IMFClock            *m_pClock;  // Presentation clock. Can be NULL.
  SchedulerCallback   *m_pCB;     // Weak reference; do not delete.

  DWORD         m_dwThreadID;
  HANDLE        m_hSchedulerThread;
  HANDLE        m_hThreadReadyEvent;
  HANDLE        m_hFlushEvent;

  float         m_fRate;              // Playback rate.
  MFTIME        m_PerFrameInterval;   // Duration of each frame.
  LONGLONG      m_PerFrame_1_4th;     // 1/4th of the frame duration.
  MFTIME        m_LastSampleTime;     // Most recent sample time.
};


// Defines the callback method to present samples. 
struct SchedulerCallback
{
  virtual HRESULT PresentSample(IMFSample *pSample, LONGLONG llTarget) = 0;
};
