// Copyright (C) 2007-2014 Team MediaPortal
// http://www.team-mediaportal.com
// 
// This file is part of MediaPortal 2
// 
// MediaPortal 2 is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// MediaPortal 2 is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.

#include "d3d9.h"

DECLARE_INTERFACE_(IEVRCallback, IUnknown)
{
  STDMETHOD(PresentSurface)(THIS_ WORD cx, WORD cy, WORD arx, WORD ary, IDirect3DTexture9** dwImg)PURE;
};

