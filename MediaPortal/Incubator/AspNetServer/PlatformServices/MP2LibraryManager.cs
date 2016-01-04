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
