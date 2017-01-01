#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.Common.Services.Dokan.Native
{
  [Flags]
  internal enum SECURITY_INFORMATION : uint
  {
    OWNER_SECURITY_INFORMATION = 0x00000001,
    GROUP_SECURITY_INFORMATION = 0x00000002,
    DACL_SECURITY_INFORMATION = 0x00000004,
    SACL_SECURITY_INFORMATION = 0x00000008,
    UNPROTECTED_SACL_SECURITY_INFORMATION = 0x10000000,
    UNPROTECTED_DACL_SECURITY_INFORMATION = 0x20000000,
    PROTECTED_SACL_SECURITY_INFORMATION = 0x40000000,
    PROTECTED_DACL_SECURITY_INFORMATION = 0x80000000
  }
}
