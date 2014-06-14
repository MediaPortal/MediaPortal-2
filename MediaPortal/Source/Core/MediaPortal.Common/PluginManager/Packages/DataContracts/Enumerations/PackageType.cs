#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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

namespace MediaPortal.Common.PluginManager.Packages.DataContracts.Enumerations
{
  [Flags]
  public enum PackageType
  {
    /// <summary>
    /// Package for MP2-Client only. This typically applies to skins and GUI plugins.
    /// </summary>
    Client = 1,
    /// <summary>
    /// Package for MP2-Server only. This typically applies to service plugins, resources etc.
    /// </summary>
    Server = 2,
    /// <summary>
    /// Package for both Client and Server use. This typically applies to resource accessors, metadata extractors, shared libraries etc.
    /// </summary>
    Shared = Client | Server
  }
}