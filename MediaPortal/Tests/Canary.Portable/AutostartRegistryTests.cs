#region Copyright (C) 2007-2021 Team MediaPortal

/*
    Copyright (C) 2007-2021 Team MediaPortal
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
using System.Text;
using MediaPortal.Utilities.SystemAPI;
using NUnit.Framework;

namespace Canary.Portable
{
  /// <summary>
  /// Pins both halves of <see cref="AutostartRegistry"/>: the <c>CurrentVersion\Run</c> value on
  /// Windows and the XDG desktop entry on Linux.
  ///
  /// Each platform branch is asserted on the platform that runs it, with <see cref="Assume"/> skipping
  /// the other one. That is deliberate: the desktop entry code can never execute on Windows and the
  /// registry code can never execute on Linux, so a test forcing both everywhere would have to fake the
  /// very decision the class is built around. The Linux container runs the XDG tests, which is the
  /// platform whose behaviour was newly written here.
  ///
  /// The XDG tests do not touch a real profile. The base directory specification defines
  /// <c>XDG_CONFIG_HOME</c> and <c>XDG_CONFIG_DIRS</c> as the locations to use, so pointing them at a
  /// temp directory is the supported way to redirect them, not a hook added for testing.
  /// </summary>
  [TestFixture]
  public class AutostartRegistryTests
  {
    private string _configHome;
    private string _configDirs;
    private string _originalConfigHome;
    private string _originalConfigDirs;

    [SetUp]
    public void RedirectXdgDirectoriesToTemp()
    {
      _originalConfigHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
      _originalConfigDirs = Environment.GetEnvironmentVariable("XDG_CONFIG_DIRS");
      _configHome = Path.Combine(Path.GetTempPath(), "mp2-autostart-user-" + Guid.NewGuid().ToString("N"));
      _configDirs = Path.Combine(Path.GetTempPath(), "mp2-autostart-machine-" + Guid.NewGuid().ToString("N"));
      Environment.SetEnvironmentVariable("XDG_CONFIG_HOME", _configHome);
      Environment.SetEnvironmentVariable("XDG_CONFIG_DIRS", _configDirs);
    }

    [TearDown]
    public void RestoreXdgDirectories()
    {
      Environment.SetEnvironmentVariable("XDG_CONFIG_HOME", _originalConfigHome);
      Environment.SetEnvironmentVariable("XDG_CONFIG_DIRS", _originalConfigDirs);
      TryDelete(_configHome);
      TryDelete(_configDirs);
    }

    private static void TryDelete(string path)
    {
      try
      {
        if (Directory.Exists(path))
          Directory.Delete(path, true);
        else if (File.Exists(path))
          File.Delete(path);
      }
      catch (IOException)
      {
        // Temp leftovers are not worth failing a test over.
      }
    }

    private static string NewRegisterName(string kind)
    {
      return "MP2-Canary-" + kind + "-" + Guid.NewGuid().ToString("N").Substring(0, 8);
    }

    [Test]
    public void IsSupportedIsTrueOnThePlatformsThatHaveAutostart()
    {
      // Windows and Linux are the two platforms the project targets and the two this suite can run on.
      Assert.That(AutostartRegistry.IsSupported, Is.True);
    }

    [Test]
    public void XdgRegistrationRoundTripsThroughTheDesktopEntry()
    {
      Assume.That(OperatingSystem.IsLinux(), "the desktop entry branch only runs on Linux");
      string name = NewRegisterName("user");
      // A path with a space exercises the quoting, because an unquoted Exec value would come back
      // truncated at the first space.
      const string path = "/opt/MediaPortal 2/MP2-Client.exe";

      Assert.That(AutostartRegistry.GetRegisteredApplicationPath(name, true), Is.Null,
          "nothing may be registered under a name that was never used");

      AutostartRegistry.Register(path, name, true);
      try
      {
        var file = Path.Combine(_configHome, "autostart", name + ".desktop");
        Assert.That(File.Exists(file), Is.True,
            "the user-scope entry must land in $XDG_CONFIG_HOME/autostart/<name>.desktop; " + Describe(file));
        string content = File.ReadAllText(file);
        Assert.That(content, Does.Contain("[Desktop Entry]"));
        Assert.That(content, Does.Contain("Type=Application"));
        Assert.That(content, Does.Contain("Name=" + name));
        Assert.That(AutostartRegistry.GetRegisteredApplicationPath(name, true), Is.EqualTo(path));

        // Registering again replaces the entry instead of adding a second one.
        const string otherPath = "/opt/MediaPortal 2/MP2-Client-restart.exe";
        AutostartRegistry.Register(otherPath, name, true);
        Assert.That(AutostartRegistry.GetRegisteredApplicationPath(name, true), Is.EqualTo(otherPath));
      }
      finally
      {
        AutostartRegistry.Unregister(name, true);
      }

      Assert.That(AutostartRegistry.GetRegisteredApplicationPath(name, true), Is.Null);
      // Unregistering something that is not registered must not throw: the launcher calls it on the way
      // out whether or not autostart was ever switched on.
      Assert.DoesNotThrow(() => AutostartRegistry.Unregister(name, true));
    }

    [Test]
    public void XdgMachineScopeUsesTheFirstExistingConfigDir()
    {
      Assume.That(OperatingSystem.IsLinux(), "the desktop entry branch only runs on Linux");
      string name = NewRegisterName("machine");
      const string path = "/usr/bin/mp2-server";

      // The specification says the first existing directory wins, so the first one is deliberately left
      // without an autostart subdirectory.
      var second = Path.Combine(_configDirs, "second");
      Directory.CreateDirectory(Path.Combine(second, "autostart"));
      Environment.SetEnvironmentVariable("XDG_CONFIG_DIRS",
          Path.Combine(_configDirs, "first") + Path.PathSeparator + second);

      AutostartRegistry.Register(path, name, false);
      try
      {
        Assert.That(File.Exists(Path.Combine(second, "autostart", name + ".desktop")), Is.True);
        Assert.That(AutostartRegistry.GetRegisteredApplicationPath(name, false), Is.EqualTo(path));
      }
      finally
      {
        AutostartRegistry.Unregister(name, false);
      }
    }

    [Test]
    public void DesktopEntryQuotingSurvivesCharactersThatWouldBreakALine()
    {
      Assume.That(OperatingSystem.IsLinux(), "the desktop entry branch only runs on Linux");
      string name = NewRegisterName("quote");
      var builder = new StringBuilder("/tmp/mp2 ");
      builder.Append('"').Append("quoted").Append('"');
      builder.Append(" back\\slash");
      string path = builder.ToString();

      AutostartRegistry.Register(path, name, true);
      try
      {
        Assert.That(AutostartRegistry.GetRegisteredApplicationPath(name, true), Is.EqualTo(path),
            "the Exec value must come back byte for byte, or a registered path cannot be trusted");
      }
      finally
      {
        AutostartRegistry.Unregister(name, true);
      }
    }

    [Test]
    public void RegistryRegistrationRoundTrips()
    {
      Assume.That(OperatingSystem.IsWindows(), "the registry branch only runs on Windows");
      string name = NewRegisterName("user");
      const string path = @"C:\Program Files\Team MediaPortal\MediaPortal 2\MP2-Client.exe";

      try
      {
        Assert.That(AutostartRegistry.GetRegisteredApplicationPath(name, true), Is.Null);
        AutostartRegistry.Register(path, name, true);
        Assert.That(AutostartRegistry.GetRegisteredApplicationPath(name, true), Is.EqualTo(path));
      }
      finally
      {
        // This runs on a developer machine, so the value is removed even if an assertion above fails.
        AutostartRegistry.Unregister(name, true);
      }

      Assert.That(AutostartRegistry.GetRegisteredApplicationPath(name, true), Is.Null);
      Assert.DoesNotThrow(() => AutostartRegistry.Unregister(name, true));
    }

    private static string Describe(string expectedFile)
    {
      var sb = new StringBuilder("expected file ");
      sb.Append(expectedFile).Append("; contents of that directory: ");
      var dir = Path.GetDirectoryName(expectedFile);
      if (dir == null || !Directory.Exists(dir))
        return sb.Append("(directory does not exist)").ToString();
      var files = Directory.GetFiles(dir);
      if (files.Length == 0)
        return sb.Append("(empty)").ToString();
      sb.Append(string.Join(", ", Array.ConvertAll(files, Path.GetFileName)));
      return sb.ToString();
    }
  }
}
