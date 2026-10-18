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
using System.Collections.Generic;
using System.IO;
using System.Text;
using MediaPortal.Utilities.Exceptions;
using Microsoft.Win32;

namespace MediaPortal.Utilities.SystemAPI
{
  /// <summary>
  /// "Start this application when the user logs in", answered per platform.
  /// </summary>
  /// <remarks>
  /// This used to be three methods on <see cref="WindowsAPI"/>, which meant the last
  /// <c>CA1416</c> call sites in <c>MediaPortal.Utilities</c> sat in a class named after one operating
  /// system and kept the whole assembly pinned to a <c>-windows</c> target. Reading the callers first
  /// (there are three: the client's autostart setting, ServiceMonitor's app controller and the client
  /// launcher, all of which pass <c>user: true</c>) showed that the feature is not "registry access"
  /// but autostart - and autostart has a real non-Windows equivalent, so this is a translation rather
  /// than a stub:
  /// <list type="bullet">
  /// <item><description>Windows: the <c>...\CurrentVersion\Run</c> value. That code was moved here
  /// unchanged, including the exception it throws when the key is missing.</description></item>
  /// <item><description>Linux: a desktop entry in the XDG autostart directory. User scope is
  /// <c>$XDG_CONFIG_HOME/autostart</c> (default <c>~/.config/autostart</c>), machine scope comes from
  /// <c>$XDG_CONFIG_DIRS</c> (default <c>/etc/xdg/autostart</c>) - the same "this user" vs "every user
  /// of the machine" split the <c>user</c> parameter already expressed on Windows, and the same
  /// "writing for everybody needs privilege" property. The path lives in the entry's <c>Exec</c> key
  /// instead of a registry value.</description></item>
  /// <item><description>macOS and Android: not supported. Android has no user-facing autostart; macOS
  /// would need <c>~/Library/LaunchAgents</c> property lists, a format that cannot be verified by the
  /// Linux container this port is checked in, so it is reported as unsupported instead of guessed.
  /// Unsupported means reading answers "nothing registered" - the same answer an unregistered
  /// application gives on every platform - while registering says so out loud rather than quietly
  /// doing nothing.</description></item>
  /// </list>
  /// The Linux branch is exercised on every platform by
  /// <c>MediaPortal\Tests\Canary.Portable\AutostartRegistryTests.cs</c>: a desktop entry is plain file
  /// I/O, and the specification itself provides the redirection (<c>XDG_CONFIG_HOME</c>,
  /// <c>XDG_CONFIG_DIRS</c>) that keeps the test out of a real user profile.
  /// </remarks>
  public static class AutostartRegistry
  {
    /// <summary>
    /// Registry key holding the autostart values on Windows. Kept here because this class is what reads
    /// and writes it; <see cref="WindowsAPI.AUTOSTART_REGISTRY_KEY"/> still names the same key.
    /// </summary>
    public const string AUTOSTART_REGISTRY_KEY = @"Software\Microsoft\Windows\Currentversion\Run";

    private const string DESKTOP_ENTRY_EXTENSION = ".desktop";

    /// <summary>
    /// Gets whether registering an application for autostart is possible on the current platform.
    /// </summary>
    public static bool IsSupported
    {
      get { return OperatingSystem.IsWindows() || OperatingSystem.IsLinux(); }
    }

