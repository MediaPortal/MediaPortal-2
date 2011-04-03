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

// IQualProp Interface http://msdn.microsoft.com/en-us/library/dd376915(v=VS.85).aspx
// Reports performance information. The EVR uses this information for quality-control management.

// Retrieves the average frame rate achieved.
HRESULT STDMETHODCALLTYPE EVRCustomPresenter::get_AvgFrameRate(int *piAvgFrameRate)
{
  return E_NOTIMPL;
}


// Retrieves the average time difference between when a frame was due for rendering and when rendering actually began (this is returned as a value in milliseconds).
HRESULT STDMETHODCALLTYPE EVRCustomPresenter::get_AvgSyncOffset(int *piAvg)
{
  return E_NOTIMPL;
}


// Retrieves the average time difference between when a frame was due for rendering and when rendering actually began (this is returned as a standard deviation).
HRESULT STDMETHODCALLTYPE EVRCustomPresenter::get_DevSyncOffset(int *piDev)
{
  return E_NOTIMPL;
}

// Retrieves the number of frames drawn since streaming started.
HRESULT STDMETHODCALLTYPE EVRCustomPresenter::get_FramesDrawn(int *pcFramesDrawn)
{
  return E_NOTIMPL;
}


// Retrieves the number of frames dropped by the renderer.
HRESULT STDMETHODCALLTYPE EVRCustomPresenter::get_FramesDroppedInRenderer(int *pcFrames)
{
  return 0;
}


// Gets the jitter (variation in time) between successive frames delivered to the video renderer
HRESULT STDMETHODCALLTYPE EVRCustomPresenter::get_Jitter(int *piJitter)
{
  return 0;
}

