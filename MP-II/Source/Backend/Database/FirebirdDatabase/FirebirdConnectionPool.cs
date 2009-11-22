#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using System.Threading;
using FirebirdSql.Data.FirebirdClient;
using MediaPortal.Utilities.Exceptions;

namespace MediaPortal.Database.Firebird
{
  /// <summary>
  /// A thread-save connection pool of <see cref="FbConnection"/> instances.
  /// </summary>
  public class FirebirdConnectionPool : IDisposable
  {
    protected IList<FbConnection> _availableConnections;
    protected IList<FbConnection> _usedConnections;
    protected object _syncRoot = new object();

    public FirebirdConnectionPool(string connectionString, int numConnections)
    {
      if (numConnections <= 0)
        throw new ArgumentException("The thread pool must contain at least one connection", "numConnections");
      _availableConnections = new List<FbConnection>(numConnections);
      _usedConnections = new List<FbConnection>(numConnections);
      for (int i = 0; i < numConnections; i++)
      {
        FbConnection con = new FbConnection(connectionString);
        con.Open();
        _availableConnections.Add(con);
      }
    }

    ~FirebirdConnectionPool()
    {
      Dispose();
    }

    public void Dispose()
    {
      Close();
    }

    public void Close()
    {
      foreach (FbConnection connection in _availableConnections)
        connection.Close();
      _availableConnections.Clear();
      foreach (FbConnection connection in _usedConnections)
        connection.Close();
      _usedConnections.Clear();
      lock (_syncRoot)
        Monitor.PulseAll(_syncRoot);
    }

    public FbConnection AcquireConnection()
    {
      lock (_syncRoot)
      {
        while (_availableConnections.Count == 0)
          if (_usedConnections.Count == 0)
            throw new EnvironmentException("The thread pool is being shut down");
          Monitor.Wait(_syncRoot);
        FbConnection result = _availableConnections[0];
        _availableConnections.RemoveAt(0);
        _usedConnections.Add(result);
        return result;
      }
    }

    public void ReleaseConnection(FbConnection connection)
    {
      lock (_syncRoot)
      {
        _usedConnections.Remove(connection);
        _availableConnections.Add(connection);
        Monitor.Pulse(_syncRoot);
      }
    }
  }
}