#pragma once


DECLARE_INTERFACE_(IVMR9Callback, IUnknown)
{
	STDMETHOD(PresentImage)  (THIS_ WORD cx, WORD cy, WORD arx, WORD ary, DWORD dwImg)PURE;
	STDMETHOD(PresentSurface)(THIS_ WORD cx, WORD cy, WORD arx, WORD ary, DWORD dwImg)PURE;
};
