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
using MediaPortal.Common.Exceptions;
using NUnit.Framework;

namespace Tests.Server.Common
{
  /// <summary>
  /// Tests for the exception reporting of the launchers. It runs without a logger and without a log file,
  /// which is exactly the state of a host that failed while starting.
  /// </summary>
  /// <remarks>
  /// Reporting used to recurse into the exception itself instead of into its inner exception, so the first
  /// exception that had an inner one ended the process with "Stack overflow." and buried the reason of the
  /// failure under thousands of log lines - measured when the server failed to bind its HTTP port.
  /// </remarks>
  [TestFixture]
  public class TestLauncherExceptionHandling
  {
    #region Helpers

    /// <summary>
    /// Exception chain of <paramref name="levels"/> levels, the outermost first. The messages say which
    /// level they came from, so a report can be checked for reporting each level once.
    /// </summary>
    private static Exception Chain(int levels)
    {
      Exception exception = null;
      for (int level = levels; level >= 1; level--)
        exception = new Exception("level-" + level + " failed", exception);
      return exception;
    }

    private static int CountOccurrences(string text, string search)
    {
      int count = 0;
      int index = text.IndexOf(search, StringComparison.Ordinal);
      while (index >= 0)
      {
        count++;
        index = text.IndexOf(search, index + search.Length, StringComparison.Ordinal);
      }
      return count;
    }

    #endregion

    [Test]
    public void EveryExceptionOfTheChainIsReported()
    {
      string report = LauncherExceptionHandling.ExceptionInfo("starting the core failed", Chain(3), false);

      Assert.That(report, Does.Contain("level-1 failed"));
      Assert.That(report, Does.Contain("level-2 failed"));
      Assert.That(report, Does.Contain("level-3 failed"));
    }

    [Test]
    public void EveryExceptionOfTheChainIsReportedExactlyOnce()
    {
      string inner = LauncherExceptionHandling.WriteInnerException(Chain(3).InnerException);

      Assert.That(CountOccurrences(inner, "level-2 failed"), Is.EqualTo(1));
      Assert.That(CountOccurrences(inner, "level-3 failed"), Is.EqualTo(1));
    }

    [Test]
    public void AnExceptionWithoutInnerExceptionIsReportedAsOneLine()
    {
      Assert.That(LauncherExceptionHandling.WriteInnerException(new Exception("only one")),
          Is.EqualTo("  only one" + Environment.NewLine));
    }

    [Test]
    public void NoInnerExceptionIsNoReport()
    {
      Assert.That(LauncherExceptionHandling.WriteInnerException(null), Is.Empty);
    }

    [Test]
    public void ReportingStopsAtALimitSoThatItCannotBringDownTheProcess()
    {
      // A chain this deep cannot be built by accident, but reporting it must still return instead of
      // running out of stack while building the string.
      string inner = LauncherExceptionHandling.WriteInnerException(Chain(MaxLevels + 10).InnerException);
      Assert.That(CountOccurrences(inner, "failed"), Is.EqualTo(MaxLevels));
    }

    private const int MaxLevels = 50;
  }
}