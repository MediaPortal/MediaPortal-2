//////////////////////////////////////////////////////////////////////////
//
// Scheduler.cpp: Schedules when video frames are presented.
// 
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO
// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
//
// Copyright (c) Microsoft Corporation. All rights reserved.
//
//
//////////////////////////////////////////////////////////////////////////

#include <windows.h>
#include <mfidl.h>
#include <mferror.h>
#include <mfapi.h>
#include <math.h>
#include <streams.h>  // CAutolock

#include "EVRCustomPresenter.h"


// Messages for the scheduler thread.
enum ScheduleEvent
{
  eTerminate = WM_USER,
  eSchedule = WM_USER + 1,
  eFlush = WM_USER + 2
};

const DWORD SCHEDULER_TIMEOUT = 5000;


// Constructor
Scheduler::Scheduler() :
m_pCB(NULL),
m_pClock(NULL),
m_dwThreadID(0),
m_hSchedulerThread(NULL),
m_hThreadReadyEvent(NULL),
m_hFlushEvent(NULL),
m_fRate(1.0f),
m_LastSampleTime(0),
m_PerFrameInterval(0),
m_PerFrame_1_4th(0)
{
}


// Destructor
Scheduler::~Scheduler()
{
  SAFE_RELEASE(m_pClock);
}


// Specifies the frame rate of the video, in frames per second.
void Scheduler::SetFrameRate(const MFRatio& fps)
{
  UINT64 AvgTimePerFrame = 0;

  // Convert to a duration.
  HRESULT hr = MFFrameRateToAverageTimePerFrame(fps.Numerator, fps.Denominator, &AvgTimePerFrame);

  m_PerFrameInterval = (MFTIME)AvgTimePerFrame;

  // Calculate 1/4th of this value, because we use it frequently.
  m_PerFrame_1_4th = m_PerFrameInterval / 4;
}


// Starts the scheduler's worker thread.
HRESULT Scheduler::StartScheduler(IMFClock *pClock)
{
  if (m_hSchedulerThread != NULL)
  {
    return E_UNEXPECTED;
  }

  HRESULT hr = S_OK;
  DWORD dwID = 0;

  CopyComPointer(m_pClock, pClock);

  // Set a high the timer resolution (ie, short timer period).
  timeBeginPeriod(1);

  // Create an event to wait for the thread to start.
  m_hThreadReadyEvent = CreateEvent(NULL, FALSE, FALSE, NULL);
  if (m_hThreadReadyEvent == NULL)
  {
    hr = HRESULT_FROM_WIN32(GetLastError());
    if (FAILED(hr))
    {
      if (m_hThreadReadyEvent)
      {
        CloseHandle(m_hThreadReadyEvent);
        m_hThreadReadyEvent = NULL;
      }
      return hr;
    }
  }

  // Create an event to wait for flush commands to complete.
  m_hFlushEvent = CreateEvent(NULL, FALSE, FALSE, NULL);
  if (m_hFlushEvent == NULL)
  {
    hr = HRESULT_FROM_WIN32(GetLastError());
    if (FAILED(hr))
    {
      if (m_hThreadReadyEvent)
      {
        CloseHandle(m_hThreadReadyEvent);
        m_hThreadReadyEvent = NULL;
      }
      return hr;
    }
  }

  // Create the scheduler thread.
  m_hSchedulerThread = CreateThread(NULL, 0, SchedulerThreadProc, (LPVOID)this, 0, &dwID);
  if (m_hSchedulerThread == NULL)
  {
    hr = HRESULT_FROM_WIN32(GetLastError());
    if (FAILED(hr))
    {
      if (m_hThreadReadyEvent)
      {
        CloseHandle(m_hThreadReadyEvent);
        m_hThreadReadyEvent = NULL;
      }
      return hr;
    }
  }

  HANDLE hObjects[] = { m_hThreadReadyEvent, m_hSchedulerThread };
  DWORD dwWait = 0;

  // Wait for the thread to signal the "thread ready" event.
  dwWait = WaitForMultipleObjects(2, hObjects, FALSE, INFINITE);  // Wait for EITHER of these handles.
  if (WAIT_OBJECT_0 != dwWait)
  {
    // The thread terminated early for some reason. This is an error condition.
    CloseHandle(m_hSchedulerThread);
    m_hSchedulerThread = NULL;
    hr = E_UNEXPECTED;
    if (FAILED(hr))
    {
      if (m_hThreadReadyEvent)
      {
        CloseHandle(m_hThreadReadyEvent);
        m_hThreadReadyEvent = NULL;
      }
      return hr;
    }
  }

  m_dwThreadID = dwID;

  if (m_hThreadReadyEvent)
  {
    CloseHandle(m_hThreadReadyEvent);
    m_hThreadReadyEvent = NULL;
  }

  return hr;
}


