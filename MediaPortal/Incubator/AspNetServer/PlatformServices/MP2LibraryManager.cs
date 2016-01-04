#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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
using System.Linq;
using System.Reflection;
using MediaPortal.Common;
using MediaPortal.Common.PluginManager;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.PlatformAbstractions;

namespace MediaPortal.Plugins.AspNetServer.PlatformServices
{
  /// <summary>
  /// Implementation of <see cref="ILibraryManager"/> the scope of which is limited to assemblies loaded into the current application domain
  /// </summary>
  /// <remarks>
  /// Some components of ASP.Net (e.g. MVC) use the ILibraryManager to find libraries, in which certain classes
  /// (e.g. classes derived from Controller) can be contained. By restricting the scope of this ILibraryManager
  /// to loaded assemblies, all classes to be used by ASP.Net must be contained in assemblies that have been
  /// loaded into the current application domain before the WebApplication using them is started. In MP2, this
  /// is taken care of by the PluginManager.
  /// </remarks>
  public class MP2LibraryManager : ILibraryManager
  {
    #region Inner classes

    public class LoadedAssemblyLibrary : Library
    {
      public LoadedAssemblyLibrary(Assembly assembly)
        : base(assembly.GetName().Name, 
               assembly.GetName().Version.ToString(),
               assembly.Location,
               "Assembly",
               assembly.GetReferencedAssemblies().Select(assemblyName => assemblyName.Name),
               new List<AssemblyName> { assembly.GetName() })
      {
      }
    }

    #endregion

    #region Private fields

    private readonly ILogger _log;

    #endregion

    #region Constructor

    public MP2LibraryManager(ILoggerFactory loggerfactory)
    {
      _log = loggerfactory.CreateLogger<MP2LibraryManager>();

      // This is a temporary workaround to make sure our MP2LibraryManager and in particular MP2LibraryExporter have access
      // to all assemblies directly or indirectly referenced by this plugin or any plugin that depends on this plugin.
      // ToDo: Remove this once Microsoft has removed the dependency to ILibraryManager and ILibraryExporter
      LoadAllReferencesFor(Assembly.GetExecutingAssembly());
      var thisPlugin = ServiceRegistration.Get<IPluginManager>().AvailablePlugins.First(kvp => kvp.Value.Metadata.PluginId == Guid.Parse("F2F6988F-C436-4D74-9819-3947E0DD6974")).Value;
      var dependentPluginAssemblies = thisPlugin.DependentPlugins.SelectMany(plugin => plugin.LoadedAssemblies);
      foreach (var assembly in dependentPluginAssemblies)
        LoadAllReferencesFor(assembly);
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Loads all assemblies directly and indirectly referenced by <param name="rootAssembly"></param>
    /// </summary>
    /// <param name="rootAssembly"></param>
    /// <remarks>
    /// ToDo: Remove this once Microsoft has removed the dependency to ILibraryManager and ILibraryExporter
    /// </remarks>
    private void LoadAllReferencesFor(Assembly rootAssembly)
    {
      _log.LogDebug("LoadAllReferences called for {0}", rootAssembly.FullName);

      var alreadyProcessed = new HashSet<Assembly>();
      var queue = new Queue<Assembly>();
      queue.Enqueue(rootAssembly);

      while (queue.Count > 0)
      {
        var assembly = queue.Dequeue();

        // Do nothing if this assembly was already processed.
        if (!alreadyProcessed.Add(assembly))
          continue;

        _log.LogDebug("Loading references for {0}", assembly.FullName);

        // Find referenced assemblies
        foreach (var referencedAssemblyName in assembly.GetReferencedAssemblies())
        {
          var loadedAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.FullName == referencedAssemblyName.FullName);
          if (loadedAssembly == null)
          {
            try
            {
              loadedAssembly = Assembly.Load(referencedAssemblyName);
              _log.LogDebug("  Loaded Assembly {0}", referencedAssemblyName.FullName);
            }
            catch (Exception e)
            {
              _log.LogWarning("  Cannot load Assembly {0}", referencedAssemblyName.FullName, e);
            }
          }
          if (loadedAssembly != null)
            queue.Enqueue(loadedAssembly);
        }
      }
    }

    #endregion

    #region ILibraryManager implementation

    public IEnumerable<Library> GetReferencingLibraries(string name)
    {
      _log.LogDebug("GetReferencingLibraries called for name {0}", name);
      var result = AppDomain.CurrentDomain.GetAssemblies()
        .Where(a => a.GetReferencedAssemblies().Select(an => an.Name).Contains(name, StringComparer.OrdinalIgnoreCase))
        .Select(a => new LoadedAssemblyLibrary(a))
        .ToList();
      _log.LogDebug("  Result: {0}", string.Join(", ", result.Select(l => "'" + l.Name + "'")));
      return result;
    }

    public Library GetLibrary(string name)
    {
      _log.LogDebug("GetLibrary called for name {0}", name);
      var assembly = AppDomain.CurrentDomain.GetAssemblies().SingleOrDefault(a => String.Equals(a.GetName().Name, name, StringComparison.OrdinalIgnoreCase));
      if (assembly == null)
      {
        _log.LogWarning("GetLibrary could not finded loaded assembly for name {0}", name);
        return null;
      }
      _log.LogDebug("  Result: {0}", assembly.GetName().Name);
      return new LoadedAssemblyLibrary(assembly);
    }

    public IEnumerable<Library> GetLibraries()
    {
      _log.LogDebug("GetLibraries called");
      var result = AppDomain.CurrentDomain.GetAssemblies().Select(assembly => new LoadedAssemblyLibrary(assembly)).ToList();
      _log.LogDebug("  Result: {0}", string.Join(", ", result.Select(l => "'" + l.Name + "'")));
      return result;
    }

    #endregion
  }
}
