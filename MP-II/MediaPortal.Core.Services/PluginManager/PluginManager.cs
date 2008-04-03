#region Copyright (C) 2007-2008 Team MediaPortal

/*
 *  Copyright (C) 2007-2008 Team MediaPortal
 *  http://www.team-mediaportal.com
 *
 *  This file is part of MediaPortal II
 *
 *  MediaPortal II is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  MediaPortal II is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
 * 
 */

#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.Core.PathManager;
using MediaPortal.Core.Localisation;
using MediaPortal.Core.PluginManager;
using MediaPortal.Services.PluginManager.PluginDetails;
using MediaPortal.Services.PluginManager.PluginSpace;

namespace MediaPortal.Services.PluginManager
{
  /// <summary>
  /// A <see cref="IPluginManager"/> implementation that uses .plugin files to find
  /// what plug-ins are available
  /// </summary>
  public class PluginManager: IPluginManager
  {
    private List<string> _pluginFiles;
    private List<string> _disabledPlugins;
    private PluginTree _pluginTree;

    public PluginManager()
    {
      AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
      AppDomain.CurrentDomain.AssemblyLoad += CurrentDomain_AssemblyLoad;
    }

		~PluginManager()
		{
			AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;
      AppDomain.CurrentDomain.AssemblyLoad -= CurrentDomain_AssemblyLoad;
    }

    #region Event handlers
    /// <summary>
    /// Method to hook in the resolving mechanism of the system to find plugin dll files
    /// in our plugins folder.
    /// </summary>
    /// <param name="sender">The <see cref="ResolveEventHandler"/> sender argument. Not
    /// interesting here.</param>
    /// <param name="args">Holds the information about the assembly to resolve.</param>
    /// <returns></returns>
    Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
    {
      string dllName = ExtractDllNameFromAssemblyName(args.Name);
      if (dllName != null)
      {
        string[] folders = System.IO.Directory.GetDirectories("plugins");
        for (int i = 0; i < folders.Length; ++i)
        {
          // Calculate the directory where the plugins are located.
          string fname = String.Format(@"{0}\{1}\{2}.dll", System.IO.Directory.GetCurrentDirectory(), folders[i], dllName);
          if (System.IO.File.Exists(fname))
          {
            return Assembly.LoadFile(fname);
          }
        }
      }
      return null;
    }

    /// <summary>
    /// Check if the loaded assembly is one of our plugins. Mark the plugin as "loaded".
    /// </summary>
    /// <param name="sender">The <see cref="AssemblyLoadEventHandler"/> sender argument.
    /// Not interesting here.</param>
    /// <param name="args">Holds the information about the assembly which was loaded.</param>
    void CurrentDomain_AssemblyLoad(object sender, AssemblyLoadEventArgs args)
    {
      if (_pluginTree != null)
      {
        string dllName = args.LoadedAssembly.CodeBase;
        foreach (PluginInfo info in _pluginTree.Plugins)
        {
          if (info.Name == dllName)
            info.Loaded = true;
        }
      }
    }
    #endregion

    #region Private
    /// <summary>
    /// Given an assembly name in the form "assembly-text-name, Version, Culture, PublicKeyToken",
    /// this method extracts the first part ("assembly-text-name" in this case).
    /// </summary>
    /// <param name="assemblyName">Assembly name in the form
    /// "assembly-text-name, Version, Culture, PublicKeyToken".</param>
    /// <returns>Extracted first part in the <paramref name="assemblyName"/>
    /// parameter, which should be the name of the assembly dll file.</returns>
    static string ExtractDllNameFromAssemblyName(string assemblyName)
    {
      int pos = assemblyName.IndexOf(",");
      if (pos == -1)
        return null;
      return assemblyName.Substring(0, pos);
    }

