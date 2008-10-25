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

using namespace System;
using namespace System::Reflection;
using namespace System::Runtime::CompilerServices;
using namespace System::Runtime::InteropServices;
using namespace System::Security::Permissions;

[assembly:AssemblyTitleAttribute("BassPlayer.ASIOInterop")];
[assembly:AssemblyDescriptionAttribute("Provides .NET access to ASIO drivers")];
[assembly:AssemblyConfigurationAttribute("")];
[assembly:AssemblyCompanyAttribute("Team MediaPortal")];
[assembly:AssemblyProductAttribute("MediaPortal")];
[assembly:AssemblyCopyrightAttribute("Copyright © 2008 Team MediaPortal")];
[assembly:AssemblyTrademarkAttribute("")];
[assembly:AssemblyCultureAttribute("")];
[assembly:AssemblyVersionAttribute("2.0.0.0")];
[assembly:ComVisible(false)];
[assembly:CLSCompliantAttribute(true)];
[assembly:SecurityPermission(SecurityAction::RequestMinimum, UnmanagedCode = true)];