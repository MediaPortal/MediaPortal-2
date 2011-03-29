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

// IMFVideoDisplayControl Interface http://msdn.microsoft.com/en-us/library/ms704002(v=VS.85).aspx
// Controls how the enhanced video renderer (EVR) displays video.

// Queries how the EVR handles the aspect ratio of the source video.
HRESULT STDMETHODCALLTYPE EVRCustomPresenter::GetAspectRatioMode(DWORD *pdwAspectRatioMode)
{
  return E_NOTIMPL;
}


// Retrieves the border color for the video.
HRESULT STDMETHODCALLTYPE EVRCustomPresenter::GetBorderColor(COLORREF *pClr)
{
  return E_NOTIMPL;
}


// Retrieves a copy of the current image being displayed by the video renderer.
HRESULT STDMETHODCALLTYPE EVRCustomPresenter::GetCurrentImage(BITMAPINFOHEADER *pBih, BYTE **pDib, DWORD *pcbDib,LONGLONG *pTimeStamp)
{
  return E_NOTIMPL;
}


// Queries whether the EVR is currently in full-screen mode.
HRESULT STDMETHODCALLTYPE EVRCustomPresenter::GetFullscreen(BOOL *pfFullscreen)
{
  return E_NOTIMPL;
}


// Retrieves the range of sizes that the EVR can display without significantly degrading performance or image quality.
HRESULT STDMETHODCALLTYPE EVRCustomPresenter::GetIdealVideoSize(SIZE *pszMin, SIZE *pszMax)
{
  return E_NOTIMPL;
}


// Retrieves the size and aspect ratio of the video, prior to any stretching by the video renderer.
HRESULT STDMETHODCALLTYPE EVRCustomPresenter::GetNativeVideoSize(SIZE *pszVideo, SIZE *pszARVideo)
{
  return E_NOTIMPL;
}


// Retrieves various video rendering settings.
HRESULT STDMETHODCALLTYPE EVRCustomPresenter::GetRenderingPrefs(DWORD *pdwRenderFlags)
{
  return E_NOTIMPL;
}


// Retrieves the source and destination rectangles for the video.
HRESULT STDMETHODCALLTYPE EVRCustomPresenter::GetVideoPosition(MFVideoNormalizedRect *pnrcSource, LPRECT prcDest)
{
  CAutoLock lock(this);

  CheckPointer(pnrcSource, E_POINTER);
  CheckPointer(prcDest, E_POINTER);

  *pnrcSource = m_nrcSource;
  *prcDest = m_pD3DPresentEngine->GetDestinationRect();

  return S_OK;
}


// Retrieves the clipping window for the video.
HRESULT STDMETHODCALLTYPE EVRCustomPresenter::GetVideoWindow(HWND *phwndVideo)
{
  CAutoLock lock(this);

  CheckPointer(phwndVideo, E_POINTER);

  // The D3DPresentEngine object stores the handle.
  *phwndVideo = m_pD3DPresentEngine->GetVideoWindow();

  return S_OK;
}


// Repaints the current video frame.
HRESULT STDMETHODCALLTYPE EVRCustomPresenter::RepaintVideo()
{
  HRESULT hr = S_OK;

  CAutoLock lock(this);

  // We cannot repaint after shutdown.
  hr = CheckShutdown();
  CHECK_HR(hr, "EVRCustomPresenter::RepaintVideo cannot repaint after shutdown");

  // Ignore the request if we have not presented any samples yet.
  if (m_bPrerolled)
  {
    m_bRepaint = TRUE;
    (void)ProcessOutput();
  }

  return hr;
}


// Specifies how the EVR handles the aspect ratio of the source video.
HRESULT STDMETHODCALLTYPE EVRCustomPresenter::SetAspectRatioMode(DWORD dwAspectRatioMode)
{
  return E_NOTIMPL;
}


// Sets the border color for the video.
HRESULT STDMETHODCALLTYPE EVRCustomPresenter::SetBorderColor(COLORREF Clr)
{
  return E_NOTIMPL;
}


