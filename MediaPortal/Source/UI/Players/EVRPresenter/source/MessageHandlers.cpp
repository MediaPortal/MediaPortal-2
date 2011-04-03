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

// Flushes any samples that are waiting to be presented.
HRESULT EVRCustomPresenter::Flush()
{
  Log("EVRCustomPresenter::Flush");

  m_bPrerolled = FALSE;

  // The scheduler might have samples that are waiting for
  // their presentation time. Tell the scheduler to flush.

  // This call blocks until the scheduler threads discards all scheduled samples.
  m_scheduler.Flush();

  // Flush the frame-step queue.
  m_FrameStep.samples.Clear();

  if (m_RenderState == RENDER_STATE_STOPPED)
  {
      // Repaint with black.
      (void)m_pD3DPresentEngine->PresentSample(NULL, 0);
  }

  return S_OK; 
}


// Called when streaming begins.
HRESULT EVRCustomPresenter::BeginStreaming()
{
  Log("EVRCustomPresenter::BeginStreaming");

  HRESULT hr = S_OK;

  // Start the scheduler thread. 
  hr = m_scheduler.StartScheduler(m_pClock);

  return hr;
}


// Called when streaming ends.
HRESULT EVRCustomPresenter::EndStreaming()
{
  Log("EVRCustomPresenter::EndStreaming");

  HRESULT hr = S_OK;
    
  // Stop the scheduler thread.
  hr = m_scheduler.StopScheduler();

  return hr;
}


// Attempts to get a new output sample from the mixer.
HRESULT EVRCustomPresenter::ProcessInputNotify()
{
  HRESULT hr = S_OK;

  // Set the flag that says the mixer has a new sample.
  m_bSampleNotify = TRUE;

  if (m_pMediaType == NULL)
  {
    // We don't have a valid media type yet.
    hr = MF_E_TRANSFORM_TYPE_NOT_SET;
  }
  else
  {
    // Try to process an output sample.
    ProcessOutputLoop();
  }
 
  return hr;
}


// Performs end-of-stream actions if the end of stream flag was set.
HRESULT EVRCustomPresenter::CheckEndOfStream()
{
  if (!m_bEndStreaming)
  {
    // The EVR did not send the MFVP_MESSAGE_ENDOFSTREAM message.
    return S_OK; 
  }

  if (m_bSampleNotify)
  {
    // The mixer still has input. 
    return S_OK;
  }

  if (m_SamplePool.AreSamplesPending())
  {
    // Samples are still scheduled for rendering.
    return S_OK;
  }

  // Everything is complete. Now we can tell the EVR that we are done.
  NotifyEvent(EC_COMPLETE, (LONG_PTR)S_OK, 0);
  m_bEndStreaming = FALSE;

  return S_OK;
}


// Attempts to set an output type on the mixer.
HRESULT EVRCustomPresenter::RenegotiateMediaType()
{
  Log("EVRCustomPresenter::RenegotiateMediaType");

  HRESULT hr = S_OK;
  BOOL bFoundMediaType = FALSE;

  IMFMediaType *pMixerType = NULL;
  IMFMediaType *pOptimalType = NULL;
  IMFVideoMediaType *pVideoType = NULL;

  CheckPointer(m_pMixer, MF_E_INVALIDREQUEST);

  // Loop through all of the mixer's proposed output types.
  DWORD iTypeIndex = 0;
  while (!bFoundMediaType && (hr != MF_E_NO_MORE_TYPES))
  {
    SAFE_RELEASE(pMixerType);
    SAFE_RELEASE(pOptimalType);

    // Step 1. Get the next media type supported by mixer.
    hr = m_pMixer->GetOutputAvailableType(0, iTypeIndex++, &pMixerType);
    if (FAILED(hr))
    {
      Log("EVRCustomPresenter::RenegotiateMediaType no usable media type found");
      break;
    }

    // From now on, if anything in this loop fails, try the next type, until we succeed or the mixer runs out of types.

    // Step 2. Check if we support this media type. 
    hr = IsMediaTypeSupported(pMixerType);
    if (FAILED(hr))
    {
      Log("EVRCustomPresenter::RenegotiateMediaType EVRCustomPresenter::IsMediaTypeSupported failed");
      continue;
    }

    // Step 3. Adjust the mixer's type to match our requirements.
    hr = CreateOptimalVideoType(pMixerType, &pOptimalType);
    if (FAILED(hr))
    {
      Log("EVRCustomPresenter::RenegotiateMediaType EVRCustomPresenter::CreateOptimalVideoType failed");
      continue;
    }

    // Step 4. Check if the mixer will accept this media type.
    hr = m_pMixer->SetOutputType(0, pOptimalType, MFT_SET_TYPE_TEST_ONLY);
    if (FAILED(hr))
    {
      Log("EVRCustomPresenter::RenegotiateMediaType IMFTransform::SetOutputType");
      continue;
    }

    // Step 5. Try to set the media type on ourselves.
    hr = SetMediaType(pOptimalType);
    if (FAILED(hr))
    {
      Log("EVRCustomPresenter::RenegotiateMediaType EVRCustomPresenter::SetMediaType failed");
      continue;
    }

    // Step 6. Set output media type on mixer.
    hr = m_pMixer->SetOutputType(0, pOptimalType, 0);
    assert(SUCCEEDED(hr)); // This should succeed unless the MFT lied in the previous call.
    if (FAILED(hr))
    {
      Log("EVRCustomPresenter::RenegotiateMediaType IMFTransform::SetOutputType failed");
      SetMediaType(NULL);
      continue;
    }

    // valid media type found and output set, exit loop
    bFoundMediaType = TRUE;
  }

  SAFE_RELEASE(pMixerType);
  SAFE_RELEASE(pOptimalType);
  SAFE_RELEASE(pVideoType);

  return hr;
}

