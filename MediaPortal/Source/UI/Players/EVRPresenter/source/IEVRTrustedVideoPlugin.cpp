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

// IEVRTrustedVideoPlugin Interface http://msdn.microsoft.com/en-us/library/aa473784(v=VS.85).aspx
// Enables the presenter to work with protected media.

// Queries whether the plug-in can limit the effective video resolution.
HRESULT STDMETHODCALLTYPE EVRCustomPresenter::CanConstrict(BOOL *pYes)
{
  CheckPointer(pYes, E_POINTER);
  *pYes = TRUE;
  return S_OK;
}


// Enables or disables the ability of the plug-in to export the video image.
HRESULT STDMETHODCALLTYPE EVRCustomPresenter::DisableImageExport(BOOL bDisable)
{
  return S_OK;
}


// Queries whether the plug-in has any transient vulnerabilities at this time.
HRESULT STDMETHODCALLTYPE EVRCustomPresenter::IsInTrustedVideoMode(BOOL *pYes)
{
  CheckPointer(pYes, E_POINTER);
  *pYes = TRUE;
  return S_OK;
}


// Limits the effective video resolution.
HRESULT STDMETHODCALLTYPE EVRCustomPresenter::SetConstriction(DWORD dwKPix)
{
  return S_OK;
}


