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

namespace MediaPortal.Common.Services.Dokan
{
  [Flags]
  public enum FileAccess : long
  {
    GenericRead = 2147483648,
    GenericWrite = 1073741824,
    GenericExecute = 536870912,
    ReadData = 1,
    WriteData = 2,
    AppendData = 4,
    ReadExtendedAttributes = 8,
    WriteExtendedAttributes = 16,
    Execute = 32,
    ReadAttributes = 128,
    WriteAttributes = 256,
    Delete = 65536,
    ReadPermissions = 131072,
    ChangePermissions = 262144,
    SetOwnership = 524288,
    Synchronize = 1048576,
    Reserved = 16777216
  }
}
