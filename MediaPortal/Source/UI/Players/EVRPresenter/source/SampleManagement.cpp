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

#include <mfapi.h>

#include "EVRCustomPresenter.h"

// Get video frames from the mixer and schedule them for presentation.
void EVRCustomPresenter::ProcessOutputLoop()
{
  HRESULT hr = S_OK;

  // Process as many samples as possible.
  while (hr == S_OK)
  {
    // If the mixer doesn't have a new input sample, break from the loop.
    if (!m_bSampleNotify)
    {
      hr = MF_E_TRANSFORM_NEED_MORE_INPUT;
      break;
    }

    // Try to process a sample.
    // ProcessOutput can return S_FALSE to indicate it did not process a sample. If so, we break out of the loop.
    hr = ProcessOutput();
  }

  if (hr == MF_E_TRANSFORM_NEED_MORE_INPUT)
  {
    // The mixer has run out of input data. Check if we're at the end of the stream.
    CheckEndOfStream();
  }
}


// Attempts to get a new output sample from the mixer.
HRESULT EVRCustomPresenter::ProcessOutput()
{
  // Method is called if mixer has a new input sample (m_bSampleNotiy) or on a repaint request (m_bRepaint)
  assert(m_bSampleNotify || m_bRepaint);  

  HRESULT     hr = S_OK;
  DWORD       dwStatus = 0;
  LONGLONG    mixerStartTime = 0;
  LONGLONG    mixerEndTime = 0;
  MFTIME      systemTime = 0;
  BOOL        bRepaint = m_bRepaint; // Temporarily store this state flag.  

  MFT_OUTPUT_DATA_BUFFER dataBuffer;
  ZeroMemory(&dataBuffer, sizeof(dataBuffer));

  IMFSample *pSample = NULL;

  // If the clock is not running, we present the first sample, and then don't present any more until the clock starts. 
  if ((m_RenderState != RENDER_STATE_STARTED) && !m_bRepaint && m_bPrerolled)
  {
    return S_FALSE;
  }

  // Make sure we have a pointer to the mixer.
  CheckPointer(m_pMixer, MF_E_INVALIDREQUEST);

  // Try to get a free sample from the video sample pool.
  hr = m_SamplePool.GetSample(&pSample);
  if (hr == MF_E_SAMPLEALLOCATOR_EMPTY)
  {
    return S_FALSE; // No free samples. We'll try again when a sample is released.
  }
  if (FAILED(hr))
  {
    Log("EVRCustomPresenter::ProcessOutput SamplePool::GetSample() failed");
    SAFE_RELEASE(dataBuffer.pEvents);
    SAFE_RELEASE(pSample);
    return hr;
  }

  // From now on, we have a valid video sample pointer, where the mixer will write the video data.
  assert(pSample != NULL);

  // (If the following assertion fires, it means we are not managing the sample pool correctly.)
  assert(MFGetAttributeUINT32(pSample, MFSamplePresenter_SampleCounter, (UINT32)-1) == m_TokenCounter);

  // Repaint request. Ask the mixer for the most recent sample.
  if (m_bRepaint)
  {
    SetDesiredSampleTime(pSample, m_scheduler.LastSampleTime(), m_scheduler.FrameDuration());
    m_bRepaint = FALSE; // OK to clear this flag now.   
  }
  else
  {
    // Not a repaint request. Clear the desired sample time; the mixer will give us the next frame in the stream.
    ClearDesiredSampleTime(pSample);

    // Latency: Record the starting time for the ProcessOutput operation. 
    if (m_pClock)
    {
      (void)m_pClock->GetCorrelatedTime(0, &mixerStartTime, &systemTime);
    }
  }

  // Now we are ready to get an output sample from the mixer. 
  dataBuffer.dwStreamID = 0;
  dataBuffer.pSample = pSample;
  dataBuffer.dwStatus = 0;

  hr = m_pMixer->ProcessOutput(0, 1, &dataBuffer, &dwStatus);

  if (FAILED(hr))
  {
    // Return the sample to the pool.
    HRESULT hr2 = m_SamplePool.ReturnSample(pSample);
    if (FAILED(hr2) && (hr == hr2))
    {
      Log("EVRCustomPresenter::ProcessOutput SamplePool::ReturnSample() failed");
      SAFE_RELEASE(dataBuffer.pEvents);
      SAFE_RELEASE(pSample);
      return hr;
    }
   
    // Handle some known error codes from ProcessOutput.

    if (hr == MF_E_TRANSFORM_TYPE_NOT_SET)
    {
      // The mixer's format is not set. Negotiate a new format.
      hr = RenegotiateMediaType();
    }
    else if (hr == MF_E_TRANSFORM_STREAM_CHANGE)
    {
      // There was a dynamic media type change. Clear our media type.
      SetMediaType(NULL);
    }
    else if (hr == MF_E_TRANSFORM_NEED_MORE_INPUT)
    {
      // The mixer needs more input.  We have to wait for the mixer to get more input.
      m_bSampleNotify = FALSE; 
    }
  }
  else
  {
    // We got an output sample from the mixer.
    if (m_pClock && !bRepaint)
    {
      // Latency: Record the ending time for the ProcessOutput operation, and notify the EVR of the latency. 
      (void)m_pClock->GetCorrelatedTime(0, &mixerEndTime, &systemTime);

      LONGLONG latencyTime = mixerEndTime - mixerStartTime;
      NotifyEvent(EC_PROCESSING_LATENCY, (LONG_PTR)&latencyTime, 0);
    }

    // Set up notification for when the sample is released.
    hr = TrackSample(pSample);
    if (FAILED(hr))
    {
      Log("EVRCustomPresenter::ProcessOutput EVRCustomPresenter::TrackSample() failed");
      SAFE_RELEASE(dataBuffer.pEvents);
      SAFE_RELEASE(pSample);
      return hr;
    }

    // Schedule the sample.
    if ((m_FrameStep.state == FRAMESTEP_NONE) || bRepaint)
    {
      hr = DeliverSample(pSample, bRepaint);
      if (FAILED(hr))
      {
        Log("EVRCustomPresenter::ProcessOutput EVRCustomPresenter::DeliverSample() failed");
        SAFE_RELEASE(dataBuffer.pEvents);
        SAFE_RELEASE(pSample);
        return hr;
      }
    }
    else
    {
      // We are frame-stepping (and this is not a repaint request).
      hr = DeliverFrameStepSample(pSample);
      if (FAILED(hr))
      {
        Log("EVRCustomPresenter::ProcessOutput EVRCustomPresenter::DeliverFrameStepSample() failed");
        SAFE_RELEASE(dataBuffer.pEvents);
        SAFE_RELEASE(pSample);
        return hr;
      }
    }
    m_bPrerolled = TRUE; // We have presented at least one sample now.
  }

  SAFE_RELEASE(dataBuffer.pEvents);
  SAFE_RELEASE(pSample);

  return hr;
}


