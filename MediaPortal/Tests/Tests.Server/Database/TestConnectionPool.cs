#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Database.SQLite;
using NUnit.Framework;

namespace Tests.Server.Database
{
  /// <summary>
  /// Behaviour tests for <see cref="ConnectionPool{T}"/>, which holds the SQLite connections the
  /// database layer checks out per operation.
  /// </summary>
  /// <remarks>
  /// The pool used to be dead code: <c>NO_POOL</c> was defined, so connections came from the driver's
  /// internal pool. It is the only pooling the provider has now, because the driver's pool hands back
  /// connections with the registered collations removed - measured as "no such collation sequence:
  /// NOCASE" for the next user of the connection. That makes the pool's limits and its disposal worth
  /// asserting.
  /// </remarks>
  [TestFixture]
  public class TestConnectionPool
  {
    #region Helpers

    /// <summary>
    /// Stand-in for a connection. The pool only ever calls <see cref="IDisposable.Dispose"/>, so a
    /// fake keeps the assertions about handle lifetimes exact.
    /// </summary>
    private sealed class FakeConnection : IDisposable
    {
      public bool Disposed;

      public void Dispose()
      {
        Disposed = true;
      }
    }

    private ILogger _ambientLogger;

    [SetUp]
    public void SetUp()
    {
      // The pool logs each connection it opens, and the registry has no logger in a bare test run.
      _ambientLogger = ServiceRegistration.Get<ILogger>(false);
      ServiceRegistration.Remove<ILogger>();
      ServiceRegistration.Set<ILogger>(new NoLogger());
    }

    [TearDown]
    public void TearDown()
    {
      if (_ambientLogger != null)
        ServiceRegistration.Set(_ambientLogger);
      else
        ServiceRegistration.Remove<ILogger>();
    }

    #endregion

    [Test]
    public void TestReturnedConnectionIsReusedBeforeAnotherIsCreated()
    {
      int created = 0;
      using (var pool = new ConnectionPool<FakeConnection>(() => { created++; return new FakeConnection(); }, 2, 100))
      {
        var first = pool.GetConnection();
        var second = pool.GetConnection();
        Assert.That(second, Is.Not.SameAs(first));
        Assert.That(created, Is.EqualTo(2));

        pool.PutConnection(first);
        Assert.That(pool.GetConnection(), Is.SameAs(first), "a checked-in connection is handed out again");
        Assert.That(created, Is.EqualTo(2), "and no new one is created while one is available");

        pool.PutConnection(second);
        pool.PutConnection(first);
      }
    }

    [Test]
    public void TestDisposingThePoolDisposesThePooledConnections()
    {
      var connections = new FakeConnection[2];
      int index = 0;
      var pool = new ConnectionPool<FakeConnection>(() => connections[index++] = new FakeConnection(), 2, 100);

      var first = pool.GetConnection();
      var second = pool.GetConnection();
      pool.PutConnection(first);
      pool.PutConnection(second);

      pool.Dispose();

      Assert.That(connections.Select(c => c.Disposed), Is.All.EqualTo(true),
          "an open SqliteConnection keeps the database file locked, so the pool has to close what it holds");
      Assert.That(() => pool.GetConnection(), Throws.InstanceOf<ObjectDisposedException>());
    }

    [Test]
    public void TestDisposingThePoolWithAConnectionOutstandingSaysSo()
    {
      var pool = new ConnectionPool<FakeConnection>(() => new FakeConnection(), 1, 100);
      pool.GetConnection();

      // Not a nicety: the caller that never returns a connection is the reason the file stays locked,
      // and a silent Dispose would hide that bug.
      Assert.That(pool.Dispose, Throws.InstanceOf<TimeoutException>());
    }

    [Test]
    public void TestMoreRequestsThanTheMaximumWaitForAConnection()
    {
      var pool = new ConnectionPool<FakeConnection>(() => new FakeConnection(), 1, 100);
      var held = pool.GetConnection();

      Assert.That(pool.GetConnection, Throws.InstanceOf<TimeoutException>());

      // Once the connection comes back, the next caller gets it instead of timing out.
      pool.PutConnection(held);
      Assert.That(pool.GetConnection(), Is.SameAs(held));
      pool.PutConnection(held);
    }

    /// <summary>
    /// The limit has to be enforced against concurrent callers. "Read the count, then increment" let
    /// every thread through the gate at once, which with open SQLite connections means more open
    /// handles than the configuration allows.
    /// </summary>
    [Test]
    public void TestConcurrentCallersDoNotExceedTheMaximum()
    {
      const int maximum = 3;
      int created = 0;
      int peak = 0;

      var pool = new ConnectionPool<FakeConnection>(() =>
      {
        // Widen the window in which the threads overlap, so a non-atomic reservation shows up.
        Thread.Sleep(20);
        int now = Interlocked.Increment(ref created);
        int previousPeak;
        while (now > (previousPeak = Volatile.Read(ref peak)))
          Interlocked.CompareExchange(ref peak, now, previousPeak);
        return new FakeConnection();
      }, maximum, 10000);

      var errors = new System.Collections.Concurrent.ConcurrentBag<Exception>();
      Parallel.For(0, 8, _ =>
      {
        try
        {
          var connection = pool.GetConnection();
          Thread.Sleep(10);
          pool.PutConnection(connection);
        }
        catch (Exception e)
        {
          errors.Add(e);
        }
      });

      Assert.That(errors, Is.Empty, "all eight callers eventually get a connection: {0}",
          string.Join("; ", errors.Select(e => e.GetType().Name + ": " + e.Message)));
      Assert.That(Volatile.Read(ref peak), Is.LessThanOrEqualTo(maximum),
          "the pool must not open more connections than its maximum");

      pool.Dispose();
    }
  }
}