// Stops the scheduler's worker thread.
HRESULT Scheduler::StopScheduler()
{
  if (m_hSchedulerThread == NULL)
  {
    return S_OK;
  }

  // Ask the scheduler thread to exit.
  PostThreadMessage(m_dwThreadID, eTerminate, 0, 0);

  // Wait for the thread to exit.
  WaitForSingleObject(m_hSchedulerThread, INFINITE);

  // Close handles.
  CloseHandle(m_hSchedulerThread);
  m_hSchedulerThread = NULL;

  CloseHandle(m_hFlushEvent);
  m_hFlushEvent = NULL;

  // Discard samples.
  m_ScheduledSamples.Clear();

  // Restore the timer resolution.
  timeEndPeriod(1);

  return S_OK;
}


// Flushes all samples that are queued for presentation.
HRESULT Scheduler::Flush()
{
  if (m_hSchedulerThread == NULL)
  {
    Log("Flush: No Scheduler Thread");
  }

  if (m_hSchedulerThread)
  {
    // Ask the scheduler thread to flush.
    PostThreadMessage(m_dwThreadID, eFlush, 0, 0);

    // Wait for the scheduler thread to signal the flush event,
    // OR for the thread to terminate.
    HANDLE objects[] = { m_hFlushEvent, m_hSchedulerThread };

    WaitForMultipleObjects(ARRAY_SIZE(objects), objects, FALSE, SCHEDULER_TIMEOUT);
  }

  return S_OK;
}


// Schedules a new sample for presentation.
HRESULT Scheduler::ScheduleSample(IMFSample *pSample, BOOL bPresentNow)
{
  if (m_pCB == NULL)
  {
    return MF_E_NOT_INITIALIZED;
  }

  if (m_hSchedulerThread == NULL)
  {
    return MF_E_NOT_INITIALIZED;
  }

  HRESULT hr = S_OK;
  DWORD dwExitCode = 0;

  GetExitCodeThread(m_hSchedulerThread, &dwExitCode);
  if (dwExitCode != STILL_ACTIVE)
  {
    return E_FAIL;
  }

  if (bPresentNow || (m_pClock == NULL))
  {
    // Present the sample immediately.
    m_pCB->PresentSample(pSample, 0);
  }
  else
  {
    // Queue the sample and ask the scheduler thread to wake up.
    hr = m_ScheduledSamples.Enqueue(pSample);

    if (SUCCEEDED(hr))
    {
      PostThreadMessage(m_dwThreadID, eSchedule, 0, 0);
    }
  }

  CHECK_HR(hr, "Scheduler::ScheduleSample failed");

  return hr;
}


// Processes all the samples in the queue.
HRESULT Scheduler::ProcessSamplesInQueue(LONG *plNextSleep)
{
  HRESULT hr = S_OK;
  LONG lWait = 0;
  IMFSample *pSample = NULL;

  // Process samples until the queue is empty or until the wait time > 0.

  while (true)
  {
    // Process the next sample in the queue. If the sample is not ready
    // for presentation. the value returned in lWait is > 0, which
    // means the scheduler should sleep for that amount of time.

    if (!ProcessSample(&lWait))
    {
      break;
    }
    if (lWait > 0)
    {
      break;
    }
  }

  // If the wait time is zero, it means we stopped because the queue is
  // empty (or an error occurred). Set the wait time to infinite; this will
  // make the scheduler thread sleep until it gets another thread message.
  if (lWait == 0)
  {
    lWait = INFINITE;
  }

  *plNextSleep = lWait;
  return hr;
}


