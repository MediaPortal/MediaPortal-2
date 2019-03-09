#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace UPnP.Infrastructure
{
#if DEBUG
  /// <summary>
  /// Helper class for tracing holders of ReaderWriterLockSlim.
  /// </summary>
  internal static class LockDebug
  {
    public static ConcurrentDictionary<ReaderWriterLockSlim, List<StackTrace>> ReadLockHolders = new ConcurrentDictionary<ReaderWriterLockSlim, List<StackTrace>>();

    public static void AddReader(ReaderWriterLockSlim rwLock, StackTrace st)
    {
      lock (ReadLockHolders)
      {
        if (!ReadLockHolders.ContainsKey(rwLock))
          ReadLockHolders[rwLock] = new List<StackTrace>();
        ReadLockHolders[rwLock].Add(st);
      }
    }

    public static void RemoveReader(ReaderWriterLockSlim rwLock, StackTrace st)
    {
      lock (ReadLockHolders)
      {
        if (ReadLockHolders.ContainsKey(rwLock))
          ReadLockHolders[rwLock].Remove(st);
      }
    }
  }
#endif 

  /// <summary>
  /// Helper structure to enter and exit a <see cref="ReaderWriterLockSlim"/> using <see cref="IDisposable"/> pattern.
  /// Attention:
  /// There is no handling of failures to retrieve lock, so at this time full thread safety is NOT guaranteed.
  /// </summary>
  public struct ReadContext : IDisposable
  {
    private readonly ReaderWriterLockSlim _rwLock;
    private readonly bool _lockTaken;
#if DEBUG
    private readonly StackTrace _st;
#endif

    public ReadContext(ReaderWriterLockSlim rwLock, int timeout)
    {
      _rwLock = rwLock;
      _lockTaken = _rwLock.TryEnterReadLock(timeout);
#if DEBUG
      _st = new StackTrace();
      if (!_lockTaken)
        UPnPConfiguration.LOGGER.Warn("UPnP: Could not enter read lock. Caller: " + _st);
      else
        LockDebug.AddReader(_rwLock, _st);
#else
      if (!_lockTaken)
        UPnPConfiguration.LOGGER.Warn("UPnP: Could not enter read lock. Caller: " + new StackTrace());
#endif
    }

    public void Dispose()
    {
      if (_lockTaken)
        _rwLock.ExitReadLock();
#if DEBUG
      LockDebug.RemoveReader(_rwLock, _st);
#endif
    }
  }

  /// <summary>
  /// Helper structure to enter and exit a <see cref="ReaderWriterLockSlim"/> using <see cref="IDisposable"/> pattern.
  /// Attention:
  /// There is no handling of failures to retrieve lock, so at this time full thread safety is NOT guaranteed.
  /// </summary>
  public struct UpgradeAbleReadContext : IDisposable
  {
    private readonly ReaderWriterLockSlim _rwLock;
    private readonly bool _lockTaken;

    public UpgradeAbleReadContext(ReaderWriterLockSlim rwLock, int timeout)
    {
      _rwLock = rwLock;
      _lockTaken = _rwLock.TryEnterUpgradeableReadLock(timeout);
#if DEBUG
      if (!_lockTaken)
        UPnPConfiguration.LOGGER.Warn("UPnP: Could not enter read lock. Caller: " + new StackTrace());
#endif
    }

    public void Dispose()
    {
      if (_lockTaken)
        _rwLock.ExitUpgradeableReadLock();
    }
  }

  /// <summary>
  /// Helper structure to enter and exit a <see cref="ReaderWriterLockSlim"/> using <see cref="IDisposable"/> pattern.
  /// Attention:
  /// There is no handling of failures to retrieve lock, so at this time full thread safety is NOT guaranteed.
  /// </summary>
  public struct WriteContext : IDisposable
  {
    private readonly ReaderWriterLockSlim _rwLock;
    private readonly bool _lockTaken;

    public WriteContext(ReaderWriterLockSlim rwLock, int timeout)
    {
      _rwLock = rwLock;
      _lockTaken = _rwLock.TryEnterWriteLock(timeout);
#if DEBUG
      if (!_lockTaken)
        UPnPConfiguration.LOGGER.Warn("UPnP: Could not enter write lock. Caller: " + new StackTrace());
#endif
    }

    public void Dispose()
    {
      if (_lockTaken)
        _rwLock.ExitWriteLock();
    }
  }

  public static class ReaderWriterLockExtensions
  {
    public static IDisposable EnterRead(this ReaderWriterLockSlim _lock, int maxMs = 2000)
    {
      return new ReadContext(_lock, maxMs);
    }
    public static IDisposable EnterUpgradeAbleRead(this ReaderWriterLockSlim _lock, int maxMs = 2000)
    {
      return new UpgradeAbleReadContext(_lock, maxMs);
    }
    public static IDisposable EnterWrite(this ReaderWriterLockSlim _lock, int maxMs = 2000)
    {
      return new WriteContext(_lock, maxMs);
    }
  }
}