// Sets or unsets full-screen rendering mode.
HRESULT STDMETHODCALLTYPE EVRCustomPresenter::SetFullscreen(BOOL fFullscreen)
{
  return E_NOTIMPL;
}


// Sets various preferences related to video rendering.
HRESULT STDMETHODCALLTYPE EVRCustomPresenter::SetRenderingPrefs(DWORD dwRenderFlags)
{
  return E_NOTIMPL;
}


// Sets the source and destination rectangles for the video.
HRESULT STDMETHODCALLTYPE EVRCustomPresenter::SetVideoPosition(const MFVideoNormalizedRect *pnrcSource, const LPRECT prcDest)
{
  CAutoLock lock(this);

  // One parameter can be NULL, but not both.
  if (pnrcSource == NULL && prcDest == NULL)
  {
    return E_POINTER;
  }

  // Validate the rectangles.
  if (pnrcSource)
  {
    // The source rectangle cannot be flipped.
    if ((pnrcSource->left > pnrcSource->right) || (pnrcSource->top > pnrcSource->bottom))
    {
      return E_INVALIDARG;
    }

    // The source rectangle has range (0..1)
    if ((pnrcSource->left < 0) || (pnrcSource->right > 1) || (pnrcSource->top < 0) || (pnrcSource->bottom > 1))
    {
      return E_INVALIDARG;
    }
  }

  if (prcDest)
  {
    // The destination rectangle cannot be flipped.
    if ((prcDest->left > prcDest->right) || (prcDest->top > prcDest->bottom))
    {
      return E_INVALIDARG;
    }
  }

  HRESULT hr = S_OK;

  // Update the source rectangle. Source clipping is performed by the mixer.
  if (pnrcSource)
  {
    m_nrcSource = *pnrcSource;

    if (m_pMixer)
    {
      hr = SetMixerSourceRect(m_pMixer, m_nrcSource);
      CHECK_HR(hr, "EVRCustomPresenter::SetVideoPosition EVRCustomPresenter::SetMixerSourceRect() failed");
    }
  }

  // Update the destination rectangle.
  if (prcDest)
  {
    RECT rcOldDest = m_pD3DPresentEngine->GetDestinationRect();

    // Check if the destination rectangle changed.
    if (!EqualRect(&rcOldDest, prcDest))
    {
      hr = m_pD3DPresentEngine->SetDestinationRect(*prcDest);
      CHECK_HR(hr, "EVRCustomPresenter::SetVideoPosition D3DPresentEngineSetDestinationRect() failed");
    
      // Set a new media type on the mixer.
      if (m_pMixer)
      {
        hr = RenegotiateMediaType();
        if (hr == MF_E_TRANSFORM_TYPE_NOT_SET)
        {
          // This error means that the mixer is not ready for the media type.
          // Not a failure case -- the EVR will notify us when we need to set the type on the mixer.
          hr = S_OK;
        }
        else
        {
          CHECK_HR(hr, "EVRCustomPresenter::SetVideoPosition EVRCustomPresenter::RenegotiateMediaType() failed");

          // The media type changed. Request a repaint of the current frame.
          m_bRepaint = TRUE;
          (void)ProcessOutput(); // Ignore errors, the mixer might not have a video frame.
        }
      }
    }
  }

  return hr;
}


// Sets the clipping window for the video.
HRESULT STDMETHODCALLTYPE EVRCustomPresenter::SetVideoWindow(HWND hwndVideo)
{
  CAutoLock lock(this);

  if (!IsWindow(hwndVideo))
  {
    return E_INVALIDARG;
  }

  HRESULT hr = S_OK;
  HWND oldHwnd = m_pD3DPresentEngine->GetVideoWindow();

  // TODO Albert: Change comment. No new Direct3D device will be created.
  // If the window has changed, notify the D3DPresentEngine object. This will cause a new Direct3D device to be created.
  if (oldHwnd != hwndVideo)
  {
    hr = m_pD3DPresentEngine->SetVideoWindow(hwndVideo);

    // Tell the EVR that the device has changed.
    NotifyEvent(EC_DISPLAY_CHANGED, 0, 0);  
  }

  return hr;
}