// Processes a sample.
bool Scheduler::ProcessSample(LONG *plNextSleep)
{
  HRESULT hr = S_OK;

  LONGLONG hnsPresentationTime = 0;
  LONGLONG hnsTimeNow = 0;
  MFTIME   hnsSystemTime = 0;

  BOOL bPresentNow = TRUE;
  BOOL bIsLate = FALSE;
  LONG lNextSleep = 0;

  IMFSample *pSample;
  // Note: Dequeue returns S_FALSE when the queue is empty.
  hr = m_ScheduledSamples.Dequeue(&pSample);
  if (hr != S_OK)
    return false;

  if (m_pClock)
  {
    // Get the sample's time stamp. It is valid for a sample to
    // have no time stamp.
    hr = pSample->GetSampleTime(&hnsPresentationTime);

    // Get the clock time. (But if the sample does not have a time stamp, 
    // we don't need the clock time.)
    if (SUCCEEDED(hr))
    {
      hr = m_pClock->GetCorrelatedTime(0, &hnsTimeNow, &hnsSystemTime);

      // Calculate the time until the sample's presentation time. 
      // A negative value means the sample is late.
      LONGLONG hnsDelta = hnsPresentationTime - hnsTimeNow;

      // Usually we use 1/4th of frame time for scheduling, except for the case where GUI rendering takes already more than this value.
      LONGLONG hnsCompareThreshold = max(m_PerFrame_1_4th, m_averageFrameRenderDuration);

      if (m_fRate < 0)
      {
        // For reverse playback, the clock runs backward. Therefore the delta is reversed.
        hnsDelta = -hnsDelta;
      }

      if (hnsDelta < 0)
      {
        // This sample is late.
        bIsLate = TRUE;
      }
      // Check if the frame is either about 1/4th of per frame time ahead, or the last averaged render time
      else if (hnsDelta < hnsCompareThreshold)
      {
        // Good time to present
        bPresentNow = TRUE;
      }
      else if (hnsDelta > hnsCompareThreshold)
      {
        // This sample is still too early. Go to sleep.
        lNextSleep = MFTimeToMsec(hnsDelta - hnsCompareThreshold);

        // Adjust the sleep time for the clock rate. (The presentation clock runs
        // at m_fRate, but sleeping uses the system clock.)
        lNextSleep = (LONG)(lNextSleep / fabsf(m_fRate));

        // Don't present yet.
        bPresentNow = FALSE;
      }
    }
  }

  if (bPresentNow)
  {
    LONGLONG startTime = GetCurrentTimestamp();

    hr = m_pCB->PresentSample(pSample, hnsPresentationTime);

    LONGLONG delta = GetCurrentTimestamp() - startTime;

    // Calculate exponential moving average
    m_averageFrameRenderDuration = (m_alpha * delta) + (1.0 - m_alpha) * m_averageFrameRenderDuration;
  }
  else if (!bIsLate)
  {
    // The sample is not ready yet. Return it to the queue.
    hr = m_ScheduledSamples.PutBack(pSample);
  }

  *plNextSleep = lNextSleep;

  SAFE_RELEASE(pSample);
  return true;
}

// ThreadProc for the scheduler thread.
DWORD WINAPI Scheduler::SchedulerThreadProc(LPVOID lpParameter)
{
  Scheduler* pScheduler = reinterpret_cast<Scheduler*>(lpParameter);
  if (pScheduler == NULL)
  {
    return -1;
  }
  return pScheduler->SchedulerThreadProcPrivate();
}


// Non-static version of the ThreadProc.
DWORD Scheduler::SchedulerThreadProcPrivate()
{
  HRESULT hr = S_OK;

  MSG   msg;
  LONG  lWait = INFINITE;
  BOOL  bExitThread = FALSE;

  // Force the system to create a message queue for this thread.
  PeekMessage(&msg, NULL, WM_USER, WM_USER, PM_NOREMOVE);

  // Signal to the scheduler that the thread is ready.
  SetEvent(m_hThreadReadyEvent);

  while (!bExitThread)
  {
    // Wait for a thread message OR until the wait time expires.
    DWORD dwResult = MsgWaitForMultipleObjects(0, NULL, FALSE, lWait, QS_POSTMESSAGE);

    if (dwResult == WAIT_TIMEOUT)
    {
      // If we timed out, then process the samples in the queue
      hr = ProcessSamplesInQueue(&lWait);
      if (FAILED(hr))
      {
        bExitThread = TRUE;
      }
    }

    while (PeekMessage(&msg, NULL, 0, 0, PM_REMOVE))
    {
      BOOL bProcessSamples = TRUE;

      switch (msg.message)
      {
      case eTerminate:
        bExitThread = TRUE;
        break;

      case eFlush:
        // Flushing: Clear the sample queue and set the event.
        m_ScheduledSamples.Clear();
        lWait = INFINITE;
        SetEvent(m_hFlushEvent);
        break;

      case eSchedule:
        // Process as many samples as we can.
        if (bProcessSamples)
        {
          hr = ProcessSamplesInQueue(&lWait);
          if (FAILED(hr))
          {
            bExitThread = TRUE;
          }
          bProcessSamples = (lWait != INFINITE);
        }
        break;
      }
    }
  }

  return (SUCCEEDED(hr) ? 0 : 1);
}


#define ABS64(num) (num >=0 ? num : -num)
#define LowDW(num) ((unsigned __int64)(unsigned long)(num & 0xFFFFFFFFUL))
#define HighDW(num) ((unsigned __int64)(num >> 32))

