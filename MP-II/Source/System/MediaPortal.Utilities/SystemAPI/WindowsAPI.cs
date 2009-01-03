#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
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
using System.Text;
using MediaPortal.Utilities.Exceptions;
using MediaPortal.Utilities.Win32;
using Microsoft.Win32;

namespace MediaPortal.Utilities.SystemAPI
{
  /// <summary>
  /// For calls to the Windows API, this class should be used instead of directly using
  /// the underlaying system's API. This class hides the concrete underlaying system and will
  /// use different system functions depending on the underlaying system.
  /// </summary>
  public static class WindowsAPI
  {
    public const string AUTOSTART_REGISTRY_KEY = @"Software\Microsoft\Windows\Currentversion\Run";
    public const string BALLOONTIPS_REGISTRY_HIVE = @"Software\Microsoft\Windows\Currentversion\Explorer\Advanced";
    public const string BALLOONTIPS_REGISTRY_NAME = "EnableBalloonTips";

    public const int S_OK = 0x0;
    public const int S_FALSE = 0x1;

    public const int MAX_PATH = 260;

    /// <summary>
    /// Use this enum to denote special system folders.
    /// </summary>
    public enum SpecialFolder
    {
      MyMusic,
      MyVideos,
      MyPictures,
    }

    /// <summary>
    /// Returns the path of the given system's special folder.
    /// </summary>
    /// <param name="folder">Folder to retrieve.</param>
    /// <param name="folderPath">Will be set to the folder path if the result value is <c>true</c>.</param>
    /// <returns><c>true</c>, if the specified special folder could be retrieved. Else <c>false</c>
    /// will be returned.</returns>
    public static bool GetSpecialFolder(SpecialFolder folder, out string folderPath)
    {
      folderPath = null;
      switch (folder)
      {
        case SpecialFolder.MyMusic:
          folderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
          return true;
        case SpecialFolder.MyPictures:
          folderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
          return true;
        case SpecialFolder.MyVideos:
          StringBuilder sb = new StringBuilder(MAX_PATH);
          if (Win32API.SHGetFolderPath(IntPtr.Zero, Win32API.CSIDL_MYVIDEO, IntPtr.Zero, Win32API.SHGFP_TYPE_CURRENT, sb) == S_OK)
          {
            folderPath = sb.ToString();
            return true;
          }
          return false;
        default:
          throw new NotImplementedException(string.Format(
              "The handling for special folder '{0}' isn't implemented yet", folder.ToString()));
      }
    }

    /// <summary>
    /// Gets the information if Balloon Tips are enabled in this system, or sets the option in the registry.
    /// </summary>
    /// <exception cref="EnvironmentException">If the appropriate registry key cannot accessed
    /// (will only be thrown in the setter).</exception>
    public static bool IsShowBalloonTips
    {
      get
      {
        using (RegistryKey key = Registry.CurrentUser.OpenSubKey(BALLOONTIPS_REGISTRY_HIVE))
          return key == null ? false : (int) key.GetValue(BALLOONTIPS_REGISTRY_NAME, 0) != 0;
      }
      set
      {
        using (RegistryKey key = Registry.CurrentUser.CreateSubKey(BALLOONTIPS_REGISTRY_HIVE))
        {
          if (key == null)
            throw new EnvironmentException(@"Unable to access/create registry key 'HKCU\{0}'",
                BALLOONTIPS_REGISTRY_HIVE);
          if (value)
            key.SetValue(BALLOONTIPS_REGISTRY_NAME, 0, RegistryValueKind.DWord);
          else
            key.DeleteValue(BALLOONTIPS_REGISTRY_NAME, false);
        }
      }
    }

    /// <summary>
    /// Adds the application with the specified <paramref name="applicationPath"/> to the autostart
    /// registry key. The application will be automatically started the next system startup.
    /// </summary>
    /// <param name="applicationPath">Path of the application to be auto-started.</param>
    /// <param name="registerName">The name used in the registry as key for the autostart value.</param>
    /// <param name="user">If set to <c>true</c>, the autostart application will be added to the HCKU
    /// registry hive, else it will be added to the HKLM hive.</param>
    /// <exception cref="EnvironmentException">If the appropriate registry key cannot accessed.</exception>
    public static void AddAutostartApplication(string applicationPath, string registerName, bool user)
    {
      RegistryKey root = user ? Registry.CurrentUser : Registry.LocalMachine;
      using (RegistryKey key = root.OpenSubKey(AUTOSTART_REGISTRY_KEY))
      {
        if (key == null)
          throw new EnvironmentException(@"Unable to access/create registry key '{0}\{1}'",
              user ? "HKCU" : "HKLM", AUTOSTART_REGISTRY_KEY);
        key.SetValue(registerName, applicationPath, RegistryValueKind.ExpandString);
      }
    }

    /// <summary>
    /// Removes an application from the autostart registry key.
    /// </summary>
    /// <param name="registerName">The name used in the registry as key for the autostart value.</param>
    /// <param name="user">If set to <c>true</c>, the autostart application will be removed from the HCKU
    /// registry hive, else it will be removed from the HKLM hive.</param>
    /// <exception cref="EnvironmentException">If the appropriate registry key cannot accessed.</exception>
    public static void RemoveAutostartApplication(string registerName, bool user)
    {
      RegistryKey root = user ? Registry.CurrentUser : Registry.LocalMachine;
      using (RegistryKey key = root.OpenSubKey(AUTOSTART_REGISTRY_KEY))
      {
        if (key == null)
          throw new EnvironmentException(@"Unable to access registry key '{0}\{1}'",
              user ? "HKCU" : "HKLM", AUTOSTART_REGISTRY_KEY);
        key.DeleteValue(registerName, false);
      }
    }

    /// <summary>
    /// Returns the application path for the application registered to be autostarted with the
    /// specified <paramref name="registerName"/>.
    /// </summary>
    /// <param name="registerName">The name used in the registry as key for the autostart value.</param>
    /// <param name="user">If set to <c>true</c>, the autostart application path will be searched in the HCKU
    /// registry hive, else it will be searched in the HKLM hive.</param>
    /// <returns>Application path registered to be autostarted with the specified
    /// <paramref name="registerName"/>.</returns>
    public static string GetAutostartApplicationPath(string registerName, bool user)
    {
      RegistryKey root = user ? Registry.CurrentUser : Registry.LocalMachine;
      using (RegistryKey key = root.OpenSubKey(AUTOSTART_REGISTRY_KEY))
        return key == null ? null : key.GetValue(registerName) as string;
    }
  }
}