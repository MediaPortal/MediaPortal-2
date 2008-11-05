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

#include "InstalledDriver.h"

// we need this for registry access
using namespace Microsoft::Win32;

// and we need this for typed lists
using namespace System::Collections::Generic;

namespace Media
{
  namespace Players
  {
    namespace BassPlayer
    {
      namespace ASIOInterop
      {
        InstalledDriver::InstalledDriver(String^ name, String^ clsId)
        {
          // remember the name and CLSID
          _name = name;
          _clsId = clsId;
        }

        array<InstalledDriver^>^ InstalledDriver::GetInstalledDriversFromRegistry()
        {
          // create a generic list of installed drivers
          List<InstalledDriver^> list = gcnew List<InstalledDriver^>();

          // our settings are in the local machine
          RegistryKey^ localMachine = Registry::LocalMachine;

          // in the software/asio folder
          RegistryKey^ asioRoot = localMachine->OpenSubKey("SOFTWARE\\ASIO");

          // Check for SubKeyCount here too! Not doing so will cause a crash
          // on Vista calling GetSubKeyNames().
          if (asioRoot != nullptr && asioRoot->SubKeyCount > 0)
          {
            // now read all the names of subkeys below that
            array<String^>^ subkeyNames = asioRoot->GetSubKeyNames();

            // iterate through and get the stuff we need
            for (int index = 0; index < subkeyNames->Length; index++)
            {
              // get the registry key detailing the driver
              RegistryKey^ driverKey = asioRoot->OpenSubKey(subkeyNames[index]);

              // and extract what we need
              String^ name = static_cast<String^>(driverKey->GetValue("Description"));
              String^ clsid = static_cast<String^>(driverKey->GetValue("CLSID"));

              // If the description value is not present, use the subkeyname.
              if (!name)
                name = subkeyNames[index];

              // and close again
              driverKey->Close();

              // add to our list
              list.Add(gcnew InstalledDriver(name, clsid));
            }
          }
          // and return as an array
          return list.ToArray();
        }

        String^ InstalledDriver::ClsId::get()
        {
          return _clsId;
        }

        String^ InstalledDriver::Name::get()
        {
          return _name;
        }

        String^ InstalledDriver::ToString()
        {
          return _name;
        }
      }
    }
  }
}