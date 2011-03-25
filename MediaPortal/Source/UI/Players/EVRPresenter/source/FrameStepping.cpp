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

// Gets ready to frame step.
HRESULT EVRCustomPresenter::PrepareFrameStep(DWORD cSteps)
{
  HRESULT hr = S_OK;

  // Cache the step count.
  m_FrameStep.steps += cSteps;

  // Set the frame-step state. 
  m_FrameStep.state = FRAMESTEP_WAITING_START;

  // If the clock is are already running, we can start frame-stepping now. Otherwise, we will start when the clock starts.
  if (m_RenderState == RENDER_STATE_STARTED)
  {
    hr = StartFrameStep();       
  }

  return hr;
}


// If the presenter is waiting to frame-step, this method starts the frame-step operation.
HRESULT EVRCustomPresenter::StartFrameStep()
{
  assert(m_RenderState == RENDER_STATE_STARTED);

  HRESULT hr = S_OK;
  IMFSample *pSample = NULL;

  if (m_FrameStep.state == FRAMESTEP_WAITING_START)
  {
    // We have a frame-step request, and are waiting for the clock to start.
    // Set the state to "pending," which means we are waiting for samples.
    m_FrameStep.state = FRAMESTEP_PENDING;

    // If the frame-step queue already has samples, process them now.
    // We break the loop when the frame-step queue is empty or the frame-step operation is complete
    while (!m_FrameStep.samples.IsEmpty() && (m_FrameStep.state == FRAMESTEP_PENDING))
    {
      hr = m_FrameStep.samples.RemoveFront(&pSample);
      if (FAILED(hr))
      {
        SAFE_RELEASE(pSample)
        CHECK_HR(hr, "EVRCustomPresenter::StartFrameStep VideoSampleList::RemoveFront() failed");
      }

      hr = DeliverFrameStepSample(pSample);
      if (FAILED(hr))
      {
        SAFE_RELEASE(pSample)
        CHECK_HR(hr, "EVRCustomPresenter::StartFrameStep EVRCustomPresenter::DeliverFrameStepSample() failed");
      }

      SAFE_RELEASE(pSample);
    }
  }
  else if (m_FrameStep.state == FRAMESTEP_NONE)
  {
    // We are not frame stepping. Therefore, if the frame-step queue has samples, we need to process them normally.
    while (!m_FrameStep.samples.IsEmpty())
    {
      hr = m_FrameStep.samples.RemoveFront(&pSample);
      if (FAILED(hr))
      {
        SAFE_RELEASE(pSample)
          CHECK_HR(hr, "EVRCustomPresenter::StartFrameStep VideoSampleList::RemoveFront() failed");
      }

      hr = DeliverSample(pSample, FALSE);
      if (FAILED(hr))
      {
        SAFE_RELEASE(pSample)
        CHECK_HR(hr, "EVRCustomPresenter::StartFrameStep EVRCustomPresenter::DeliverSample() failed");
      }

      SAFE_RELEASE(pSample);
    }
  }

  return hr;
}


// Completes a frame-step operation.
HRESULT EVRCustomPresenter::CompleteFrameStep(IMFSample *pSample)
{
  HRESULT hr = S_OK;
  MFTIME hnsSampleTime = 0;
  MFTIME hnsSystemTime = 0;

  // Update our state.
  m_FrameStep.state = FRAMESTEP_COMPLETE;
  m_FrameStep.pSampleNoRef = NULL;

  // Notify the EVR that the frame-step is complete.
  NotifyEvent(EC_STEP_COMPLETE, FALSE, 0); // FALSE = completed (not cancelled)

  // If we are scrubbing (rate == 0), also send the "scrub time" event.
  if (IsScrubbing())
  {
    // Get the time stamp from the sample.
    hr = pSample->GetSampleTime(&hnsSampleTime);
    if (FAILED(hr))
    {
      // No time stamp. Use the current presentation time.
      if (m_pClock)
      {
        hr = m_pClock->GetCorrelatedTime(0, &hnsSampleTime, &hnsSystemTime);
      }
      hr = S_OK; // Not an error condition.
    }

    NotifyEvent(EC_SCRUB_TIME, LODWORD(hnsSampleTime), HIDWORD(hnsSampleTime));
  }

  return hr;
}


