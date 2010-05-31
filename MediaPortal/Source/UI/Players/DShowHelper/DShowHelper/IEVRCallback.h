#pragma once

DECLARE_INTERFACE_(IEVRCallback, IUnknown)
{
	STDMETHOD(PresentSurface)(THIS_ WORD cx, WORD cy, WORD arx, WORD ary, DWORD dwImg)PURE;
};
