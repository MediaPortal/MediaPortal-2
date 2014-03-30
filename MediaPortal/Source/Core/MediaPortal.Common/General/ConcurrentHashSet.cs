#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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

using System.Collections.Generic;
using System.Threading;

namespace MediaPortal.Common.General
{
  /// <summary>
  /// This class provides a thread-safe wrapper for a HashSet.
  /// </summary>
  public class ConcurrentHashSet<T>
  {
    private const int LOCK_SLEEP_TIME = 5;
    private readonly HashSet<T> _set = new HashSet<T>();
    private int _owner;

    #region Lock Helpers
    private void AcquireLock()
    {
      int thread = Thread.CurrentThread.ManagedThreadId;
      while( Interlocked.CompareExchange( ref _owner, thread, 0 ) != 0 )
      {
        Thread.Sleep( LOCK_SLEEP_TIME );
      }
    }

    private void ReleaseLock()
    {
      Interlocked.Exchange( ref _owner, 0 );
    }
    #endregion

    public bool Add( T item )
    {
      AcquireLock();
      var result = _set.Add( item );      
      ReleaseLock();
      return result;
    }

    public bool Remove( T item )
    {
      AcquireLock();
      var result = _set.Remove( item );
      ReleaseLock();
      return result;
    }

    public bool Contains( T item )
    {
      AcquireLock();
      var result = _set.Contains( item );
      ReleaseLock();
      return result;
    }

    public int Count
    {
      get
      {
        AcquireLock();
        var result = _set.Count;
        ReleaseLock();
        return result;
      }
    }
  }
}
