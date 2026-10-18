/*
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace MediaPortal.Frontend.Player
{
  /// <summary>
  /// Finding libVLC on the machine this runs on.
  /// </summary>
  /// <remarks>
  /// <para>
  /// libVLC is not vendored and not downloaded: it is a few hundred files (the core, plus a plugin per decoder,
  /// demuxer and output) under a GPL licence, so it arrives the way VLC always arrives - installed. What this
  /// class owns is the answer to "where", and an honest sentence when the answer is "nowhere". A player that
  /// cannot find its engine is a supported state; a <see cref="DllNotFoundException"/> on a TV is a crash with
  /// no explanation.
  /// </para>
  /// <para>
  /// The order is fixed and each step is the reason the next one exists: an explicit directory from the caller
  /// (a portable install, a test), then <c>MP2_LIBVLC</c> (the same reason, without recompiling), then the
  /// platform's own usual places, then - on the platforms whose loader can search - "no directory, ask the
  /// loader". Windows is deliberately last-resort-less: there is no loader search for <c>libvlc.dll</c>, so on
  /// Windows "not found" is the real answer and says which folders were looked at.
  /// </para>
  /// </remarks>
  public static class NativeLibVlc
  {
    /// <summary>Environment variable holding a directory to look in before the usual places.</summary>
    public const string EnvironmentVariableName = "MP2_LIBVLC";

    /// <summary>File names that mean "the engine is in this folder", per platform.</summary>
    public static IReadOnlyList<string> LibraryFileNames
    {
      get
      {
        if (OperatingSystem.IsWindows())
          return new[] { "libvlc.dll" };
        if (OperatingSystem.IsMacCatalyst() || OperatingSystem.IsIOS())
          // iOS/tvOS ship libVLC inside the app bundle; there is no folder to find.
          return Array.Empty<string>();
        return new[] { "libvlc.so.5", "libvlc.so" };
      }
    }

    /// <summary>
    /// The folders this platform puts libVLC in, in the order they should be trusted. Empty where the platform
    /// has no such notion (Android, where the app package carries the libraries).
    /// </summary>
    public static IReadOnlyList<string> DefaultDirectories()
    {
      if (OperatingSystem.IsWindows())
      {
        List<string> directories = new List<string>();
        foreach (string? programFiles in new[]
        {
          Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
          Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
        })
        {
          if (!string.IsNullOrEmpty(programFiles))
            directories.Add(Path.Combine(programFiles!, "VideoLAN", "VLC"));
        }
        string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (!string.IsNullOrEmpty(local))
          directories.Add(Path.Combine(local, "Programs", "VideoLAN", "VLC"));
        return directories;
      }

      if (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS() || OperatingSystem.IsMacCatalyst())
        return Array.Empty<string>();

      // Linux and the BSD-ish hosts: the multiarch tuple differs per architecture, so all the plausible ones
      // are listed rather than guessed from the process bitness.
      return new[]
      {
        "/usr/lib/x86_64-linux-gnu",
        "/usr/lib/aarch64-linux-gnu",
        "/usr/lib/arm-linux-gnueabihf",
        "/usr/lib64",
        "/usr/lib",
        "/usr/local/lib"
      };
    }

    /// <summary>
    /// Where the engine is, or why this run has to go without it.
    /// </summary>
    /// <param name="configuredDirectory">
    /// A directory from the caller (<see cref="PlayerOptions.LibraryDirectory"/>). Null to skip that step.
    /// </param>
    /// <param name="directory">
    /// The folder to load from, or null meaning "no folder - let the platform's loader find it". Null is a
    /// working answer on Linux and Android and not one on Windows.
    /// </param>
    /// <param name="detail">
    /// What was looked at and what came of it. Always filled, because this string is what a user or a log reads
    /// when playback is missing.
    /// </param>
    /// <returns>True when a player may be built; false when it cannot, with the reason in <paramref name="detail"/>.</returns>
    public static bool TryFind(string? configuredDirectory, out string? directory, out string detail)
    {
      directory = null;

      string? configured = configuredDirectory;
      if (string.IsNullOrWhiteSpace(configured))
        configured = Environment.GetEnvironmentVariable(EnvironmentVariableName);

      if (!string.IsNullOrWhiteSpace(configured))
      {
        if (ContainsEngine(configured!, out string? file))
        {
          directory = configured;
          detail = $"'{file}' found in the configured directory '{configured}'";
          return true;
        }
        detail =
            $"the configured libVLC directory '{configured}' does not contain " +
            $"{string.Join(" or ", LibraryFileNames)}";
        return false;
      }

      // The app packages of the mobile platforms carry the libraries themselves; there is nothing to locate and
      // nothing to fail.
      if (OperatingSystem.IsAndroid())
      {
        detail = "on Android the app package provides libVLC; no directory to find";
        return true;
      }

      foreach (string candidate in DefaultDirectories())
      {
        if (ContainsEngine(candidate, out string? file))
        {
          directory = candidate;
          detail = $"'{file}' found in '{candidate}'";
          return true;
        }
      }

      if (OperatingSystem.IsWindows())
      {
        IReadOnlyList<string> searched = DefaultDirectories();
        detail = searched.Count == 0
            ? "no folder to search on this Windows host, and " + EnvironmentVariableName + " is not set"
            : $"libVLC was not found in {string.Join(", ", searched)}; install VLC or set " +
              EnvironmentVariableName + " to its folder";
        return false;
      }

      // No folder on this host holds it, but the loader may still resolve the SONAME - a distro package installs
      // exactly that. Whether it resolves is decided when the first call is made, and reported as a failure then.
      directory = null;
      detail =
          "not found in " + string.Join(", ", DefaultDirectories()) +
          "; asking the system loader for " + string.Join(" or ", LibraryFileNames);
      return true;
    }

    /// <summary>True when this folder holds one of the engine's library files.</summary>
    public static bool ContainsEngine(string directory, out string? libraryFile)
    {
      libraryFile = null;
      if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
        return false;
      foreach (string name in LibraryFileNames)
      {
        if (File.Exists(Path.Combine(directory, name)))
        {
          libraryFile = name;
          return true;
        }
      }
      return false;
    }
  }
}
