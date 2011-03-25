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

// IMFClockStateSink Interface http://msdn.microsoft.com/en-us/library/ms701593(v=VS.85).aspx
// Notifies the presenter when the EVR's clock changes state.

// Called when the presentation clock pauses.
HRESULT STDMETHODCALLTYPE EVRCustomPresenter::OnClockPause(MFTIME hnsSystemTime)
{
  Log("EVRCustomPresenter::OnClockPause");

  HRESULT hr = S_OK;

  CAutoLock lock(this);

  // We cannot pause the clock after shutdown.
  hr = CheckShutdown();
  CHECK_HR(hr, "EVRCustomPresenter::OnClockPause cannot pause after shutdown");

  // Set the state. (No other actions are necessary.)
  m_RenderState = RENDER_STATE_PAUSED;

  return hr;
}


// Called when the presentation clock restarts from the same position while paused.
HRESULT STDMETHODCALLTYPE EVRCustomPresenter::OnClockRestart(MFTIME hnsSystemTime)
{
  Log("EVRCustomPresenter::OnClockRestart");

  HRESULT hr = S_OK;

  CAutoLock lock(this);

  // We cannot restart the clock after shutdown.
  hr = CheckShutdown();
  CHECK_HR(hr, "EVRCustomPresenter::OnClockRestart cannot restart after shutdown");

  // The EVR calls OnClockRestart only while paused.
  assert(m_RenderState == RENDER_STATE_PAUSED);
  m_RenderState = RENDER_STATE_STARTED;

  // Possibly we are in the middle of frame-stepping OR we have samples waiting in the frame-step queue. 
  hr = StartFrameStep();
  CHECK_HR(hr, "EVRCustomPresenter::OnClockRestart EVRCustomPresenter::StartFrameStep() failed")

  // Now resume the presentation loop.
  ProcessOutputLoop();

  return hr;
}


// Called when the rate changes on the presentation clock.
HRESULT STDMETHODCALLTYPE EVRCustomPresenter::OnClockSetRate(MFTIME hnsSystemTime, float fRate)
{
  Log("EVRCustomPresenter::OnClockSetRate (rate=%f)", fRate);

  HRESULT hr = S_OK;

  CAutoLock lock(this);
  
  // We cannot set the rate after shutdown.
  hr = CheckShutdown();
  CHECK_HR(hr, "EVRCustomPresenter::OnClockRestart cannot set rate after shutdown");

  // If the rate is changing from zero (scrubbing) to non-zero, cancel the frame-step operation.
  if ((m_fRate == 0.0f) && (fRate != 0.0f))
  {
    CancelFrameStep();
    m_FrameStep.samples.Clear();
  }

  m_fRate = fRate;

  // Tell the scheduler about the new rate.
  m_scheduler.SetClockRate(fRate);

  return hr;
}


// Called when the presentation clock starts.
HRESULT STDMETHODCALLTYPE EVRCustomPresenter::OnClockStart(MFTIME hnsSystemTime, LONGLONG llClockStartOffset)
{
  Log("EVRCustomPresenter::OnClockStart (offset = %I64d)", llClockStartOffset);

  HRESULT hr = S_OK;

  CAutoLock lock(this);

  // We cannot start after shutdown
  hr = CheckShutdown();
  CHECK_HR(hr, "EVRCustomPresenter::OnClockRestart cannot start after shutdown");

  // Check if the clock is already active (not stopped). 
  if (IsActive())
  {
    m_RenderState = RENDER_STATE_STARTED;
    
    // If the clock position changes while the clock is active, it is a seek request. We need to flush all pending samples.
    if (llClockStartOffset != PRESENTATION_CURRENT_POSITION)
    {
      Flush();
    }
  }
  else
  {
    // The clock has started from the stopped state. 
    m_RenderState = RENDER_STATE_STARTED;

    // Possibly we are in the middle of frame-stepping or have samples waiting in the frame-step queue.
    hr = StartFrameStep();
    CHECK_HR(hr, "EVRCustomPresenter::OnClockRestart EVRCustomPresenter::StartFrameStep() failed");
  }

  // Now try to get new output samples from the mixer.
  ProcessOutputLoop();

  return hr;
}


// Called when the presentation clock stops.
HRESULT STDMETHODCALLTYPE EVRCustomPresenter::OnClockStop(MFTIME hnssSystemTime)
{
  Log("EVRCustomPresenter::OnClockStop");

  HRESULT hr = S_OK;

  CAutoLock lock(this);

  // We cannot stop after shutdown
  hr = CheckShutdown();
  CHECK_HR(hr, "EVRCustomPresenter::OnClockStop cannot stop after shutdown");

  // set render state to stop and flush queue
  if (m_RenderState != RENDER_STATE_STOPPED)
  {
    m_RenderState = RENDER_STATE_STOPPED;
    Flush();

    // If we are in the middle of frame-stepping, cancel it now.
    if (m_FrameStep.state != FRAMESTEP_NONE)
    {
      CancelFrameStep();
    }
  }

  return hr;
}