// Schedule a video sample for presentation.
HRESULT EVRCustomPresenter::DeliverSample(IMFSample *pSample, BOOL bRepaint)
{
  assert(pSample != NULL);

  HRESULT hr = S_OK;
  D3DPresentEngine::DeviceState state = D3DPresentEngine::DeviceOK;

  // If we are not actively playing, OR we are scrubbing (rate = 0) OR this is a repaint request, 
  // then we need to present the sample immediately. Otherwise, schedule it normally.
  BOOL bPresentNow = ((m_RenderState != RENDER_STATE_STARTED) || IsScrubbing() || bRepaint);

  // Check the D3D device state.
  hr = m_pD3DPresentEngine->CheckDeviceState(&state);
  if (SUCCEEDED(hr))
  {
    hr = m_scheduler.ScheduleSample(pSample, bPresentNow);
  }

  if (FAILED(hr))
  {
    // Notify the EVR that we have failed during streaming. The EVR will notify the pipeline (ie, it will 
    // notify the Filter Graph Manager in DirectShow or the Media Session in Media Foundation).
    NotifyEvent(EC_ERRORABORT, hr, 0);
  }
  else if (state == D3DPresentEngine::DeviceReset)
  {
    // The Direct3D device was re-set. Notify the EVR.
    NotifyEvent(EC_DISPLAY_CHANGED, S_OK, 0);
  }

  return hr;
}