// Cancels the frame-step operation.
HRESULT EVRCustomPresenter::CancelFrameStep()
{
  FRAMESTEP_STATE oldState = m_FrameStep.state;

  m_FrameStep.state = FRAMESTEP_NONE;
  m_FrameStep.steps = 0;
  m_FrameStep.pSampleNoRef = NULL;
  // Don't clear the frame-step queue yet, because we might frame step again.

  if (oldState > FRAMESTEP_NONE && oldState < FRAMESTEP_COMPLETE)
  {
    // We were in the middle of frame-stepping when it was cancelled. Notify the EVR.
    NotifyEvent(EC_STEP_COMPLETE, TRUE, 0); // TRUE = cancelled
  }

  return S_OK;
}

// Process a video sample for frame-stepping.
HRESULT EVRCustomPresenter::DeliverFrameStepSample(IMFSample *pSample)
{
  HRESULT hr = S_OK;
  IUnknown *pUnk = NULL;

  // For rate 0, discard any sample that ends earlier than the clock time.
  if (IsScrubbing() && m_pClock && IsSampleTimePassed(m_pClock, pSample))
  {
    // Discard this sample.
  }
  else if (m_FrameStep.state >= FRAMESTEP_SCHEDULED)
  {
    // A frame was already submitted. Put this sample on the frame-step queue, 
    // in case we are asked to step to the next frame. If frame-stepping is
    // cancelled, this sample will be processed normally.
    hr = m_FrameStep.samples.InsertBack(pSample);
    CHECK_HR(hr, "EVRCustomPresenter::DeliverFrameStepSample VideoSampleList::InsertBack() failed");
  }
  else
  {
    // We're ready to frame-step.

    // Decrement the number of steps.
    if (m_FrameStep.steps > 0)
    {
      m_FrameStep.steps--;
    }

    if (m_FrameStep.steps > 0)
    {
      // This is not the last step. Discard this sample.
    }
    else if (m_FrameStep.state == FRAMESTEP_WAITING_START)
    {
      // This is the right frame, but the clock hasn't started yet. Put the
      // sample on the frame-step queue. When the clock starts, the sample
      // will be processed.
      hr = m_FrameStep.samples.InsertBack(pSample);
      CHECK_HR(hr, "EVRCustomPresenter::DeliverFrameStepSample VideoSampleList::InsertBack() failed");
    }
    else
    {
      // This is the right frame *and* the clock has started. Deliver this sample.
      hr = DeliverSample(pSample, FALSE);
      CHECK_HR(hr, "EVRCustomPresenter::DeliverFrameStepSample EVRCustomPresenter::DeliverSample() failed");

      // QI for IUnknown so that we can identify the sample later.
      // (Per COM rules, an object alwayss return the same pointer when QI'ed for IUnknown.)
      hr = pSample->QueryInterface(__uuidof(IUnknown), (void**)&pUnk);
      if (FAILED(hr))
      {
        SAFE_RELEASE(pUnk);
        CHECK_HR(hr, "EVRCustomPresenter::DeliverFrameStempSample IMFSample::QueryInterface() failed");
      }
      // Save this value.
      m_FrameStep.pSampleNoRef = (DWORD_PTR)pUnk; // No add-ref. 

      // NOTE: We do not AddRef the IUnknown pointer, because that would prevent the 
      // sample from invoking the OnSampleFree callback after the sample is presented. 
      // We use this IUnknown pointer purely to identify the sample later; we never
      // attempt to dereference the pointer.

      // Update our state.
      m_FrameStep.state = FRAMESTEP_SCHEDULED;
    }
  }

  SAFE_RELEASE(pUnk);
  return hr;
}

