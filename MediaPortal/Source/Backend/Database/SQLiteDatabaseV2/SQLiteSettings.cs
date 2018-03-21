#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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
using System.Runtime.InteropServices;
using System.Text;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Settings;

namespace MediaPortal.Database.SQLite
{
  public class SQLiteSettings
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
    // - We set locking_mode=EXCLUSIVE; This means that the database file can only be accessed by one
    //   connection. To be able to access the database with multiple connections this setting MUST
    //   NOT be used without shared cache mode. In shared cache mode, multiple connections within one
    //   process (even if they are used in multiple threads of that process) are seen as one connection
    //   towards the operating system. That way, we can access the database with multiple connections
    //   in multiple threads, but at the same time have an exclusive lock on the database file, which
    //   improves the performance materially.
    //   Note: This setting is disabled by default because it makes our installer hang. The installer
    //         starts our BackendServices and later tries to start the installed MP2 Server service so
    //         that a second process wants to access the database, which - as per above - can't work.
    //         After install, however, this setting can be enabled.
    //         ToDo: Improve the installer so that it doesn't start all BackendServices
    // - Additionally we set the wal_autocheckpoint to 32768, i.e. every time a commit leads to
    //   a .wal file which is bigger than 32768 pages, a checkpoint is performed.
    // - Finally, we tell SQLite to store all its temporary files in RAM instead of writing them to disk.
    private const string DEFAULT_INITIALIZATION_COMMAND = "PRAGMA wal_autocheckpoint=32768;PRAGMA temp_store=MEMORY;";

    internal const string EXCLUSIVE_MODE_COMMAND = "PRAGMA locking_mode=EXCLUSIVE;";
    private const bool DEFAULT_USE_EXCLUSIVE_MODE = false;

    private const string WAL_AUTOCHECKPOINT_COMMAND_TEMPLATE = "PRAGMA wal_autocheckpoint={0};";
    private const int DEFAULT_WAL_AUTOCHECKPOINT = 32768;

    private const string TEMP_STORE_MEMORY_COMMAND = "PRAGMA temp_store=MEMORY;";
    private const bool DEFAULT_USE_TEMP_STORE_MEMOY = true;

    private const string THREADS_COMMAND = "PRAGMA threads={0};";
    private const bool DEFAULT_USE_THREADS = true;

    // If SQLite increases the size of the database file because it needs more space, it increases it
    // by this value - even if it only needs one byte more space. Setting the chunk size to a high value
    // dramatically increases import performance for big MediaLibraries (because less system calls are necessary)
    // and at the same time increases later query speeds in particular on spinning hard discs, as it reduces
    // the fragmentation of the database file in the file system.
    // At the same time, chunk size is the minimum size of the database file. 16MB as default value is a
    // compromise between the interest of people with very small MediaLibraries not to waste disc space and
    // the interest of people with very big MediaLibraries in speed.
    private const int DEFAULT_CHUNK_SIZE_IN_MEGABYTES = 16;

    #endregion

    #region Constructors/Destructors

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

    [Setting(SettingScope.Global, DEFAULT_USE_EXCLUSIVE_MODE)]
    public bool UseExclusiveMode { get; set; }

    [Setting(SettingScope.Global, DEFAULT_WAL_AUTOCHECKPOINT)]
    public int WalAutocheckpointAfterPages { get; set; }

    [Setting(SettingScope.Global, DEFAULT_USE_TEMP_STORE_MEMOY)]
    public bool UseTempStoreMemory { get; set; }

    [Setting(SettingScope.Global, DEFAULT_USE_THREADS)]
    public bool UseMultiThreading { get; set; }

    [Setting(SettingScope.Global)]
    public int CacheSizeInKiloBytes { get; set; }

    [Setting(SettingScope.Global, DEFAULT_CHUNK_SIZE_IN_MEGABYTES)]
    public int ChunkSizeInMegabytes { get; set; }

#if DEBUG
    [Setting(SettingScope.Global, true)]
#else
    [Setting(SettingScope.Global, false)]
#endif
    public bool EnableDebugLogging { get; set; }

    [Setting(SettingScope.Global, false)]
    public bool EnableTraceLogging { get; set; }

    public string InitializationCommand
    {
      get
      {
        var sb = new StringBuilder();
        if (UseExclusiveMode)
          sb.Append(EXCLUSIVE_MODE_COMMAND);
        sb.AppendFormat(WAL_AUTOCHECKPOINT_COMMAND_TEMPLATE, WalAutocheckpointAfterPages);
        if (UseTempStoreMemory)
          sb.Append(TEMP_STORE_MEMORY_COMMAND);
        if (UseMultiThreading)
          sb.AppendFormat(THREADS_COMMAND, 1);
        return sb.ToString();
      }
    }

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
        if (GlobalMemoryStatusEx(mem))
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
      ServiceRegistration.Get<ILogger>().Info("SQLiteDatabase: ChunkSize: {0}MB (Default ChunkSize: {1}MB)", ChunkSizeInMegabytes, DEFAULT_CHUNK_SIZE_IN_MEGABYTES);
      ServiceRegistration.Get<ILogger>().Info("SQLiteDatabase: LockTimeout: {0}ms (Default LockTimeout: {1}ms)", LockTimeout, DEFAULT_LOCK_TIMEOUT);
      ServiceRegistration.Get<ILogger>().Info("SQLiteDatabase: Initialization Command: '{0}' (Default Initialization Command: '{1}')", InitializationCommand, DEFAULT_INITIALIZATION_COMMAND);
    }

    #endregion
  }
}
