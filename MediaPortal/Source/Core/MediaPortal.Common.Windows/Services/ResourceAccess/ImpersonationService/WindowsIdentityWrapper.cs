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
using System.Net;
using System.Security.Principal;
using System.Threading;

namespace MediaPortal.Common.Services.ResourceAccess.ImpersonationService
{
  /// <summary>
  /// Wrapper class for a <see cref="WindowsIdentity"/> object and the corresponding <see cref="NetworkCredential"/>
  /// object. It ensures that the <see cref="WindowsIdentity"/> can be impersonated safely multiple times in a
  /// multithreaded environment. Same goes for using the underlying user token for starting impersonated external processes.
  /// </summary>
  internal sealed class WindowsIdentityWrapper : IDisposable
  {
    #region Consts

    private const int DISPOSED = 1;
    private const int NOT_DISPOSED = 0;

    #endregion

    #region Private fields

    private readonly WindowsIdentity _identity;
    private readonly NetworkCredential _credential;

    /// <summary>
    /// Indicates whether or not <see cref="Dispose"/> was called
    /// </summary>
    /// <remarks>
    /// If <see cref="_disposed"/> is <see cref="NOT_DISPOSED"/>, the object can be normally used.
    /// If <see cref="_disposed"/> is <see cref="DISPOSED"/>, <see cref="Dispose"/> was called. However,
    /// this does not mean that the underlying <see cref="_identity"/> has already been disposed. This
    /// is only the case, if additionally <see cref="_usageCount"/> is -1, i.e. the last
    /// <see cref="TokenWrapper"/> that has been created by getting <see cref="TokenWrapper"/>
    /// has also been disposed. The reason therefore is that we need to make sure that <see cref="_identity"/>
    /// is not disposed before all <see cref="TokenWrapper"/>s derived from it are disposed.
    /// This field is used to ensure that calling <see cref="Dispose"/> only has an effect when it was
    /// called for the first time. 
    /// </remarks>
    private int _disposed;

    /// <summary>
    /// Indicates how many <see cref="TokenWrapper"/> objects derived from <see cref="_identity"/>
    /// are currently in use.
    /// </summary>
    /// <remarks>
    /// If <see cref="Dispose"/> has not yet been called:
    ///   >  0: indicates the number of active <see cref="TokenWrapper"/>s
    ///   == 0: indicates that <see cref="_identity"/> is currently not impersonated
    /// If <see cref="Dispose"/> has been called:
    ///   >=  0: indicates the number of active <see cref="TokenWrapper"/>s minus one
    ///   == -1: indicates that <see cref="_identity"/> can actually be disposed.
    /// </remarks>
    private int _usageCount;

    #endregion

    #region Constructor

    /// <summary>
    /// Creates a new instance of <see cref="WindowsIdentityWrapper"/>
    /// </summary>
    /// <param name="identity"><see cref="WindowsIdentity"/> to wrap</param>
    /// <param name="credential">Corresponding <see cref="NetworkCredential"/></param>
    /// <exception cref="ArgumentNullException"></exception> if <paramref name="identity"/>
    /// or <paramref name="credential"/> is <c>null</c>.
    internal WindowsIdentityWrapper(WindowsIdentity identity, NetworkCredential credential)
    {
      if(identity == null)
        throw new ArgumentNullException("identity");
      if (credential == null)
        throw new ArgumentNullException("credential");

      _identity = identity;
      _credential = credential;
      _disposed = NOT_DISPOSED;
    }

    #endregion

    #region Internal properties

    /// <summary>
    /// <see cref="_credential"/>.UserName
    /// </summary>
    internal String UserName { get { return _credential.UserName; } }

    /// <summary>
    /// <see cref="_credential"/>.Domain
    /// </summary>
    internal String Domain { get { return _credential.Domain; } }

    /// <summary>
    /// Token of the underlying <see cref="WindowsIdentity"/> wrapped in a <see cref="TokenWrapper"/>
    /// </summary>
    /// <remarks>
    /// Do not manually close the contained token. It will be closed automatically when disposing this object.
    /// The <see cref="TokenWrapper"/> ensures that this object is not disposed before all <see cref="TokenWrapper"/>s
    /// are disposed.
    /// </remarks>
    internal TokenWrapper TokenWrapper
    {
      get
      {
        if (Interlocked.Increment(ref _usageCount) > 0)
          return new TokenWrapper(_identity.Token, DecrementUsageCount);
        return new TokenWrapper(IntPtr.Zero, null);
      }
    }

    /// <summary>
    /// Runs the specified action as this impersonated Windows identity.
    /// </summary>
    /// <param name="action"><see cref="Action"/> to run.</param>
    /// <remarks>
    /// This method can be reliably used with the async/await pattern by passing an async delegate and awaiting the resulting task.
    /// </remarks>
    internal void RunImpersonated(Action action)
    {
      if (action == null)
        throw new ArgumentNullException("action");

      // The TokenWrapper is not used directly, its only used for ref counting usages of
      // the WindowsIdentity to ensure it isn't disposed whilst we are using it.
      using (TokenWrapper)
      {
        // The identity may have been disposed before we referenced it so check the token is valid.
        if (TokenWrapper.Token != IntPtr.Zero)
          WindowsIdentity.RunImpersonated(_identity.AccessToken, action);
        // Else impersonation is unavailable so just run directly
        else
          action();
      }
    }

    /// <summary>
    /// Runs the specified function as this impersonated Windows identity.
    /// </summary>
    /// <typeparam name="T">The type of object the function returns.</typeparam>
    /// <param name="func">The <see cref="Func{T}"/> to run.</param>
    /// <returns>The result of the function.</returns>
    /// <remarks>
    /// This method can be reliably used with the async/await pattern by passing an async delegate and awaiting the resulting task.
    /// </remarks>
    internal T RunImpersonated<T>(Func<T> func)
    {
      if (func == null)
        throw new ArgumentNullException("func");

      // The TokenWrapper is not used directly, its only used for ref counting usages of
      // the WindowsIdentity to ensure it isn't disposed whilst we are using it.
      using (TokenWrapper)
      {
        // The identity may have been disposed before we referenced it so check the token is valid.
        if (TokenWrapper.Token != IntPtr.Zero)
          return WindowsIdentity.RunImpersonated(_identity.AccessToken, func);
        // Else impersonation is unavailable so just run directly
        else
          return func();
      }
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Used as <see cref="Action"/> to let <see cref="TokenWrapper"/> signal that it was disposed.
    /// </summary>
    private void DecrementUsageCount()
    {
      if (Interlocked.Decrement(ref _usageCount) < 0)
        _identity.Dispose();
    }

    #endregion

    #region IDisposable implementation

    public void Dispose()
    {
      if (Interlocked.Exchange(ref _disposed, DISPOSED) == NOT_DISPOSED && Interlocked.Decrement(ref _usageCount) < 0)
        _identity.Dispose();
    }

    #endregion
  }
}
