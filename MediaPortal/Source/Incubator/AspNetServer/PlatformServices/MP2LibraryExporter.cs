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
using Microsoft.Extensions.CompilationAbstractions;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace MediaPortal.Plugins.AspNetServer.PlatformServices
{
  /// <summary>
  /// Temporary implementation of ILibraryExporter
  /// </summary>
  /// <remarks>
  /// This is a temporary hack until Microsoft.AspNet.MVC.Razor no longer depends on Microsoft.Extensions.CompilationAbstractions
  /// For details see here:
  /// - https://github.com/aspnet/Mvc/issues/3633
  /// - https://github.com/dotnet/cli/issues/376
  /// - https://github.com/davidfowl/dotnetcli-aspnet5/tree/master/HelloMvc/Infrastructure
  /// ToDo: Remove this class if not needed anymore
  /// </remarks>
  public class MP2LibraryExporter : ILibraryExporter
  {
    #region Private fields

    private readonly ILogger _log;

    #endregion

    #region Constructor

    public MP2LibraryExporter(ILoggerFactory loggerfactory)
    {
      _log = loggerfactory.CreateLogger<MP2LibraryManager>();
    }

    #endregion

    #region ILibraryExporter implementation

    public LibraryExport GetExport(string name)
    {
      _log.LogDebug("GetExport called for name '{0}'", name);
      var assembly = AppDomain.CurrentDomain.GetAssemblies().SingleOrDefault(a => String.Equals(a.GetName().Name, name, StringComparison.OrdinalIgnoreCase));
      if (assembly == null)
      {
        _log.LogWarning("GetExport could not find loaded assembly for name {0}", name);
        return null;
      }
      _log.LogDebug("  Result: {0}", assembly.GetName().Name);
      return new LibraryExport(new MetadataFileReference(assembly.GetName().Name, assembly.Location));
    }

    public LibraryExport GetAllExports(string name)
    {
      _log.LogDebug("GetAllExports called for name '{0}'", name);
      var metadataFileReferenceList = AppDomain.CurrentDomain.GetAssemblies()
        .Where(assembly => !assembly.IsDynamic)
        .Select(assembly => new MetadataFileReference(assembly.GetName().Name, assembly.Location) as IMetadataReference).ToList();
      return new LibraryExport(metadataFileReferenceList, null);
    }

    #endregion
  }
}