// Given a video sample, sets a callback that is invoked when the sample is no longer in use. 
HRESULT EVRCustomPresenter::TrackSample(IMFSample *pSample)
{
  HRESULT hr = S_OK;
  IMFTrackedSample *pTracked = NULL;

  hr = pSample->QueryInterface(__uuidof(IMFTrackedSample), (void**)&pTracked);
  if (FAILED(hr))
  {
    Log("EVRCustomPresenter::TrackSample IMFSample::QueryInterface() failed");
    SAFE_RELEASE(pTracked);
    return hr;
  }
  
  hr = pTracked->SetAllocator(&m_SampleFreeCB, NULL);
  if (FAILED(hr))
  {
    Log("EVRCustomPresenter::TrackSample IMFTrackedSample::SetAllocator() failed");
    SAFE_RELEASE(pTracked);
    return hr;
  }

  SAFE_RELEASE(pTracked);
  return hr;
}


// Releases resources that the presenter uses to render video. 
void EVRCustomPresenter::ReleaseResources()
{
  // Note: This method flushes the scheduler queue and releases the video samples.
  // It does not release helper objects such as the D3DPresentEngine, or free
  // the presenter's media type.

  // Increment the token counter to indicate that all existing video samples
  // are "stale." As these samples get released, we'll dispose of them. 
  //
  // Note: The token counter is required because the samples are shared
  // between more than one thread, and they are returned to the presenter 
  // through an asynchronous callback (OnSampleFree). Without the token, we
  // might accidentally re-use a stale sample after the ReleaseResources
  // method returns.

  m_TokenCounter++;

  Flush();

  m_SamplePool.Clear();

  m_pD3DPresentEngine->ReleaseResources();
}


// Sets the "desired" sample time on a sample. This tells the mixer to output an earlier frame, not the next frame.
HRESULT EVRCustomPresenter::SetDesiredSampleTime(IMFSample *pSample, const LONGLONG& hnsSampleTime, const LONGLONG& hnsDuration)
{
  CheckPointer(pSample, E_POINTER);

  HRESULT hr = S_OK;
  IMFDesiredSample *pDesired = NULL;

  hr = pSample->QueryInterface(__uuidof(IMFDesiredSample), (void**)&pDesired);
  if (SUCCEEDED(hr))
  {
    // This method has no return value.
    (void)pDesired->SetDesiredSampleTimeAndDuration(hnsSampleTime, hnsDuration);
  }

  SAFE_RELEASE(pDesired);
  return hr;
}


// Clears the desired sample time.
HRESULT EVRCustomPresenter::ClearDesiredSampleTime(IMFSample *pSample)
{
  CheckPointer(pSample, E_POINTER);

  HRESULT hr = S_OK;
    
  IMFDesiredSample *pDesired = NULL;
  IUnknown *pUnkSwapChain = NULL;
    
  // We store some custom attributes on the sample, so we need to cache them and reset them.
  // This works around the fact that IMFDesiredSample::Clear() removes all of the attributes from the sample. 

  UINT32 counter = MFGetAttributeUINT32(pSample, MFSamplePresenter_SampleCounter, (UINT32)-1);

  (void)pSample->GetUnknown(MFSamplePresenter_SampleSwapChain, IID_IUnknown, (void**)&pUnkSwapChain);

  hr = pSample->QueryInterface(__uuidof(IMFDesiredSample), (void**)&pDesired);
  if (SUCCEEDED(hr))
  {
    // This method has no return value.
    (void)pDesired->Clear();
    
    hr = pSample->SetUINT32(MFSamplePresenter_SampleCounter, counter);
    if (FAILED(hr))
    {
      Log("EVRCustomPresenter::ClerDesiredSampleTime IMFSample::SetUINT32() failed");
      SAFE_RELEASE(pUnkSwapChain);
      SAFE_RELEASE(pDesired);
      return hr;
    }
      
    if (pUnkSwapChain)
    {
      hr = pSample->SetUnknown(MFSamplePresenter_SampleSwapChain, pUnkSwapChain);
      if (FAILED(hr))
      {
        Log("EVRCustomPresenter::ClerDesiredSampleTime IMFSample::SetUINT32() failed");
        SAFE_RELEASE(pUnkSwapChain);
        SAFE_RELEASE(pDesired);
        return hr;
      }
    }
  }

 SAFE_RELEASE(pUnkSwapChain);
 SAFE_RELEASE(pDesired);

 return hr;
}


