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
using System.IO;
using MediaPortal.Common;
using MediaPortal.Common.Services.PathManager;
using NUnit.Framework;

namespace Tests.Server.Utilities
{
  /// <summary>
  /// Tests for the separators <see cref="PathManager"/> answers with.
  /// </summary>
  /// <remarks>
  /// On Windows a backslash and a slash mean the same thing, so every one of these paths looks fine there.
  /// On Linux a backslash is an ordinary character of a file name, which turned the request for the SQL
  /// scripts of a subschema - written as <c>&lt;APPLICATION_ROOT&gt;\Scripts\</c> - into one file called
  /// <c>/srv/mediaportal\Scripts\</c>. The database could not be created and the server did not start.
  /// </remarks>
  [TestFixture]
  public class TestPathManagerSeparators
  {
    private string _root;
    private PathManager _pathManager;

    [SetUp]
    public void SetUp()
    {
      _root = Path.Combine(Path.GetTempPath(), "mp2-paths-" + Guid.NewGuid().ToString("N"));
      Directory.CreateDirectory(_root);
      _pathManager = new PathManager();
      _pathManager.SetPath(PathLabel, _root);
    }

    [TearDown]
    public void TearDown()
    {
      Directory.Delete(_root, true);
    }

    private const string PathLabel = "<TEST_ROOT>";

    [Test]
    public void SubPathBehindALabelIsAPathForTheRunningPlatform()
    {
      string expected = Path.Combine(_root, "Scripts");

      // How the subschemas ask for their SQL scripts.
      string resolved = _pathManager.GetPath(PathLabel + @"\Scripts\");

      Assert.That(resolved.TrimEnd(Path.DirectorySeparatorChar), Is.EqualTo(expected));
      if (!OperatingSystem.IsWindows())
        Assert.That(resolved, Does.Not.Contain("\\"),
            "a backslash is a character of a file name here, not a separator");
    }

    [Test]
    public void ASlashBehindALabelResolvesToTheSameDirectory()
    {
      // Windows keeps the slash there, as it always did: on Windows both separators are accepted by the file
      // APIs, so a path manager that returned the platform separator everywhere would be a bigger change
      // than the bug it fixes. What matters is that the same directory is meant.
      string expected = Path.Combine(_root, "Scripts");
      string resolved = _pathManager.GetPath(PathLabel + "/Scripts/")
          .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

      Assert.That(resolved.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar),
          Is.EqualTo(expected));
    }

    [Test]
    public void LabelsCanNestAndKeepTheirSubPaths()
    {
      _pathManager.SetPath("<SUBSCHEMA>", _pathManager.GetPath(PathLabel + @"\Scripts\"));

      string resolved = _pathManager.GetPath("<SUBSCHEMA>\\MediaPortal-Basis-create-1.0.sql");

      Assert.That(resolved, Is.EqualTo(Path.Combine(_root, "Scripts", "MediaPortal-Basis-create-1.0.sql")));
    }

    [Test]
    public void AStoredLabelDoesNotEndWithASeparatorSoThatSubPathsCanAttach()
    {
      _pathManager.SetPath("<WITH_SEPARATOR>", _root + Path.DirectorySeparatorChar);
      Assert.That(_pathManager.GetPath("<WITH_SEPARATOR>"), Is.EqualTo(_root));
    }

    [Test]
    public void AnUnknownLabelIsStillAnError()
    {
      Assert.That(() => _pathManager.GetPath("<DOES_NOT_EXIST>\\Scripts"),
          Throws.TypeOf<ArgumentException>());
    }
  }
}