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

using MediaPortal.Common.ResourceAccess;
using MediaPortal.Utilities.Process;
using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;

namespace Test.TranscodingService
{
  public class TestImpersonationService : IImpersonationService
  {
    public bool TryRegisterCredential(ResourcePath path, NetworkCredential credential)
    {
      return true;
    }

    public bool TryUnregisterCredential(ResourcePath path)
    {
      return true;
    }

    public IDisposable CheckImpersonationFor(ResourcePath path)
    {
      return null;
    }

    public Task<ProcessExecutionResult> ExecuteWithResourceAccessAsync(ResourcePath path, string executable, string arguments, ProcessPriorityClass priorityClass = ProcessPriorityClass.Normal, int maxWaitMs = 10000)
    {
      return ProcessUtils.ExecuteAsync(executable, arguments, priorityClass, maxWaitMs);
    }

    public void Dispose()
    {
      
    }
  }
}
