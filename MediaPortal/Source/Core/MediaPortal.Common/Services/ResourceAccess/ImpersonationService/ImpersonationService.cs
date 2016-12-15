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
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Security.Principal;
using System.Threading.Tasks;
using MediaPortal.Common.Logging;
using MediaPortal.Common.PathManager;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.Logging;
using MediaPortal.Utilities.Process;

namespace MediaPortal.Common.Services.ResourceAccess.ImpersonationService
{
  /// <summary>
  /// Standard implementation of <see cref="IImpersonationService"/>
  /// </summary>
  internal sealed class ImpersonationService : IImpersonationService
  {
    #region Private fields

    /// <summary>
    /// Debug logger (<see cref="NoLogger"/> for release builds, <see cref="FileLogger"/> for debug builds)
    /// </summary>
    private readonly ILogger _debugLogger;

    /// <summary>
    /// Dictionary of <see cref="ResourcePath"/>s and the corresponding
    /// <see cref="WindowsIdentity"/>s to use when accessing them
    /// </summary>
    private readonly ConcurrentDictionary<ResourcePath, WindowsIdentityWrapper> _ids;

    #endregion

    #region Constructor

    /// <summary>
    /// Creates a new instance of <see cref="ImpersonationService"/>
    /// </summary>
    internal ImpersonationService()
    {
#if DEBUG
      _debugLogger = FileLogger.CreateFileLogger(ServiceRegistration.Get<IPathManager>().GetPath(@"<LOG>\ImpersonationService.log"), LogLevel.Debug, false, true);
#else
      _debugLogger = new NoLogger();
#endif
      _ids = new ConcurrentDictionary<ResourcePath, WindowsIdentityWrapper>();
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Tries to impersonate a <see cref="WindowsIdentityWrapper"/>
    /// </summary>
    /// <param name="idWrapper"><see cref="WindowsIdentityWrapper"/> to impersonate</param>
    /// <param name="ctxWrapper"><see cref="WindowsImpersonationContextWrapper"/> resulting from the impersonation</param>
    /// <returns><c>true</c> if impersonation was successful; otherwiese <c>false</c></returns>
    private bool TryImpersonate(WindowsIdentityWrapper idWrapper, out WindowsImpersonationContextWrapper ctxWrapper)
    {
      try
      {
        ctxWrapper = idWrapper.Impersonate();
        return true;
      }
      catch (Exception e)
      {
        _debugLogger.Error("ImpersonationService: Error when trying to impersonate User '{0}' (Domain '{1}')", e, idWrapper.UserName, idWrapper.Domain);
        ctxWrapper = null;
        return false;
      }
    }

    /// <summary>
    /// Tries to find the best matching <see cref="WindowsIdentityWrapper"/> for a given <see cref="ResourcePath"/>
    /// </summary>
    /// <param name="path"><see cref="ResourcePath"/> for which a <see cref="WindowsIdentityWrapper"/> is needed</param>
    /// <param name="idWrapper"><see cref="WindowsIdentityWrapper"/> that matches best for <see cref="path"/></param>
    /// <returns><c>true</c> if a <see cref="WindowsIdentityWrapper"/> was found; otherwise <c>false</c></returns>
    /// <remarks>
    /// Assuming the following credentials are registered:
    ///   - User1: {03dd2da6-4da8-4d3e-9e55-80e3165729a3}:////Computer/Share_A/
    ///   - User2: {03dd2da6-4da8-4d3e-9e55-80e3165729a3}:////Computer/Share_A/Directory_X/
    /// This method returns the following results for the given <see cref="ResourcePath"/>s:
    ///   - User1 for {03dd2da6-4da8-4d3e-9e55-80e3165729a3}:////Computer/Share_A/
    ///   - User1 for {03dd2da6-4da8-4d3e-9e55-80e3165729a3}:////Computer/Share_A/Directory_Y/
    ///   - User2 for {03dd2da6-4da8-4d3e-9e55-80e3165729a3}:////Computer/Share_A/Directory_X/
    ///   - User2 for {03dd2da6-4da8-4d3e-9e55-80e3165729a3}:////Computer/Share_A/Directory_X/Subdirectory/
    ///   - null  for {03dd2da6-4da8-4d3e-9e55-80e3165729a3}:////Computer/Share_B/
    /// </remarks>
    private bool TryGetBestMatchingIdentityForPath(ResourcePath path, out WindowsIdentityWrapper idWrapper)
    {
      idWrapper = null;
      var pathLength = 0;
      var pathString = path.ToString();
      foreach (var kvp in _ids)
      {
        var keyString = kvp.Key.ToString();
        if (!pathString.StartsWith(keyString))
          continue;
        var keyLength = keyString.Length;
        if (keyLength <= pathLength)
          continue;
        pathLength = keyLength;
        idWrapper = kvp.Value;
      }
      return (idWrapper != null);
    }

    #endregion

    #region IImpersonationService implementation

    /// <summary>
    /// Registers a <see cref="NetworkCredential"/> for a given <see cref="ResourcePath"/>
    /// </summary>
    /// <param name="path">
    /// For this <see cref="ResourcePath"/> and all subpaths the <see cref="credential"/> is impersonated,
    /// assuming that no better matching path is registered.
    /// </param>
    /// <param name="credential"><see cref="NetworkCredential"/> to impersonate for accessing <paramref name="path"/></param>
    /// <returns><c>true</c> if registration was successful; otherwise <c>false</c></returns>
    public bool TryRegisterCredential(ResourcePath path, NetworkCredential credential)
    {
      _debugLogger.Info("ImpersonationService: Trying to register credential (User: '{0}' Domain: '{1}') for ResourcePath '{2}'", credential.UserName, credential.Domain, path);

      // If there is already a credential registered for exactly the same ResourcePath,
      // we unregister the old credential and log a warning. It should have been
      // unregistered with TryUnregisterCredential before.
      WindowsIdentityWrapper oldIdWrapper;
      if (_ids.TryRemove(path, out oldIdWrapper))
      {
        _debugLogger.Warn("ImpersonationService: There was already a credential registered For ResourcePath '{0}'. The old credential was unregistered.", path);
        oldIdWrapper.Dispose();
      }

      var logonHelper = new LogonHelper(_debugLogger);
      WindowsIdentity id;

      // We use LogonType.NewCredentials because this logon type allows the caller to clone its current token
      // and specify new credentials only for outbound connections. The new logon session has the same local
      // identifier but uses different credentials for other network connections.
      // This logon type is only supported by LogonProvider.WinNt50.
      if (logonHelper.TryLogon(credential, LogonHelper.LogonType.NewCredentials, LogonHelper.LogonProvider.WinNt50, out id))
      {
        var idWrapper = new WindowsIdentityWrapper(id, credential);
        if(!_ids.TryAdd(path, idWrapper))
        {
          // In a multithreaded environment, a new credential could have been added
          // despite the TryUnregisterCredential call above in the meantime.
          _debugLogger.Error("ImpersonationService: For ResourcePath '{0}' there was already a credential registered. Cannot register new credential.", path);
          idWrapper.Dispose();
          return false;
        }
        _debugLogger.Info("ImpersonationService: Successfully registered credential for ResourcePath '{0}': User: '{1}' (Domain: '{2}')", path, idWrapper.UserName, idWrapper.Domain);
        return true;
      }
      _debugLogger.Error("ImpersonationService: Could not register credential for ResourcePath '{0}': User: '{1}' (Domain: '{2}')", path, credential.UserName, credential.Domain);
      return false;
    }

    /// <summary>
    /// Tries to unregister the <see cref="NetworkCredential"/> for a given <see cref="ResourcePath"/>
    /// </summary>
    /// <param name="path"><see cref="ResourcePath"/> for which the <see cref="NetworkCredential"/> shall be unregistered</param>
    /// <returns><c>true</c> if <paramref name="path"/> was registered before and could now be unregistered; otherwise <c>false</c></returns>
    public bool TryUnregisterCredential(ResourcePath path)
    {
      WindowsIdentityWrapper idWrapper;
      if (_ids.TryRemove(path, out idWrapper))
      {
        idWrapper.Dispose();
        _debugLogger.Info("ImpersonationService: Successfully unregistered credential for ResourcePath '{0}': User: '{1}' (Domain: '{2}')", path, idWrapper.UserName, idWrapper.Domain);
        return true;
      }
      _debugLogger.Warn("ImpersonationService: Could not unregister credential for ResourcePath '{0}': There was no credential registered.", path);
      return false;
    }

    /// <summary>
    /// Finds the best matching registered credential for <paramref name="path"/> and impersonates it
    /// </summary>
    /// <param name="path"><see cref="ResourcePath"/> for which impersonation must be checked</param>
    /// <returns>
    /// <see cref="WindowsImpersonationContext"/> that represents the impersonation. The impersonation
    /// is maintained until this object is disposed. It MUST be disposed when impersonation is no longer
    /// needed to avoid resource leakage. If <paramref name="path"/> does not require impersonation,
    /// the current impersonation is not changed.
    /// </returns>
    public IDisposable CheckImpersonationFor(ResourcePath path)
    {
      WindowsIdentityWrapper bestMatchingIdentity;
      if (TryGetBestMatchingIdentityForPath(path, out bestMatchingIdentity))
      {
        WindowsImpersonationContextWrapper ctxWrapper;
        if (TryImpersonate(bestMatchingIdentity, out ctxWrapper))
          return ctxWrapper;
      }
      return new WindowsImpersonationContextWrapper(null, null);
    }

    /// <summary>
    /// Finds the best matching registered credential for <paramref name="path"/>
    /// and executes an external program with this credential
    /// </summary>
    /// <param name="path"><see cref="ResourcePath"/> to which the external program shall have access</param>
    /// <param name="executable">External program to execute</param>
    /// <param name="arguments">Arguments for the external program</param>
    /// <param name="priorityClass">Process priority</param>
    /// <param name="maxWaitMs">Maximum time to wait for completion</param>
    /// <returns>A <see cref="Task"/> representing the result of executing the external program</returns>
    public Task<ProcessExecutionResult> ExecuteWithResourceAccessAsync(ResourcePath path, string executable, string arguments, ProcessPriorityClass priorityClass, int maxWaitMs)
    {
      WindowsIdentityWrapper bestMatchingIdentity;
      return TryGetBestMatchingIdentityForPath(path, out bestMatchingIdentity) ?
        AsyncImpersonationProcess.ExecuteAsync(executable, arguments, bestMatchingIdentity, _debugLogger, priorityClass, maxWaitMs) :
        ProcessUtils.ExecuteAsync(executable, arguments, priorityClass, maxWaitMs);
    }

    #endregion

    #region IDisposable implementation

    public void Dispose()
    {
      foreach (var kvp in _ids)
        TryUnregisterCredential(kvp.Key);
    }

    #endregion
  }
}
