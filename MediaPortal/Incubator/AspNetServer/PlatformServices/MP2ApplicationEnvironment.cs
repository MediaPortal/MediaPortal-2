#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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
using System.Reflection;
using System.Runtime.Versioning;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using Microsoft.Extensions.PlatformAbstractions;

namespace MediaPortal.Plugins.AspNetServer.PlatformServices
{
  /// <summary>
  /// Provides information on the environment, in which AspNetServer runs
  /// </summary>
  /// <remarks>
  /// This class is needed to bootstrap the ASP.Net 5 Server.
  /// We treat MP2-Server.exe as "Application". This may be wrong, as from an ASP.Net 5 Server application, 
  /// the "Web"-Application may also be AspNetServer.dll so that ApplicationBasePath may have to return
  /// the respective plugin directory; same applies to ApplicationName. No idea what it right here.
  /// </remarks>
  public class MP2ApplicationEnvironment : IApplicationEnvironment
  {
    public string ApplicationBasePath
    {
      get
      {
        return AppDomain.CurrentDomain.BaseDirectory;
      }
    }

    public string ApplicationName
    {
      get
      {
        return Assembly.GetEntryAssembly()?.GetName().Name;
      }
    }

    public string ApplicationVersion
    {
      get
      {
        return Assembly.GetEntryAssembly()?.GetName().Version.ToString();
      }
    }

    public FrameworkName RuntimeFramework
    {
      get
      {
        var frameworkName = AppDomain.CurrentDomain.SetupInformation.TargetFrameworkName;
        return string.IsNullOrEmpty(frameworkName) ? null : new FrameworkName(frameworkName);
      }
    }
  }
}
