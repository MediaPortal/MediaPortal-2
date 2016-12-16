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

using System.Diagnostics;
using System.Threading.Tasks;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Utilities.Process;

namespace MediaPortal.Common.Services.ResourceAccess.ImpersonationService
{
  /// <summary>
  /// Extension methods for the <see cref="ILocalFsResourceAccessor"/> interface
  /// </summary>
  public static class ILocalFsResourceAccessorExtensions
  {
    /// <summary>
    /// Executes an external program and ensures that it has access to the respective resource
    /// </summary>
    /// <param name="lfsra"><see cref="ILocalFsResourceAccessor"/> to which the external program needs access to</param>
    /// <param name="executable">External program to execute</param>
    /// <param name="arguments">Arguments for the external program</param>
    /// <param name="priorityClass">Process priority</param>
    /// <param name="maxWaitMs">Maximum time to wait for completion</param>
    /// <returns>A <see cref="Task"/> representing the result of executing the external program</returns>
    /// <remarks>
    /// This is a convenience method that enables executing an external procgram directly on the <see cref="ILocalFsResourceAccessor"/>
    /// interface to which the external program needs access. The purpose of an <see cref="ILocalFsResourceAccessor"/> is providing
    /// access to a resource - not executing programs which is why this method is implemented as an extension method instead of
    /// a method directly on the interface.
    /// </remarks>
    public static Task<ProcessExecutionResult> ExecuteWithResourceAccessAsync(this ILocalFsResourceAccessor lfsra, string executable, string arguments, ProcessPriorityClass priorityClass = ProcessPriorityClass.Normal, int maxWaitMs = ProcessUtils.DEFAULT_TIMEOUT)
    {
      return ServiceRegistration.Get<IImpersonationService>().ExecuteWithResourceAccessAsync(lfsra.CanonicalLocalResourcePath, executable, arguments, priorityClass, maxWaitMs);
    }
  }
}
