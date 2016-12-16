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
    /// <see cref="WindowsImpersonationContext"/> that has been created by calling <see cref="Impersonate"/>
    /// has also been disposed. The reason therefore is that we need to make sure that <see cref="_identity"/>
    /// is not disposed before all <see cref="WindowsImpersonationContext"/>s derived from it are disposed.
    /// This field is used to ensure that calling <see cref="Dispose"/> only has an effect when it was
    /// called for the first time. 
    /// </remarks>
    private int _disposed;

    /// <summary>
    /// Indicates how many <see cref="WindowsImpersonationContext"/> objects derived from <see cref="_identity"/>
    /// are currently in use.
    /// </summary>
    /// <remarks>
    /// If <see cref="Dispose"/> has not yet been called:
    ///   >  0: indicates the number of active <see cref="WindowsImpersonationContext"/>s
    ///   == 0: indicates that <see cref="_identity"/> is currently not impersonated
    /// If <see cref="Dispose"/> has been called:
    ///   >=  0: indicates the number of active <see cref="WindowsImpersonationContext"/>s minus one
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
    /// and <see cref="WindowsImpersonationContextWrapper"/>s are disposed.
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

    #endregion

    #region Internal methods

    /// <summary>
    /// Impersonates the underlying <see cref="WindowsIdentity"/>
    /// </summary>
    /// <returns>
    /// <see cref="WindowsImpersonationContextWrapper"/> containing the resulting <see cref="WindowsImpersonationContext"/>.
    /// This object MUST be disposed to avoid resource leaking!
    /// </returns>
    /// <remarks>
    /// A call to this method is only successful, if <see cref="_usageCount"/> >= 0 before calling, i.e. it is not
    /// successful, if <see cref="Dispose"/> was called and there are no other <see cref="WindowsImpersonationContextWrapper"/>s in use.
    /// </remarks>
    internal WindowsImpersonationContextWrapper Impersonate()
    {
      WindowsImpersonationContext ctx = null;
      if(Interlocked.Increment(ref _usageCount) > 0)
        ctx = _identity.Impersonate();
      return new WindowsImpersonationContextWrapper(ctx, DecrementUsageCount);
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Used as <see cref="Action"/> to let <see cref="WindowsImpersonationContextWrapper"/> and
    /// <see cref="TokenWrapper"/> signal that they were disposed.
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
