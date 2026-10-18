#region Copyright (C) 2007-2021 Team MediaPortal

/*
    Copyright (C) 2007-2021 Team MediaPortal
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
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using MediaPortal.Common.Logging;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Utilities.Process;

namespace MediaPortal.Common.Services.ResourceAccess
{
  /// <summary>
  /// <see cref="IImpersonationService"/> for platforms without Windows logon sessions: everything runs
  /// as the current user, and registering a credential is refused.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Impersonation in MP2 means <c>LogonUser</c> plus a token duplicated into the calling thread, and for
  /// external programs <c>CreateProcessAsUser</c> with that token. That is the Windows
  /// <c>WindowsImpersonationService</c>, now in <c>MediaPortal.Common.Windows</c>. There is no equivalent
  /// on Linux (a setuid helper or per-request <c>runuser</c> would be a different security model, not a
  /// port of this one), so the portable core needs an answer for callers that ask.
  /// </para>
  /// <para>
  /// What this class does is the no-credential path of the Windows implementation, member by member:
  /// <c>RunImpersonatedFor</c> invokes the delegate directly when no registered identity matches the
  /// path, <c>CreateProcessWithResourceAccess</c> falls back to <c>ProcessUtils.Create</c>, and
  /// <c>TryUnregisterCredential</c> returns <c>false</c> for a path that has nothing registered. So a
  /// platform without impersonation is not a new code path for the callers - it is the path every
  /// Windows installation takes for the resources nobody registered a credential for.
  /// </para>
  /// <para>
  /// <see cref="TryRegisterCredential"/> is the one member that could have been faked by storing the
  /// credential and returning <c>true</c>. It refuses instead, because the caller would then be told it
  /// may reach a protected SMB share as the current user, which is a claim this class cannot keep. The
  /// callers already handle <c>false</c>: <c>NetworkNeighborhoodResourceProvider</c> continues with the
  /// ambient credentials and only logs that the registration was refused.
  /// </para>
  /// </remarks>
  public sealed class CurrentUserImpersonationService : IImpersonationService
  {
    private bool _refusedLogged;

    public bool TryRegisterCredential(ResourcePath path, NetworkCredential credential)
    {
      if (!_refusedLogged)
      {
        _refusedLogged = true;
        // Once per process: the network neighbourhood provider tries to register a credential per share,
        // and each refusal would otherwise be an identical line.
        ServiceRegistration.Get<ILogger>().Info(
            "CurrentUserImpersonationService: This platform cannot impersonate another user, so the " +
            "credential for '{0}' (user '{1}') was not registered. Resources are accessed with the " +
            "credentials of the current user.", path, credential?.UserName);
      }
      return false;
    }

    public bool TryUnregisterCredential(ResourcePath path)
    {
      return false;
    }

    public void RunImpersonatedFor(ResourcePath path, Action action)
    {
      action.Invoke();
    }

    public T RunImpersonatedFor<T>(ResourcePath path, Func<T> func)
    {
      return func.Invoke();
    }

    public Task<ProcessExecutionResult> ExecuteWithResourceAccessAsync(ResourcePath path, string executable,
        string arguments, ProcessPriorityClass priorityClass = ProcessPriorityClass.Normal,
        int maxWaitMs = ProcessUtils.DEFAULT_TIMEOUT)
    {
      return ProcessUtils.ExecuteAsync(executable, arguments, priorityClass, maxWaitMs);
    }

    public IProcess CreateProcessWithResourceAccess(ResourcePath path, ProcessStartInfo startInfo)
    {
      return ProcessUtils.Create(startInfo);
    }

    public void Dispose() {}
  }
}
