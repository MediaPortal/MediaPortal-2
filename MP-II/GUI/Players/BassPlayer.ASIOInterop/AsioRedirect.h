#pragma region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#pragma endregion

//-----------------------------------------------------------------------------------
// Based on code originally written by Rob Philpott and published on CodeProject.com:
//   http://www.codeproject.com/KB/audio-video/Asio_Net.aspx
//
// ASIO is a trademark and software of Steinberg Media Technologies GmbH
//-----------------------------------------------------------------------------------

#ifndef _ASIOREDIRECT_H_
#define _ASIOREDIRECT_H_

#pragma unmanaged

#include "rpc.h"
//#include "rpcndr.h"
//#include "Windows.h"
//#include "Ole2.h"

#define IEEE754_64FLOAT		-1

// including Asio.h directly confuses things somewhat, so we redirect to it from here
#include "Asio.h"

// now we define the COM interface
interface IAsio : public IUnknown
{
  // See ASIO SDK fur further info
  
  virtual ASIOBool init(void *sysHandle) = 0;
  virtual void getDriverName(char *name) = 0;	
  virtual long getDriverVersion() = 0;
  virtual void getErrorMessage(char *string) = 0;	
  virtual ASIOError start() = 0;
  virtual ASIOError stop() = 0;
  virtual ASIOError getChannels(long *numInputChannels, long *numOutputChannels) = 0;
  virtual ASIOError getLatencies(long *inputLatency, long *outputLatency) = 0;
  virtual ASIOError getBufferSize(long *minSize, long *maxSize, long *preferredSize, long *granularity) = 0;
  virtual ASIOError canSampleRate(ASIOSampleRate sampleRate) = 0;
  virtual ASIOError getSampleRate(ASIOSampleRate *sampleRate) = 0;
  virtual ASIOError setSampleRate(ASIOSampleRate sampleRate) = 0;
  virtual ASIOError getClockSources(ASIOClockSource *clocks, long *numSources) = 0;
  virtual ASIOError setClockSource(long reference) = 0;
  virtual ASIOError getSamplePosition(ASIOSamples *sPos, ASIOTimeStamp *tStamp) = 0;
  virtual ASIOError getChannelInfo(ASIOChannelInfo *info) = 0;
  virtual ASIOError createBuffers(ASIOBufferInfo *bufferInfos, long numChannels, long bufferSize, ASIOCallbacks *callbacks) = 0;
  virtual ASIOError disposeBuffers() = 0;
  virtual ASIOError controlPanel() = 0;
  virtual ASIOError future(long selector,void *opt) = 0;
  virtual ASIOError outputReady() = 0;
};

#pragma managed
#endif // _ASIOREDIRECT_H_