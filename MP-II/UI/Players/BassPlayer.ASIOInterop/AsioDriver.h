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

#include "AsioRedirect.h"
#include "BufferInfo.h"
#include "Channel.h"
#include "InstalledDriver.h"
#include "LatencyInfo.h"

#pragma once
#pragma managed
using namespace System;

namespace Media
{
  namespace Players
  {
    namespace BassPlayer
    {
      namespace ASIOInterop
      {
        // represents an ASIO driver (also some static for all drivers)
        public ref class AsioDriver
        {
        internal:

          // we'll maintain a list of drivers
          static array<InstalledDriver^>^ _installedDrivers;

          // but you can only have one active at once
          static AsioDriver^ _instance;

          // our elusive COM interface
          IAsio* _pDriver;

          // a struct which specifies callback addresses
          ASIOCallbacks* _pCallbacks;

				  // eventArgs instance to pass on to bufferupdate event
          EventArgs^ _bufferUpdateEventArgs;

          // the number of input channels supported by the driver, and our max
          int _nInputs;

          // and the number of output channels
          int _nOutputs;

          // the buffersize we're actually using
          int _bufferSize;

          // is it usefull to call outputReady each time we have updated the outputbuffers 
          ASIOError _outputReadySupport;

          // the static callback methods - we'll forward these to instance members
          static void OnSampleRateDidChange(ASIOSampleRate rate);
          static long OnAsioMessage(long selector, long value, void* message, double* opt);

          // select a driver once an instance of this class has been created
          bool InternalSelectDriver(InstalledDriver^ installedDriver, IntPtr sysHandle);

          // our instance based handlers
          void OnBufferSwitch(long doubleBufferIndex, ASIOBool directProcess);
          ASIOTime* OnBufferSwitchTimeInfo(ASIOTime* params, long doubleBufferIndex, ASIOBool directProcess);

          // safety function to make sure all is well before attempting any operations
          void CheckInitialised();

          // the input channels
          array<Channel^>^ _inputChannels;

          // the output channels
          array<Channel^>^ _outputChannels;

          // C++ likes 'non-trivial' events
          EventHandler^ _bufferUpdateEvent;

          // return the instance of currently selected driver
          static property AsioDriver^ Instance { AsioDriver^ get(); }

          // the last occurred AsioError
          ASIOError _lastASIOError;

        public:

          // returns the installed drivers
          static property array<InstalledDriver^>^ InstalledDrivers	{ array<InstalledDriver^>^ get(); }

          // select and initialise driver
          static AsioDriver^ SelectDriver(InstalledDriver^ installedDriver, IntPtr sysHandle);

          // basic information properties
          property int              Version        { int get(); }
          property String^          DriverName     { String^ get(); }
          property int              BufferSize     { int get(); }
          property BufferInfo^      Buffer         { BufferInfo^ get(); }
          property LatencyInfo^     Latency        { LatencyInfo^ get(); }
          property array<Channel^>^ InputChannels  { array<Channel^>^ get(); }
          property array<Channel^>^ OutputChannels { array<Channel^>^ get(); }
          property ASIOError        LastASIOError  { ASIOError get(); }

				  // contructor
          AsioDriver();

          // basic methods
          bool Start();
          bool Stop();
          bool ShowControlPanel();
          bool CreateBuffers(bool useMaxBufferSize);
          bool DisposeBuffers();
          void Release();
          int GetSampleRate();
          bool SetSampleRate(int rate);
          bool CanSampleRate(int rate);
          String^ GetErrorMessage();

          // and the buffer update event - bit strange the way this works in c++
          event EventHandler^ BufferUpdate
          {
            void add(EventHandler^ e) { _bufferUpdateEvent += e; }
            void remove(EventHandler^ e) { _bufferUpdateEvent -= e; }
          }
        };
      }
    }
  }
}