#pragma warning(disable: 4723)
__int64 _stdcall Scheduler::cMulDiv64(__int64 operant, __int64 multiplier, __int64 divider)
{
  // Declare 128bit storage
  union {
    unsigned long DW[4];
    struct {
      unsigned __int64 LowQW;
      unsigned __int64 HighQW;
    };
  } var128, quotient;
  // Change semantics for intermediate results for Full Div by renaming the vars
#define REMAINDER quotient
#define QUOTIENT var128

  bool negative = ((operant ^ multiplier ^ divider) & 0x8000000000000000LL) != 0;

  // Take absolute values because algorithm is for unsigned only
  operant = ABS64(operant);
  multiplier = ABS64(multiplier);
  divider = ABS64(divider);

  // integer division by zero needs to be handled in the calling method
  if (divider == 0)
  {
    return 1 / divider;
#pragma warning(default: 4723)
  }

  // Multiply
  if (multiplier == 0)
  {
    return 0;
  }

  var128.HighQW = 0;

  if (multiplier == 1)
  {
    var128.LowQW = operant;
  }
  else if (((multiplier | operant) & 0xFFFFFFFF00000000LL) == 0)
  {
    // 32*32 multiply
    var128.LowQW = operant * multiplier;
  }
  else
  {
    // Full multiply: var128 = operant * multiplier
    var128.LowQW = LowDW(operant) * LowDW(multiplier);
    unsigned __int64 tmp = var128.DW[1] + LowDW(operant) * HighDW(multiplier);
    unsigned __int64 tmp2 = tmp + HighDW(operant) * LowDW(multiplier);
    if (tmp2 < tmp)
    {
      var128.DW[3]++;
    }
    var128.DW[1] = LowDW(tmp2);
    var128.DW[2] = HighDW(tmp2);
    var128.HighQW += HighDW(operant) * HighDW(multiplier);
  }

  // Divide
  if (HighDW(divider) == 0)
  {
    if (divider != 1)
    {
      // 32 bit divisor, do 128:32
      quotient.DW[3] = (unsigned long)(var128.DW[3] / divider);
      unsigned __int64 tmp = ((var128.DW[3] % divider) << 32) | var128.DW[2];
      quotient.DW[2] = (unsigned long)(tmp / divider);
      tmp = ((tmp % divider) << 32) | var128.DW[1];
      quotient.DW[1] = (unsigned long)(tmp / divider);
      tmp = ((tmp % divider) << 32) | var128.DW[0];
      quotient.DW[0] = (unsigned long)(tmp / divider);
      var128 = quotient;
    }
  }
  else
  {
    // 64 bit divisor, do full division (128:64)
    int c = 128;
    quotient.LowQW = 0;
    quotient.HighQW = 0;
    do
    {
      REMAINDER.HighQW = (REMAINDER.HighQW << 1) | (REMAINDER.DW[1] >> 31);
      REMAINDER.LowQW = (REMAINDER.LowQW << 1) | (QUOTIENT.DW[3] >> 31);
      QUOTIENT.HighQW = (QUOTIENT.HighQW << 1) | (QUOTIENT.DW[1] >> 31);
      QUOTIENT.LowQW = (QUOTIENT.LowQW << 1);
      if (REMAINDER.HighQW > 0 || REMAINDER.LowQW >= (unsigned __int64)divider)
      {
        if (REMAINDER.LowQW < (unsigned __int64)divider)
        {
          REMAINDER.LowQW -= divider;
          REMAINDER.HighQW--;
        }
        else
        {
          REMAINDER.LowQW -= divider;
        }
        if (++QUOTIENT.LowQW == 0)
        {
          QUOTIENT.HighQW++;
        }
      }
    } while (--c > 0);
  }

  // Apply Sign
  if (negative)
  {
    return -(__int64)var128.LowQW;
  }
  else
  {
    return (__int64)var128.LowQW;
  }
}


LONGLONG Scheduler::GetCurrentTimestamp()
{
  LONGLONG result;
  if (!g_bTimerInitializer)
  {
    CAutoLock lock(&lock);
    DWORD_PTR oldmask = SetThreadAffinityMask(GetCurrentThread(), 1);
    g_bQPCAvail = QueryPerformanceFrequency((LARGE_INTEGER*)&g_lPerfFrequency);
    SetThreadAffinityMask(GetCurrentThread(), oldmask);
    g_bTimerInitializer = true;
    if (g_lPerfFrequency.QuadPart == 0)
    {
      // Bug in HW? Frequency cannot be zero
      g_bQPCAvail = false;
    }
  }
  if (g_bQPCAvail)
  {
    ULARGE_INTEGER tics;
    QueryPerformanceCounter((LARGE_INTEGER*)&tics);
    result = cMulDiv64(tics.QuadPart, 10000000, g_lPerfFrequency.QuadPart); // to keep accuracy
  }
  else
  {
    result = timeGetTime() * 10000; // ms to 100ns units
  }
  return result;
}

