#region Copyright (C) 2007-2013 Team MediaPortal

/*
    Copyright (C) 2007-2013 Team MediaPortal
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
using System.Management;
using System.Runtime.InteropServices;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Settings;

namespace MediaPortal.Database.SQLite
{
  class SQLiteSettings
  {

    #region Constants

    // The following Constants are the default values for the respective settings

    // Default page size used in the database file
    // Set page size to NTFS cluster size = 4096 bytes; supposed to give better performance
    // For BLOBs > 50kb a page size of 8192 is said to give more performance (http://www.sqlite.org/intern-v-extern-blob.html)
    private const int DEFAULT_PAGE_SIZE = 4096;

    // Default time in ms the database will wait for a lock
    // If a lock cannot be obtained, the database enginge waits DEFAULT_LOCK_TIMEOUT ms before it throws an exception  
    private const int DEFAULT_LOCK_TIMEOUT = 30000;

    // Default name of the database file
    private const string DEFAULT_DATABASE_FILE_NAME = "Datastore.s3db";

    // Default SQL commands to be executed on every connection to initialize it.
    // There are currently three commands:
    // We set locking_mode=EXCLUSIVE; This means that the database file can only be accessed by one
    // connection. To be able to access the database with multiple connections this setting MUST
    // NOT be used without shared cache mode. In shared cache mode, multiple connections within one
    // process (even if they are used in multiple threads of that process) are seen as one connection
    // towards the operating system. That way, we can access the database with multiple connections
    // in multiple threads, but at the same time have an exclusive lock on the database file, which
    // improves the performance materially.
    // Additionally we set the wal_autocheckpoint to 32768, i.e. every time a commit leads to
    // a .wal file which is bigger than 32768 pages, a checkpoint is performed.
    // Finally, we tell SQLite to store all its temporary files in RAM instead of writing them to disk.
    private const string DEFAULT_INITIALIZATION_COMMAND = "PRAGMA locking_mode=EXCLUSIVE;PRAGMA wal_autocheckpoint=32768;PRAGMA temp_store=MEMORY;";

    #endregion

    #region Construnctors/Destructors

    public SQLiteSettings()
    {
      CacheSizeInKiloBytes = GetOptimalCacheSizeInKiloBytes(GetRamInMegaBytes());
    }

    #endregion

    #region Public properties

    [Setting(SettingScope.Global, DEFAULT_PAGE_SIZE)]
    public int PageSize { get; set; }

    [Setting(SettingScope.Global, DEFAULT_LOCK_TIMEOUT)]
    public int LockTimeout { get; set; }

    [Setting(SettingScope.Global, DEFAULT_DATABASE_FILE_NAME)]
    public string DatabaseFileName { get; set; }

    [Setting(SettingScope.Global, DEFAULT_INITIALIZATION_COMMAND)]
    public string InitializationCommand { get; set; }

    [Setting(SettingScope.Global)]
    public int CacheSizeInKiloBytes { get; set; }

    [Setting(SettingScope.Global, false)]
    public bool EnableTraceLogging { get; set; }

    public int CacheSizeInPages
    {
      get
      {
        return CacheSizeInKiloBytes * 1024 / PageSize;
      }
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Calculates the optimal cache size for the SQLiteDatabase in KiloBytes
    /// </summary>
    /// <remarks>
    /// RAM up to 512MB: CacheSize 32MB
    /// RAM up to 1GB: CacheSize 64MB
    /// RAM up to 2GB: CacheSize 128MB
    /// RAM over 2GB: CacheSize 256MB
    /// </remarks>
    /// <returns>Optimal cache size in KiloBytes</returns>
    private int GetOptimalCacheSizeInKiloBytes(int availableRamInMegabytes)
    {
      if (availableRamInMegabytes <= 512)
        return 32 * 1024;
      if (availableRamInMegabytes <= 1024)
        return 64 * 1024;
      if (availableRamInMegabytes <= 2048)
        return 128 * 1024;
      return 256 * 1024;
    }

    /// <summary>
    /// This class is only used as parameter to pInvoke <see cref="GlobalMemoryStatusEx"/>
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private class MemoryStatusEx
    {
      public uint dwLength;
      public uint dwMemoryLoad;
      public ulong ullTotalPhys;
      public ulong ullAvailPhys;
      public ulong ullTotalPageFile;
      public ulong ullAvailPageFile;
      public ulong ullTotalVirtual;
      public ulong ullAvailVirtual;
      public ulong ullAvailExtendedVirtual;
      public MemoryStatusEx()
      {
        dwLength = (uint)Marshal.SizeOf(typeof(MemoryStatusEx));
      }
    }

    /// <summary>
    /// Native method used to detect the available RAM in <see cref="GetRamInMegaBytes"/>
    /// </summary>
    /// <param name="lpBuffer">Object of type <see cref="MemoryStatusEx"/></param>
    /// <returns><c>true</c> if the call was successful and <see cref="lpBuffer"/> was filled correctly, otherwise false</returns>
    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx([In, Out] MemoryStatusEx lpBuffer);

    /// <summary>
    /// Determines the amount of RAM available to the operating system in total.
    /// </summary>
    /// <returns>Total RAM in MegaBytes</returns>
    private int GetRamInMegaBytes()
    {
      int result = 0;
      var mem = new MemoryStatusEx();
      try
      {
        if(GlobalMemoryStatusEx(mem))
          result = Convert.ToInt32(mem.ullTotalPhys / 1048576);
        else
          ServiceRegistration.Get<ILogger>().Warn("SQLiteDatabase: Error when trying to detect the total available RAM. Using minimum cache size for SQLiteDatabase.");
      }
      catch (Exception)
      {
        ServiceRegistration.Get<ILogger>().Warn("SQLiteDatabase: Exception when trying to detect the total available RAM. Using minimum cache size for SQLiteDatabase.");
      }
      return result;
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Logs all the settings contained in this class to the Logfile
    /// </summary>
    public void LogSettings()
    {
      ServiceRegistration.Get<ILogger>().Info("SQLiteDatabase: Database Filename: '{0}' (Default Database Filename: '{1}')", DatabaseFileName, DEFAULT_DATABASE_FILE_NAME);
      ServiceRegistration.Get<ILogger>().Info("SQLiteDatabase: PageSize: {0} Bytes (Default PageSize: {1} Bytes)", PageSize, DEFAULT_PAGE_SIZE);
      ServiceRegistration.Get<ILogger>().Info("SQLiteDatabase: CacheSize: {0} pages = {1}KB (RAM: {2}MB, Default CacheSize: {3}KB)", CacheSizeInPages, CacheSizeInKiloBytes, GetRamInMegaBytes(), GetOptimalCacheSizeInKiloBytes(GetRamInMegaBytes()));
      ServiceRegistration.Get<ILogger>().Info("SQLiteDatabase: LockTimeout: {0}ms (Default LockTimeout: {1}ms)", LockTimeout, DEFAULT_LOCK_TIMEOUT);
      ServiceRegistration.Get<ILogger>().Info("SQLiteDatabase: Initialization Command: '{0}' (Default Initialization Command: '{1}')", InitializationCommand, DEFAULT_INITIALIZATION_COMMAND);
    }

    #endregion

  }
}