// Returns TRUE if the entire duration of pSample is in the past.
BOOL EVRCustomPresenter::IsSampleTimePassed(IMFClock *pClock, IMFSample *pSample)
{
  assert(pClock != NULL);
  assert(pSample != NULL);

  CheckPointer(pClock, E_POINTER);
  CheckPointer(pSample, E_POINTER);

  HRESULT hr = S_OK;
  MFTIME hnsTimeNow = 0;
  MFTIME hnsSystemTime = 0;
  MFTIME hnsSampleStart = 0;
  MFTIME hnsSampleDuration = 0;

  // The sample might lack a time-stamp or a duration, and the clock might not report a time.

  hr = pClock->GetCorrelatedTime(0, &hnsTimeNow, &hnsSystemTime);

  if (SUCCEEDED(hr))
  {
    hr = pSample->GetSampleTime(&hnsSampleStart);
  }

  if (SUCCEEDED(hr))
  {
    hr = pSample->GetSampleDuration(&hnsSampleDuration);
  }

  if (SUCCEEDED(hr))
  {
    if (hnsSampleStart + hnsSampleDuration < hnsTimeNow)
    {
      return TRUE; 
    }
  }

  return FALSE;
}


// Callback that is invoked when a sample is released.
HRESULT EVRCustomPresenter::OnSampleFree(IMFAsyncResult *pResult)
{
  HRESULT hr = S_OK;
  IUnknown *pObject = NULL;
  IMFSample *pSample = NULL;
  IUnknown *pUnk = NULL;

  // Get the sample from the async result object.
  hr = pResult->GetObject(&pObject);
  if (FAILED(hr))
  {
    Log("EVRCustomPresenter::OnSampleFree IMFAsyncResult::GetObject() failed");
    SAFE_RELEASE(pObject);
    NotifyEvent(EC_ERRORABORT, hr, 0);
    return hr;
  }

  hr = pObject->QueryInterface(__uuidof(IMFSample), (void**)&pSample);
  if (FAILED(hr))
  {
    Log("EVRCustomPresenter::OnSampleFree IUnknown::QueryInterface() failed");
    SAFE_RELEASE(pObject);
    SAFE_RELEASE(pSample);
    NotifyEvent(EC_ERRORABORT, hr, 0);
    return hr;
  }

  // If this sample was submitted for a frame-step, then the frame step is complete.
  if (m_FrameStep.state == FRAMESTEP_SCHEDULED) 
  {
    // QI the sample for IUnknown and compare it to our cached value.
    hr = pSample->QueryInterface(__uuidof(IMFSample), (void**)&pUnk);
    if (FAILED(hr))
    {
      Log("EVRCustomPresenter::OnSampleFree IMFsample::QueryInterface() failed");
      SAFE_RELEASE(pObject);
      SAFE_RELEASE(pSample);
      SAFE_RELEASE(pUnk);
      NotifyEvent(EC_ERRORABORT, hr, 0);
      return hr;
    }
 
    if (m_FrameStep.pSampleNoRef == (DWORD_PTR)pUnk)
    {
      // Notify the EVR. 
      hr = CompleteFrameStep(pSample);
      if (FAILED(hr))
      {
        Log("EVRCustomPresenter::OnSampleFree EVRCustomPresenter::CompleteFrameStep() failed");
        SAFE_RELEASE(pObject);
        SAFE_RELEASE(pSample);
        SAFE_RELEASE(pUnk);
        NotifyEvent(EC_ERRORABORT, hr, 0);
        return hr;
      }
    }

    // Note: Although pObject is also an IUnknown pointer, it's not guaranteed to be the 
    // exact pointer value returned via QueryInterface, hence the need for the second QI.
  }

  {
    CAutoLock lock(this);
    
    if (MFGetAttributeUINT32(pSample, MFSamplePresenter_SampleCounter, (UINT32)-1) == m_TokenCounter)
    {
      // Return the sample to the sample pool.
      hr = m_SamplePool.ReturnSample(pSample);
      if (FAILED(hr))
      {
        Log("EVRCustomPresenter::OnSampleFree VideoSampleList::ResturnSample() failed");
        SAFE_RELEASE(pObject);
        SAFE_RELEASE(pSample);
        SAFE_RELEASE(pUnk);
        NotifyEvent(EC_ERRORABORT, hr, 0);
        return hr;
      }

      // Now that a free sample is available, process more data if possible.
      (void)ProcessOutputLoop();
    }
  }
 
  SAFE_RELEASE(pObject);
  SAFE_RELEASE(pSample);
  SAFE_RELEASE(pUnk);

  return hr;
}

