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
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using MediaPortal.Utilities.Process;

namespace MediaPortal.Common.ResourceAccess
{
  /// <summary>
  /// Provides the possibility to impersonate a thread to get access to protected resources
  /// </summary>
  /// <remarks>
  /// Resources such as CIFS or SMB shares are often protected. To access them it is necessary
  /// to impersonate a thread and thereby make sure to have sufficient privileges to access that
  /// resource.
  /// The implementation of this interface provides the possibility to register credentials that
  /// can be used to impersonate for getting access to a particular resource. Based on the registered
  /// credentials, a caller can check whether it is necessary to impersonate for accessing a
  /// specific resource and, if so, impersonate with the right credentials.
  /// Starting an external process that needs access to a protected resource is particularly
  /// difficult because the new process needs to be started with the right credentials; an
  /// implementation of this interface therefore also provides methods to start external
  /// processes in a way that they have access to a specific protected resource.
  /// </remarks>
  public interface IImpersonationService : IDisposable
  {
    /// <summary>
    /// Registers a credential that has access to a specific protected resource
    /// </summary>
    /// <param name="path"><see cref="ResourcePath"/> to which the <paramref name="credential"/> provides access</param>
    /// <param name="credential"><see cref="NetworkCredential"/> that has access to <paramref name="path"/></param>
    /// <returns><c>true</c>, if the credential was registered successfully; else <c>false</c></returns>
    bool TryRegisterCredential(ResourcePath path, NetworkCredential credential);
    
    /// <summary>
    /// Unregisters a credential that was previously registered successfully
    /// </summary>
    /// <param name="path"><see cref="ResourcePath"/> for which the credential should be unregistered</param>
    /// <returns><c>true</c>, if the credential was unregistered successfully; else <c>false</c></returns>
    bool TryUnregisterCredential(ResourcePath path);
    
    /// <summary>
    /// Checks wheteher impersonation is necessary to access a resource and, if so, impersonates the calling thread
    /// </summary>
    /// <param name="path"><see cref="ResourcePath"/> to check</param>
    /// <returns>
    /// An <see cref="IDisposable"/> until the disposal of which the impersonaion is maintained.
    /// If no impersonation is required to access <paramref name="path"/>, <c>null</c> is returned. It is therefore
    /// recommended to use <example>using (ImpersonationService.CheckImpersonationFor(path)) { Access to Resource }</example>,
    /// which handles null automatically.
    /// </returns>
    IDisposable CheckImpersonationFor(ResourcePath path);

    /// <summary>
    /// Executes the <paramref name="executable"/> and waits a maximum of <paramref name="maxWaitMs"/> miliseconds for completion.
    /// If the process does not end in this time, it is aborted. This method automatically decides if impersonation is  necessary
    /// so that the external process can access the specified resource based on the given <paramref name="path"/> and the
    /// previously registered credentials.
    /// </summary>
    /// <param name="path"><see cref="ResourcePath"/> to which the external process should have access</param>
    /// <param name="executable">Program to execute</param>
    /// <param name="arguments">Program arguments</param>
    /// <param name="priorityClass">Process priority</param>
    /// <param name="maxWaitMs">Maximum time to wait for completion</param>
    /// <returns>A task representing the result of executing the process</returns>
    Task<ProcessExecutionResult> ExecuteWithResourceAccessAsync(ResourcePath path, string executable, string arguments, ProcessPriorityClass priorityClass = ProcessPriorityClass.Normal, int maxWaitMs = ProcessUtils.DEFAULT_TIMEOUT);
  }
}
