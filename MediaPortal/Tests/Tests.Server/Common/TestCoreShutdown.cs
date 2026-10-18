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

using MediaPortal.Common;
using MediaPortal.Common.Logging;
using NUnit.Framework;

namespace Tests.Server.Common
{
  /// <summary>
  /// <see cref="ApplicationCore.StopCoreServices"/> has to work after a startup that never finished:
  /// the host calls it on the way out of a failed startup, and the services it stops are exactly the
  /// ones a failed startup may not have registered.
  /// </summary>
  /// <remarks>
  /// Observed in the Linux container run. With the HTTP server missing, startup aborted, shutdown asked
  /// for <c>IRemoteResourceInformationService</c> strictly and threw - a second <c>FATAL</c> two lines
  /// below the one that explained the failure. Fixing the equivalent case in
  /// <see cref="MediaPortal.Backend.BackendExtension.ShutdownBackendServices"/> had surfaced this one,
  /// which is how it became clear the teardown has to be best-effort throughout rather than patched one
  /// missing service at a time.
  /// </remarks>
  [TestFixture]
  public class TestCoreShutdown
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
    public void TestStopCoreServicesWithNothingRegisteredDoesNotThrow()
    {
      Assert.That(ApplicationCore.StopCoreServices, Throws.Nothing);
    }
  }
}
