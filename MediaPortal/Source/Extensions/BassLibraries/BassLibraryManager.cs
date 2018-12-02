#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using MediaPortal.Utilities;
using MediaPortal.Utilities.FileSystem;
using Un4seen.Bass;

namespace MediaPortal.Extensions.BassLibraries
{
  /// <summary>
  /// Manages the Bass audio library and its plugins.
  /// </summary>
  public class BassLibraryManager : IDisposable
  {
    #region Static members

    private static readonly object _syncObj = new object();
    private static volatile BassLibraryManager _bassLibraryManager = null;
    private readonly ICollection<int> _decoderPluginHandles = new List<int>();

    [Obsolete("Player plugins are now located in BassLibraries plugin. Setting other folder is no longer supported.")]
    public static BassLibraryManager Get(string playerPluginsDirectory)
    {
      return Get();
    }

    /// <summary>
    /// Loads and initializes the Bass library.
    /// </summary>
    /// <returns>The new instance.</returns>
    public static BassLibraryManager Get()
    {
      lock (_syncObj)
      {
        if (_bassLibraryManager != null)
          return _bassLibraryManager;
        string playerPluginsDirectory = FileUtils.BuildAssemblyRelativePath("Plugins");
        _bassLibraryManager = new BassLibraryManager();
        _bassLibraryManager.Initialize(playerPluginsDirectory);
        return _bassLibraryManager;
      }
    }

    #endregion

    #region Private members

    private BassLibraryManager()
    {
    }

    private void Initialize(string playerPluginsDirectory)
    {
      // Register BASS.Net, necessary to avoid splash screen
      BassRegistration.BassRegistration.Register();

      // Initialize the NoSound device to enable calling BASS functions before our output device is properly initialized
      if (!Bass.BASS_Init(BassConstants.BassNoSoundDevice, 44100, 0, IntPtr.Zero))
        throw new BassLibraryException("BASS_Init");

      // Set volume to linear scale as requested by the IVolumeControl interface
      if (!Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_CURVE_VOL, false))
        throw new BassLibraryException("BASS_SetConfig");

      // Disable update thread while the player is not running
      if (!Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_UPDATEPERIOD, 0))
        throw new BassLibraryException("BASS_SetConfig");

      LoadPlugins(playerPluginsDirectory);
    }

    private void LoadPlugins(string playerPluginsDirectory)
    {
      Log.Info("Loading player add-ins from '{0}'", playerPluginsDirectory);

      if (!Directory.Exists(playerPluginsDirectory))
      {
        Log.Error("Unable to find player add-ins folder '{0}'", playerPluginsDirectory);
        return;
      }

      // Fix for MP2-285: loading of some BASS plugin fails because unmanaged dlls are not found in executable folder.
      // The working directory is temporary changed while loading plugins.
      string currentDirectory = Directory.GetCurrentDirectory();
      Directory.SetCurrentDirectory(playerPluginsDirectory);

      IDictionary<int, string> plugins = Bass.BASS_PluginLoadDirectory(playerPluginsDirectory);
      foreach (string pluginFile in plugins.Values)
        Log.Debug("Loaded plugin '{0}'", pluginFile);
      CollectionUtils.AddAll(_decoderPluginHandles, plugins.Keys);

      if (plugins.Count == 0)
        Log.Info("No audio decoders loaded; probably already loaded.");
      else
        Log.Info("Loaded {0} audio decoders.", plugins.Count);

      Directory.SetCurrentDirectory(currentDirectory);
    }

    #endregion

    #region IDisposable Members

    public void Dispose()
    {
      lock (_syncObj)
      {
        Log.Debug("BassLibraryManager.Dispose()");

        Log.Debug("Unloading all BASS player plugins");
        foreach (int pluginHandle in _decoderPluginHandles)
          Bass.BASS_PluginFree(pluginHandle);
        _decoderPluginHandles.Clear();

        // Free the NoSound device
        if (!Bass.BASS_SetDevice(BassConstants.BassNoSoundDevice))
          throw new BassLibraryException("BASS_SetDevice");

        if (!Bass.BASS_Free())
          throw new BassLibraryException("BASS_Free");

        _bassLibraryManager = null;
      }
    }

    #endregion
  }
}
