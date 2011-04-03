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

// IMFVideoPresenter Interface http://msdn.microsoft.com/en-us/library/ms700214(v=VS.85).aspx
// Processes messages from the EVR

// Retrieves the presenter's media type.
HRESULT STDMETHODCALLTYPE EVRCustomPresenter::GetCurrentMediaType(IMFVideoMediaType **ppMediaType)
{
  Log("EVRCustomPresenter::GetCurrentMediaType");

  HRESULT hr = S_OK;

  if (ppMediaType == NULL)
  {
    return E_POINTER;
  }

  *ppMediaType = NULL;

  CAutoLock lock(this);

  hr = CheckShutdown();
  CHECK_HR(hr, "EVRCustomPresenter::GetCurrentMediaType presenter is shutdown");

  CheckPointer(m_pMediaType, MF_E_NOT_INITIALIZED);

  // Get IMFVideoMediaType pointer and store as an IMFMediaType pointer by callin QueryInterface
  hr = m_pMediaType->QueryInterface(__uuidof(IMFVideoMediaType), (void**)&ppMediaType);
  CHECK_HR(hr, "EVRCustomPresenter::GetCurrentMediaType IMFMediaType::QueryInterface() failed");

  return hr;
}


// Sends a message to the video presenter.
HRESULT STDMETHODCALLTYPE EVRCustomPresenter::ProcessMessage(MFVP_MESSAGE_TYPE eMessage, ULONG_PTR ulParam)
{
  // Albert: Don't produce so much log output
  //Log("EVRCustomPresenter::ProcessMessage");

  HRESULT hr = S_OK;

  CAutoLock lock(this);

  hr = CheckShutdown();
  CHECK_HR(hr, "EVRCustomPresenter::ProcessMessage presenter is shutdown");

  switch (eMessage)
  {
    // Flush all pending samples.
    case MFVP_MESSAGE_FLUSH:
      Log("ProcessMessage: MFVP_MESSAGE_FLUSH");
      hr = Flush();
    break;

    // Renegotiate the media type with the mixer.
    case MFVP_MESSAGE_INVALIDATEMEDIATYPE:
      Log("ProcessMessage: MFVP_MESSAGE_INVALIDATEMEDIATYPE");
      hr = RenegotiateMediaType();
    break;

    // The mixer received a new input sample. 
    case MFVP_MESSAGE_PROCESSINPUTNOTIFY:
      // Albert: Don't produce so much log output
      //Log("ProcessMessage: MFVP_MESSAGE_PROCESSINPUTNOTIFY");
      hr = ProcessInputNotify();
    break;

    // Streaming is about to start.
    case MFVP_MESSAGE_BEGINSTREAMING:
      Log("ProcessMessage: MFVP_MESSAGE_BEGINSTREAMING");
      hr = BeginStreaming();
    break;

    // Streaming has ended. (The EVR has stopped.)
    case MFVP_MESSAGE_ENDSTREAMING:
      Log("ProcessMessage: MFVP_MESSAGE_ENDSTREAMING");
      hr = EndStreaming();
    break;

    // All input streams have ended.
    case MFVP_MESSAGE_ENDOFSTREAM:
      Log("ProcessMessage: MFVP_MESSAGE_ENDOFSTREAM");
      // Set the end of stream flag. 
      m_bEndStreaming = TRUE; 
      // Check if it's time to send the EC_COMPLETE event to the EVR.
      hr = CheckEndOfStream();
    break;

    // Frame-stepping is starting.
    case MFVP_MESSAGE_STEP:
      Log("ProcessMessage: MFVP_MESSAGE_STEP");
      hr = PrepareFrameStep(LODWORD(ulParam));
    break;

    // Cancels frame-stepping.
    case MFVP_MESSAGE_CANCELSTEP:
      Log("ProcessMessage: MFVP_MESSAGE_CANCELSTEP");
      hr = CancelFrameStep();
    break;

    // Unknown message. (This case should never occur.)
    default:
      Log("ProcessMessage: Unknown Message");
      hr = E_INVALIDARG;
    break;
  }

  return hr;
}

