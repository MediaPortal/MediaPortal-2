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
using System.Threading;

namespace MediaPortal.Common.Services.ResourceAccess.ImpersonationService
{
  /// <summary>
  /// Thread-safe wrapper class for a user token
  /// </summary>
  /// <remarks>
  /// An instance of this class is returned by <see cref="WindowsIdentityWrapper.Token"/>.
  /// An <see cref="Action"/> can be supplied which is called exactly once when this object
  /// is disposed even when calling <see cref="Dispose"/> multiple times in a multithreaded environment.
  /// Do not close the token manually, this is taken care of by the <see cref="WindowsIdentityWrapper"/>.
  /// Make sure that objects of this class are properly disposed after usage so that the underlying
  /// <see cref="WindowsIdentityWrapper"/> is actually disposed.
  /// </remarks>
  internal sealed class TokenWrapper : IDisposable
  {
    #region Consts

    private const int DISPOSED = 1;
    private const int NOT_DISPOSED = 0;

    #endregion

    #region Private fields

    private readonly IntPtr _token;
    private readonly Action _notifyDispose;
    private int _disposed;

    #endregion

    #region Constructor

    /// <summary>
    /// Creates a new instance of <see cref="TokenWrapper"/>
    /// </summary>
    /// <param name="token">token to wrap</param>
    /// <param name="notifyDispose"><see cref="Action"/> to call when this object is disposed</param>
    internal TokenWrapper(IntPtr token, Action notifyDispose)
    {
      _token = token;
      _notifyDispose = notifyDispose;
      _disposed = NOT_DISPOSED;
    }

    #endregion

    #region Internal properties

    internal IntPtr Token { get { return _token; } }

    #endregion

    #region IDisposable implementation

    /// <summary>
    /// Calls <see cref="_notifyDispose"/> (if it is not <c>null</c>). Ensures that this happens only once.
    /// </summary>
    public void Dispose()
    {
      if (Interlocked.Exchange(ref _disposed, DISPOSED) == NOT_DISPOSED)
      {
        if (_notifyDispose != null)
          _notifyDispose.Invoke();
      }
    }

    #endregion
  }
}
