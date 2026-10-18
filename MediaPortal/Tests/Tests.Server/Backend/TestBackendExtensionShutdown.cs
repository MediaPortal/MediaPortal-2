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

using MediaPortal.Backend;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using NUnit.Framework;

namespace Tests.Server.Backend
{
  /// <summary>
  /// <see cref="BackendExtension.ShutdownBackendServices"/> runs on the way out of a startup that may
  /// have failed, so it must not depend on the services it is asking for having been registered.
  /// </summary>
  /// <remarks>
  /// Measured, twice: once in the Linux container run and once on Windows when two servers collided on
  /// a port. In both cases the actual failure was one <c>FATAL</c> line above a
  /// <c>ServiceNotFoundException: Could not find the IUserProfileDataManagement service</c> thrown from
  /// shutdown, which is the worst possible order for those two lines to appear in a log.
  /// </remarks>
  [TestFixture]
  public class TestBackendExtensionShutdown
  {
    private ILogger _ambientLogger;

    [SetUp]
    public void SetUp()
    {
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

    [Test]
    public void TestShutdownWithNothingRegisteredDoesNotThrow()
    {
      // The whole point: no database, no user profile service, no client manager, no media library, no
      // backend server. Shutdown used to throw on the first of them.
      Assert.That(BackendExtension.ShutdownBackendServices, Throws.Nothing);
    }

    /// <summary>
      /// What the container actually hit: the failed startup had already taken the logging service away
      /// by the time shutdown asked for it, which replaced one exception with another.
    /// </summary>
    [Test]
    public void TestShutdownWithoutALoggerDoesNotThrow()
    {
      ServiceRegistration.Remove<ILogger>();

      Assert.That(BackendExtension.ShutdownBackendServices, Throws.Nothing);

      // Put the ambient logger back for the fixtures that run after this one.
      if (_ambientLogger != null)
        ServiceRegistration.Set(_ambientLogger);
    }
  }
}