    /// <summary>
    /// Registers an application to be started at login.
    /// </summary>
    /// <param name="applicationPath">Path of the application to start.</param>
    /// <param name="registerName">Name identifying the application.</param>
    /// <param name="user"><c>true</c> to register for the current user only, <c>false</c> for every
    /// user of the machine.</param>
    /// <exception cref="PlatformNotSupportedException">Thrown if the platform offers no autostart
    /// mechanism.</exception>
    /// <exception cref="EnvironmentException">Thrown if the autostart location cannot be
    /// written.</exception>
    public static void Register(string applicationPath, string registerName, bool user)
    {
      if (OperatingSystem.IsWindows())
      {
        RegistryKey root = user ? Registry.CurrentUser : Registry.LocalMachine;
        // open RegisteryKey with write access
        using (RegistryKey key = root.OpenSubKey(AUTOSTART_REGISTRY_KEY, true))
        {
          if (key == null)
            throw new EnvironmentException(@"Unable to access/create registry key '{0}\{1}'",
                user ? "HKCU" : "HKLM", AUTOSTART_REGISTRY_KEY);
          key.SetValue(registerName, applicationPath, RegistryValueKind.ExpandString);
        }
        return;
      }

      if (!IsSupported)
        throw new PlatformNotSupportedException(
            string.Format("There is no autostart mechanism on {0}.", Environment.OSVersion.Platform));

      var dir = AutostartDirectory(user);
      var builder = new StringBuilder();
      builder.AppendLine("[Desktop Entry]");
      builder.AppendLine("Type=Application");
      builder.AppendLine("Name=" + registerName);
      // Key-file quoting, so that a path containing spaces stays one line and can be read back exactly.
      builder.AppendLine("Exec=" + Quote(applicationPath));
      builder.AppendLine("X-GNOME-Autostart-enabled=true");
      try
      {
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, registerName + DESKTOP_ENTRY_EXTENSION), builder.ToString());
      }
      catch (Exception ex)
      {
        throw new EnvironmentException(@"Unable to write autostart entry '{0}\{1}'", ex, dir,
            registerName + DESKTOP_ENTRY_EXTENSION);
      }
    }

    /// <summary>
    /// Removes an application from the autostart registration. Does nothing if it is not registered,
    /// which matches the registry branch and the "unregister on exit" calls that fire regardless of
    /// whether anything was ever registered.
    /// </summary>
    /// <exception cref="PlatformNotSupportedException">Thrown if the platform offers no autostart
    /// mechanism.</exception>
    /// <exception cref="EnvironmentException">Thrown if the autostart entry exists but cannot be
    /// removed.</exception>
    public static void Unregister(string registerName, bool user)
    {
      if (OperatingSystem.IsWindows())
      {
        RegistryKey root = user ? Registry.CurrentUser : Registry.LocalMachine;
        // open RegisteryKey with write access
        using (RegistryKey key = root.OpenSubKey(AUTOSTART_REGISTRY_KEY, true))
        {
          if (key == null)
            throw new EnvironmentException(@"Unable to access registry key '{0}\{1}'",
                user ? "HKCU" : "HKLM", AUTOSTART_REGISTRY_KEY);
          key.DeleteValue(registerName, false);
        }
        return;
      }

      if (!IsSupported)
        throw new PlatformNotSupportedException(
            string.Format("There is no autostart mechanism on {0}.", Environment.OSVersion.Platform));

      var file = Path.Combine(AutostartDirectory(user), registerName + DESKTOP_ENTRY_EXTENSION);
      try
      {
        if (File.Exists(file))
          File.Delete(file);
      }
      catch (Exception ex)
      {
        throw new EnvironmentException(@"Unable to remove autostart entry '{0}'", ex, file);
      }
    }

    /// <summary>
    /// Returns the path registered for autostart under <paramref name="registerName"/>, or <c>null</c>
    /// if nothing is registered.
    /// </summary>
    public static string GetRegisteredApplicationPath(string registerName, bool user)
    {
      if (OperatingSystem.IsWindows())
      {
        RegistryKey root = user ? Registry.CurrentUser : Registry.LocalMachine;
        using (RegistryKey key = root.OpenSubKey(AUTOSTART_REGISTRY_KEY))
          return key == null ? null : key.GetValue(registerName) as string;
      }

      if (!IsSupported)
        return null;

      var file = Path.Combine(AutostartDirectory(user), registerName + DESKTOP_ENTRY_EXTENSION);
      try
      {
        if (!File.Exists(file))
          return null;
        foreach (var line in File.ReadLines(file))
        {
          if (!line.StartsWith("Exec=", StringComparison.Ordinal))
            continue;
          return Unquote(line.Substring("Exec=".Length).Trim());
        }
      }
      catch (IOException)
      {
        // "Nothing registered" beats an exception on a settings page; a registry value that cannot be
        // read fails the same way for its callers.
        return null;
      }
      return null;
    }

    /// <summary>
    /// Resolves the autostart directory following the XDG base directory specification.
    /// </summary>
    private static string AutostartDirectory(bool user)
    {
      if (user)
      {
        var configHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
        if (string.IsNullOrWhiteSpace(configHome))
        {
          // Not SpecialFolder.ApplicationData: on Unix that already *is* ~/.config, while on Windows it
          // is AppData\Roaming. UserProfile is the home directory on both.
          var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
          if (string.IsNullOrEmpty(home))
            home = Environment.GetEnvironmentVariable("HOME");
          configHome = Path.Combine(home, ".config");
        }
        return Path.Combine(configHome, "autostart");
      }

      // Machine scope: /etc/xdg/autostart unless XDG_CONFIG_DIRS says otherwise. Writing there needs
      // the same privilege the HKLM branch needed.
      var configDirs = Environment.GetEnvironmentVariable("XDG_CONFIG_DIRS");
      var dirs = new List<string>();
      if (!string.IsNullOrWhiteSpace(configDirs))
        dirs.AddRange(configDirs.Split(Path.PathSeparator));
      else
        dirs.Add(Path.Combine(Path.DirectorySeparatorChar.ToString(), "etc", "xdg"));
      foreach (var dir in dirs)
      {
        var candidate = Path.Combine(dir, "autostart");
        if (Directory.Exists(candidate))
          return candidate;
      }
      return Path.Combine(dirs[0], "autostart");
    }

    private static string Quote(string value)
    {
      var sb = new StringBuilder("\"");
      foreach (var c in value)
      {
        switch (c)
        {
          case '"': sb.Append("\\\""); break;
          case '\\': sb.Append("\\\\"); break;
          case '\t': sb.Append("\\t"); break;
          case '\n': sb.Append("\\n"); break;
          default: sb.Append(c); break;
        }
      }
      return sb.Append('"').ToString();
    }

    private static string Unquote(string value)
    {
      if (value.Length < 2 || value[0] != '"' || value[value.Length - 1] != '"')
        return value;
      var inner = value.Substring(1, value.Length - 2);
      var sb = new StringBuilder();
      for (int i = 0; i < inner.Length; i++)
      {
        if (inner[i] != '\\' || i == inner.Length - 1)
        {
          sb.Append(inner[i]);
          continue;
        }
        i++;
        switch (inner[i])
        {
          case 'n': sb.Append('\n'); break;
          case 't': sb.Append('\t'); break;
          default: sb.Append(inner[i]); break;
        }
      }
      return sb.ToString();
    }
  }
}