    /// <summary>
    /// Loads all the available plugins
    /// </summary>
    /// <remarks>
    /// Note that by using an attribute to hold the plugin name and description, we 
    /// do not need to actually start the plugin to get it's name and description
    /// </remarks>
    private void LoadPlugins()
    {
      //Test data -> get from config in future
      _pluginFiles = new List<string>();
      _disabledPlugins = new List<string>();

      DirectoryInfo plugins = ServiceScope.Get<IPathManager>().GetDirectoryInfo(@"<APPLICATION_ROOT>\Plugins");
      foreach(DirectoryInfo pluginDirectory in plugins.GetDirectories())
      {
        foreach(FileInfo pluginFile in pluginDirectory.GetFiles("*.plugin"))
        {
          _pluginFiles.Add(pluginFile.FullName);
        }
       
        // should be moved to better location in plugintree.load
        // add resources section to .plugin file?
        if (System.IO.Directory.Exists(Path.Combine(pluginDirectory.FullName, "Language")))
        {
          ServiceScope.Get<ILocalisation>().AddDirectory(Path.Combine(pluginDirectory.FullName, "Language"));
        }
      }

      _pluginTree = new PluginTree();
      _pluginTree.Load(_pluginFiles, _disabledPlugins);

      ServiceScope.Add<IPluginTree>(_pluginTree);
    }

    #endregion

    #region IPluginManager Members

    public object GetPluginItem<T>(string location, string id)
    {
      return _pluginTree.BuildItem<T>(location, id, null, false);
    }

    public List<T> GetAllPluginItems<T>(string location)
    {
      return _pluginTree.BuildItems<T>(location, null, false);
    }

    /// <summary>
    /// Gets an enumerable list of available plugins
    /// </summary>
    /// <returns>An <see cref="IEnumerable<IPlugin>"/> list.</returns>
    /// <remarks>A configuration program can use this list to present the user a list of available plugins that he can (de)activate.</remarks>
    public IEnumerable<IPluginInfo> GetAvailablePlugins()
    {
      return null; // pluginInfo.Values;
    }

    /// <summary>
    /// Starts all plug-ins that are activated by the user.
    /// </summary>
    public void Startup()
    {
      ServiceScope.Get<ILogger>().Debug("Plugin Manager: Startup");
      //ServiceScope.Get<IMessageBroker>().Register(this);
      LoadPlugins();
    }

    /// <summary>
    /// Stops all plug-ins
    /// </summary>
    public void StopAll()
    {
      // Is this method required?
      // old code needs to re-written
      //foreach (IPlugin plugin in runningPlugins.Values)
      //{
      //  plugin.Dispose();
      //}
      //runningPlugins.Clear();
    }

		/// <summary>
		/// Starts a PlugIn by name
		/// </summary>
		/// <param name="name">PlugIn Name</param>
		/// <returns>true if PlugIn was started</returns>
		public bool StartPlugIn(string name)
		{
			ServiceScope.Get<ILogger>().Debug("Plugin Manager: StartPlugIn({0})", name);
			// let us check if plugin is already existing and loaded
			if (_pluginTree != null)
			{
				foreach (PluginInfo info in _pluginTree.Plugins)
				{
					if (info.Name == name)
					{
						if (info.Enabled) return true;     // nothing to do for us 
						else
						{
							// to be started
							info.Enabled = true;
							return true;
						}
					}
				}
				// plugin not loaded up to now => what to do?
			}
			
			return false;
		}

		/// <summary>
		/// Stops PlugIn by name.
		/// </summary>
		/// <param name="name">PlugIn Name</param>
		/// <returns>true if PlugIn was stopped</returns>
		public bool StopPlugIn(string name)
		{
			ServiceScope.Get<ILogger>().Debug("Plugin Manager: StopPlugIn({0})", name);
			if (_pluginTree != null)
			{
				foreach (PluginInfo info in _pluginTree.Plugins)
				{
					if (info.Name == name)
					{
						info.Enabled = false;
						_pluginTree.RemovePlugin(info);
						return true;
					}
				}
			}
			return false;
		}
		#endregion

		#region IStatus Implementation
		public List<string> GetStatus()
		{
			List<string> status = new List<string>();
			status.Add("=== PlugInManager");
			if (_pluginTree != null)
			{
				foreach (PluginInfo info in _pluginTree.Plugins)
				{
					status.Add(String.Format("     {0}, Running = {1}, Loaded = {2}", info.ToString(), info.Enabled, info.Loaded));
				}
			}
			return status;
		}
		#endregion
	}
}
