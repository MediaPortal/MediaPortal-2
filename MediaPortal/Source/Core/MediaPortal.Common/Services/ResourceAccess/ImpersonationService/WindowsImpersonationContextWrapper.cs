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
using System.Security.Principal;
using System.Threading;

namespace MediaPortal.Common.Services.ResourceAccess.ImpersonationService
{
  /// <summary>
  /// Thread-safe wrapper class for a <see cref="WindowsImpersonationContext"/> object
  /// </summary>
  /// <remarks>
  /// An instance of this class is returned by <see cref="ImpersonationService.CheckImpersonationFor"/>.
  /// The wrapped <see cref="WindowsImpersonationContext"/> is disposed when this wrapper is disposed.
  /// It is ensured that <see cref="WindowsImpersonationContext"/> is only disposed once, even when calling
  /// <see cref="Dispose"/> multiple times in a multithreaded environment.
  /// Additionally, an <see cref="Action"/> can be supplied which is called exactly once when this object
  /// is disposed even when calling <see cref="Dispose"/> multiple times in a multithreaded environment.
  /// Both, the <see cref="WindowsImpersonationContext"/> and the <see cref="Action"/> can be <c>null</c>,
  /// to be able to provide an <see cref="IDisposable"/> if no impersonation is necessary.
  /// </remarks>
  public sealed class WindowsImpersonationContextWrapper : IDisposable
  {
    #region Consts

    private const int DISPOSED = 1;
    private const int NOT_DISPOSED = 0;

    #endregion

    #region Private fields

    private readonly WindowsImpersonationContext _ctx;
    private readonly Action _notifyDispose;
    private int _disposed;

    #endregion

    #region Constructor

    /// <summary>
    /// Creates a new instance of <see cref="WindowsImpersonationContextWrapper"/>
    /// </summary>
    /// <param name="ctx"><see cref="WindowsImpersonationContext"/> to wrap (can be <c>null</c>)</param>
    /// <param name="notifyDispose"><see cref="Action"/> to call when this object is disposed</param>
    internal WindowsImpersonationContextWrapper(WindowsImpersonationContext ctx, Action notifyDispose)
    {
       _ctx = ctx;
      _notifyDispose = notifyDispose;
      _disposed = NOT_DISPOSED;
    }

    #endregion

    #region IDisposable implementation

    /// <summary>
    /// Disposes the underlying <see cref="WindowsImpersonationContext"/> (if it is not <c>null</c>); and
    /// calls <see cref="_notifyDispose"/> (if it is not <c>null</c>). Ensures that both happens only once.
    /// </summary>
    public void Dispose()
    {
      if (Interlocked.Exchange(ref _disposed, DISPOSED) == NOT_DISPOSED)
      {
        if (_ctx != null)
          _ctx.Dispose();
        if (_notifyDispose != null)
          _notifyDispose.Invoke();
      }
    }

    #endregion
  }
}
