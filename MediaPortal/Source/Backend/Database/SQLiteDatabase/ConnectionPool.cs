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
using System.Data.Common;
using MediaPortal.Common;
using MediaPortal.Common.Logging;

namespace MediaPortal.Database.SQLite
{
  /// <summary>
  /// A generic object pool in particular for <see cref="DbConnection"/>s
  /// </summary>
  /// <remarks>
  /// ConnectionPool maintains a pool of open connections to a database. If a connection is requested
  /// from the pool, it tries to return an already opened connection from the pool. If no more connection
  /// is available from the pool, a new connection is instantiated, opened and returned. After usage, the
  /// connection has to be returned to the pool.
  /// A delegate may be specified, which is used to instantiate and initialize new connections. 
  /// A maximum number of open connections may be specified.
  /// This class is thread save. If a connection is requested from the pool, the maximum number of connections
  /// has already been instantiated but all open connections are currently in use, the requesting thread is
  /// blocked until a connection is returned to the pool. A maximum timeout may be specified. If no connection is returned
  /// to the pool during this timeout, a <see cref="TimeoutException"/> is thrown.
  /// The caller has to ensure that a connection is returned to the pool only once. The ConnectionPool does not
  /// check for duplicates, because there is no "ConcurrentSet" available in .NET, a <see cref="ConcurrentBag{T}"/>
  /// is used as backing store for a <see cref="BlockingCollection{T}"/>instead because this is the fastest
  /// implementation of a ConcurrentCollection.
  /// </remarks>
  public class ConnectionPool<T> : IDisposable where T:IDisposable
  {
    #region Constants

    private const int DEFAULT_MAX_CONNECTIONS = 10;
    private const int DEFAULT_CONNECTION_TIMEOUT = 30000;

    #endregion

    #region Delegates

    private readonly Func<T> _connectionGenerator;

    #endregion

    #region Variables

    private readonly BlockingCollection<T> _pool;

    // Maximum number of connections in the pool (standard: 10)
    private readonly int _maxConnections = DEFAULT_MAX_CONNECTIONS;

    // Time in ms the pool will wait for a connection to be returned
    // when _maxConnections is reached (standard: 30.000)
    private readonly int _connectionTimeout = DEFAULT_CONNECTION_TIMEOUT;
    
    // Current number of connections
    private int _numberOfConnections;

    // Indicates whether the ConnectionPool was already disposed
    private bool _disposed;

    #endregion

    #region Constructors/Destructors

    /// <summary>
    /// Instantiates a new ConnectionPool with the standard values for
    /// <see cref="_maxConnections"/> (10) and <see cref="_connectionTimeout"/> (30 seconds)
    /// </summary>
    /// <param name="connectionGenerator">
    /// Delegate used to instantiate a new connection and initialize it
    /// </param>
    public ConnectionPool(Func<T> connectionGenerator)
    {
      if (connectionGenerator == null)
        throw new ArgumentNullException("connectionGenerator");

      _connectionGenerator = connectionGenerator;
      _pool = new BlockingCollection<T>(new ConcurrentBag<T>());
    }

    /// <summary>
    /// Instantiates a new ConnectionPool with the supplied values for
    /// <see cref="_maxConnections"/> and <see cref="_connectionTimeout"/>
    /// </summary>
    /// <param name="connectionGenerator">
    /// Delegate used to instantiate a new connection and initialize it
    /// </param>
    /// <param name="maxConnections">Maximum number of connections in the pool</param>
    /// <param name="connectionTimeout">
    /// Time in ms the pool will wait for a connection
    /// to be returned when <see cref="_maxConnections"/> is reached
    /// </param>
    public ConnectionPool(Func<T> connectionGenerator, int maxConnections, int connectionTimeout) : this(connectionGenerator)
    {
      if (maxConnections < 1)
        throw new ArgumentOutOfRangeException("maxConnections");
      if (connectionTimeout < 1)
        throw new ArgumentOutOfRangeException("connectionTimeout");

      _maxConnections = maxConnections;
      _connectionTimeout = connectionTimeout;
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Provide the caller with an opened and initialized database connection
    /// </summary>
    public T GetConnection()
    {
      if (_disposed)
        throw new ObjectDisposedException("ConnectionPool");

      T result;
      
      // If a connection is available in the pool, take and return this connection
      if (_pool.TryTake(out result))
        return result;

      // No connection available in the pool. If _maxConnections is not reached, yet,
      // instantiate and initialize a new connection and return it to the caller.
      if (_numberOfConnections < _maxConnections)
      {
        _numberOfConnections++;
        ServiceRegistration.Get<ILogger>().Debug("SQLiteDatabase: {0} connections in use", _numberOfConnections);
        return _connectionGenerator();
      }

      // No connection available in the pool and _maxConnections is already reached.
      // Wait _connectionTimeout ms for a connection to be returned to the pool.
      if (_pool.TryTake(out result, _connectionTimeout))
        return result;

      // _connectionTiemout ms later and still no connection returned to the pool.
      throw new TimeoutException("ConnectionPool");
    }

    /// <summary>
    /// Return a used database connection to the pool
    /// </summary>
    /// <param name="connection">Connection to be returned</param>
    public void PutConnection(T connection)
    {
      _pool.Add(connection);
    }

    #endregion

    #region IDisposable Implementation

    /// <summary>
    /// Disposes of all the connections in the pool. System.Data.SQLite makes sure that
    /// during disposing the connections are closed.
    /// </summary>
    public void Dispose()
    {
      if (!_disposed)
      {
        _disposed = true;
        for (int i = 1; i <= _numberOfConnections; i++)
        {
          T connection;
          if (!_pool.TryTake(out connection, _connectionTimeout))
            throw new TimeoutException("Disposing ConnectionPool failed. Probably a connection wasn't returned to the pool.");
          connection.Dispose();
        }
      }
    }

    #endregion
  }
}
